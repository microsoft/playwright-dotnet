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

using System.Net;
using System.Text;

namespace Microsoft.Playwright.Tests;

public class BrowserContextRouteTests : BrowserTestEx
{
    [PlaywrightTest("browsercontext-route.spec.ts", "should intercept")]
    public async Task ShouldIntercept()
    {
        bool intercepted = false;

        await using var context = await Browser.NewContextAsync();
        IPage page = null;

        await context.RouteAsync("**/empty.html", (route) =>
        {
            intercepted = true;

            StringAssert.Contains("empty.html", route.Request.Url);
#pragma warning disable 0612
            Assert.False(string.IsNullOrEmpty(route.Request.Headers["user-agent"]));
#pragma warning restore 0612
            Assert.AreEqual(HttpMethod.Get.Method, route.Request.Method);
            Assert.Null(route.Request.PostData);
            Assert.True(route.Request.IsNavigationRequest);
            Assert.AreEqual("document", route.Request.ResourceType);
            Assert.AreEqual(page.MainFrame, route.Request.Frame);
            Assert.AreEqual("about:blank", page.MainFrame.Url);

            route.ContinueAsync();
        });

        page = await context.NewPageAsync();
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.True(response.Ok);
        Assert.True(intercepted);
    }

    [PlaywrightTest("browsercontext-route.spec.ts", "should unroute")]
    public async Task ShouldUnroute()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var intercepted = new List<int>();


        await context.RouteAsync("**/*", route =>
        {
            intercepted.Add(1);
            route.ContinueAsync();
        });

        await context.RouteAsync("**/empty.html", (route) =>
        {
            intercepted.Add(2);
            route.ContinueAsync();
        });

        await context.RouteAsync("**/empty.html", (route) =>
        {
            intercepted.Add(3);
            route.ContinueAsync();
        });

        Action<IRoute> handler4 = (route) =>
        {
            intercepted.Add(4);
            route.ContinueAsync();
        };
        await context.RouteAsync("**/empty.html", handler4);

        await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(new List<int>() { 4 }, intercepted);

        intercepted.Clear();
        await context.UnrouteAsync("**/empty.html", handler4);
        await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(new List<int>() { 3 }, intercepted);

