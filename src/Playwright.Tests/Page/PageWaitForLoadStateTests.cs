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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-wait-for-load-state.spec.ts.</summary>
public class PageWaitForLoadStateTests : UpstreamPageTest
{
    [UpstreamTest("WA-077 should pick up ongoing navigation")]
    public async Task ShouldPickUpOngoingNavigation()
    {
        ServerResponse response = null;
        Server.SetRoute("/one-style.css", (req, res) => response = res);
        await PromiseAll(
            Server.WaitForRequest("/one-style.css"),
            Page.GotoAsync(Server.Prefix + "/one-style.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded }));
        var waitPromise = Page.WaitForLoadStateAsync();
        response.StatusCode = 404;
        response.End("Not found");
        await waitPromise;
    }

    [UpstreamTest("WA-078 should respect timeout")]

    public async Task ShouldRespectTimeout()
    {
        Server.SetRoute("/one-style.css", (req, res) => { });
        await Page.GotoAsync(Server.Prefix + "/one-style.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var error = await Catch(() => Page.WaitForLoadStateAsync(LoadState.Load, new() { Timeout = 1 }));
        Assert.That(error.Message, Does.Contain("Timeout 1ms exceeded."));
        // .NET: left out, a .NET stack trace has the frames of the library:
        // expect(error.stack.split('\n')[1]).toContain(__filename);
    }

    [UpstreamTest("WA-079 should resolve immediately if loaded")]
    public async Task ShouldResolveImmediatelyIfLoaded()
    {
        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        await Page.WaitForLoadStateAsync();
    }

    [UpstreamTest("WA-081 should resolve immediately if load state matches")]
    public async Task ShouldResolveImmediatelyIfLoadStateMatches()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/one-style.css", (req, res) => { });
        await Page.GotoAsync(Server.Prefix + "/one-style.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    [UpstreamTest("WA-082 should work with pages that have loaded before being connected to")]
    public async Task ShouldWorkWithPagesThatHaveLoadedBeforeBeingConnectedTo()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window['_popup'] = window.open(document.location.href)"));
        // The url is about:blank in FF.
        // expect(popup.url()).toBe(server.EMPTY_PAGE);
        await popup.WaitForLoadStateAsync();
        Assert.That(popup.Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("WA-083 should wait for load state of empty url popup")]
    public async Task ShouldWaitForLoadStateOfEmptyUrlPopup()
    {
        var (popup, readyState) = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync<string>(@"() => {
      const popup = window.open('');
      return popup.document.readyState;
    }"));
        await popup.WaitForLoadStateAsync();
        var isOldFirefox = BrowserName == "firefox" && BrowserMajorVersion < 148;
        Assert.That(readyState, Is.EqualTo(isOldFirefox && !IsBidi ? "uninitialized" : "complete"));
        Assert.That(await popup.EvaluateAsync<string>("() => document.readyState"), Is.EqualTo(isOldFirefox && !IsBidi ? "uninitialized" : "complete"));
    }

    [UpstreamTest("WA-084 should wait for load state of about:blank popup ")]
    public async Task ShouldWaitForLoadStateOfAboutBlankPopup()
    {
        var (popup, _) = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window.open('about:blank') && 1"));
        await popup.WaitForLoadStateAsync();
        Assert.That(await popup.EvaluateAsync<string>("() => document.readyState"), Is.EqualTo("complete"));
    }

    [UpstreamTest("WA-085 should wait for load state of about:blank popup with noopener ")]
    public async Task ShouldWaitForLoadStateOfAboutBlankPopupWithNoopener()
    {
        var (popup, _) = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window.open('about:blank', null, 'noopener') && 1"));
        await popup.WaitForLoadStateAsync();
        Assert.That(await popup.EvaluateAsync<string>("() => document.readyState"), Is.EqualTo("complete"));
    }

    [UpstreamTest("WA-086 should wait for load state of popup with network url ")]
    public async Task ShouldWaitForLoadStateOfPopupWithNetworkUrl()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("url => window.open(url) && 1", Server.EmptyPage));
        await popup.WaitForLoadStateAsync();
        Assert.That(await popup.EvaluateAsync<string>("() => document.readyState"), Is.EqualTo("complete"));
    }

    [UpstreamTest("WA-087 should wait for load state of popup with network url and noopener ")]
    public async Task ShouldWaitForLoadStateOfPopupWithNetworkUrlAndNoopener()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("url => window.open(url, null, 'noopener') && 1", Server.EmptyPage));
        await popup.WaitForLoadStateAsync();
        Assert.That(await popup.EvaluateAsync<string>("() => document.readyState"), Is.EqualTo("complete"));
    }

