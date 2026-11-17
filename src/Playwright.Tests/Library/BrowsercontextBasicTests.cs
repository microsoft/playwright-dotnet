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

using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-basic.spec.ts.</summary>
public class BrowsercontextBasicTests
{
    [UpstreamTest("BC-001 should create new context @smoke")]
    public async Task ShouldCreateNewContext(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        Assert.That(browser.Contexts.Count, Is.EqualTo(0));
        var context = await browser.NewContextAsync(contextOptions);
        Assert.That(browser.Contexts, Is.EqualTo(new[] { context }));
        Assert.That(browser, Is.SameAs(context.Browser));
        var context2 = await browser.NewContextAsync(contextOptions);
        Assert.That(browser.Contexts, Is.EqualTo(new[] { context, context2 }));
        await context.CloseAsync();
        Assert.That(browser.Contexts, Is.EqualTo(new[] { context2 }));
        Assert.That(browser, Is.SameAs(context.Browser));
        await context2.CloseAsync();
    }

    [UpstreamTest("BC-002 should be able to click across browser contexts")]
    public async Task ShouldBeAbleToClickAcrossBrowserContexts(IBrowser browser, BrowserNewPageOptions newPageOptions)
    {
        Assert.That(browser.Contexts.Count, Is.EqualTo(0));

        var createPage = async () =>
        {
            var page = await browser.NewPageAsync(newPageOptions);
            await page.SetContentAsync("<button>Click me</button>");
            await page.Locator("button").EvaluateAsync(@"button => {
      window['clicks'] = 0;
      button.addEventListener('click', () => ++window['clicks'], false);
    }");
            return page;
        };

        var clickInPage = async (IPage page, int count) =>
        {
            for (var i = 0; i < count; ++i)
            {
                await page.Locator("button").ClickAsync();
            }
        };

        var getClicks = async (IPage page) => await page.EvaluateAsync<int>("() => window['clicks']");

        var page1 = await createPage();
        var page2 = await createPage();

        const int CLICK_COUNT = 20;
        await PromiseAll(
            clickInPage(page1, CLICK_COUNT),
            clickInPage(page2, CLICK_COUNT));
        Assert.That(await getClicks(page1), Is.EqualTo(CLICK_COUNT));
        Assert.That(await getClicks(page2), Is.EqualTo(CLICK_COUNT));

        await page1.CloseAsync();
        await page2.CloseAsync();
    }

    [UpstreamTest("BC-003 should be able to hover across browser contexts in parallel")]
    public async Task ShouldBeAbleToHoverAcrossBrowserContextsInParallel(IBrowser browser, string browserName, bool headless, BrowserNewContextOptions contextOptions)
    {
        Info.Fixme(browserName == "firefox" && !headless, "Hover is flaky in headed Firefox");

        var html = @"
    <style>
      [role=""tooltip""] { display: none; }
      [role=""tooltip""].visible { display: block; }
    </style>
    <button>Hover me</button>
    <div role=""tooltip"">Tooltip content</div>
    <script>
      const button = document.querySelector('button');
      const tooltip = document.querySelector('[role=""tooltip""]');
      button.addEventListener('pointerenter', () => tooltip.classList.add('visible'));
      button.addEventListener('pointerleave', () => tooltip.classList.remove('visible'));
    </script>
  ";

        var pages = await PromiseAll(new[] { 1, 2, 3, 4, 5 }.Select(async _ =>
        {
            var context = await browser.NewContextAsync(contextOptions);
            var page = await context.NewPageAsync();
            await page.SetContentAsync(html);
            return page;
        }));

        await PromiseAll(pages.Select(async (page, index) =>
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Hover me" }).HoverAsync();
            await Expect(page.GetByRole(AriaRole.Tooltip), $"Tooltip for page {index + 1} should be visible").ToBeVisibleAsync();
        }));

