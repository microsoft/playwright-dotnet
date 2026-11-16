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
using System.Web;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-wait-for-navigation.spec.ts.</summary>
public class PageWaitForNavigationTests
{
    [UpstreamTest("WA-095 should work")]
    public async Task ShouldWork(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (response, _) = await PromiseAll(
            page.WaitForNavigationAsync(),
            page.EvaluateAsync("url => window.location.href = url", server.Prefix + "/grid.html"));
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(response.Url, Does.Contain("grid.html"));
    }

    [UpstreamTest("WA-096 should respect timeout")]

    [Fixme("dotnet: the message is 'Timeout 5000ms exceeded.', without the 'page.waitForNavigation: ' prefix of the call, and the log has 'until \"Load\"', upstream expects 'until \"load\"'")]
    public async Task ShouldRespectTimeout(IPage page, Server server)
    {
        var promise = page.WaitForNavigationAsync(new() { Url = "**/frame.html", Timeout = 5000 });
        await page.GotoAsync(server.EmptyPage);
        var error = await Catch(() => promise);
        Assert.That(error.Message, Does.Contain("page.waitForNavigation: Timeout 5000ms exceeded."));
        Assert.That(error.Message, Does.Contain("waiting for navigation to \"**/frame.html\" until \"load\""));
        Assert.That(error.Message, Does.Contain($"navigated to \"{server.EmptyPage}\""));
    }

    [UpstreamTest("WA-097 should work with both domcontentloaded and load")]
    public async Task ShouldWorkWithBothDomcontentloadedAndLoad(IPage page, Server server)
    {
        ServerResponse response = null;
        server.SetRoute("/one-style.css", (req, res) => response = res);
        var navigationPromise = page.GotoAsync(server.Prefix + "/one-style.html");
        var domContentLoadedPromise = page.WaitForNavigationAsync(new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
        });

        var bothFired = false;
        // .NET: Promise.all([...]).then(() => bothFired = true) is a local function that is called in its place.
        async Task BothFired()
        {
            await PromiseAll(
                page.WaitForNavigationAsync(new() { WaitUntil = WaitUntilState.Load }),
                domContentLoadedPromise);
            bothFired = true;
        }
        var bothFiredPromise = BothFired();

