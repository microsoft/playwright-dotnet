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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-page-event.spec.ts.</summary>
public class BrowsercontextPageEventTests : UpstreamBrowserTest
{
    [UpstreamTest("BC-062 should have url")]
    public async Task ShouldHaveUrl()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        var (otherPage, _) = await PromiseAll(
            context.WaitForPageAsync(),
            page.EvaluateAsync("url => window.open(url)", Server.EmptyPage));
        Assert.That(otherPage.Url, Is.EqualTo(Server.EmptyPage));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-063 should have url after domcontentloaded")]
    public async Task ShouldHaveUrlAfterDomcontentloaded()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        var (otherPage, _) = await PromiseAll(
            context.WaitForPageAsync(),
            page.EvaluateAsync("url => window.open(url)", Server.EmptyPage));
        await otherPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.That(otherPage.Url, Is.EqualTo(Server.EmptyPage));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-064 should have about:blank url with domcontentloaded")]
    public async Task ShouldHaveAboutBlankUrlWithDomcontentloaded()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        var (otherPage, _) = await PromiseAll(
            context.WaitForPageAsync(),
            page.EvaluateAsync("url => window.open(url)", "about:blank"));
        await otherPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.That(otherPage.Url, Is.EqualTo("about:blank"));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-065 should have about:blank for empty url with domcontentloaded")]
    public async Task ShouldHaveAboutBlankForEmptyUrlWithDomcontentloaded()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        var (otherPage, _) = await PromiseAll(
            context.WaitForPageAsync(),
            page.EvaluateAsync("() => window.open()"));
        await otherPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.That(otherPage.Url, Is.EqualTo("about:blank"));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-066 should report when a new page is created and closed")]
    public async Task ShouldReportWhenANewPageIsCreatedAndClosed()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        var (otherPage, _) = await PromiseAll(
            context.WaitForPageAsync(),
            page.EvaluateAsync("url => window.open(url)", Server.CrossProcessPrefix + "/empty.html"));
        // The url is about:blank in FF when 'page' event is fired.
        Assert.That(otherPage.Url, Does.Contain(Server.CrossProcessPrefix));
        Assert.That(await otherPage.EvaluateAsync<string>("() => ['Hello', 'world'].join(' ')"), Is.EqualTo("Hello world"));
        Assert.That(await otherPage.QuerySelectorAsync("body"), Is.Not.Null);

        var allPages = context.Pages;
        Assert.That(allPages, Does.Contain(page));
        Assert.That(allPages, Does.Contain(otherPage));

        var closeEventReceived = false;
        // .NET: once() is a handler that removes itself first.
        EventHandler<IPage> onClose = null;
        onClose = (_, _) =>
        {
            otherPage.Close -= onClose;
            closeEventReceived = true;
        };
        otherPage.Close += onClose;
        await otherPage.CloseAsync();
        Assert.That(closeEventReceived, Is.True);

        allPages = context.Pages;
        Assert.That(allPages, Does.Contain(page));
        Assert.That(allPages, Does.Not.Contain(otherPage));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-067 should report initialized pages")]
    public async Task ShouldReportInitializedPages()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var pagePromise = context.WaitForPageAsync();
        IgnoreErrors(context.NewPageAsync());
        var newPage = await pagePromise;
        Assert.That(newPage.Url, Is.EqualTo("about:blank"));

        var popupPromise = context.WaitForPageAsync();
        var evaluatePromise = newPage.EvaluateAsync("() => window.open('about:blank')");
        var popup = await popupPromise;
        Assert.That(popup.Url, Is.EqualTo("about:blank"));
        await evaluatePromise;
        await context.CloseAsync();
    }

    [UpstreamTest("BC-068 should not crash while redirecting of original request was missed")]
    public async Task ShouldNotCrashWhileRedirectingOfOriginalRequestWasMissed()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        ServerResponse serverResponse = null;
        Server.SetRoute("/one-style.css", (req, res) => serverResponse = res);
        // Open a new page. Use window.open to connect to the page later.
        var (newPage, _, _) = await PromiseAll(
            context.WaitForPageAsync(),
            page.EvaluateAsync("url => window.open(url)", Server.Prefix + "/one-style.html"),
            Server.WaitForRequest("/one-style.css"));
        // Issue a redirect.
        serverResponse.WriteHead(302, new() { ["location"] = "/injectedstyle.css" });
        serverResponse.End();
        await newPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.That(newPage.Url, Is.EqualTo(Server.Prefix + "/one-style.html"));
        // Cleanup.
        await context.CloseAsync();
    }

    [UpstreamTest("BC-069 should have an opener")]
    public async Task ShouldHaveAnOpener()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await PromiseAll(
            context.WaitForPageAsync(),
            page.GotoAsync(Server.Prefix + "/popup/window-open.html"));
        Assert.That(popup.Url, Is.EqualTo(Server.Prefix + "/popup/popup.html"));
        Assert.That(await popup.OpenerAsync(), Is.SameAs(page));
        Assert.That(await page.OpenerAsync(), Is.Null);
        await context.CloseAsync();
    }

    [UpstreamTest("BC-070 should fire page lifecycle events")]
    public async Task ShouldFirePageLifecycleEvents()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var events = new List<string>();
        // .NET: the parameter of the handler is `page` upstream, which is a variable of the test here.
        context.Page += (_, page1) =>
        {
            events.Add("CREATED: " + page1.Url);
            page1.Close += (_, _) => events.Add("DESTROYED: " + page1.Url);
        };
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.CloseAsync();
        Assert.That(events, Is.EqualTo(new[]
        {
            "CREATED: about:blank",
            $"DESTROYED: {Server.EmptyPage}",
        }));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-071 should work with Shift-clicking")]
    public async Task ShouldWorkWithShiftClicking()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync("<a href=\"/one-style.html\">yo</a>");
        var popup = await PromiseAll(
            context.WaitForPageAsync(),
            page.ClickAsync("a", new() { Modifiers = new[] { KeyboardModifier.Shift } }));
        Assert.That(await popup.OpenerAsync(), Is.Null);
        await context.CloseAsync();
    }

    [UpstreamTest("BC-072 should work with Ctrl-clicking")]
    public async Task ShouldWorkWithCtrlClicking()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync("<a href=\"/one-style.html\">yo</a>");
        var popup = await PromiseAll(
            context.WaitForPageAsync(),
            page.ClickAsync("a", new() { Modifiers = new[] { KeyboardModifier.ControlOrMeta } }));
        Assert.That(await popup.OpenerAsync(), Is.SameAs(BrowserName == "firefox" ? page : null));
        await context.CloseAsync();
    }
}