    [UpstreamTest("WA-088 should work with clicking target=_blank")]
    public async Task ShouldWorkWithClickingTargetBlank()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a target=_blank rel=\"opener\" href=\"/one-style.html\">yo</a>");
        var popup = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.ClickAsync("a"));
        await popup.WaitForLoadStateAsync();
        Assert.That(await popup.EvaluateAsync<string>("() => document.readyState"), Is.EqualTo("complete"));
    }

    [UpstreamTest("WA-089 should wait for load state of newPage")]
    public async Task ShouldWaitForLoadStateOfNewPage()
    {
        Info.Fixme(IsElectron, "BrowserContext.newPage does not work in Electron");

        var (newPage, _) = await PromiseAll(
            Page.Context.WaitForPageAsync(),
            Page.Context.NewPageAsync());
        await newPage.WaitForLoadStateAsync();
        Assert.That(await newPage.EvaluateAsync<string>("() => document.readyState"), Is.EqualTo("complete"));
    }

    [UpstreamTest("WA-090 should resolve after popup load")]
    public async Task ShouldResolveAfterPopupLoad()
    {
        await Page.GotoAsync(Server.EmptyPage);
        // Stall the 'load' by delaying css.
        ServerResponse cssResponse = null;
        Server.SetRoute("/one-style.css", (req, res) => cssResponse = res);
        var (popup, _, _) = await PromiseAll(
            Page.WaitForPopupAsync(),
            Server.WaitForRequest("/one-style.css"),
            Page.EvaluateAsync("url => window['popup'] = window.open(url)", Server.Prefix + "/one-style.html"));
        var resolved = false;
        // .NET: popup.waitForLoadState().then(() => resolved = true) is a local function that is called in its place.
        async Task WaitForLoadState()
        {
            await popup.WaitForLoadStateAsync();
            resolved = true;
        }
        var loadSatePromise = WaitForLoadState();
        // Round trips!
        for (var i = 0; i < 5; i++)
        {
            await Page.EvaluateAsync("window");
        }
        Assert.That(resolved, Is.EqualTo(false));
        cssResponse.End("");
        await loadSatePromise;
        Assert.That(resolved, Is.EqualTo(true));
        Assert.That(popup.Url, Is.EqualTo(Server.Prefix + "/one-style.html"));
    }

    [UpstreamTest("WA-091 should work for frame")]
    public async Task ShouldWorkForFrame()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        var frame = Page.Frames[1];

        var requestPromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = Page.RouteAsync(Server.Prefix + "/one-style.css", route => requestPromise.TrySetResult(route));
        await frame.GotoAsync(Server.Prefix + "/one-style.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var request = await requestPromise.Task;
        var resolved = false;
        // .NET: frame.waitForLoadState().then(() => resolved = true) is a local function that is called in its place.
        async Task WaitForLoadState()
        {
            await frame.WaitForLoadStateAsync();
            resolved = true;
        }
        var loadPromise = WaitForLoadState();
        // give the promise a chance to resolve, even though it shouldn't
        await Page.EvaluateAsync("1");
        Assert.That(resolved, Is.EqualTo(false));
        await request.ContinueAsync();
        await loadPromise;
    }

    [UpstreamTest("WA-092 should work with javascript: iframe")]
    public async Task ShouldWorkWithJavascriptIframe()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<iframe src=\"javascript:false\"></iframe>", new() { WaitUntil = WaitUntilState.Commit });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.Load);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [UpstreamTest("WA-093 should work with broken data-url iframe")]
    public async Task ShouldWorkWithBrokenDataUrlIframe()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<iframe src=\"data:text/html\"></iframe>", new() { WaitUntil = WaitUntilState.Commit });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.Load);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [UpstreamTest("WA-094 should work with broken blob-url iframe")]
    public async Task ShouldWorkWithBrokenBlobUrlIframe()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<iframe src=\"blob:\"></iframe>", new() { WaitUntil = WaitUntilState.Commit });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.Load);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