        intercepted.Clear();
        await context.UnrouteAsync("**/empty.html");
        await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(new List<int>() { 1 }, intercepted);
    }

    [PlaywrightTest]
    public async Task ShouldUnroutePageWithBaseUrl()
    {
        var options = new BrowserNewContextOptions();
        options.BaseURL = Server.Prefix;

        await using var context = await Browser.NewContextAsync(options);
        var page = await context.NewPageAsync();
        var intercepted = new List<int>();

        await page.RouteAsync("/empty.html", (route) =>
        {
            intercepted.Add(1);
            route.ContinueAsync();
        });

        Action<IRoute> handler2 = (route) =>
        {
            intercepted.Add(2);
            route.ContinueAsync();
        };
        await page.RouteAsync("/empty.html", handler2);

        await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(new List<int>() { 2 }, intercepted);

        intercepted.Clear();
        await page.UnrouteAsync("/empty.html", handler2);
        await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(new List<int>() { 1 }, intercepted);
    }

    [PlaywrightTest("browsercontext-route.spec.ts", "should yield to page.route")]
    public async Task ShouldYieldToPageRoute()
    {
        await using var context = await Browser.NewContextAsync();
        await context.RouteAsync("**/empty.html", (route) =>
        {
            route.FulfillAsync(new() { Status = (int)HttpStatusCode.OK, Body = "context" });
        });

        var page = await context.NewPageAsync();
        await page.RouteAsync("**/empty.html", (route) =>
        {
            route.FulfillAsync(new() { Status = (int)HttpStatusCode.OK, Body = "page" });
        });

        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("page", await response.TextAsync());
    }

    [PlaywrightTest("browsercontext-route.spec.ts", "should fall back to context.route")]
    public async Task ShouldFallBackToContextRoute()
    {
        await using var context = await Browser.NewContextAsync();
        await context.RouteAsync("**/empty.html", (route) =>
        {
            route.FulfillAsync(new() { Status = (int)HttpStatusCode.OK, Body = "context" });
        });

        var page = await context.NewPageAsync();
        await page.RouteAsync("**/non-empty.html", (route) =>
        {
            route.FulfillAsync(new() { Status = (int)HttpStatusCode.OK, Body = "page" });
        });

        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("context", await response.TextAsync());
    }

    [PlaywrightTest]
    public async Task ShouldThrowOnInvalidRouteUrl()
    {
        await using var context = await Browser.NewContextAsync();

        var regexParseExceptionType = typeof(System.Text.RegularExpressions.Regex).Assembly
            .GetType("System.Text.RegularExpressions.RegexParseException", throwOnError: true);

        Assert.Throws(regexParseExceptionType, () =>
            context.RouteAsync("[", route =>
            {
                route.ContinueAsync();
            })
        );
    }

    [PlaywrightTest("browsercontext-route.spec.ts", "should support the times parameter with route matching")]
    public async Task ShouldSupportTheTimesParameterWithRouteMatching()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        List<int> intercepted = new();
        await context.RouteAsync("**/empty.html", (route) =>
        {
            intercepted.Add(1);
            route.ContinueAsync();
        }, new() { Times = 1 });

        await page.GotoAsync(Server.EmptyPage);
        await page.GotoAsync(Server.EmptyPage);
        await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(1, intercepted.Count);
    }

    [PlaywrightTest("browsercontext-route.spec.ts", "should support async handler w/ times")]
    public async Task ShouldSupportAsyncHandlerWithTimes()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await context.RouteAsync("**/empty.html", async (route) =>
        {
            await Task.Delay(100);
            await route.FulfillAsync(new() { Body = "<html>intercepted</html>", ContentType = "text/html" });
        }, new() { Times = 1 });

        await page.GotoAsync(Server.EmptyPage);
        await Expect(page.Locator("body")).ToHaveTextAsync("intercepted");
        await page.GotoAsync(Server.EmptyPage);
        await Expect(page.Locator("body")).Not.ToHaveTextAsync("intercepted");
    }

    [PlaywrightTest("browsercontext-route.spec.ts", "should chain fallback")]
    public async Task ShouldChainFallback()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var intercepted = new List<int>();
        await context.RouteAsync("**/empty.html", (route) =>
        {
            intercepted.Add(1);
            route.FallbackAsync();
        });
        await context.RouteAsync("**/empty.html", (route) =>
        {
            intercepted.Add(2);
            route.FallbackAsync();
        });
        await context.RouteAsync("**/empty.html", (route) =>
        {
            intercepted.Add(3);
            route.FallbackAsync();
        });

        await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(new List<int>() { 3, 2, 1 }, intercepted);
    }

    [PlaywrightTest("browsercontext-route.spec.ts", "should not chain fulfill")]
    public async Task ShouldNotChainFulfill()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var failed = false;
        await context.RouteAsync("**/empty.html", (route) =>
        {
            failed = true;
        });
        await context.RouteAsync("**/empty.html", (route) =>
        {
            route.FulfillAsync(new() { Status = 200, Body = "fulfilled" });
        });
        await context.RouteAsync("**/empty.html", (route) =>
        {
            route.FallbackAsync();
        });

        var response = await page.GotoAsync(Server.EmptyPage);
        var body = await response.BodyAsync();
        Assert.AreEqual(Encoding.UTF8.GetString(body), "fulfilled");
        Assert.IsFalse(failed);
    }

    [PlaywrightTest("browsercontext-route.spec.ts", "should not chain abort")]
    public async Task ShouldNotChainAbort()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var failed = false;
        await context.RouteAsync("**/empty.html", (route) =>
        {
            failed = true;
        });
        await context.RouteAsync("**/empty.html", (route) =>
        {
            route.AbortAsync();
        });
        await context.RouteAsync("**/empty.html", (route) =>
        {
            route.FallbackAsync();
        });

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () =>
        {
            await page.GotoAsync(Server.EmptyPage);
        });
        Assert.NotNull(exception);
        Assert.IsFalse(failed);
    }

    [PlaywrightTest("browsercontext-route.spec.ts", "should chain fallback into page")]
    public async Task ShouldChainFallbackIntoPage()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var interceped = new List<int>();
        await context.RouteAsync("**/empty.html", (route) =>
        {
            interceped.Add(1);
            route.FallbackAsync();
        });
        await context.RouteAsync("**/empty.html", (route) =>
        {
            interceped.Add(2);
            route.FallbackAsync();
        });
        await context.RouteAsync("**/empty.html", (route) =>
        {
            interceped.Add(3);
            route.FallbackAsync();
        });
        await page.RouteAsync("**/empty.html", (route) =>
        {
            interceped.Add(4);
            route.FallbackAsync();
        });
        await page.RouteAsync("**/empty.html", (route) =>
        {
            interceped.Add(5);
            route.FallbackAsync();
        });
        await page.RouteAsync("**/empty.html", (route) =>
        {
            interceped.Add(6);
            route.FallbackAsync();
        });
        await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(new List<int>() { 6, 5, 4, 3, 2, 1 }, interceped);
    }

    [PlaywrightTest("browsercontext-route.spec.ts", "should chain fallback w/ dynamic URL")]
    public async Task ShouldChainFallbackWithDynamicURL()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var interceped = new List<int>();
        await context.RouteAsync("**/bar", (route) =>
        {
            interceped.Add(1);
            route.FallbackAsync(new() { Url = Server.EmptyPage });
        });
        await context.RouteAsync("**/foo", (route) =>
        {
            interceped.Add(2);
            route.FallbackAsync(new() { Url = "http://localhost/bar" });
        });
        await context.RouteAsync("**/empty.html", (route) =>
        {
            interceped.Add(3);
            route.FallbackAsync(new() { Url = "http://localhost/foo" });
        });
        await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(new List<int>() { 3, 2, 1 }, interceped);
    }
}