        await PromiseAll(pages.Select(page => page.Context.CloseAsync()));
    }

    [UpstreamTest("BC-004 window.open should use parent tab context")]
    public async Task WindowOpenShouldUseParentTabContext(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window.open(url)", server.EmptyPage));
        Assert.That(popup.Context, Is.SameAs(context));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-005 should isolate localStorage and cookies @smoke")]
    public async Task ShouldIsolateLocalStorageAndCookies(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        // Create two incognito contexts.
        var context1 = await browser.NewContextAsync(contextOptions);
        var context2 = await browser.NewContextAsync(contextOptions);
        Assert.That(context1.Pages.Count, Is.EqualTo(0));
        Assert.That(context2.Pages.Count, Is.EqualTo(0));

        // Create a page in first incognito context.
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(server.EmptyPage);
        await page1.EvaluateAsync(@"() => {
    localStorage.setItem('name', 'page1');
    document.cookie = 'name=page1';
  }");

        Assert.That(context1.Pages.Count, Is.EqualTo(1));
        Assert.That(context2.Pages.Count, Is.EqualTo(0));

        // Create a page in second incognito context.
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(server.EmptyPage);
        await page2.EvaluateAsync(@"() => {
    localStorage.setItem('name', 'page2');
    document.cookie = 'name=page2';
  }");

        Assert.That(context1.Pages.Count, Is.EqualTo(1));
        Assert.That(context2.Pages.Count, Is.EqualTo(1));
        Assert.That(context1.Pages[0], Is.SameAs(page1));
        Assert.That(context2.Pages[0], Is.SameAs(page2));

        // Make sure pages don't share localstorage or cookies.
        Assert.That(await page1.EvaluateAsync<string>("() => localStorage.getItem('name')"), Is.EqualTo("page1"));
        Assert.That(await page1.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("name=page1"));
        Assert.That(await page2.EvaluateAsync<string>("() => localStorage.getItem('name')"), Is.EqualTo("page2"));
        Assert.That(await page2.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("name=page2"));

        // Cleanup contexts.
        await PromiseAll(
            context1.CloseAsync(),
            context2.CloseAsync());
        Assert.That(browser.Contexts.Count, Is.EqualTo(0));
    }

    [UpstreamTest("BC-006 should propagate default viewport to the page")]
    public async Task ShouldPropagateDefaultViewportToThePage(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 456, Height = 789 } });
        var page = await context.NewPageAsync();
        await VerifyViewport(page, 456, 789);
        await context.CloseAsync();
    }

    [UpstreamTest("BC-007 should make a copy of default viewport")]
    public async Task ShouldMakeACopyOfDefaultViewport(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var viewport = new ViewportSize { Width = 456, Height = 789 };
        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = viewport });
        viewport.Width = 567;
        var page = await context.NewPageAsync();
        await VerifyViewport(page, 456, 789);
        await context.CloseAsync();
    }

    [UpstreamTest("BC-008 should respect deviceScaleFactor")]
    public async Task ShouldRespectDeviceScaleFactor(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { DeviceScaleFactor = 3 });
        var page = await context.NewPageAsync();
        Assert.That(await page.EvaluateAsync<int>("window.devicePixelRatio"), Is.EqualTo(3));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-009 should not allow deviceScaleFactor with null viewport")]
    public async Task ShouldNotAllowDeviceScaleFactorWithNullViewport(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        // .NET: viewport: null is ViewportSize.NoViewport.
        var error = await Catch(() => browser.NewContextAsync(new(contextOptions) { ViewportSize = ViewportSize.NoViewport, DeviceScaleFactor = 1 }));
        Assert.That(error.Message, Does.Contain("\"deviceScaleFactor\" option is not supported with null \"viewport\""));
    }

    [UpstreamTest("BC-010 should not allow isMobile with null viewport")]
    public async Task ShouldNotAllowIsMobileWithNullViewport(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        // .NET: viewport: null is ViewportSize.NoViewport.
        var error = await Catch(() => browser.NewContextAsync(new(contextOptions) { ViewportSize = ViewportSize.NoViewport, IsMobile = true }));
        Assert.That(error.Message, Does.Contain("\"isMobile\" option is not supported with null \"viewport\""));
    }

    [UpstreamTest("BC-011 close() should work for empty context")]
    public async Task CloseShouldWorkForEmptyContext(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        await context.CloseAsync();
    }

    [UpstreamTest("BC-012 close() should abort waitForEvent")]
    public async Task CloseShouldAbortWaitForEvent(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var promise = Catch(() => context.WaitForPageAsync());
        await context.CloseAsync();
        var error = await promise;
        Assert.That(error.Message, Does.Contain(TargetClosedErrorMessage));
    }

    [UpstreamTest("BC-013 close() should be callable twice")]
    public async Task CloseShouldBeCallableTwice(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        await context.CloseAsync();
        await context.CloseAsync();
    }

    [UpstreamTest("BC-014 should pass self to close event")]
    public async Task ShouldPassSelfToCloseEvent(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var newContext = await browser.NewContextAsync(contextOptions);
        var closedContext = await PromiseAll(
            Once<IBrowserContext>(h => newContext.Close += h, h => newContext.Close -= h),
            newContext.CloseAsync());
        Assert.That(closedContext, Is.SameAs(newContext));
    }

    [UpstreamTest("BC-015 should not report frameless pages on error")]
    public async Task ShouldNotReportFramelessPagesOnError(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.End($"<a href=\"{server.EmptyPage}\" target=\"_blank\">Click me</a>");
        });
        IPage popup = null;
        context.Page += (_, p) => popup = p;
        await page.GotoAsync(server.EmptyPage);
        await page.ClickAsync("\"Click me\"");
        await context.CloseAsync();
        if (popup != null)
        {
            // This races on Firefox :/
            Assert.That(popup.IsClosed, Is.True);
            Assert.That(popup.MainFrame, Is.Not.Null);
        }
    }

    [UpstreamTest("BC-016 should return all of the pages")]
    public async Task ShouldReturnAllOfThePages(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        var second = await context.NewPageAsync();
        var allPages = context.Pages;
        Assert.That(allPages.Count, Is.EqualTo(2));
        Assert.That(allPages, Does.Contain(page));
        Assert.That(allPages, Does.Contain(second));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-017 should close all belonging pages once closing context")]
    public async Task ShouldCloseAllBelongingPagesOnceClosingContext(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        await context.NewPageAsync();
        Assert.That(context.Pages.Count, Is.EqualTo(1));

        await context.CloseAsync();
        Assert.That(context.Pages.Count, Is.EqualTo(0));
    }

    [UpstreamTest("BC-018 should disable javascript")]
    public async Task ShouldDisableJavascript(IBrowser browser, string browserName, BrowserNewContextOptions contextOptions)
    {
        {
            var context = await browser.NewContextAsync(new(contextOptions) { JavaScriptEnabled = false });
            var page = await context.NewPageAsync();
            await page.GotoAsync("data:text/html, <script>var something = \"forbidden\"</script>");
            Exception error = null;
            error = await Catch(() => page.EvaluateAsync("something"));
            if (browserName == "webkit")
            {
                Assert.That(error.Message, Does.Contain("Can't find variable: something"));
            }
            else
            {
                Assert.That(error.Message, Does.Contain("something is not defined"));
            }
            await context.CloseAsync();
        }

        {
            var context = await browser.NewContextAsync(contextOptions);
            var page = await context.NewPageAsync();
            await page.GotoAsync("data:text/html, <script>var something = \"forbidden\"</script>");
            Assert.That(await page.EvaluateAsync<string>("something"), Is.EqualTo("forbidden"));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("BC-019 should be able to navigate after disabling javascript")]
    public async Task ShouldBeAbleToNavigateAfterDisablingJavascript(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { JavaScriptEnabled = false });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        await context.CloseAsync();
    }

    [UpstreamTest("BC-020 should not hang on promises after disabling javascript")]
    public async Task ShouldNotHangOnPromisesAfterDisablingJavascript(string browserName, ContextFactory contextFactory, bool isBidi)
    {
        Info.Fixme(browserName == "firefox" && !isBidi);
        var context = await contextFactory(new() { JavaScriptEnabled = false });
        var page = await context.NewPageAsync();
        Assert.That(await page.EvaluateAsync<int>("() => 1"), Is.EqualTo(1));
        Assert.That(await page.EvaluateAsync<int>("async () => 2"), Is.EqualTo(2));
    }

    [UpstreamTest("BC-021 setContent should work after disabling javascript")]
    public async Task SetContentShouldWorkAfterDisablingJavascript(ContextFactory contextFactory)
    {
        var context = await contextFactory(new() { JavaScriptEnabled = false });
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<h1>Hello</h1>");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello");
    }

    [UpstreamTest("BC-022 should retry click after disabling javascript")]
    public async Task ShouldRetryClickAfterDisablingJavascript(ContextFactory contextFactory)
    {
        var context = await contextFactory(new() { JavaScriptEnabled = false });
        var page = await context.NewPageAsync();
        await page.SetContentAsync(@"
    <details><summary>Continue</summary>Details</details>
    <div id=""cover"" style=""position: fixed; inset: 0;""></div>
  ");
        var clicked = false;
        // .NET: page.locator('summary').click().then(() => clicked = true) is a local function that is called in its place.
        async Task Click()
        {
            await page.Locator("summary").ClickAsync();
            clicked = true;
        }
        var clickPromise = Click();
        // Give it enough time to go through a few retries while the button is covered.
        await page.WaitForTimeoutAsync(2000);
        Assert.That(clicked, Is.EqualTo(false));
        await page.Locator("#cover").EvaluateAsync("e => e.remove()");
        await clickPromise;
        await Expect(page.Locator("details")).ToHaveAttributeAsync("open", "");
    }

    [UpstreamTest("BC-023 should work with offline option")]
    public async Task ShouldWorkWithOfflineOption(IBrowser browser, Server server, string browserName, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { Offline = true });
        var page = await context.NewPageAsync();
        Exception error = null;
        if (browserName == "firefox")
        {
            // Firefox navigates to an error page, and this navigation might conflict with the
            // next navigation we do in test.
            // So we need to wait for the navigation explicitly.
            (error, _) = await PromiseAll(
                Catch(() => page.GotoAsync(server.EmptyPage)),
                Once<IFrame>(h => page.FrameNavigated += h, h => page.FrameNavigated -= h));
        }
        else
        {
            error = await Catch(() => page.GotoAsync(server.EmptyPage));
        }
        Assert.That(error, Is.Not.Null);
        await context.SetOfflineAsync(false);
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-024 fetch with keepalive should throw when offline")]
    public async Task FetchWithKeepaliveShouldThrowWhenOffline(ContextFactory contextFactory, Server server)
    {
        var context = await contextFactory();
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);

        var url = server.Prefix + "/fetch";
        server.SetRoute("/fetch", (req, res) =>
        {
            res.SetHeader("Content-Type", "application/json");
            res.End(JsonSerializer.Serialize("hello"));
        });

        var okResponse = await page.EvaluateAsync<string>("url => fetch(url, { cache: 'no-store', keepalive: true }).then(response => response.json())", url);
        Assert.That(okResponse, Is.EqualTo("hello"));

        await context.SetOfflineAsync(true);
        var offlineResponse = await page.EvaluateAsync<string>(@"async url => {
    try {
      const response = await fetch(url, { cache: 'no-store', keepalive: true });
      return await response.json();
    } catch {
      return 'error';
    }
  }", url);
        Assert.That(offlineResponse, Is.EqualTo("error"));
    }

    [UpstreamTest("BC-025 should emulate navigator.onLine")]
    public async Task ShouldEmulateNavigatorOnLine(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        Assert.That(await page.EvaluateAsync<bool>("() => window.navigator.onLine"), Is.EqualTo(true));
        await context.SetOfflineAsync(true);
        Assert.That(await page.EvaluateAsync<bool>("() => window.navigator.onLine"), Is.EqualTo(false));
        await context.SetOfflineAsync(false);
        Assert.That(await page.EvaluateAsync<bool>("() => window.navigator.onLine"), Is.EqualTo(true));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-026 should emulate navigator.onLine across navigations")]
    public async Task ShouldEmulateNavigatorOnLineAcrossNavigations(IBrowser browser, Server server, string browserName, int browserMajorVersion, BrowserNewContextOptions contextOptions)
    {
        Info.Skip(browserName == "chromium" && browserMajorVersion < 155, "does not survive cross-process navgiation");

        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.EvaluateAsync<bool>("() => window.navigator.onLine"), Is.EqualTo(true));
        await context.SetOfflineAsync(true);
        Assert.That(await page.EvaluateAsync<bool>("() => window.navigator.onLine"), Is.EqualTo(false));

        await page.GotoAsync("about:blank");
        Assert.That(await page.EvaluateAsync<bool>("() => window.navigator.onLine"), Is.EqualTo(false));
        await page.GotoAsync("data:text/html,<title>offline</title>");
        Assert.That(await page.EvaluateAsync<bool>("() => window.navigator.onLine"), Is.EqualTo(false));

        if (browserName == "chromium")
        {
            // Try a cross-process navigation in Chromium, which allows routing while offline.
            await page.RouteAsync("**/*", route => route.FulfillAsync(new() { ContentType = "text/html", Body = "<html></html>" }));
            await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
            Assert.That(await page.EvaluateAsync<bool>("() => window.navigator.onLine"), Is.EqualTo(false));
        }

        await context.SetOfflineAsync(false);
        Assert.That(await page.EvaluateAsync<bool>("() => window.navigator.onLine"), Is.EqualTo(true));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-027 should emulate offline event")]
    public async Task ShouldEmulateOfflineEvent(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        var events = await page.EvaluateHandleAsync(@"() => {
    const events = [];
    window.addEventListener('offline', () => events.push('offline'));
    window.addEventListener('online', () => events.push('online'));
    return events;
  }");
        await context.SetOfflineAsync(true);
        await Expect(async () => Assert.That(await events.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "offline" }))).ToPassAsync();
        await context.SetOfflineAsync(false);
        await Expect(async () => Assert.That(await events.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "offline", "online" }))).ToPassAsync();
        await context.CloseAsync();
    }

    [UpstreamTest("BC-028 should emulate media in popup")]
    public async Task ShouldEmulateMediaInPopup(IBrowser browser, Server server, BrowserNewContextOptions contextOptions, BrowserNewPageOptions newPageOptions)
    {
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ColorScheme = ColorScheme.Dark });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            var (popup, _) = await PromiseAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("url => { window.open(url); }", server.EmptyPage));
            Assert.That(await popup.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"), Is.EqualTo(false));
            Assert.That(await popup.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(true));
            await context.CloseAsync();
        }
        {
            var page = await browser.NewPageAsync(new(newPageOptions) { ColorScheme = ColorScheme.Light });
            await page.GotoAsync(server.EmptyPage);
            var (popup, _) = await PromiseAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("url => { window.open(url); }", server.EmptyPage));
            Assert.That(await popup.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"), Is.EqualTo(true));
            Assert.That(await popup.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(false));
            await page.CloseAsync();
        }
    }

    [UpstreamTest("BC-029 should emulate media in cross-process iframe")]
    public async Task ShouldEmulateMediaInCrossProcessIframe(IBrowser browser, Server server, BrowserNewPageOptions newPageOptions)
    {
        var page = await browser.NewPageAsync(new(newPageOptions) { ColorScheme = ColorScheme.Dark });
        await page.GotoAsync(server.EmptyPage);
        await AttachFrame(page, "frame1", server.CrossProcessPrefix + "/empty.html");
        var frame = page.Frames[1];
        Assert.That(await frame.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(true));
        await page.CloseAsync();
    }

    [UpstreamTest("BC-031 should create two pages in parallel in various contexts")]
    public async Task ShouldCreateTwoPagesInParallelInVariousContexts(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context1 = await browser.NewContextAsync(contextOptions);
        var context2 = await browser.NewContextAsync(contextOptions);
        await PromiseAll(
            context1.NewPageAsync(),
            context1.NewPageAsync(),
            context2.NewPageAsync(),
            context2.NewPageAsync());
        await context1.CloseAsync();
        await context2.CloseAsync();
        var context3 = await browser.NewContextAsync(contextOptions);
        await PromiseAll(
            context3.NewPageAsync(),
            context3.NewPageAsync());
        await context3.CloseAsync();
    }
}
