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
public class PageAutowaitingNoHangTests
{
    [UpstreamTest("AW-013 clicking on links which do not commit navigation")]
    public async Task ClickingOnLinksWhichDoNotCommitNavigation(IPage page, Server server, Server httpsServer)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync($"<a href='{httpsServer.EmptyPage}'>foobar</a>");
        await page.ClickAsync("a");
    }

    [UpstreamTest("AW-014 calling window.stop async")]
    public async Task CallingWindowStopAsync(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) => { });
        await page.EvaluateAsync(@"url => {
    window.location.href = url;
    setTimeout(() => window.stop(), 100);
  }", server.EmptyPage);
    }

    [UpstreamTest("AW-015 calling window.stop sync")]
    public async Task CallingWindowStopSync(IPage page, Server server, string browserName)
    {
        await page.EvaluateAsync(@"url => {
    window.location.href = url;
    window.stop();
  }", server.EmptyPage);
    }

    [UpstreamTest("AW-016 assigning location to about:blank")]
    public async Task AssigningLocationToAboutBlank(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync("window.location.href = \"about:blank\";");
    }

    [UpstreamTest("AW-017 assigning location to about:blank after non-about:blank")]
    public async Task AssigningLocationToAboutBlankAfterNonAboutBlank(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) => { });
        await page.EvaluateAsync($@"
      window.location.href = ""{server.EmptyPage}"";
      window.location.href = ""about:blank"";");
    }

    [UpstreamTest("AW-018 calling window.open and window.close")]
    public async Task CallingWindowOpenAndWindowClose(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"() => {
    const popup = window.open(window.location.href);
    popup.close();
  }");
    }

    [UpstreamTest("AW-019 opening a popup")]
    public async Task OpeningAPopup(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window.open(window.location.href) && 1"));
    }

    [UpstreamTest("AW-020 clicking in the middle of navigation that aborts")]
    public async Task ClickingInTheMiddleOfNavigationThatAborts(IPage page, Server server)
    {
        var abortPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var stallPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        server.SetRoute("/stall.html", async (req, res) =>
        {
            // .NET: a second stallCallback(), for a request that the browser sends again, does nothing.
            stallPromise.TrySetResult();
            await abortPromise.Task;
            req.Socket.Destroy();
        });

        await page.GotoAsync(server.Prefix + "/one-style.html");
        IgnoreErrors(page.GotoAsync(server.Prefix + "/stall.html"));
        await stallPromise.Task;

        var clickPromise = page.ClickAsync("body");
        await page.WaitForTimeoutAsync(1000);
        abortPromise.SetResult();

        await clickPromise;
    }

    [UpstreamTest("AW-021 clicking in the middle of navigation that commits")]
    public async Task ClickingInTheMiddleOfNavigationThatCommits(IPage page, Server server)
    {
        var abortPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var stallPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        server.SetRoute("/stall.html", async (req, res) =>
        {
            // .NET: a second stallCallback(), for a request that the browser sends again, does nothing.
            stallPromise.TrySetResult();
            await abortPromise.Task;
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End("hello world");
        });

        await page.GotoAsync(server.Prefix + "/one-style.html");
        IgnoreErrors(page.GotoAsync(server.Prefix + "/stall.html"));
        await stallPromise.Task;

        var clickPromise = page.ClickAsync("body");
        await page.WaitForTimeoutAsync(1000);
        abortPromise.SetResult();

        await clickPromise;
        await Expect(page.Locator("body")).ToContainTextAsync("hello world");
    }

    [UpstreamTest("AW-022 clicking a link intercepted by the Navigation API same-document")]
    public async Task ClickingALinkInterceptedByTheNavigationAPISameDocument(IPage page, Server server)
    {
        server.SetRoute("/intercept.html", (req, res) =>
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

        await page.GotoAsync(server.Prefix + "/intercept.html");
        await Expect(page.Locator("#status")).ToHaveTextAsync("initial");

        // Should not hang waiting for the same-document navigation to commit.
        await page.Locator("#go").ClickAsync();

        await Expect(page.Locator("#status")).ToHaveTextAsync("intercepted:/other");
        Assert.That(new Uri(page.Url).AbsolutePath, Is.EqualTo("/other"));
    }

    [UpstreamTest("AW-023 goBack in the middle of navigation that commits")]
    public async Task GoBackInTheMiddleOfNavigationThatCommits(IPage page, Server server)
    {
        var abortPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var stallPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        server.SetRoute("/stall.html", async (req, res) =>
        {
            // .NET: a second stallCallback(), for a request that the browser sends again, does nothing.
            stallPromise.TrySetResult();
            await abortPromise.Task;
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End("hello world");
        });

        await page.GotoAsync(server.Prefix + "/one-style.html");
        IgnoreErrors(page.GotoAsync(server.Prefix + "/stall.html"));
        await stallPromise.Task;

        var goBackPromise = Catch(() => page.GoBackAsync());
        await page.WaitForTimeoutAsync(1000);
        abortPromise.SetResult();

        await goBackPromise;
    }
}
