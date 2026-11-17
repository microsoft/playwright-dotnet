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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-wait-for-url.spec.ts.</summary>
public class PageWaitForUrlTests : UpstreamPageTest
{
    [UpstreamTest("WA-110 should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync("url => window.location.href = url", Server.Prefix + "/grid.html");
        await Page.WaitForURLAsync("**/grid.html");
    }

    [UpstreamTest("WA-111 should respect timeout")]
    [LocalFixme("dotnet: the message is 'Timeout 2500ms exceeded.', without the 'page.waitForURL: ' prefix of the call")]
    public async Task ShouldRespectTimeout()
    {
        var promise = Catch(() => Page.WaitForURLAsync("**/frame.html", new() { Timeout = 2500 }));
        await Page.GotoAsync(Server.EmptyPage);
        var error = await promise;
        Assert.That(error.Message, Does.Contain("page.waitForURL: Timeout 2500ms exceeded."));
    }

    [UpstreamTest("WA-112 should work with both domcontentloaded and load")]
    public async Task ShouldWorkWithBothDomcontentloadedAndLoad()
    {
        ServerResponse response = null;
        Server.SetRoute("/one-style.css", (req, res) => response = res);
        var navigationPromise = Page.GotoAsync(Server.Prefix + "/one-style.html");
        var domContentLoadedPromise = Page.WaitForURLAsync("**/one-style.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var bothFired = false;
        // .NET: const bothFiredPromise = Promise.all([...]).then(() => bothFired = true);
        async Task BothFiredPromise()
        {
            await PromiseAll(
                Page.WaitForURLAsync("**/one-style.html", new() { WaitUntil = WaitUntilState.Load }),
                domContentLoadedPromise);
            bothFired = true;
        }
        var bothFiredPromise = BothFiredPromise();

        await Server.WaitForRequest("/one-style.css");
        await domContentLoadedPromise;
        Assert.That(bothFired, Is.EqualTo(false));
        response.End();
        await bothFiredPromise;
        await navigationPromise;
    }

    [UpstreamTest("WA-113 should work with commit")]
    public async Task ShouldWorkWithCommit()
    {
        Server.SetRoute("/script.js", (req, res) => { });
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("content-type", "text/html");
            res.End("<title>Hello</title><script src=\"script.js\"></script>");
        });
        IgnoreErrors(Page.GotoAsync(Server.EmptyPage));
        await Page.WaitForURLAsync("**/empty.html", new() { WaitUntil = WaitUntilState.Commit });
        Assert.That(await Page.TitleAsync(), Is.EqualTo("Hello"));
    }

    [UpstreamTest("WA-114 should work with commit and about:blank")]
    public async Task ShouldWorkWithCommitAndAboutBlank()
    {
        await Page.WaitForURLAsync("about:blank", new() { WaitUntil = WaitUntilState.Commit });
    }

    [UpstreamTest("WA-115 should work with clicking on anchor links")]
    public async Task ShouldWorkWithClickingOnAnchorLinks()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a href='#foobar'>foobar</a>");
        await Page.ClickAsync("a");
        await Page.WaitForURLAsync("**/*#foobar");
    }

    [UpstreamTest("WA-116 should work with history.pushState()")]
    public async Task ShouldWorkWithHistoryPushState()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
    <a onclick='javascript:pushState()'>SPA</a>
    <script>
      function pushState() { history.pushState({}, '', 'wow.html') }
    </script>
  ");
        await Page.ClickAsync("a");
        await Page.WaitForURLAsync("**/wow.html");
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/wow.html"));
    }

    [UpstreamTest("WA-117 should work with history.replaceState()")]
    public async Task ShouldWorkWithHistoryReplaceState()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
    <a onclick='javascript:replaceState()'>SPA</a>
    <script>
      function replaceState() { history.replaceState({}, '', '/replaced.html') }
    </script>
  ");
        await Page.ClickAsync("a");
        await Page.WaitForURLAsync("**/replaced.html");
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/replaced.html"));
    }

    [UpstreamTest("WA-118 should work with DOM history.back()/history.forward()")]
    public async Task ShouldWorkWithDOMHistoryBackHistoryForward()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
    <a id=back onclick='javascript:goBack()'>back</a>
    <a id=forward onclick='javascript:goForward()'>forward</a>
    <script>
      function goBack() { history.back(); }
      function goForward() { history.forward(); }
      history.pushState({}, '', '/first.html');
      history.pushState({}, '', '/second.html');
    </script>
  ");
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/second.html"));

        await Page.ClickAsync("a#back");
        await Page.WaitForURLAsync("**/first.html");
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/first.html"));

        await Page.ClickAsync("a#forward");
        await Page.WaitForURLAsync("**/second.html");
        Assert.That(Page.Url, Is.EqualTo(Server.Prefix + "/second.html"));
    }

    [UpstreamTest("WA-119 should work with url match for same document navigations")]
    public async Task ShouldWorkWithUrlMatchForSameDocumentNavigations()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var resolved = false;
        // .NET: const waitPromise = page.waitForURL(/third\.html/).then(() => resolved = true);
        async Task WaitPromise()
        {
            await Page.WaitForURLAsync(new Regex(@"third\.html"));
            resolved = true;
        }
        var waitPromise = WaitPromise();
        Assert.That(resolved, Is.EqualTo(false));
        await Page.EvaluateAsync(@"() => {
    history.pushState({}, '', '/first.html');
  }");
        Assert.That(resolved, Is.EqualTo(false));
        await Page.EvaluateAsync(@"() => {
    history.pushState({}, '', '/second.html');
  }");
        Assert.That(resolved, Is.EqualTo(false));
        await Page.EvaluateAsync(@"() => {
    history.pushState({}, '', '/third.html');
  }");
        await waitPromise;
        Assert.That(resolved, Is.EqualTo(true));
    }

    [UpstreamTest("WA-120 should work on frame")]
    public async Task ShouldWorkOnFrame()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        var frame = Page.Frames[1];
        await frame.EvaluateAsync("url => window.location.href = url", Server.Prefix + "/grid.html");
        await frame.WaitForURLAsync("**/grid.html");
    }
}
