/**
 * Copyright 2018 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-autowaiting-no-hang.spec.ts.</summary>
public class PageAutowaitingNoHangTests : UpstreamPageTest
{
    [UpstreamTest("AW-013 clicking on links which do not commit navigation")]
    public async Task ClickingOnLinksWhichDoNotCommitNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync($"<a href='{HttpsServer.EmptyPage}'>foobar</a>");
        await Page.ClickAsync("a");
    }

    [UpstreamTest("AW-014 calling window.stop async")]
    public async Task CallingWindowStopAsync()
    {
        Server.SetRoute("/empty.html", (req, res) => { });
        await Page.EvaluateAsync(@"url => {
    window.location.href = url;
    setTimeout(() => window.stop(), 100);
  }", Server.EmptyPage);
    }

    [UpstreamTest("AW-015 calling window.stop sync")]
    public async Task CallingWindowStopSync()
    {
        await Page.EvaluateAsync(@"url => {
    window.location.href = url;
    window.stop();
  }", Server.EmptyPage);
    }

    [UpstreamTest("AW-016 assigning location to about:blank")]
    public async Task AssigningLocationToAboutBlank()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync("window.location.href = \"about:blank\";");
    }

    [UpstreamTest("AW-017 assigning location to about:blank after non-about:blank")]
    public async Task AssigningLocationToAboutBlankAfterNonAboutBlank()
    {
        Server.SetRoute("/empty.html", (req, res) => { });
        await Page.EvaluateAsync($@"
      window.location.href = ""{Server.EmptyPage}"";
      window.location.href = ""about:blank"";");
    }

    [UpstreamTest("AW-018 calling window.open and window.close")]
    public async Task CallingWindowOpenAndWindowClose()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"() => {
    const popup = window.open(window.location.href);
    popup.close();
  }");
    }

    [UpstreamTest("AW-019 opening a popup")]
    public async Task OpeningAPopup()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window.open(window.location.href) && 1"));
    }

    [UpstreamTest("AW-020 clicking in the middle of navigation that aborts")]
    public async Task ClickingInTheMiddleOfNavigationThatAborts()
    {
        var abortPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var stallPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Server.SetRoute("/stall.html", async (req, res) =>
        {
            // .NET: a second stallCallback(), for a request that the browser sends again, does nothing.
            stallPromise.TrySetResult();
            await abortPromise.Task;
            req.Socket.Destroy();
        });

        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        IgnoreErrors(Page.GotoAsync(Server.Prefix + "/stall.html"));
        await stallPromise.Task;

        var clickPromise = Page.ClickAsync("body");
        await Page.WaitForTimeoutAsync(1000);
        abortPromise.SetResult();

        await clickPromise;
    }

    [UpstreamTest("AW-021 clicking in the middle of navigation that commits")]
    public async Task ClickingInTheMiddleOfNavigationThatCommits()
    {
        var abortPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var stallPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Server.SetRoute("/stall.html", async (req, res) =>
        {
            // .NET: a second stallCallback(), for a request that the browser sends again, does nothing.
            stallPromise.TrySetResult();
            await abortPromise.Task;
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End("hello world");
        });

        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        IgnoreErrors(Page.GotoAsync(Server.Prefix + "/stall.html"));
        await stallPromise.Task;

        var clickPromise = Page.ClickAsync("body");
        await Page.WaitForTimeoutAsync(1000);
        abortPromise.SetResult();

        await clickPromise;
        await Expect(Page.Locator("body")).ToContainTextAsync("hello world");
    }

    [UpstreamTest("AW-022 clicking a link intercepted by the Navigation API same-document")]
    public async Task ClickingALinkInterceptedByTheNavigationAPISameDocument()
    {
        Server.SetRoute("/intercept.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End(@"
      <a id=""go"" href=""/other"">go</a>
      <p id=""status"">initial</p>
      <script>
        navigation.addEventListener('navigate', event => {
          if (!event.canIntercept)
            return;
          event.intercept({
            handler: async () => {
              const dest = new URL(event.destination.url).pathname;
              document.getElementById('status').textContent = 'intercepted:' + dest;
            },
          });
        });
      </script>
    ");
        });

        await Page.GotoAsync(Server.Prefix + "/intercept.html");
        await Expect(Page.Locator("#status")).ToHaveTextAsync("initial");

        // Should not hang waiting for the same-document navigation to commit.
        await Page.Locator("#go").ClickAsync();

        await Expect(Page.Locator("#status")).ToHaveTextAsync("intercepted:/other");
        Assert.That(new Uri(Page.Url).AbsolutePath, Is.EqualTo("/other"));
    }

    [UpstreamTest("AW-023 goBack in the middle of navigation that commits")]
    public async Task GoBackInTheMiddleOfNavigationThatCommits()
    {
        var abortPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var stallPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Server.SetRoute("/stall.html", async (req, res) =>
        {
            // .NET: a second stallCallback(), for a request that the browser sends again, does nothing.
            stallPromise.TrySetResult();
            await abortPromise.Task;
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End("hello world");
        });

        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        IgnoreErrors(Page.GotoAsync(Server.Prefix + "/stall.html"));
        await stallPromise.Task;

        var goBackPromise = Catch(() => Page.GoBackAsync());
        await Page.WaitForTimeoutAsync(1000);
        abortPromise.SetResult();

        await goBackPromise;
    }
}
