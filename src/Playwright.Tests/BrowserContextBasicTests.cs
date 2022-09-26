/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Net;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class BrowserContextBasicTests : BrowserTestEx
{
    [PlaywrightTest("browsercontext-basic.spec.ts", "should create new context")]
    public async Task ShouldCreateNewContext()
    {
        await using var browser = await BrowserType.LaunchAsync();
        Assert.IsEmpty(browser.Contexts);
        await using var context = await browser.NewContextAsync();
        Assert.That(browser.Contexts, Has.Length.EqualTo(1));
        CollectionAssert.Contains(browser.Contexts, context);
        Assert.AreEqual(browser, context.Browser);
        await context.CloseAsync();
        Assert.IsEmpty(browser.Contexts);
        Assert.AreEqual(browser, context.Browser);
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "window.open should use parent tab context")]
    public async Task WindowOpenShouldUseParentTabContext()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        var popupTargetCompletion = new TaskCompletionSource<IPage>();
        page.Popup += (_, e) => popupTargetCompletion.SetResult(e);

        var (popupTarget, _) = await TaskUtils.WhenAll(
            popupTargetCompletion.Task,
            page.EvaluateAsync("url => window.open(url)", Server.EmptyPage)
        );

        Assert.AreEqual(context, popupTarget.Context);
        await context.CloseAsync();
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "should isolate localStorage and cookies")]
    public async Task ShouldIsolateLocalStorageAndCookies()
    {
        // Create two incognito contexts.
        await using var browser = await BrowserType.LaunchAsync();
        var context1 = await browser.NewContextAsync();
        var context2 = await browser.NewContextAsync();
        Assert.IsEmpty(context1.Pages);
        Assert.IsEmpty(context2.Pages);

        // Create a page in first incognito context.
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.EmptyPage);
        await page1.EvaluateAsync(@"() => {
                localStorage.setItem('name', 'page1');
                document.cookie = 'name=page1';
            }");

        Assert.That(context1.Pages, Has.Count.EqualTo(1));
        Assert.IsEmpty(context2.Pages);

        // Create a page in second incognito context.
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        await page2.EvaluateAsync(@"() => {
                localStorage.setItem('name', 'page2');
                document.cookie = 'name=page2';
            }");

        Assert.That(context1.Pages, Has.Count.EqualTo(1));
        Assert.AreEqual(page1, context1.Pages.FirstOrDefault());
        Assert.That(context2.Pages, Has.Count.EqualTo(1));
        Assert.AreEqual(page2, context2.Pages.FirstOrDefault());

        // Make sure pages don't share localstorage or cookies.
        Assert.AreEqual("page1", await page1.EvaluateAsync<string>("() => localStorage.getItem('name')"));
        Assert.AreEqual("name=page1", await page1.EvaluateAsync<string>("() => document.cookie"));
        Assert.AreEqual("page2", await page2.EvaluateAsync<string>("() => localStorage.getItem('name')"));
        Assert.AreEqual("name=page2", await page2.EvaluateAsync<string>("() => document.cookie"));

        // Cleanup contexts.
        await TaskUtils.WhenAll(context1.CloseAsync(), context2.CloseAsync());
        Assert.IsEmpty(browser.Contexts);
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "should propagate default viewport to the page")]
    public async Task ShouldPropagateDefaultViewportToThePage()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 456,
                Height = 789
            }
        });

        var page = await context.NewPageAsync();
        await TestUtils.VerifyViewportAsync(page, 456, 789);
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "should make a copy of default viewport")]
    public async Task ShouldMakeACopyOfDefaultViewport()
    {
        var viewport = new ViewportSize
        {
            Width = 456,
            Height = 789
        };

        await using var context = await Browser.NewContextAsync(new() { ViewportSize = viewport });

        viewport.Width = 567;

        var page = await context.NewPageAsync();

        await TestUtils.VerifyViewportAsync(page, 456, 789);
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "should respect deviceScaleFactor")]
    public async Task ShouldRespectDeviceScaleFactor()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            DeviceScaleFactor = 3
        });

        var page = await context.NewPageAsync();
        Assert.AreEqual(3, await page.EvaluateAsync<int>("window.devicePixelRatio"));
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "should not allow deviceScaleFactor with null viewport")]
    public async Task ShouldNotAllowDeviceScaleFactorWithViewportDisabled()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Browser.NewContextAsync(new()
        {
            ViewportSize = ViewportSize.NoViewport,
            DeviceScaleFactor = 3,
        }));
        Assert.AreEqual("\"deviceScaleFactor\" option is not supported with null \"viewport\"", exception.Message);
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "should not allow isMobile with null viewport")]
    public async Task ShouldNotAllowIsMobileWithViewportDisabled()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Browser.NewContextAsync(new()
        {
            ViewportSize = ViewportSize.NoViewport,
            IsMobile = true,
        }));
        Assert.AreEqual("\"isMobile\" option is not supported with null \"viewport\"", exception.Message);
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "close() should work for empty context")]
    public async Task CloseShouldWorkForEmptyContext()
    {
        var context = await Browser.NewContextAsync();
        await context.CloseAsync();
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "close() should abort waitForEvent")]
    public async Task CloseShouldAbortWaitForEvent()
    {
        var context = await Browser.NewContextAsync();
        var waitTask = context.WaitForPageAsync();
        await context.CloseAsync();
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => waitTask);
        Assert.AreEqual("Context closed", exception.Message);
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "should not report frameless pages on error")]
    public async Task ShouldNotReportFramelessPagesOnError()
    {
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        Server.SetRoute("/empty.html", context =>
        {
            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync($"<a href=\"{Server.EmptyPage}\" target=\"_blank\">Click me</a>");
        });

        IPage popup = null;
        context.Page += (_, e) => popup = e;
        await page.GotoAsync(Server.EmptyPage);
        await page.ClickAsync("'Click me'");
        await context.CloseAsync();

        if (popup != null)
        {
            Assert.True(popup.IsClosed);
            Assert.NotNull(popup.MainFrame);
        }
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "close() should be callable twice")]
    public async Task CloseShouldBeCallableTwice()
    {
        var context = await Browser.NewContextAsync();
        await TaskUtils.WhenAll(context.CloseAsync(), context.CloseAsync());
        await context.CloseAsync();
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "should return all of the pages")]
    public async Task ShouldReturnAllOfThePages()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var second = await context.NewPageAsync();

        Assert.AreEqual(2, context.Pages.Count);
        CollectionAssert.Contains(context.Pages, page);
        CollectionAssert.Contains(context.Pages, second);
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "BrowserContext.pages()", "should close all belonging pages once closing context")]
    public async Task ShouldCloseAllBelongingPagesOnceClosingContext()
    {
        await using var context = await Browser.NewContextAsync();
        await context.NewPageAsync();

        Assert.That(context.Pages, Has.Count.EqualTo(1));

        await context.CloseAsync();

        Assert.IsEmpty(context.Pages);
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "should disable javascript")]
    public async Task ShouldDisableJavascript()
    {
        await using (var context = await Browser.NewContextAsync(new() { JavaScriptEnabled = false }))
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync("data:text/html, <script>var something = 'forbidden'</script>");

            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.EvaluateAsync("something"));

            StringAssert.Contains(
                TestConstants.IsWebKit ? "Can't find variable: something" : "something is not defined",
                exception.Message);
        }

        await using (var context = await Browser.NewContextAsync())
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync("data:text/html, <script>var something = 'forbidden'</script>");
            Assert.AreEqual("forbidden", await page.EvaluateAsync<string>("something"));
        }
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "should be able to navigate after disabling javascript")]
    public async Task ShouldBeAbleToNavigateAfterDisablingJavascript()
    {
        await using var context = await Browser.NewContextAsync(new() { JavaScriptEnabled = false });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "should work with offline option")]
    public async Task ShouldWorkWithOfflineOption()
    {
        await using var context = await Browser.NewContextAsync(new() { Offline = true });
        var page = await context.NewPageAsync();
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.GotoAsync(Server.EmptyPage));
        await context.SetOfflineAsync(false);
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("browsercontext-basic.spec.ts", "should emulate navigator.onLine")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldEmulateNavigatorOnLine()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        Assert.True(await page.EvaluateAsync<bool>("() => window.navigator.onLine"));
        await context.SetOfflineAsync(true);
        Assert.False(await page.EvaluateAsync<bool>("() => window.navigator.onLine"));
        await context.SetOfflineAsync(false);
        Assert.True(await page.EvaluateAsync<bool>("() => window.navigator.onLine"));
    }
}
