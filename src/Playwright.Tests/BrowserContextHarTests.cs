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

using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests;

public class BrowserContextHarTests : ContextTestEx
{
    [PlaywrightTest("browsercontext-har.spec.ts", "should context.routeFromHAR, matching the method and following redirects")]
    public async Task ShouldContextRouteFromHarMatchingTheMethodAndFollowingRedirects()
    {
        var path = TestUtils.GetAsset("har-fulfill.har");
        await Context.RouteFromHARAsync(path);
        var page = await Context.NewPageAsync();
        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.AreEqual(await page.EvaluateAsync<string>("window.value"), "foo");
        // HAR contains a POST for the css file that should not be used.
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should page.routeFromHAR, matching the method and following redirects")]
    public async Task ShouldPageRouteFromHarMatchingTheMethodAndFollowingRedirects()
    {
        var path = TestUtils.GetAsset("har-fulfill.har");
        var page = await Context.NewPageAsync();
        await page.RouteFromHARAsync(path);
        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.AreEqual(await page.EvaluateAsync<string>("window.value"), "foo");
        // HAR contains a POST for the css file that should not be used.
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "fallback:continue should continue when not found in har")]
    public async Task FallbackContinueShouldContinueWhenNotFoundInHar()
    {
        var path = TestUtils.GetAsset("har-fulfill.har");

        await Context.RouteFromHARAsync(path, new() { NotFound = HarNotFound.Fallback });
        var page = await Context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/one-style.html");
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "by default should abort requests not found in har")]
    public async Task ByDefaultshouldAbortRequestsNotFoundInHar()
    {
        var path = TestUtils.GetAsset("har-fulfill.har");

        await Context.RouteFromHARAsync(path);
        var page = await Context.NewPageAsync();

        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(()
            => page.GotoAsync(Server.EmptyPage));
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "fallback:continue should continue requests on bad har")]
    public async Task FallbackContinueShouldContinueRequestsOnBadHar()
    {
        using var tmpDir = new TempDirectory();
        string path = Path.Combine(tmpDir.Path, "har.har");
        File.WriteAllText(path, "{ \"log\": { } }");

        await Context.RouteFromHARAsync(path, new() { NotFound = HarNotFound.Fallback });
        var page = await Context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/one-style.html");
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should only handle requests matching url filter")]
    public async Task ShouldOnlyHandleRequestsMatchingUrlFilter()
    {
        var path = TestUtils.GetAsset("har-fulfill.har");

        await Context.RouteFromHARAsync(path, new() { NotFound = HarNotFound.Fallback, UrlString = "**/*.js" });
        var page = await Context.NewPageAsync();
        await Context.RouteAsync("http://no.playwright/", async route =>
        {
            Assert.AreEqual(route.Request.Url, "http://no.playwright/");
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/html",
                Body = "<script src=\"./script.js\"></script><div>hello</div>"
            });
        });

        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.AreEqual(await page.EvaluateAsync<string>("window.value"), "foo");
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should only context.routeFromHAR requests matching url filter")]
    public async Task ShouldOnlyContextRouteFromHarRequestsMatchingUrlFilter()
    {
        var path = TestUtils.GetAsset("har-fulfill.har");

        await Context.RouteFromHARAsync(path, new() { UrlString = "**/*.js" });
        var page = await Context.NewPageAsync();
        await Context.RouteAsync("http://no.playwright/", async route =>
        {
            Assert.AreEqual(route.Request.Url, "http://no.playwright/");
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/html",
                Body = "<script src=\"./script.js\"></script><div>hello</div>"
            });
        });

        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.AreEqual(await page.EvaluateAsync<string>("window.value"), "foo");
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should only page.routeFromHAR requests matching url filter")]
    public async Task ShouldOnlyPageRouteFromHarRequestsMatchingUrlFilter()
    {
        var path = TestUtils.GetAsset("har-fulfill.har");

        var page = await Context.NewPageAsync();
        await page.RouteFromHARAsync(path, new() { UrlString = "**/*.js" });
        await Context.RouteAsync("http://no.playwright/", async route =>
        {
            Assert.AreEqual(route.Request.Url, "http://no.playwright/");
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/html",
                Body = "<script src=\"./script.js\"></script><div>hello</div>"
            });
        });

        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.AreEqual(await page.EvaluateAsync<string>("window.value"), "foo");
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should support regex filter")]
    public async Task ShouldSupportRegexFilter()
    {
        var path = TestUtils.GetAsset("har-fulfill.har");

        await Context.RouteFromHARAsync(path, new() { UrlRegex = new Regex(@".*(\.js|.*\.css|no.playwright\/)$") });
        var page = await Context.NewPageAsync();
        await page.GotoAsync("http://no.playwright/");
        Assert.AreEqual(await page.EvaluateAsync<string>("window.value"), "foo");
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "newPage should fulfill from har, matching the method and following redirects")]
    public async Task NewPageShouldFulfillFromHarMatchingTheMethodAndFollowingRedirects()
    {
        var path = TestUtils.GetAsset("har-fulfill.har");
        var page = await Browser.NewPageAsync();
        await page.RouteFromHARAsync(path);
        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.AreEqual(await page.EvaluateAsync<string>("window.value"), "foo");
        // HAR contains a POST for the css file that should not be used.
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
        await page.CloseAsync();
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should change document URL after redirected navigation")]
    public async Task ShouldChangeDocumentUrlAfterRedirectedNavigation()
    {
        var path = TestUtils.GetAsset("har-redirect.har");
        await Context.RouteFromHARAsync(path);
        var page = await Context.NewPageAsync();
        var waitForUrl = page.WaitForURLAsync("https://www.theverge.com/");
        var (response, _) = await TaskUtils.WhenAll(
            page.WaitForNavigationAsync(),
            page.GotoAsync("https://www.theverge.com/"));
        await waitForUrl;
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        Assert.AreEqual(response.Request.Url, "https://www.theverge.com/");
        Assert.AreEqual(await page.EvaluateAsync<string>("location.href"), "https://www.theverge.com/");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should change document URL after redirected navigation on click")]
    public async Task ShouldChangeDocumentUrlAfterRedirectedNavigationOnClick()
    {
        var path = TestUtils.GetAsset("har-redirect.har");
        await Context.RouteFromHARAsync(path, new() { UrlRegex = new Regex(".*theverge.*") });
        var page = await Context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync("<a href=\"https://www.theverge.com/\">click me</a>");
        var responseTask = page.WaitForNavigationAsync();
        await page.ClickAsync("text=click me");
        var response = await responseTask;
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        Assert.AreEqual(response.Request.Url, "https://www.theverge.com/");
        Assert.AreEqual(await page.EvaluateAsync<string>("location.href"), "https://www.theverge.com/");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should goBack to redirected navigation")]
    public async Task ShouldGoBackToRedirectedNavigation()
    {
        var path = TestUtils.GetAsset("har-redirect.har");
        await Context.RouteFromHARAsync(path, new() { UrlRegex = new Regex(".*theverge.*") });
        var page = await Context.NewPageAsync();
        await page.GotoAsync("https://www.theverge.com/");
        await page.GotoAsync(Server.EmptyPage);
        await Expect(page).ToHaveURLAsync(Server.EmptyPage);
        var response = await page.GoBackAsync();
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        Assert.AreEqual(response.Request.Url, "https://www.theverge.com/");
        Assert.AreEqual(await page.EvaluateAsync<string>("location.href"), "https://www.theverge.com/");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should goForward to redirected navigation")]
    // Flaky in firefox
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldGoForwardToRedirectedNavigation()
    {
        var path = TestUtils.GetAsset("har-redirect.har");
        await Context.RouteFromHARAsync(path, new() { UrlRegex = new Regex(".*theverge.*") });
        var page = await Context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await Expect(page).ToHaveURLAsync(Server.EmptyPage);
        await page.GotoAsync("https://www.theverge.com/");
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        await page.GoBackAsync();
        await Expect(page).ToHaveURLAsync(Server.EmptyPage);
        var response = await page.GoForwardAsync();
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        Assert.AreEqual(response.Request.Url, "https://www.theverge.com/");
        Assert.AreEqual(await page.EvaluateAsync<string>("location.href"), "https://www.theverge.com/");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should reload redirected navigation")]
    public async Task ShouldReloadRedirectedNavigation()
    {
        var path = TestUtils.GetAsset("har-redirect.har");
        await Context.RouteFromHARAsync(path, new() { UrlRegex = new Regex(".*theverge.*") });
        var page = await Context.NewPageAsync();
        await page.GotoAsync("https://www.theverge.com/");
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        var response = await page.ReloadAsync();
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        Assert.AreEqual(response.Request.Url, "https://www.theverge.com/");
        Assert.AreEqual(await page.EvaluateAsync<string>("location.href"), "https://www.theverge.com/");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should fulfill from har with content in a file")]
    public async Task ShouldFulfillFromHarWithContentInAFile()
    {
        var path = TestUtils.GetAsset("har-sha1.har");
        await Context.RouteFromHARAsync(path);
        var page = await Context.NewPageAsync();
        await page.GotoAsync("http://no.playwright/");
        Assert.AreEqual(await page.ContentAsync(), "<html><head></head><body>Hello, world</body></html>");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should round-trip har.zip")]
    public async Task ShouldRoundTripHarZip()
    {
        using var tmpDir = new TempDirectory();
        var harPath = Path.Join(tmpDir.Path, "har.zip");
        var context1 = await Browser.NewContextAsync(new() { RecordHarMode = HarMode.Minimal, RecordHarPath = harPath });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        var context2 = await Browser.NewContextAsync();
        await context2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        StringAssert.Contains("hello, world", await page2.ContentAsync());
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }


    [PlaywrightTest("browsercontext-har.spec.ts", "should produce extracted zip")]
    public async Task ShouldProduceExtractedZip()
    {
        using var tmpDir = new TempDirectory();
        var harPath = Path.Join(tmpDir.Path, "har.har");
        var context1 = await Browser.NewContextAsync(new() { RecordHarMode = HarMode.Minimal, RecordHarPath = harPath, RecordHarContent = HarContentPolicy.Attach });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        Assert.True(File.Exists(harPath));
        StringAssert.DoesNotContain("background-color", File.ReadAllText(harPath));

        var context2 = await Browser.NewContextAsync();
        await context2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        StringAssert.Contains("hello, world", await page2.ContentAsync());
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should round-trip extracted har.zip")]
    public async Task ShouldRoundTripExtractedHarZip()
    {
        using var tmpDir = new TempDirectory();
        var harPath = Path.Join(tmpDir.Path, "har.zip");
        var context1 = await Browser.NewContextAsync(new() { RecordHarMode = HarMode.Minimal, RecordHarPath = harPath });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        ZipFile.ExtractToDirectory(harPath, tmpDir.ToString());

        var context2 = await Browser.NewContextAsync();
        await context2.RouteFromHARAsync(Path.Join(tmpDir.ToString(), "har.har"));
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        StringAssert.Contains("hello, world", await page2.ContentAsync());
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should round-trip har with postData")]
    public async Task ShouldRoundTripHarWithPostData()
    {
        Server.SetRoute("/echo", ctx => ctx.Request.Body.CopyToAsync(ctx.Response.Body));

        using var tmpDir = new TempDirectory();
        var harPath = Path.Join(tmpDir.Path, "har.zip");
        var context1 = await Browser.NewContextAsync(new() { RecordHarMode = HarMode.Minimal, RecordHarPath = harPath });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.EmptyPage);

        var fetchFunction = @"async (body) => {
                const response = await fetch('/echo', { method: 'POST', body });
                return await response.text();
            };";

        Assert.AreEqual(await page1.EvaluateAsync<string>(fetchFunction, "1"), "1");
        Assert.AreEqual(await page1.EvaluateAsync<string>(fetchFunction, "2"), "2");
        Assert.AreEqual(await page1.EvaluateAsync<string>(fetchFunction, "3"), "3");
        await context1.CloseAsync();

        var context2 = await Browser.NewContextAsync();
        await context2.RouteFromHARAsync(harPath);
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(await page2.EvaluateAsync<string>(fetchFunction, "1"), "1");
        Assert.AreEqual(await page2.EvaluateAsync<string>(fetchFunction, "2"), "2");
        Assert.AreEqual(await page2.EvaluateAsync<string>(fetchFunction, "3"), "3");
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page2.EvaluateAsync<string>(fetchFunction, "4"));
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should disambiguate by header")]
    public async Task ShouldDisambiguateByHeader()
    {
        Server.SetRoute("/echo", async ctx =>
        {
            await ctx.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(ctx.Request.Headers["baz"][0]));
            await ctx.Response.CompleteAsync();
        });

        using var tmpDir = new TempDirectory();
        var harPath = Path.Join(tmpDir.Path, "har.zip");
        var context1 = await Browser.NewContextAsync(new() { RecordHarMode = HarMode.Minimal, RecordHarPath = harPath });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.EmptyPage);

        var fetchFunction = @"async (bazValue) => {
                const response = await fetch('/echo', {
                method: 'POST',
                body: '',
                headers: {
                    foo: 'foo-value',
                    bar: 'bar-value',
                    baz: bazValue,
                }
                });
                return await response.text();
            }";

        Assert.AreEqual(await page1.EvaluateAsync<string>(fetchFunction, "baz1"), "baz1");
        Assert.AreEqual(await page1.EvaluateAsync<string>(fetchFunction, "baz2"), "baz2");
        Assert.AreEqual(await page1.EvaluateAsync<string>(fetchFunction, "baz3"), "baz3");
        await context1.CloseAsync();

        var context2 = await Browser.NewContextAsync();
        await context2.RouteFromHARAsync(harPath);
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(await page2.EvaluateAsync<string>(fetchFunction, "baz1"), "baz1");
        Assert.AreEqual(await page2.EvaluateAsync<string>(fetchFunction, "baz2"), "baz2");
        Assert.AreEqual(await page2.EvaluateAsync<string>(fetchFunction, "baz3"), "baz3");
        Assert.AreEqual(await page2.EvaluateAsync<string>(fetchFunction, "baz4"), "baz1");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should update har.zip for context")]
    public async Task ShouldUpdateHarZipForContent()
    {
        using var tmpDir = new TempDirectory();
        var harPath = Path.Join(tmpDir.Path, "har.zip");
        var context1 = await Browser.NewContextAsync();
        await context1.RouteFromHARAsync(harPath, new() { Update = true });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        var context2 = await Browser.NewContextAsync();
        await context2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        StringAssert.Contains("hello, world", await page2.ContentAsync());
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should update har.zip for page")]
    public async Task ShouldUpdateHarZipForPage()
    {
        using var tmpDir = new TempDirectory();
        var harPath = Path.Join(tmpDir.Path, "har.zip");
        var context1 = await Browser.NewContextAsync();
        var page1 = await context1.NewPageAsync();
        await page1.RouteFromHARAsync(harPath, new() { Update = true });
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        var context2 = await Browser.NewContextAsync();
        await context2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        StringAssert.Contains("hello, world", await page2.ContentAsync());
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [PlaywrightTest("browsercontext-har.spec.ts", "should update extracted har.zip for page")]
    public async Task ShouldUpdateExtractedHarZipForPage()
    {
        using var tmpDir = new TempDirectory();
        var harPath = Path.Join(tmpDir.Path, "har.har");
        var context1 = await Browser.NewContextAsync();
        var page1 = await context1.NewPageAsync();
        await page1.RouteFromHARAsync(harPath, new() { Update = true });
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        var context2 = await Browser.NewContextAsync();
        await context2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        StringAssert.Contains("hello, world", await page2.ContentAsync());
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }
}