        await server.WaitForRequest("/one-style.css");
        await domContentLoadedPromise;
        Assert.That(bothFired, Is.EqualTo(false));
        response.End();
        await bothFiredPromise;
        await navigationPromise;
    }

    [UpstreamTest("WA-098 should work with commit")]
    public async Task ShouldWorkWithCommit(IPage page, Server server)
    {
        server.SetRoute("/script.js", (req, res) => { });
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("content-type", "text/html");
            res.End("<title>Hello</title><script src=\"script.js\"></script>");
        });
        IgnoreErrors(page.GotoAsync(server.EmptyPage));
        await page.WaitForNavigationAsync(new() { WaitUntil = WaitUntilState.Commit });
        Assert.That(await page.TitleAsync(), Is.EqualTo("Hello"));
    }

    [UpstreamTest("WA-099 should work with clicking on anchor links")]
    public async Task ShouldWorkWithClickingOnAnchorLinks(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a href='#foobar'>foobar</a>");
        var response = await PromiseAll(
            page.WaitForNavigationAsync(),
            page.ClickAsync("a"));
        Assert.That(response, Is.Null);
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage + "#foobar"));
    }

    [UpstreamTest("WA-100 should work with clicking on links which do not commit navigation")]
    public async Task ShouldWorkWithClickingOnLinksWhichDoNotCommitNavigation(IPage page, Server server, Server httpsServer, string browserName, string platform, string channel)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync($"<a href='{httpsServer.EmptyPage}'>foobar</a>");
        var error = await PromiseAll(
            Catch(() => page.WaitForNavigationAsync()),
            page.ClickAsync("a"));
        Assert.That(error.Message, Does.Match(ExpectedSSLError(browserName, platform, channel)));
    }

    [UpstreamTest("WA-101 should work with history.pushState()")]
    public async Task ShouldWorkWithHistoryPushState(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <a onclick='javascript:pushState()'>SPA</a>
    <script>
      function pushState() { history.pushState({}, '', 'wow.html') }
    </script>
  ");
        var response = await PromiseAll(
            page.WaitForNavigationAsync(),
            page.ClickAsync("a"));
        Assert.That(response, Is.Null);
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/wow.html"));
    }

    [UpstreamTest("WA-102 should work with history.replaceState()")]
    public async Task ShouldWorkWithHistoryReplaceState(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <a onclick='javascript:replaceState()'>SPA</a>
    <script>
      function replaceState() { history.replaceState({}, '', '/replaced.html') }
    </script>
  ");
        var response = await PromiseAll(
            page.WaitForNavigationAsync(),
            page.ClickAsync("a"));
        Assert.That(response, Is.Null);
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/replaced.html"));
    }

    [UpstreamTest("WA-103 should work with DOM history.back()/history.forward()")]
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
        var backResponse = await PromiseAll(
            page.WaitForNavigationAsync(),
            page.ClickAsync("a#back"));
        Assert.That(backResponse, Is.Null);
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/first.html"));
        var forwardResponse = await PromiseAll(
            page.WaitForNavigationAsync(),
            page.ClickAsync("a#forward"));
        Assert.That(forwardResponse, Is.Null);
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/second.html"));
    }

    [UpstreamTest("WA-105 should work with url match")]
    public async Task ShouldWorkWithUrlMatch(IPage page, Server server)
    {
        // .NET: waitForNavigation(...).then(response => response1 = response) is a local function that is called in its place.
        IResponse response1 = null;
        async Task WaitForResponse1()
        {
            response1 = await page.WaitForNavigationAsync(new() { UrlRegex = new Regex("one-style\\.html") });
        }
        var response1Promise = WaitForResponse1();
        IResponse response2 = null;
        async Task WaitForResponse2()
        {
            response2 = await page.WaitForNavigationAsync(new() { UrlRegex = new Regex("\\/frame.html") });
        }
        var response2Promise = WaitForResponse2();
        IResponse response3 = null;
        async Task WaitForResponse3()
        {
            // .NET: the predicate takes the url as a string, not as a URL.
            response3 = await page.WaitForNavigationAsync(new() { UrlFunc = url => HttpUtility.ParseQueryString(new Uri(url).Query).Get("foo") == "bar" });
        }
        var response3Promise = WaitForResponse3();
        Assert.That(response1, Is.Null);
        Assert.That(response2, Is.Null);
        Assert.That(response3, Is.Null);
        await page.GotoAsync(server.EmptyPage);
        Assert.That(response1, Is.Null);
        Assert.That(response2, Is.Null);
        Assert.That(response3, Is.Null);
        await page.GotoAsync(server.Prefix + "/frame.html");
        Assert.That(response1, Is.Null);
        await response2Promise;
        Assert.That(response2, Is.Not.Null);
        Assert.That(response3, Is.Null);
        await page.GotoAsync(server.Prefix + "/one-style.html");
        await response1Promise;
        Assert.That(response1, Is.Not.Null);
        Assert.That(response2, Is.Not.Null);
        Assert.That(response3, Is.Null);
        await page.GotoAsync(server.Prefix + "/frame.html?foo=bar");
        await response3Promise;
        Assert.That(response1, Is.Not.Null);
        Assert.That(response2, Is.Not.Null);
        Assert.That(response3, Is.Not.Null);
        await page.GotoAsync(server.Prefix + "/empty.html");
        Assert.That(response1.Url, Is.EqualTo(server.Prefix + "/one-style.html"));
        Assert.That(response2.Url, Is.EqualTo(server.Prefix + "/frame.html"));
        Assert.That(response3.Url, Is.EqualTo(server.Prefix + "/frame.html?foo=bar"));
    }

    [UpstreamTest("WA-106 should work with url match for same document navigations")]
    public async Task ShouldWorkWithUrlMatchForSameDocumentNavigations(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var resolved = false;
        // .NET: waitForNavigation(...).then(() => resolved = true) is a local function that is called in its place.
        async Task WaitForNavigation()
        {
            await page.WaitForNavigationAsync(new() { UrlRegex = new Regex("third\\.html") });
            resolved = true;
        }
        var waitPromise = WaitForNavigation();
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

    [UpstreamTest("WA-107 should work for cross-process navigations")]
    public async Task ShouldWorkForCrossProcessNavigations(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var waitPromise = page.WaitForNavigationAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var url = server.CrossProcessPrefix + "/empty.html";
        var gotoPromise = page.GotoAsync(url);
        var response = await waitPromise;
        Assert.That(response.Url, Is.EqualTo(url));
        Assert.That(page.Url, Is.EqualTo(url));
        Assert.That(await page.EvaluateAsync<string>("document.location.href"), Is.EqualTo(url));
        await gotoPromise;
    }

    [UpstreamTest("WA-108 should work on frame")]
    public async Task ShouldWorkOnFrame(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        var frame = page.Frames[1];
        var (response, _) = await PromiseAll(
            frame.WaitForNavigationAsync(),
            frame.EvaluateAsync("url => window.location.href = url", server.Prefix + "/grid.html"));
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(response.Url, Does.Contain("grid.html"));
        Assert.That(response.Frame, Is.SameAs(frame));
        Assert.That(page.Url, Does.Contain("/frames/one-frame.html"));
    }

    [UpstreamTest("WA-109 should fail when frame detaches")]

    [Fixme("dotnet: the log has 'waiting for navigation until \"Load\"', upstream expects 'waiting for navigation until \"load\"'")]
    public async Task ShouldFailWhenFrameDetaches(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        var frame = page.Frames[1];
        server.SetRoute("/empty.html", (req, res) => { });
        server.SetRoute("/one-style.css", (req, res) => { });
        // .NET: server.waitForRequest(...).then(() => page.$eval(...)) is a local function that is called in its place.
        async Task RemoveFrame()
        {
            await server.WaitForRequest("/one-style.css");
            await page.EvalOnSelectorAsync("iframe", "frame => window.builtins.setTimeout(() => frame.remove(), 0)");
        }
        var (error, _) = await PromiseAll(
            Catch(() => frame.WaitForNavigationAsync()),
            page.EvalOnSelectorAsync("iframe", "frame => { frame.contentWindow.location.href = '/one-style.html'; }"),
            // Make sure policy checks pass and navigation actually begins before removing the frame to avoid other errors
            RemoveFrame());
        Assert.That(error.Message, Does.Contain("waiting for navigation until \"load\""));
        Assert.That(error.Message, Does.Contain("frame was detached"));
    }
}
