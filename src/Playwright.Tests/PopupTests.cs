/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

public class PopupTests : BrowserTestEx
{
    [PlaywrightTest("popup.spec.ts", "should inherit user agent from browser context")]
    public async Task ShouldInheritUserAgentFromBrowserContext()
    {
        await using var context = await Browser.NewContextAsync(new() { UserAgent = "hey" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        var requestTcs = new TaskCompletionSource<string>();
        _ = Server.WaitForRequest("/popup/popup.html", request => requestTcs.TrySetResult(request.Headers["user-agent"]));

        await page.SetContentAsync("<a target=_blank rel=noopener href=\"/popup/popup.html\">link</a>");
        var popupTask = context.WaitForPageAsync(); // This is based on the python test so we can test WaitForPageAsync
        await TaskUtils.WhenAll(popupTask, page.ClickAsync("a"));

        await popupTask.Result.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        string userAgent = await popupTask.Result.EvaluateAsync<string>("() => window.initialUserAgent");
        await requestTcs.Task;

        Assert.AreEqual("hey", userAgent);
        Assert.AreEqual("hey", requestTcs.Task.Result);
    }

    [PlaywrightTest("popup.spec.ts", "should respect routes from browser context")]
    public async Task ShouldRespectRoutesFromBrowserContext()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        await page.SetContentAsync("<a target=_blank rel=noopener href=\"empty.html\">link</a>");
        bool intercepted = false;

        await context.RouteAsync("**/empty.html", (route) =>
        {
            route.ContinueAsync();
            intercepted = true;
        });

        var popupTask = context.WaitForPageAsync();
        await TaskUtils.WhenAll(popupTask, page.ClickAsync("a"));

        Assert.True(intercepted);
    }

    [PlaywrightTest("popup.spec.ts", "should inherit extra headers from browser context")]
    public async Task ShouldInheritExtraHeadersFromBrowserContext()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["foo"] = "bar"
            }
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        var requestTcs = new TaskCompletionSource<string>();
        _ = Server.WaitForRequest("/dummy.html", request => requestTcs.TrySetResult(request.Headers["foo"]));

        await page.EvaluateAsync(@"url => window._popup = window.open(url)", Server.Prefix + "/dummy.html");
        await requestTcs.Task;

        Assert.AreEqual("bar", requestTcs.Task.Result);
    }

    [PlaywrightTest("popup.spec.ts", "should inherit offline from browser context")]
    public async Task ShouldInheritOfflineFromBrowserContext()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await context.SetOfflineAsync(true);

        bool online = await page.EvaluateAsync<bool>(@"url => {
                const win = window.open(url);
                return win.navigator.onLine;
            }", Server.Prefix + "/dummy.html");

        await page.EvaluateAsync(@"url => window._popup = window.open(url)", Server.Prefix + "/dummy.html");

        Assert.False(online);
    }

    [PlaywrightTest("popup.spec.ts", "should inherit http credentials from browser context")]
    public async Task ShouldInheritHttpCredentialsFromBrowserContext()
    {
        Server.SetAuth("/title.html", "user", "pass");
        await using var context = await Browser.NewContextAsync(new()
        {
            HttpCredentials = new() { Username = "user", Password = "pass" },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        var popup = page.WaitForPopupAsync();

        await TaskUtils.WhenAll(
            popup,
            page.EvaluateAsync("url => window._popup = window.open(url)", Server.Prefix + "/title.html"));

        await popup.Result.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.AreEqual("Woof-Woof", await popup.Result.TitleAsync());
    }

    [PlaywrightTest("popup.spec.ts", "should inherit touch support from browser context")]
    public async Task ShouldInheritTouchSupportFromBrowserContext()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 400, Height = 500 },
            HasTouch = true,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        bool hasTouch = await page.EvaluateAsync<bool>(@"() => {
                const win = window.open('');
                return 'ontouchstart' in win;
            }");

        Assert.True(hasTouch);
    }

    [PlaywrightTest("popup.spec.ts", "should inherit viewport size from browser context")]
    public async Task ShouldInheritViewportSizeFromBrowserContext()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 400, Height = 500 },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        var size = await page.EvaluateAsync<ViewportSize>(@"() => {
                const win = window.open('about:blank');
                return { width: win.innerWidth, height: win.innerHeight };
            }");

        AssertEqual(400, 500, size);
    }

    [PlaywrightTest("popup.spec.ts", "should use viewport size from window features")]
    public async Task ShouldUseViewportSizeFromWindowFeatures()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 700, Height = 700 },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        var (size, popup) = await TaskUtils.WhenAll(
            page.EvaluateAsync<ViewportSize>(@"() => {
                    const win = window.open(window.location.href, 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=600,height=300,top=0,left=0');
                    return { width: win.innerWidth, height: win.innerHeight };
                }"),
            page.WaitForPopupAsync());

        await popup.SetViewportSizeAsync(500, 400);
        await popup.WaitForLoadStateAsync();
        var resized = await popup.EvaluateAsync<ViewportSize>(@"() => ({ width: window.innerWidth, height: window.innerHeight })");

        AssertEqual(600, 300, size);
        AssertEqual(500, 400, resized);
    }

    [PlaywrightTest("popup.spec.ts", "should respect routes from browser context using window.open")]
    public async Task ShouldRespectRoutesFromBrowserContextUsingWindowOpen()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        bool intercepted = false;

        await context.RouteAsync("**/empty.html", (route) =>
        {
            route.ContinueAsync();
            intercepted = true;
        });

        var popupTask = context.WaitForPageAsync();
        await TaskUtils.WhenAll(
            popupTask,
            page.EvaluateAsync("url => window.__popup = window.open(url)", Server.EmptyPage));

        Assert.True(intercepted);
    }

    [PlaywrightTest("popup.spec.ts", "BrowserContext.addInitScript should apply to an in-process popup")]
    public async Task BrowserContextAddInitScriptShouldApplyToAnInProcessPopup()
    {
        await using var context = await Browser.NewContextAsync();
        await context.AddInitScriptAsync("window.injected = 123;");
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        int injected = await page.EvaluateAsync<int>(@"() => {
                const win = window.open('about:blank');
                return win.injected;
            }");

        Assert.AreEqual(123, injected);
    }

    [PlaywrightTest("popup.spec.ts", "BrowserContext.addInitScript should apply to a cross-process popup")]
    public async Task BrowserContextAddInitScriptShouldApplyToACrossProcessPopup()
    {
        await using var context = await Browser.NewContextAsync();
        await context.AddInitScriptAsync("window.injected = 123;");
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        var popup = page.WaitForPopupAsync();

        await TaskUtils.WhenAll(
            popup,
            page.EvaluateAsync("url => window._popup = window.open(url)", Server.CrossProcessPrefix + "/title.html"));

        Assert.AreEqual(123, await popup.Result.EvaluateAsync<int>("injected"));
        await popup.Result.ReloadAsync();
        Assert.AreEqual(123, await popup.Result.EvaluateAsync<int>("injected"));
    }

    [PlaywrightTest("popup.spec.ts", "should expose function from browser context")]
    public async Task ShouldExposeFunctionFromBrowserContext()
    {
        await using var context = await Browser.NewContextAsync();
        await context.ExposeFunctionAsync("add", (int a, int b) => a + b);
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        int injected = await page.EvaluateAsync<int>(@"() => {
                const win = window.open('about:blank');
                return win.add(9, 4);
            }");

        Assert.AreEqual(13, injected);
    }

    void AssertEqual(int width, int height, ViewportSize size)
    {
        Assert.AreEqual(width, size.Width);
        Assert.AreEqual(height, size.Height);
    }
}
