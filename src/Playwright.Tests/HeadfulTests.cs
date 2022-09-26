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

namespace Microsoft.Playwright.Tests;

///<playwright-file>headful.spec.ts</playwright-file>

public class HeadfulTests : PlaywrightTestEx
{
    [PlaywrightTest("headful.spec.ts", "should have default url when launching browser")]
    public async Task ShouldHaveDefaultUrlWhenLaunchingBrowser()
    {
        using var tempDir = new TempDirectory();
        await using var browserContext = await LaunchPersistentHeaded(tempDir.Path);

        string[] pages = browserContext.Pages.Select(page => page.Url).ToArray();
        Assert.AreEqual(new[] { "about:blank" }, pages);
    }

    [PlaywrightTest("headful.spec.ts", "headless should be able to read cookies written by headful")]
    [Ignore("Flaky")]
    public async Task HeadlessShouldBeAbleToReadCookiesWrittenByHeadful()
    {
        using var userDataDir = new TempDirectory();

        // Write a cookie in headful chrome            
        await using var headfulContext = await LaunchPersistentHeaded(userDataDir.Path);
        var headfulPage = await headfulContext.NewPageAsync();
        await headfulPage.GotoAsync(Server.EmptyPage);
        await headfulPage.EvaluateAsync("() => document.cookie = 'foo=true; expires=Fri, 31 Dec 9999 23:59:59 GMT'");
        await headfulContext.CloseAsync();

        var headlessContext = await LaunchPersistentHeaded(userDataDir.Path);
        var headlessPage = await headlessContext.NewPageAsync();
        await headlessPage.GotoAsync(Server.EmptyPage);
        string cookie = await headlessPage.EvaluateAsync<string>("() => document.cookie");
        await headlessContext.CloseAsync();

        Assert.AreEqual("foo=true", cookie);
    }

    [PlaywrightTest("headful.spec.ts", "should close browser with beforeunload page")]
    public async Task ShouldCloseBrowserWithBeforeunloadPage()
    {
        using var userDataDir = new TempDirectory();
        await using var browserContext = await LaunchPersistentHeaded(userDataDir.Path);
        var page = await browserContext.NewPageAsync();

        await page.GotoAsync(Server.Prefix + "/beforeunload.html");
        // We have to interact with a page so that 'beforeunload' handlers fire.
        await page.ClickAsync("body");
    }

    [PlaywrightTest("headful.spec.ts", "should not crash when creating second context")]
    public async Task ShouldNotCrashWhenCreatingSecondContext()
    {
        await using var browser = await LaunchHeaded();

        await using (var browserContext = await browser.NewContextAsync())
        {
            await browserContext.NewPageAsync();
        }

        await using (var browserContext = await browser.NewContextAsync())
        {
            await browserContext.NewPageAsync();
        }
    }

    [PlaywrightTest("headful.spec.ts", "should click background tab")]
    public async Task ShouldClickBackgroundTab()
    {
        await using var browser = await LaunchHeaded();
        var page = await browser.NewPageAsync();
        await page.SetContentAsync($"<button>Hello</button><a target=_blank href=\"{Server.EmptyPage}\">empty.html</a>");
        await page.ClickAsync("a");
        await page.ClickAsync("button");
    }

    [PlaywrightTest("headful.spec.ts", "should close browser after context menu was triggered")]
    public async Task ShouldCloseBrowserAfterContextMenuWasTriggered()
    {
        await using var browser = await LaunchHeaded();
        var page = await browser.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/grid.html");
        await page.ClickAsync("body", new() { Button = MouseButton.Right });
    }

    [PlaywrightTest("headful.spec.ts", "should(not) block third party cookies")]
    public async Task ShouldNotBlockThirdPartyCookies()
    {
        await using var browser = await LaunchHeaded();
        var page = await browser.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        await page.EvaluateAsync(@"src => {
                let fulfill;
                const promise = new Promise(x => fulfill = x);
                const iframe = document.createElement('iframe');
                document.body.appendChild(iframe);
                iframe.onload = fulfill;
                iframe.src = src;
                return promise;
            }", Server.CrossProcessPrefix + "/grid.html");

        string documentCookie = await page.Frames.ElementAt(1).EvaluateAsync<string>(@"() => {
                document.cookie = 'username=John Doe';
                return document.cookie;
            }");

        await page.WaitForTimeoutAsync(2000);
        bool allowsThirdParty = TestConstants.IsFirefox;
        Assert.AreEqual(allowsThirdParty ? "username=John Doe" : string.Empty, documentCookie);
        var cookies = await page.Context.CookiesAsync(new[] { Server.CrossProcessPrefix + "/grid.html" });

        if (allowsThirdParty)
        {
            Assert.That(cookies, Has.Count.EqualTo(1));
            var cookie = cookies.First();
            Assert.AreEqual("127.0.0.1", cookie.Domain);
            Assert.AreEqual(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.AreEqual("username", cookie.Name);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);
            Assert.False(cookie.Secure);
            Assert.AreEqual("John Doe", cookie.Value);
        }
        else
        {
            Assert.IsEmpty(cookies);
        }
    }

    [PlaywrightTest("headful.spec.ts", "should not override viewport size when passed null")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldNotOverrideViewportSizeWhenPassedNull()
    {
        await using var browser = await LaunchHeaded();
        var context = await browser.NewContextAsync(new() { ViewportSize = ViewportSize.NoViewport });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        var popupTask = page.WaitForPopupAsync();

        await TaskUtils.WhenAll(
            popupTask,
            page.EvaluateAsync(@"() => {
                    const win = window.open(window.location.href, 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=600,height=300,top=0,left=0');
                    win.resizeTo(500, 450);
                }"));

        var popup = popupTask.Result;
        await popup.WaitForLoadStateAsync();
        await popup.WaitForFunctionAsync("() => window.outerWidth === 500 && window.outerHeight === 450");
    }

    [PlaywrightTest("headful.spec.ts", "Page.bringToFront should work")]
    public async Task PageBringToFrontShouldWork()
    {
        await using var browser = await LaunchHeaded();
        var context = await browser.NewContextAsync(new() { ViewportSize = ViewportSize.NoViewport });
        var page1 = await context.NewPageAsync();
        await page1.SetContentAsync("Page1");
        var page2 = await context.NewPageAsync();
        await page2.SetContentAsync("Page2");

        await page1.BringToFrontAsync();
        Assert.AreEqual("visible", await page1.EvaluateAsync<string>("document.visibilityState"));
        Assert.AreEqual("visible", await page2.EvaluateAsync<string>("document.visibilityState"));

        await page2.BringToFrontAsync();
        Assert.AreEqual("visible", await page1.EvaluateAsync<string>("document.visibilityState"));
        Assert.AreEqual("visible", await page2.EvaluateAsync<string>("document.visibilityState"));
    }

    private Task<IBrowserContext> LaunchPersistentHeaded(string path)
    {
        return BrowserType.LaunchPersistentContextAsync(path, new() { Headless = false });
    }

    private Task<IBrowser> LaunchHeaded()
    {
        return BrowserType.LaunchAsync(new() { Headless = false });
    }
}
