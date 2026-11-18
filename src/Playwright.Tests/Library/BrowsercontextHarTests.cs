/**
 * Copyright Microsoft Corporation. All rights reserved.
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

using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-har.spec.ts.</summary>
public class BrowsercontextHarTests : UpstreamBrowserTest
{
    [UpstreamTest("HA-068 should context.routeFromHAR, matching the method and following redirects")]
    public async Task ShouldContextRouteFromHARMatchingTheMethodAndFollowingRedirects()
    {
        var context = await ContextFixture();
        var path = Asset("har-fulfill.har");
        await context.RouteFromHARAsync(path);
        var page = await context.NewPageAsync();
        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.That(await page.EvaluateAsync<string>("window.value"), Is.EqualTo("foo"));
        // HAR contains a POST for the css file that should not be used.
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
    }

    [UpstreamTest("HA-069 should page.routeFromHAR, matching the method and following redirects")]
    public async Task ShouldPageRouteFromHARMatchingTheMethodAndFollowingRedirects()
    {
        var context = await ContextFixture();
        var path = Asset("har-fulfill.har");
        var page = await context.NewPageAsync();
        await page.RouteFromHARAsync(path);
        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.That(await page.EvaluateAsync<string>("window.value"), Is.EqualTo("foo"));
        // HAR contains a POST for the css file that should not be used.
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
    }

    [UpstreamTest("HA-070 fallback:continue should continue when not found in har")]
    public async Task FallbackContinueShouldContinueWhenNotFoundInHar()
    {
        var context = await ContextFixture();
        var path = Asset("har-fulfill.har");
        await context.RouteFromHARAsync(path, new() { NotFound = HarNotFound.Fallback });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/one-style.html");
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [UpstreamTest("HA-071 by default should abort requests not found in har")]
    public async Task ByDefaultShouldAbortRequestsNotFoundInHar()
    {
        var context = await ContextFixture();
        var path = Asset("har-fulfill.har");
        await context.RouteFromHARAsync(path);
        var page = await context.NewPageAsync();
        var error = await Catch(() => page.GotoAsync(Server.EmptyPage));
        Assert.That(error is Exception, Is.EqualTo(true));
    }

    [UpstreamTest("HA-072 fallback:continue should continue requests on bad har")]
    public async Task FallbackContinueShouldContinueRequestsOnBadHar()
    {
        var context = await ContextFixture();
        var path = Info.OutputPath("test.har");
        File.WriteAllText(path, JsonStringify(new { log = new { } }));
        await context.RouteFromHARAsync(path, new() { NotFound = HarNotFound.Fallback });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/one-style.html");
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [UpstreamTest("HA-073 should only handle requests matching url filter")]
    public async Task ShouldOnlyHandleRequestsMatchingUrlFilter()
    {
        var context = await ContextFixture();
        var path = Asset("har-fulfill.har");
        await context.RouteFromHARAsync(path, new() { NotFound = HarNotFound.Fallback, Url = "**/*.js" });
        var page = await context.NewPageAsync();
        await context.RouteAsync("http://no.playwright/", async route =>
        {
            Assert.That(route.Request.Url, Is.EqualTo("http://no.playwright/"));
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/html",
                Body = "<script src=\"./script.js\"></script><div>hello</div>",
            });
        });
        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.That(await page.EvaluateAsync<string>("window.value"), Is.EqualTo("foo"));
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    [UpstreamTest("HA-074 should only context.routeFromHAR requests matching url filter")]
    public async Task ShouldOnlyContextRouteFromHARRequestsMatchingUrlFilter()
    {
        var context = await ContextFixture();
        var path = Asset("har-fulfill.har");
        await context.RouteFromHARAsync(path, new() { Url = "**/*.js" });
        var page = await context.NewPageAsync();
        await context.RouteAsync("http://no.playwright/", async route =>
        {
            Assert.That(route.Request.Url, Is.EqualTo("http://no.playwright/"));
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/html",
                Body = "<script src=\"./script.js\"></script><div>hello</div>",
            });
        });
        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.That(await page.EvaluateAsync<string>("window.value"), Is.EqualTo("foo"));
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    [UpstreamTest("HA-075 should only page.routeFromHAR requests matching url filter")]
    public async Task ShouldOnlyPageRouteFromHARRequestsMatchingUrlFilter()
    {
        var context = await ContextFixture();
        var path = Asset("har-fulfill.har");
        var page = await context.NewPageAsync();
        await page.RouteFromHARAsync(path, new() { Url = "**/*.js" });
        await context.RouteAsync("http://no.playwright/", async route =>
        {
            Assert.That(route.Request.Url, Is.EqualTo("http://no.playwright/"));
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/html",
                Body = "<script src=\"./script.js\"></script><div>hello</div>",
            });
        });
        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.That(await page.EvaluateAsync<string>("window.value"), Is.EqualTo("foo"));
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    [UpstreamTest("HA-076 should apply overrides before routing from har")]
    public async Task ShouldApplyOverridesBeforeRoutingFromHar()
    {
        var context = await ContextFixture();
        var path = Asset("har-fulfill.har");
        await context.RouteFromHARAsync(path, new() { Url = "**/*.js" });
        var page = await context.NewPageAsync();
        await context.RouteAsync("http://no.playwright/my-script.js", async route =>
        {
            await route.FallbackAsync(new()
            {
                Url = "http://no.playwright/script2.js",
            });
        });
        await context.RouteAsync("http://test.example/", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/html",
                Body = "<script src=\"http://no.playwright/my-script.js\"></script><div>hello</div>",
            });
        });
        await page.GotoAsync("http://test.example/");
        // HAR contains script2.js that sets the value.
        Assert.That(await page.EvaluateAsync<string>("window.value"), Is.EqualTo("foo"));
    }

    [UpstreamTest("HA-077 should support regex filter")]
    public async Task ShouldSupportRegexFilter()
    {
        var context = await ContextFixture();
        var path = Asset("har-fulfill.har");
        await context.RouteFromHARAsync(path, new() { UrlRegex = new Regex(@".*(\.js|.*\.css|no.playwright\/)$") });
        var page = await context.NewPageAsync();
        await page.GotoAsync("http://no.playwright/");
        Assert.That(await page.EvaluateAsync<string>("window.value"), Is.EqualTo("foo"));
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
    }

    [UpstreamTest("HA-078 newPage should fulfill from har, matching the method and following redirects")]
    public async Task NewPageShouldFulfillFromHarMatchingTheMethodAndFollowingRedirects()
    {
        var path = Asset("har-fulfill.har");
        var page = await Browser.NewPageAsync(NewPageOptions());
        await page.RouteFromHARAsync(path);
        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.That(await page.EvaluateAsync<string>("window.value"), Is.EqualTo("foo"));
        // HAR contains a POST for the css file that should not be used.
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
        await page.CloseAsync();
    }

    [UpstreamTest("HA-079 should change document URL after redirected navigation")]
    public async Task ShouldChangeDocumentURLAfterRedirectedNavigation()
    {
        var context = await ContextFixture();
        var path = Asset("har-redirect.har");
        await context.RouteFromHARAsync(path);
        var page = await context.NewPageAsync();
        var response = await PromiseAll(
            page.WaitForNavigationAsync(),
            page.WaitForURLAsync("https://www.theverge.com/"),
            page.GotoAsync("https://theverge.com/"));
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        Assert.That(response.Request.Url, Is.EqualTo("https://www.theverge.com/"));
        Assert.That(await page.EvaluateAsync<string>("() => location.href"), Is.EqualTo("https://www.theverge.com/"));
    }

    [UpstreamTest("HA-080 should change document URL after redirected navigation on click")]
    public async Task ShouldChangeDocumentURLAfterRedirectedNavigationOnClick()
    {
        var context = await ContextFixture();
        var path = Asset("har-redirect.har");
        await context.RouteFromHARAsync(path, new() { UrlRegex = new Regex(".*theverge.*") });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync("<a href=\"https://theverge.com/\">click me</a>");
        var response = await PromiseAll(
            page.WaitForNavigationAsync(),
            page.ClickAsync("text=click me"));
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        Assert.That(response.Request.Url, Is.EqualTo("https://www.theverge.com/"));
        Assert.That(await page.EvaluateAsync<string>("() => location.href"), Is.EqualTo("https://www.theverge.com/"));
    }

    [UpstreamTest("HA-081 should goBack to redirected navigation")]
    public async Task ShouldGoBackToRedirectedNavigation()
    {
        var context = await ContextFixture();
        var path = Asset("har-redirect.har");
        await context.RouteFromHARAsync(path, new() { UrlRegex = new Regex(".*theverge.*") });
        var page = await context.NewPageAsync();
        await page.GotoAsync("https://theverge.com/");
        await page.GotoAsync(Server.EmptyPage);
        await Expect(page).ToHaveURLAsync(Server.EmptyPage);
        var response = await page.GoBackAsync();
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        Assert.That(response.Request.Url, Is.EqualTo("https://www.theverge.com/"));
        Assert.That(await page.EvaluateAsync<string>("() => location.href"), Is.EqualTo("https://www.theverge.com/"));
    }

    [UpstreamTest("HA-082 should goForward to redirected navigation")]
    public async Task ShouldGoForwardToRedirectedNavigation()
    {
        var context = await ContextFixture();
        var path = Asset("har-redirect.har");
        await context.RouteFromHARAsync(path, new() { UrlRegex = new Regex(".*theverge.*") });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await Expect(page).ToHaveURLAsync(Server.EmptyPage);
        await page.GotoAsync("https://theverge.com/");
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        await page.GoBackAsync();
        await Expect(page).ToHaveURLAsync(Server.EmptyPage);
        var response = await page.GoForwardAsync();
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        Assert.That(response.Request.Url, Is.EqualTo("https://www.theverge.com/"));
        Assert.That(await page.EvaluateAsync<string>("() => location.href"), Is.EqualTo("https://www.theverge.com/"));
    }

    [UpstreamTest("HA-083 should reload redirected navigation")]
    public async Task ShouldReloadRedirectedNavigation()
    {
        var context = await ContextFixture();
        var path = Asset("har-redirect.har");
        await context.RouteFromHARAsync(path, new() { UrlRegex = new Regex(".*theverge.*") });
        var page = await context.NewPageAsync();
        await page.GotoAsync("https://theverge.com/");
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        var response = await page.ReloadAsync();
        await Expect(page).ToHaveURLAsync("https://www.theverge.com/");
        Assert.That(response.Request.Url, Is.EqualTo("https://www.theverge.com/"));
        Assert.That(await page.EvaluateAsync<string>("() => location.href"), Is.EqualTo("https://www.theverge.com/"));
    }

    [UpstreamTest("HA-084 should fulfill from har with content in a file")]
    public async Task ShouldFulfillFromHarWithContentInAFile()
    {
        var context = await ContextFixture();
        var path = Asset("har-sha1.har");
        await context.RouteFromHARAsync(path);
        var page = await context.NewPageAsync();
        await page.GotoAsync("http://no.playwright/");
        Assert.That(await page.ContentAsync(), Is.EqualTo("<html><head></head><body>Hello, world</body></html>"));
    }

    [UpstreamTest("HA-085 should round-trip har.zip")]
    public async Task ShouldRoundTripHarZip()
    {
        var harPath = Info.OutputPath("har.ZIP");
        var context1 = await ContextFactory(new() { RecordHarMode = HarMode.Minimal, RecordHarPath = harPath });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        var harDir = Info.OutputPath("har");
        // .NET: extractZip() is ZipFile.ExtractToDirectory().
        ZipFile.ExtractToDirectory(harPath, harDir);
        var log = JsonDocument.Parse(File.ReadAllText(Path.Combine(harDir, "har.har"))).RootElement.GetProperty("log");
        ExpectToEqual(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").GetProperty("_file"), AnyString);

        var context2 = await ContextFactory();
        await context2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.That(await page2.ContentAsync(), Does.Contain("hello, world!"));
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");

        // .NET: yazl.ZipFile is a ZipArchive that writes to a MemoryStream, which is the chunks of its output.
        var chunks = new MemoryStream();
        using (var zipFile = new ZipArchive(chunks, ZipArchiveMode.Create, true))
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(harDir).Select(Path.GetFileName))
            {
                zipFile.CreateEntryFromFile(Path.Combine(harDir, entry), entry == "har.har" ? "Recording.HAR" : entry);
            }
        }
        var uppercaseHarPath = Info.OutputPath("uppercase.har.zip");
        await File.WriteAllBytesAsync(uppercaseHarPath, chunks.ToArray());

        await context2.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait });
        await context2.RouteFromHARAsync(uppercaseHarPath, new() { NotFound = HarNotFound.Abort });
        await page2.ReloadAsync();
        Assert.That(await page2.ContentAsync(), Does.Contain("hello, world!"));
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [UpstreamTest("HA-114 should reject a malformed har.zip")]
    public async Task ShouldRejectAMalformedHarZip()
    {
        // .NET: yazl.ZipFile is a ZipArchive that writes to a MemoryStream, which is the chunks of its output.
        var chunks = new MemoryStream();
        using (var zipFile = new ZipArchive(chunks, ZipArchiveMode.Create, true))
        {
            using var stream = zipFile.CreateEntry("aa/x.har").Open();
            stream.Write(Encoding.UTF8.GetBytes("{}"));
        }
        // yauzl reports '..' in an entry name as an 'error' event while reading entries.
        var zip = Encoding.Latin1.GetBytes(Encoding.Latin1.GetString(chunks.ToArray()).Replace("aa/x.har", "../x.har"));
        var harPath = Info.OutputPath("malformed.har.zip");
        await File.WriteAllBytesAsync(harPath, zip);

        var context = await ContextFactory();
        var error = await Catch(() => context.RouteFromHARAsync(harPath));
        Assert.That(error.Message, Does.Contain("invalid relative path: ../x.har"));
    }

    [UpstreamTest("HA-086 should produce extracted zip")]
    public async Task ShouldProduceExtractedZip()
    {
        var harPath = Info.OutputPath("har.har");
        var context1 = await ContextFactory(new() { RecordHarMode = HarMode.Minimal, RecordHarPath = harPath, RecordHarContent = HarContentPolicy.Attach });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        Assert.That(File.Exists(harPath), Is.True);
        var har = File.ReadAllText(harPath);
        Assert.That(har, Does.Not.Contain("background-color"));

        var context2 = await ContextFactory();
        await context2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.That(await page2.ContentAsync(), Does.Contain("hello, world!"));
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [UpstreamTest("HA-087 should round-trip extracted har.zip")]
    public async Task ShouldRoundTripExtractedHarZip()
    {
        var harPath = Info.OutputPath("har.zip");
        var context1 = await ContextFactory(new() { RecordHarMode = HarMode.Minimal, RecordHarPath = harPath });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        var harDir = Info.OutputPath("hardir");
        // .NET: extractZip() is ZipFile.ExtractToDirectory().
        ZipFile.ExtractToDirectory(harPath, harDir);

        var context2 = await ContextFactory();
        await context2.RouteFromHARAsync(Path.Combine(harDir, "har.har"));
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.That(await page2.ContentAsync(), Does.Contain("hello, world!"));
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [UpstreamTest("HA-088 should round-trip har with postData")]
    public async Task ShouldRoundTripHarWithPostData()
    {
        Server.SetRoute("/echo", async (req, res) =>
        {
            var body = await req.PostBody;
            res.End(Encoding.UTF8.GetString(body));
        });

        var harPath = Info.OutputPath("har.zip");
        var context1 = await ContextFactory(new() { RecordHarMode = HarMode.Minimal, RecordHarPath = harPath });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.EmptyPage);
        const string fetchFunction = @"async (body) => {
    const response = await fetch('/echo', { method: 'POST', body });
    return await response.text();
  }";

        Assert.That(await page1.EvaluateAsync<string>(fetchFunction, "1"), Is.EqualTo("1"));
        Assert.That(await page1.EvaluateAsync<string>(fetchFunction, "2"), Is.EqualTo("2"));
        Assert.That(await page1.EvaluateAsync<string>(fetchFunction, "3"), Is.EqualTo("3"));
        await context1.CloseAsync();

        Server.Reset();
        var context2 = await ContextFactory();
        await context2.RouteFromHARAsync(harPath);
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        Assert.That(await page2.EvaluateAsync<string>(fetchFunction, "1"), Is.EqualTo("1"));
        Assert.That(await page2.EvaluateAsync<string>(fetchFunction, "2"), Is.EqualTo("2"));
        Assert.That(await page2.EvaluateAsync<string>(fetchFunction, "3"), Is.EqualTo("3"));
        Assert.That(await Catch(() => page2.EvaluateAsync<string>(fetchFunction, "4")), Is.Not.Null);
    }

    [UpstreamTest("HA-089 should record overridden requests to har")]
    public async Task ShouldRecordOverriddenRequestsToHar()
    {
        Server.SetRoute("/echo", async (req, res) =>
        {
            var body = await req.PostBody;
            res.End(Encoding.UTF8.GetString(body));
        });

        var harPath = Info.OutputPath("har.zip");
        var context1 = await ContextFactory(new() { RecordHarMode = HarMode.Minimal, RecordHarPath = harPath });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.EmptyPage);
        const string fetchFunction = @"async (arg) => {
    const response = await fetch(arg.path, { method: 'POST', body: arg.body });
    return await response.text();
  }";
        await page1.RouteAsync("**/echo_redir", async route =>
        {
            await route.FallbackAsync(new()
            {
                Url = Server.Prefix + "/echo",
                // .NET: PostData takes the bytes of the body; the Node.js client writes the number as JSON.
                PostData = Encoding.UTF8.GetBytes((int.Parse(route.Request.PostData, CultureInfo.InvariantCulture) + 10).ToString(CultureInfo.InvariantCulture)),
            });
        });
        Assert.That(await page1.EvaluateAsync<string>(fetchFunction, new { path = "/echo_redir", body = "1" }), Is.EqualTo("11"));
        Assert.That(await page1.EvaluateAsync<string>(fetchFunction, new { path = "/echo_redir", body = "2" }), Is.EqualTo("12"));
        await context1.CloseAsync();

        Server.Reset();
        var context2 = await ContextFactory();
        await context2.RouteFromHARAsync(harPath);
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        Assert.That(await page2.EvaluateAsync<string>(fetchFunction, new { path = "/echo", body = "11" }), Is.EqualTo("11"));
        Assert.That(await page2.EvaluateAsync<string>(fetchFunction, new { path = "/echo", body = "12" }), Is.EqualTo("12"));
    }

    [UpstreamTest("HA-090 should disambiguate by header")]
    public async Task ShouldDisambiguateByHeader()
    {
        Server.SetRoute("/echo", (req, res) =>
        {
            res.End(req.Headers["baz"]);
        });

        var harPath = Info.OutputPath("har.zip");
        var context1 = await ContextFactory(new() { RecordHarMode = HarMode.Minimal, RecordHarPath = harPath });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.EmptyPage);

        const string fetchFunction = @"async (bazValue) => {
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

        Assert.That(await page1.EvaluateAsync<string>(fetchFunction, "baz1"), Is.EqualTo("baz1"));
        Assert.That(await page1.EvaluateAsync<string>(fetchFunction, "baz2"), Is.EqualTo("baz2"));
        Assert.That(await page1.EvaluateAsync<string>(fetchFunction, "baz3"), Is.EqualTo("baz3"));
        await context1.CloseAsync();

        var context2 = await ContextFactory();
        await context2.RouteFromHARAsync(harPath);
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        Assert.That(await page2.EvaluateAsync<string>(fetchFunction, "baz1"), Is.EqualTo("baz1"));
        Assert.That(await page2.EvaluateAsync<string>(fetchFunction, "baz2"), Is.EqualTo("baz2"));
        Assert.That(await page2.EvaluateAsync<string>(fetchFunction, "baz3"), Is.EqualTo("baz3"));
        Assert.That(await page2.EvaluateAsync<string>(fetchFunction, "baz4"), Is.EqualTo("baz1"));
    }

    [UpstreamTest("HA-091 should update har.zip for context")]
    public async Task ShouldUpdateHarZipForContext()
    {
        var harPath = Info.OutputPath("har.zip");
        var context1 = await ContextFactory();
        await context1.RouteFromHARAsync(harPath, new() { Update = true });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        var context2 = await ContextFactory();
        await context2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.That(await page2.ContentAsync(), Does.Contain("hello, world!"));
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [UpstreamTest("HA-092 should ignore boundary when matching multipart/form-data body")]
    public async Task ShouldIgnoreBoundaryWhenMatchingMultipartFormDataBody()
    {
        // .NET: the parameter of the handler is `req` upstream, which is a variable of the test here.
        Server.SetRoute("/empty.html", (req1, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End(@"
      <form id=""form"" action=""form.html"" enctype=""multipart/form-data"" method=""POST"">
      <input id=""file"" type=""file"" multiple />
      <button type=""submit"">Upload</button>
      </form>");
        });
        // .NET: the parameter of the handler is `req` upstream, which is a variable of the test here.
        Server.SetRoute("/form.html", (req1, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<div>done</div>");
        });

        var harPath = Info.OutputPath("har.zip");
        var context1 = await ContextFactory();
        await context1.RouteFromHARAsync(harPath, new() { Update = true });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.Prefix + "/empty.html");
        var reqPromise = Server.WaitForRequest("/form.html");
        await page1.Locator("button").ClickAsync();
        await Expect(page1.Locator("div")).ToHaveTextAsync("done");
        var req = await reqPromise;
        Assert.That(Encoding.UTF8.GetString(await req.PostBody), Does.Contain("---"));
        await context1.CloseAsync();

        var context2 = await ContextFactory();
        await context2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/empty.html");
        var requestPromise = page2.WaitForRequestAsync(new Regex(".*form.html"));
        await page2.Locator("button").ClickAsync();
        var request = await requestPromise;
        Assert.That(await request.ResponseAsync(), Is.Not.Null);
        Assert.That(request.Failure, Is.Null);
        await Expect(page2.Locator("div")).ToHaveTextAsync("done");
    }

    [UpstreamTest("HA-093 should record single set-cookie headers")]
    public async Task ShouldRecordSingleSetCookieHeaders()
    {
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.SetHeader("set-cookie", new[] { "first=foo" });
            res.End();
        });

        var harPath = Info.OutputPath("har.zip");
        var context1 = await ContextFactory();
        await context1.RouteFromHARAsync(harPath, new() { Update = true });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.EmptyPage);
        var cookie1 = await page1.EvaluateAsync<string>("() => document.cookie");
        Assert.That(cookie1, Is.EqualTo("first=foo"));
        await context1.CloseAsync();

        var context2 = await ContextFactory();
        await context2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        var cookie2 = await page2.EvaluateAsync<string>("() => document.cookie");
        Assert.That(cookie2, Is.EqualTo("first=foo"));
    }

    [UpstreamTest("HA-094 should record multiple set-cookie headers")]
    public async Task ShouldRecordMultipleSetCookieHeaders()
    {
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.SetHeader("set-cookie", new[] { "first=foo", "second=bar" });
            res.End();
        });

        var harPath = Info.OutputPath("har.zip");
        var context1 = await ContextFactory();
        await context1.RouteFromHARAsync(harPath, new() { Update = true });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.EmptyPage);
        var cookie1 = await page1.EvaluateAsync<string>("() => document.cookie");
        Assert.That(string.Join("; ", cookie1.Split("; ").Order(StringComparer.Ordinal)), Is.EqualTo("first=foo; second=bar"));
        await context1.CloseAsync();

        var context2 = await ContextFactory();
        await context2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        var cookie2 = await page2.EvaluateAsync<string>("() => document.cookie");
        Assert.That(string.Join("; ", cookie2.Split("; ").Order(StringComparer.Ordinal)), Is.EqualTo("first=foo; second=bar"));
    }

    [UpstreamTest("HA-095 should update har.zip for page")]
    public async Task ShouldUpdateHarZipForPage()
    {
        var harPath = Info.OutputPath("har.zip");
        var context1 = await ContextFactory();
        var page1 = await context1.NewPageAsync();
        await page1.RouteFromHARAsync(harPath, new() { Update = true });
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        var context2 = await ContextFactory();
        var page2 = await context2.NewPageAsync();
        await page2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.That(await page2.ContentAsync(), Does.Contain("hello, world!"));
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [UpstreamTest("HA-096 should update har.zip for page with different options")]
    public async Task ShouldUpdateHarZipForPageWithDifferentOptions()
    {
        var harPath = Info.OutputPath("har.zip");
        var context1 = await ContextFactory();
        var page1 = await context1.NewPageAsync();
        await page1.RouteFromHARAsync(harPath, new() { Update = true, UpdateContent = RouteFromHarUpdateContentPolicy.Embed, UpdateMode = HarMode.Full });
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        var context2 = await ContextFactory();
        var page2 = await context2.NewPageAsync();
        await page2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.That(await page2.ContentAsync(), Does.Contain("hello, world!"));
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [UpstreamTest("HA-097 should update extracted har.zip for page")]
    public async Task ShouldUpdateExtractedHarZipForPage()
    {
        var harPath = Info.OutputPath("har.har");
        var context1 = await ContextFactory();
        var page1 = await context1.NewPageAsync();
        await page1.RouteFromHARAsync(harPath, new() { Update = true });
        await page1.GotoAsync(Server.Prefix + "/one-style.html");
        await context1.CloseAsync();

        var context2 = await ContextFactory();
        var page2 = await context2.NewPageAsync();
        await page2.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        await page2.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.That(await page2.ContentAsync(), Does.Contain("hello, world!"));
        await Expect(page2.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
    }

    [UpstreamTest("HA-098 page.unrouteAll should stop page.routeFromHAR")]
    public async Task PageUnrouteAllShouldStopPageRouteFromHAR()
    {
        var harPath = Asset("har-fulfill.har");
        var context1 = await ContextFactory();
        var page1 = await context1.NewPageAsync();
        // The har file contains requests for another domain, so the router
        // is expected to abort all requests.
        await page1.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        await ThrowsAsync<PlaywrightException>(() => page1.GotoAsync(Server.EmptyPage));
        await page1.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait });
        var response = await page1.GotoAsync(Server.EmptyPage);
        Assert.That(response.Ok, Is.True);
    }

    [UpstreamTest("HA-099 context.unrouteAll should stop context.routeFromHAR")]
    public async Task ContextUnrouteAllShouldStopContextRouteFromHAR()
    {
        var harPath = Asset("har-fulfill.har");
        var context1 = await ContextFactory();
        var page1 = await context1.NewPageAsync();
        // The har file contains requests for another domain, so the router
        // is expected to abort all requests.
        await context1.RouteFromHARAsync(harPath, new() { NotFound = HarNotFound.Abort });
        await ThrowsAsync<PlaywrightException>(() => page1.GotoAsync(Server.EmptyPage));
        await context1.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait });
        var response = await page1.GotoAsync(Server.EmptyPage);
        Assert.That(response.Ok, Is.True);
    }

    [UpstreamTest("HA-100 should ignore aborted requests")]
    public async Task ShouldIgnoreAbortedRequests()
    {
        var path = Info.OutputPath("test.har");
        {
            // .NET: the parameter of the handler is `req` upstream, which is a variable of the block here.
            Server.SetRoute("/x", (req1, res) => { req1.Destroy(); });
            var context1 = await ContextFactory();
            await context1.RouteFromHARAsync(path, new() { Update = true });
            var page1 = await context1.NewPageAsync();
            await page1.GotoAsync(Server.EmptyPage);
            var reqPromise = Server.WaitForRequest("/x");
            var evalPromise = page1.EvaluateAsync<string>("url => fetch(url).catch(e => 'cancelled')", Server.Prefix + "/x");
            await reqPromise;
            var req = await evalPromise;
            Assert.That(req, Is.EqualTo("cancelled"));
            await context1.CloseAsync();
        }
        Server.Reset();
        {
            Server.SetRoute("/x", (req, res) =>
            {
                res.WriteHead(200, new() { ["Content-Type"] = "text/plain" });
                res.End("test");
            });
            var context2 = await ContextFactory();
            await context2.RouteFromHARAsync(path);
            var page2 = await context2.NewPageAsync();
            await page2.GotoAsync(Server.EmptyPage);
            var evalPromise = page2.EvaluateAsync<object>("url => fetch(url).catch(e => 'cancelled')", Server.Prefix + "/x");
            // .NET: page2.waitForTimeout(1000).then(() => 'timeout') is a local function that is called in its place.
            async Task<object> Then()
            {
                await page2.WaitForTimeoutAsync(1000);
                return "timeout";
            }
            var result = await await Task.WhenAny(evalPromise, Then());
            Assert.That(result, Is.EqualTo("timeout"));
        }
    }
}
