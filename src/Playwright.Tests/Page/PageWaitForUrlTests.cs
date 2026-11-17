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
public class PageWaitForUrlTests
{
    [UpstreamTest("WA-110 should work")]
    public async Task ShouldWork(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync("url => window.location.href = url", server.Prefix + "/grid.html");
        await page.WaitForURLAsync("**/grid.html");
    }

    [UpstreamTest("WA-111 should respect timeout")]
    [LocalFixme("dotnet: the message is 'Timeout 2500ms exceeded.', without the 'page.waitForURL: ' prefix of the call")]
    public async Task ShouldRespectTimeout(IPage page, Server server)
    {
        var promise = Catch(() => page.WaitForURLAsync("**/frame.html", new() { Timeout = 2500 }));
        await page.GotoAsync(server.EmptyPage);
        var error = await promise;
        Assert.That(error.Message, Does.Contain("page.waitForURL: Timeout 2500ms exceeded."));
    }

    [UpstreamTest("WA-112 should work with both domcontentloaded and load")]
    public async Task ShouldWorkWithBothDomcontentloadedAndLoad(IPage page, Server server)
    {
        ServerResponse response = null;
        server.SetRoute("/one-style.css", (req, res) => response = res);
        var navigationPromise = page.GotoAsync(server.Prefix + "/one-style.html");
        var domContentLoadedPromise = page.WaitForURLAsync("**/one-style.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var bothFired = false;
        // .NET: const bothFiredPromise = Promise.all([...]).then(() => bothFired = true);
        async Task BothFiredPromise()
        {
            await PromiseAll(
                page.WaitForURLAsync("**/one-style.html", new() { WaitUntil = WaitUntilState.Load }),
                domContentLoadedPromise);
            bothFired = true;
        }
        var bothFiredPromise = BothFiredPromise();

        await server.WaitForRequest("/one-style.css");
        await domContentLoadedPromise;
        Assert.That(bothFired, Is.EqualTo(false));
        response.End();
        await bothFiredPromise;
        await navigationPromise;
    }

    [UpstreamTest("WA-113 should work with commit")]
    public async Task ShouldWorkWithCommit(IPage page, Server server)
    {
        server.SetRoute("/script.js", (req, res) => { });
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("content-type", "text/html");
            res.End("<title>Hello</title><script src=\"script.js\"></script>");
        });
        IgnoreErrors(page.GotoAsync(server.EmptyPage));
        await page.WaitForURLAsync("**/empty.html", new() { WaitUntil = WaitUntilState.Commit });
        Assert.That(await page.TitleAsync(), Is.EqualTo("Hello"));
    }

    [UpstreamTest("WA-114 should work with commit and about:blank")]
    public async Task ShouldWorkWithCommitAndAboutBlank(IPage page, Server server)
    {
        await page.WaitForURLAsync("about:blank", new() { WaitUntil = WaitUntilState.Commit });
    }

    [UpstreamTest("WA-115 should work with clicking on anchor links")]
    public async Task ShouldWorkWithClickingOnAnchorLinks(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a href='#foobar'>foobar</a>");
        await page.ClickAsync("a");
        await page.WaitForURLAsync("**/*#foobar");
    }

    [UpstreamTest("WA-116 should work with history.pushState()")]
    public async Task ShouldWorkWithHistoryPushState(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <a onclick='javascript:pushState()'>SPA</a>
    <script>
      function pushState() { history.pushState({}, '', 'wow.html') }
    </script>
  ");
        await page.ClickAsync("a");
        await page.WaitForURLAsync("**/wow.html");
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/wow.html"));
    }

    [UpstreamTest("WA-117 should work with history.replaceState()")]
    public async Task ShouldWorkWithHistoryReplaceState(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <a onclick='javascript:replaceState()'>SPA</a>
    <script>
      function replaceState() { history.replaceState({}, '', '/replaced.html') }
    </script>
  ");
        await page.ClickAsync("a");
        await page.WaitForURLAsync("**/replaced.html");
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/replaced.html"));
    }

    [UpstreamTest("WA-118 should work with DOM history.back()/history.forward()")]
    public async Task ShouldWorkWithDOMHistoryBackHistoryForward(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <a id=back onclick='javascript:goBack()'>back</a>
    <a id=forward onclick='javascript:goForward()'>forward</a>
    <script>
      function goBack() { history.back(); }
      function goForward() { history.forward(); }
      history.pushState({}, '', '/first.html');
      history.pushState({}, '', '/second.html');
    </script>
  ");
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/second.html"));

        await page.ClickAsync("a#back");
        await page.WaitForURLAsync("**/first.html");
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/first.html"));

        await page.ClickAsync("a#forward");
        await page.WaitForURLAsync("**/second.html");
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/second.html"));
    }

    [UpstreamTest("WA-119 should work with url match for same document navigations")]
    public async Task ShouldWorkWithUrlMatchForSameDocumentNavigations(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var resolved = false;
        // .NET: const waitPromise = page.waitForURL(/third\.html/).then(() => resolved = true);
        async Task WaitPromise()
        {
            await page.WaitForURLAsync(new Regex(@"third\.html"));
            resolved = true;
        }
        var waitPromise = WaitPromise();
        Assert.That(resolved, Is.EqualTo(false));
        await page.EvaluateAsync(@"() => {
    history.pushState({}, '', '/first.html');
  }");
        Assert.That(resolved, Is.EqualTo(false));
        await page.EvaluateAsync(@"() => {
    history.pushState({}, '', '/second.html');
  }");
        Assert.That(resolved, Is.EqualTo(false));
        await page.EvaluateAsync(@"() => {
    history.pushState({}, '', '/third.html');
  }");
        await waitPromise;
        Assert.That(resolved, Is.EqualTo(true));
    }

    [UpstreamTest("WA-120 should work on frame")]
    public async Task ShouldWorkOnFrame(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        var frame = page.Frames[1];
        await frame.EvaluateAsync("url => window.location.href = url", server.Prefix + "/grid.html");
        await frame.WaitForURLAsync("**/grid.html");
    }
}
