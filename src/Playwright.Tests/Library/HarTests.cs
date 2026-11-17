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

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/har.spec.ts.</summary>
public class HarTests
{
    private static async Task<PageWithHarResult> PageWithHar(ContextFactory contextFactory, TestInfo testInfo, PageWithHarOptions options = null)
    {
        var harPath = testInfo.OutputPath(options?.OutputPath ?? "test.har");
        var context = await contextFactory(new() { RecordHarPath = harPath, RecordHarContent = options?.Content, RecordHarOmitContent = options?.OmitContent, IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        // .NET: { page, context, getLog, getZip } is the PageWithHarResult of the harness.
        return new PageWithHarResult(context, page, harPath);
    }

    [UpstreamTest("HA-001 should have version and creator")]
    public async Task ShouldHaveVersionAndCreator(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("version").GetString(), Is.EqualTo("1.2"));
        Assert.That(log.GetProperty("creator").GetProperty("name").GetString(), Is.EqualTo("Playwright"));
        // .NET: require('../../package.json') is the package.json of the upstream checkout.
        Assert.That(log.GetProperty("creator").GetProperty("version").GetString(), Does.Contain(Environment.GetEnvironmentVariable("PW_VERSION_OVERRIDE") ?? JsonDocument.Parse(File.ReadAllText(Asset("../../package.json"))).RootElement.GetProperty("version").GetString()));
    }

    [UpstreamTest("HA-002 should have browser")]
    public async Task ShouldHaveBrowser(string browserName, IBrowser browser, ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        var log = await pageWithHar.GetLog();

        Assert.That(log.GetProperty("browser").GetProperty("name").GetString(), Is.EqualTo(browserName));
        Assert.That(log.GetProperty("browser").GetProperty("version").GetString(), Is.EqualTo(browser.Version));
    }

    [UpstreamTest("HA-003 should have pages")]
    public async Task ShouldHavePages(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync("data:text/html,<title>Hello</title>");
        // For data: load comes before domcontentloaded...
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("pages").GetArrayLength(), Is.EqualTo(1));
        var pageEntry = log.GetProperty("pages")[0];
        Assert.That(pageEntry.GetProperty("id").GetString(), Is.Not.Empty);
        Assert.That(pageEntry.GetProperty("title").GetString(), Is.EqualTo("Hello"));
        Assert.That(DateTimeOffset.Parse(pageEntry.GetProperty("startedDateTime").GetString(), CultureInfo.InvariantCulture).ToUnixTimeMilliseconds(), Is.GreaterThan(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - (3600 * 1000)));
        Assert.That(pageEntry.GetProperty("pageTimings").GetProperty("onContentLoad").GetDouble(), Is.GreaterThan(0));
        Assert.That(pageEntry.GetProperty("pageTimings").GetProperty("onLoad").GetDouble(), Is.GreaterThan(0));
    }

    [UpstreamTest("HA-004 should have pages in persistent context")]
    public async Task ShouldHavePagesInPersistentContext(LaunchPersistent launchPersistent, string browserName, TestInfo testInfo)
    {
        var harPath = testInfo.OutputPath("test.har");
        var (context, page) = await launchPersistent(new() { RecordHarPath = harPath });
        await page.GotoAsync("data:text/html,<title>Hello</title>");
        // For data: load comes before domcontentloaded...
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await context.CloseAsync();
        var log = JsonDocument.Parse(File.ReadAllText(harPath)).RootElement.GetProperty("log");
        Assert.That(log.GetProperty("pages").GetArrayLength(), Is.EqualTo(1));
        var pageEntry = log.GetProperty("pages")[0];
        Assert.That(pageEntry.GetProperty("id").GetString(), Is.Not.Empty);
        Assert.That(pageEntry.GetProperty("title").GetString(), Is.EqualTo("Hello"));
    }

    [UpstreamTest("HA-005 should include request")]
    public async Task ShouldIncludeRequest(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries").GetArrayLength(), Is.EqualTo(1));
        var entry = log.GetProperty("entries")[0];
        Assert.That(entry.GetProperty("pageref").GetString(), Is.EqualTo(log.GetProperty("pages")[0].GetProperty("id").GetString()));
        Assert.That(entry.GetProperty("request").GetProperty("url").GetString(), Is.EqualTo(server.EmptyPage));
        Assert.That(entry.GetProperty("request").GetProperty("method").GetString(), Is.EqualTo("GET"));
        Assert.That(entry.GetProperty("request").GetProperty("httpVersion").GetString(), Is.EqualTo("HTTP/1.1"));
        Assert.That(entry.GetProperty("request").GetProperty("headers").GetArrayLength(), Is.GreaterThan(1));
        Assert.That(entry.GetProperty("request").GetProperty("headers").EnumerateArray().FirstOrDefault(h => h.GetProperty("name").GetString().ToLowerInvariant() == "user-agent").ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(entry.GetProperty("request").GetProperty("bodySize").GetInt32(), Is.EqualTo(0));
    }

    [UpstreamTest("HA-007 should include response")]
    public async Task ShouldIncludeResponse(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        var log = await pageWithHar.GetLog();
        var entry = log.GetProperty("entries")[0];
        Assert.That(entry.GetProperty("response").GetProperty("status").GetInt32(), Is.EqualTo(200));
        Assert.That(entry.GetProperty("response").GetProperty("statusText").GetString(), Is.EqualTo("OK"));
        Assert.That(entry.GetProperty("response").GetProperty("httpVersion").GetString(), Is.EqualTo("HTTP/1.1"));
        Assert.That(entry.GetProperty("response").GetProperty("headers").GetArrayLength(), Is.GreaterThan(1));
        Assert.That(entry.GetProperty("response").GetProperty("headers").EnumerateArray().First(h => h.GetProperty("name").GetString().ToLowerInvariant() == "content-type").GetProperty("value").GetString(), Does.Contain("text/html"));
    }

    [UpstreamTest("HA-008 should include redirectURL")]
    public async Task ShouldIncludeRedirectURL(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        server.SetRedirect("/foo.html", "/empty.html");
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/foo.html");
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries").GetArrayLength(), Is.EqualTo(2));
        var entry = log.GetProperty("entries")[0];
        Assert.That(entry.GetProperty("response").GetProperty("status").GetInt32(), Is.EqualTo(302));
        Assert.That(entry.GetProperty("response").GetProperty("redirectURL").GetString(), Is.EqualTo(server.EmptyPage));
    }

    [UpstreamTest("HA-009 should include query params")]
    public async Task ShouldIncludeQueryParams(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/har.html?name=value");
        var log = await pageWithHar.GetLog();
        ExpectToEqual(log.GetProperty("entries")[0].GetProperty("request").GetProperty("queryString"), new[] { new { name = "name", value = "value" } });
    }

    [UpstreamTest("HA-010 should include postData")]
    public async Task ShouldIncludePostData(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync("() => fetch('./post', { method: 'POST', body: 'Hello' })");
        var log = await pageWithHar.GetLog();
        ExpectToEqual(log.GetProperty("entries")[1].GetProperty("request").GetProperty("postData"), new
        {
            mimeType = "text/plain;charset=UTF-8",
            @params = Array.Empty<object>(),
            text = "Hello",
        });
    }

    [UpstreamTest("HA-011 should include binary postData")]
    public async Task ShouldIncludeBinaryPostData(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"async () => {
    await fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(16).keys())) });
  }");
        var log = await pageWithHar.GetLog();
        ExpectToEqual(log.GetProperty("entries")[1].GetProperty("request").GetProperty("postData"), new
        {
            mimeType = "application/octet-stream",
            @params = Array.Empty<object>(),
            text = string.Empty,
        });
    }

    [UpstreamTest("HA-012 should include form params")]
    public async Task ShouldIncludeFormParams(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<form method='POST' action='/post'><input type='text' name='foo' value='bar'><input type='number' name='baz' value='123'><input type='submit'></form>");
        await page.ClickAsync("input[type=submit]");
        var log = await pageWithHar.GetLog();
        ExpectToEqual(log.GetProperty("entries")[1].GetProperty("request").GetProperty("postData"), new
        {
            mimeType = "application/x-www-form-urlencoded",
            @params = new[]
            {
                new { name = "foo", value = "bar" },
                new { name = "baz", value = "123" },
            },
            text = "foo=bar&baz=123",
        });
    }

    [UpstreamTest("HA-013 should include cookies")]
    public async Task ShouldIncludeCookies(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        var context = page.Context;
        await context.AddCookiesAsync(
        [
            new() { Name = "name1", Value = "\"value1\"", Domain = "localhost", Path = "/", HttpOnly = true },
            new() { Name = "name2", Value = "val\"ue2", Domain = "localhost", Path = "/", SameSite = SameSiteAttribute.Lax },
            new() { Name = "name3", Value = "val=ue3", Domain = "localhost", Path = "/" },
            new() { Name = "name4", Value = "val,ue4", Domain = "localhost", Path = "/" },
        ]);
        await page.GotoAsync(server.EmptyPage);
        var log = await pageWithHar.GetLog();
        ExpectToEqual(log.GetProperty("entries")[0].GetProperty("request").GetProperty("cookies"), new[]
        {
            new { name = "name1", value = "\"value1\"" },
            new { name = "name2", value = "val\"ue2" },
            new { name = "name3", value = "val=ue3" },
            new { name = "name4", value = "val,ue4" },
        });
    }

    [UpstreamTest("HA-014 should include set-cookies")]
    public async Task ShouldIncludeSetCookies(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[]
            {
                "name1=value1; HttpOnly",
                "name2=\"value2\"",
                "name3=value4; Path=/; Domain=example.com; Max-Age=1500",
            });
            res.End();
        });
        await page.GotoAsync(server.EmptyPage);
        var log = await pageWithHar.GetLog();
        var cookies = log.GetProperty("entries")[0].GetProperty("response").GetProperty("cookies");
        ExpectToEqual(cookies[0], new { name = "name1", value = "value1", httpOnly = true });
        ExpectToEqual(cookies[1], new { name = "name2", value = "\"value2\"" });
        Assert.That(DateTimeOffset.Parse(cookies[2].GetProperty("expires").GetString(), CultureInfo.InvariantCulture).ToUnixTimeMilliseconds(), Is.GreaterThan(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
    }

    [UpstreamTest("HA-015 should include set-cookies without equals sign as nameless")]
    public async Task ShouldIncludeSetCookiesWithoutEqualsSignAsNameless(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "token; HttpOnly" });
            res.End();
        });
        await page.GotoAsync(server.EmptyPage);
        var log = await pageWithHar.GetLog();
        ExpectToEqual(log.GetProperty("entries")[0].GetProperty("response").GetProperty("cookies"), new[] { new { name = string.Empty, value = "token", httpOnly = true } });
    }

    [UpstreamTest("HA-016 should include set-cookies with lowercase attributes")]
    public async Task ShouldIncludeSetCookiesWithLowercaseAttributes(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "name=value; path=/; httponly; secure; samesite=Lax" });
            res.End();
        });
        await page.GotoAsync(server.EmptyPage);
        var log = await pageWithHar.GetLog();
        var cookies = log.GetProperty("entries")[0].GetProperty("response").GetProperty("cookies");
        ExpectToEqual(cookies[0], new { name = "name", value = "value", path = "/", httpOnly = true, secure = true, sameSite = "Lax" });
    }

    [UpstreamTest("HA-017 should skip invalid Expires")]
    public async Task ShouldSkipInvalidExpires(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[]
            {
                "name=value;Expires=Sat Sep 14 01:02:27 CET 2024",
            });
            res.End();
        });
        await page.GotoAsync(server.EmptyPage);
        var log = await pageWithHar.GetLog();
        var cookies = log.GetProperty("entries")[0].GetProperty("response").GetProperty("cookies");
        ExpectToEqual(cookies[0], new { name = "name", value = "value" });
    }

    [UpstreamTest("HA-018 should include set-cookies with comma")]
    public async Task ShouldIncludeSetCookiesWithComma(ContextFactory contextFactory, Server server, string browserName, TestInfo testInfo)
    {
        Info.Fixme(browserName == "webkit", "We get \"name1=val, ue1, name2=val, ue2\" as a header value");
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[]
            {
                "name1=val, ue1", "name2=val, ue2",
            });
            res.End();
        });
        await page.GotoAsync(server.EmptyPage);
        var log = await pageWithHar.GetLog();
        var cookies = log.GetProperty("entries")[0].GetProperty("response").GetProperty("cookies");
        ExpectToEqual(cookies[0], new { name = "name1", value = "val, ue1" });
        ExpectToEqual(cookies[1], new { name = "name2", value = "val, ue2" });
    }

    [UpstreamTest("HA-019 should include secure set-cookies")]
    public async Task ShouldIncludeSecureSetCookies(ContextFactory contextFactory, Server httpsServer, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        httpsServer.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[]
            {
                "name1=value1; Secure",
            });
            res.End();
        });
        await page.GotoAsync(httpsServer.EmptyPage);
        var log = await pageWithHar.GetLog();
        var cookies = log.GetProperty("entries")[0].GetProperty("response").GetProperty("cookies");
        ExpectToEqual(cookies[0], new { name = "name1", value = "value1", secure = true });
    }

    [UpstreamTest("HA-020 should record request overrides")]
    public async Task ShouldRecordRequestOverrides(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.RouteAsync("**/foo", route =>
        {
            IgnoreErrors(route.FallbackAsync(new()
            {
                Url = server.EmptyPage,
                Method = "POST",
                Headers = new Dictionary<string, string>(route.Request.Headers)
                {
                    ["content-type"] = "text/plain",
                    ["cookie"] = "foo=bar",
                    ["custom"] = "value",
                },
                PostData = Encoding.UTF8.GetBytes("Hi!"),
            }));
        });

        await page.GotoAsync(server.Prefix + "/foo");
        var log = await pageWithHar.GetLog();
        var request = log.GetProperty("entries")[0].GetProperty("request");
        Assert.That(request.GetProperty("url").GetString(), Is.EqualTo(server.EmptyPage));
        Assert.That(request.GetProperty("method").GetString(), Is.EqualTo("POST"));
        // .NET: toContainEqual(item) is toEqual(expect.arrayContaining([item])).
        ExpectToEqual(request.GetProperty("headers"), ArrayContaining(new[] { new { name = "custom", value = "value" } }));
        ExpectToEqual(request.GetProperty("cookies"), Array.Empty<object>());
        ExpectToEqual(request.GetProperty("postData"), new { mimeType = "text/plain", @params = Array.Empty<object>(), text = "Hi!" });
    }

    [UpstreamTest("HA-021 should include content @smoke")]
    public async Task ShouldIncludeContent(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/har.html");
        await page.EvaluateAsync("() => fetch('/pptr.png').then(r => r.arrayBuffer())");
        var log = await pageWithHar.GetLog();

        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").TryGetProperty("encoding", out _), Is.False);
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").GetProperty("mimeType").GetString(), Is.EqualTo("text/html; charset=utf-8"));
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").GetProperty("text").GetString(), Does.Contain("HAR Page"));
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").GetProperty("size").GetInt32(), Is.GreaterThanOrEqualTo(96));
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").GetProperty("compression").GetInt32(), Is.EqualTo(0));

        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("content").TryGetProperty("encoding", out _), Is.False);
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("content").GetProperty("mimeType").GetString(), Is.EqualTo("text/css; charset=utf-8"));
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("content").GetProperty("text").GetString(), Does.Contain("pink"));
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("content").GetProperty("size").GetInt32(), Is.GreaterThanOrEqualTo(37));
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("content").GetProperty("compression").GetInt32(), Is.EqualTo(0));

        Assert.That(log.GetProperty("entries")[2].GetProperty("response").GetProperty("content").GetProperty("encoding").GetString(), Is.EqualTo("base64"));
        Assert.That(log.GetProperty("entries")[2].GetProperty("response").GetProperty("content").GetProperty("mimeType").GetString(), Is.EqualTo("image/png"));
        Assert.That(Convert.FromBase64String(log.GetProperty("entries")[2].GetProperty("response").GetProperty("content").GetProperty("text").GetString()).Length, Is.GreaterThan(0));
        Assert.That(log.GetProperty("entries")[2].GetProperty("response").GetProperty("content").GetProperty("size").GetInt32(), Is.GreaterThanOrEqualTo(6000));
        Assert.That(log.GetProperty("entries")[2].GetProperty("response").GetProperty("content").GetProperty("compression").GetInt32(), Is.EqualTo(0));
    }

    [UpstreamTest("HA-113 should store binary content with a textual mime type as base64")]
    public async Task ShouldStoreBinaryContentWithATextualMimeTypeAsBase64(ContextFactory contextFactory, Server server, string browserName, TestInfo testInfo)
    {
        Info.Fixme(browserName == "webkit", "WebKit decodes the body of a textual resource before it gets to us");
        var body = new byte[] { 0x80, 0xc3, 0x28, 0xff, 0x41 };
        server.SetRoute("/binary.js", (req, res) =>
        {
            res.SetHeader("Content-Type", "application/javascript");
            res.End(body);
        });
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync("() => fetch('/binary.js').then(r => r.arrayBuffer())");
        var log = await pageWithHar.GetLog();
        var content = log.GetProperty("entries").EnumerateArray().First(entry => entry.GetProperty("request").GetProperty("url").GetString().EndsWith("/binary.js", StringComparison.Ordinal)).GetProperty("response").GetProperty("content");
        Assert.That(content.GetProperty("encoding").GetString(), Is.EqualTo("base64"));
        Assert.That(Convert.FromBase64String(content.GetProperty("text").GetString()), Is.EqualTo(body));
    }

    [UpstreamTest("HA-022 should use attach mode for zip extension")]
    public async Task ShouldUseAttachModeForZipExtension(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo, new() { OutputPath = "test.har.zip" });
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/har.html");
        await page.EvaluateAsync("() => fetch('/pptr.png').then(r => r.arrayBuffer())");
        var zip = await pageWithHar.GetZip();
        var log = JsonDocument.Parse(Encoding.UTF8.GetString(zip["har.har"])).RootElement.GetProperty("log");
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").TryGetProperty("text", out _), Is.False);
        Assert.That(Encoding.UTF8.GetString(zip["75841480e2606c03389077304342fac2c58ccb1b.html"]), Does.Contain("HAR Page"));
    }

    [UpstreamTest("HA-023 should omit content")]
    public async Task ShouldOmitContent(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo, new() { Content = HarContentPolicy.Omit, OutputPath = "test.har" });
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/har.html");
        await page.EvaluateAsync("() => fetch('/pptr.png').then(r => r.arrayBuffer())");
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").TryGetProperty("text", out _), Is.False);
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").TryGetProperty("_file", out _), Is.False);
    }

    [UpstreamTest("HA-024 should omit content legacy")]
    public async Task ShouldOmitContentLegacy(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo, new() { OmitContent = true, OutputPath = "test.har" });
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/har.html");
        await page.EvaluateAsync("() => fetch('/pptr.png').then(r => r.arrayBuffer())");
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").TryGetProperty("text", out _), Is.False);
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").TryGetProperty("_file", out _), Is.False);
    }

    [UpstreamTest("HA-025 should attach content")]
    public async Task ShouldAttachContent(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo, new() { Content = HarContentPolicy.Attach, OutputPath = "test.har.zip" });
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/har.html");
        await page.EvaluateAsync("() => fetch('/pptr.png').then(r => r.arrayBuffer())");
        var zip = await pageWithHar.GetZip();
        var log = JsonDocument.Parse(Encoding.UTF8.GetString(zip["har.har"])).RootElement.GetProperty("log");

        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").TryGetProperty("encoding", out _), Is.False);
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").GetProperty("mimeType").GetString(), Is.EqualTo("text/html; charset=utf-8"));
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").GetProperty("_file").GetString(), Does.Contain("75841480e2606c03389077304342fac2c58ccb1b"));
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").GetProperty("size").GetInt32(), Is.GreaterThanOrEqualTo(96));
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").GetProperty("compression").GetInt32(), Is.EqualTo(0));

        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("content").TryGetProperty("encoding", out _), Is.False);
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("content").GetProperty("mimeType").GetString(), Is.EqualTo("text/css; charset=utf-8"));
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("content").GetProperty("_file").GetString(), Does.Contain("79f739d7bc88e80f55b9891a22bf13a2b4e18adb"));
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("content").GetProperty("size").GetInt32(), Is.GreaterThanOrEqualTo(37));
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("content").GetProperty("compression").GetInt32(), Is.EqualTo(0));

        Assert.That(log.GetProperty("entries")[2].GetProperty("response").GetProperty("content").TryGetProperty("encoding", out _), Is.False);
        Assert.That(log.GetProperty("entries")[2].GetProperty("response").GetProperty("content").GetProperty("mimeType").GetString(), Is.EqualTo("image/png"));
        Assert.That(log.GetProperty("entries")[2].GetProperty("response").GetProperty("content").GetProperty("_file").GetString(), Does.Contain("a4c3a18f0bb83f5d9fe7ce561e065c36205762fa"));
        Assert.That(log.GetProperty("entries")[2].GetProperty("response").GetProperty("content").GetProperty("size").GetInt32(), Is.GreaterThanOrEqualTo(6000));
        Assert.That(log.GetProperty("entries")[2].GetProperty("response").GetProperty("content").GetProperty("compression").GetInt32(), Is.EqualTo(0));

        Assert.That(Encoding.UTF8.GetString(zip["75841480e2606c03389077304342fac2c58ccb1b.html"]), Does.Contain("HAR Page"));
        Assert.That(Encoding.UTF8.GetString(zip["79f739d7bc88e80f55b9891a22bf13a2b4e18adb.css"]), Does.Contain("pink"));
        Assert.That(zip["a4c3a18f0bb83f5d9fe7ce561e065c36205762fa.png"].Length, Is.EqualTo(log.GetProperty("entries")[2].GetProperty("response").GetProperty("content").GetProperty("size").GetInt32()));
    }

    [UpstreamTest("HA-026 should filter by glob")]
    public async Task ShouldFilterByGlob(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var harPath = testInfo.OutputPath("test.har");
        var context = await contextFactory(new() { BaseURL = server.Prefix, RecordHarPath = harPath, RecordHarUrlFilter = "/*.css", IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync("/har.html");
        await context.CloseAsync();
        var log = JsonDocument.Parse(File.ReadAllText(harPath)).RootElement.GetProperty("log");
        Assert.That(log.GetProperty("entries").GetArrayLength(), Is.EqualTo(1));
        Assert.That(log.GetProperty("entries")[0].GetProperty("request").GetProperty("url").GetString().EndsWith("one-style.css", StringComparison.Ordinal), Is.EqualTo(true));
    }

    [UpstreamTest("HA-027 should filter by regexp")]
    public async Task ShouldFilterByRegexp(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var harPath = testInfo.OutputPath("test.har");
        var context = await contextFactory(new() { RecordHarPath = harPath, RecordHarUrlFilterRegex = new Regex("HAR.X?HTML", RegexOptions.IgnoreCase), IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.Prefix + "/har.html");
        await context.CloseAsync();
        var log = JsonDocument.Parse(File.ReadAllText(harPath)).RootElement.GetProperty("log");
        Assert.That(log.GetProperty("entries").GetArrayLength(), Is.EqualTo(1));
        Assert.That(log.GetProperty("entries")[0].GetProperty("request").GetProperty("url").GetString().EndsWith("har.html", StringComparison.Ordinal), Is.EqualTo(true));
    }

    [UpstreamTest("HA-028 should include sizes")]
    public async Task ShouldIncludeSizes(ContextFactory contextFactory, Server server, Func<string, string> asset, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/har.html");
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries").GetArrayLength(), Is.EqualTo(2));
        Assert.That(log.GetProperty("entries")[0].GetProperty("request").GetProperty("url").GetString().EndsWith("har.html", StringComparison.Ordinal), Is.EqualTo(true));
        Assert.That(log.GetProperty("entries")[0].GetProperty("request").GetProperty("headersSize").GetInt32(), Is.GreaterThanOrEqualTo(100));
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("bodySize").GetInt32(), Is.EqualTo(new FileInfo(asset("har.html")).Length));
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("headersSize").GetInt32(), Is.GreaterThanOrEqualTo(100));
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("_transferSize").GetInt32(), Is.GreaterThanOrEqualTo(250));

        Assert.That(log.GetProperty("entries")[1].GetProperty("request").GetProperty("url").GetString().EndsWith("one-style.css", StringComparison.Ordinal), Is.EqualTo(true));
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("bodySize").GetInt32(), Is.EqualTo(new FileInfo(asset("one-style.css")).Length));
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("headersSize").GetInt32(), Is.GreaterThanOrEqualTo(100));
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("_transferSize").GetInt32(), Is.GreaterThanOrEqualTo(150));
    }

    [UpstreamTest("HA-029 should work with gzip compression")]
    public async Task ShouldWorkWithGzipCompression(ContextFactory contextFactory, Server server, string browserName, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        server.EnableGzip("/simplezip.json");
        var response = await page.GotoAsync(server.Prefix + "/simplezip.json");
        Assert.That(response.Headers["content-encoding"], Is.EqualTo("gzip"));
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries").GetArrayLength(), Is.EqualTo(1));
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").GetProperty("compression").GetInt32(), Is.GreaterThan(4000));
    }

    [UpstreamTest("HA-030 should calculate time")]
    public async Task ShouldCalculateTime(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/har.html");
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries")[0].GetProperty("time").GetDouble(), Is.GreaterThan(0));
    }

    [UpstreamTest("HA-031 should return receive time")]
    public async Task ShouldReturnReceiveTime(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/har.html");
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries")[0].GetProperty("timings").GetProperty("receive").GetDouble(), Is.GreaterThan(0));
    }

    [UpstreamTest("HA-032 should report the correct _transferSize with PNG files")]
    public async Task ShouldReportTheCorrectTransferSizeWithPNGFiles(ContextFactory contextFactory, Server server, Func<string, string> asset, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync($@"
    <img src=""{server.Prefix}/pptr.png"" />
  ");
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("_transferSize").GetInt32(), Is.GreaterThan(new FileInfo(asset("pptr.png")).Length));
    }

    [UpstreamTest("HA-033 should have -1 _transferSize when its a failed request")]
    public async Task ShouldHave1TransferSizeWhenItsAFailedRequest(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        server.SetRoute("/one-style.css", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/css");
            res.Socket.Destroy();
        });
        var failedRequests = new List<IRequest>();
        page.RequestFailed += (_, request) => failedRequests.Add(request);
        await page.GotoAsync(server.Prefix + "/har.html");
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries")[1].GetProperty("request").GetProperty("url").GetString().EndsWith("/one-style.css", StringComparison.Ordinal), Is.EqualTo(true));
        Assert.That(log.GetProperty("entries")[1].GetProperty("response").GetProperty("_transferSize").GetInt32(), Is.EqualTo(-1));
    }

    [UpstreamTest("HA-034 should record failed request headers")]
    public async Task ShouldRecordFailedRequestHeaders(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        server.SetRoute("/har.html", (req, res) =>
        {
            res.Socket.Destroy();
        });
        await Catch(() => page.GotoAsync(server.Prefix + "/har.html"));
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("_failureText").GetString(), Is.Not.Empty);
        var request = log.GetProperty("entries")[0].GetProperty("request");
        Assert.That(request.GetProperty("url").GetString().EndsWith("/har.html", StringComparison.Ordinal), Is.EqualTo(true));
        Assert.That(request.GetProperty("method").GetString(), Is.EqualTo("GET"));
        // .NET: toContainEqual(item) is toEqual(expect.arrayContaining([item])).
        ExpectToEqual(request.GetProperty("headers"), ArrayContaining(new[] { ObjectContaining(new { name = "User-Agent" }) }));
    }

    [UpstreamTest("HA-035 should record failed request overrides")]
    public async Task ShouldRecordFailedRequestOverrides(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.Socket.Destroy();
        });
        await page.RouteAsync("**/foo", route =>
        {
            IgnoreErrors(route.FallbackAsync(new()
            {
                Url = server.EmptyPage,
                Method = "POST",
                Headers = new Dictionary<string, string>(route.Request.Headers)
                {
                    ["content-type"] = "text/plain",
                    ["cookie"] = "foo=bar",
                    ["custom"] = "value",
                },
                PostData = Encoding.UTF8.GetBytes("Hi!"),
            }));
        });
        await Catch(() => page.GotoAsync(server.Prefix + "/foo"));
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("_failureText").GetString(), Is.Not.Empty);
        var request = log.GetProperty("entries")[0].GetProperty("request");
        Assert.That(request.GetProperty("url").GetString(), Is.EqualTo(server.EmptyPage));
        Assert.That(request.GetProperty("method").GetString(), Is.EqualTo("POST"));
        // .NET: toContainEqual(item) is toEqual(expect.arrayContaining([item])).
        ExpectToEqual(request.GetProperty("headers"), ArrayContaining(new[] { new { name = "custom", value = "value" } }));
        ExpectToEqual(request.GetProperty("cookies"), Array.Empty<object>());
        ExpectToEqual(request.GetProperty("postData"), new { mimeType = "text/plain", @params = Array.Empty<object>(), text = "Hi!" });
    }

    [UpstreamTest("HA-036 should report the correct request body size")]
    public async Task ShouldReportTheCorrectRequestBodySize(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        server.SetRoute("/api", (req, res) => res.End());
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        await PromiseAll(
            page.WaitForResponseAsync(server.Prefix + "/api1"),
            page.EvaluateAsync(@"() => {
      void fetch('/api1', {
        method: 'POST',
        body: 'abc123'
      });
    }"));
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries")[1].GetProperty("request").GetProperty("bodySize").GetInt32(), Is.EqualTo(6));
    }

    [UpstreamTest("HA-037 should report the correct request body size when the bodySize is 0")]
    public async Task ShouldReportTheCorrectRequestBodySizeWhenTheBodySizeIs0(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        server.SetRoute("/api", (req, res) => res.End());
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        await PromiseAll(
            page.WaitForResponseAsync(server.Prefix + "/api2"),
            page.EvaluateAsync(@"() => {
      void fetch('/api2', {
        method: 'POST',
        body: ''
      });
    }"));
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries")[1].GetProperty("request").GetProperty("bodySize").GetInt32(), Is.EqualTo(0));
    }

    [UpstreamTest("HA-038 should report the correct response body size when the bodySize is 0")]
    public async Task ShouldReportTheCorrectResponseBodySizeWhenTheBodySizeIs0(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        var response = await page.GotoAsync(server.EmptyPage);
        await response.FinishedAsync();
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("bodySize").GetInt32(), Is.EqualTo(0));
    }

    [UpstreamTest("HA-039 should have popup requests")]
    public async Task ShouldHavePopupRequests(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var popup = await PromiseAll(
            page.WaitForPopupAsync(),
            page.ClickAsync("a"));
        await popup.WaitForLoadStateAsync();
        var log = await pageWithHar.GetLog();

        Assert.That(log.GetProperty("pages").GetArrayLength(), Is.EqualTo(2));

        var entries = log.GetProperty("entries").EnumerateArray().Where(entry => entry.GetProperty("pageref").GetString() == log.GetProperty("pages")[1].GetProperty("id").GetString()).ToArray();
        Assert.That(entries.Length, Is.EqualTo(2));
        Assert.That(entries[0].GetProperty("request").GetProperty("url").GetString(), Is.EqualTo(server.Prefix + "/one-style.html"));
        Assert.That(entries[0].GetProperty("response").GetProperty("status").GetInt32(), Is.EqualTo(200));
        Assert.That(entries[1].GetProperty("request").GetProperty("url").GetString(), Is.EqualTo(server.Prefix + "/one-style.css"));
        Assert.That(entries[1].GetProperty("response").GetProperty("status").GetInt32(), Is.EqualTo(200));
    }

    [UpstreamTest("HA-040 should not contain internal pages")]
    public async Task ShouldNotContainInternalPages(string browserName, ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "name=value");
            res.End();
        });

        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        var context = pageWithHar.Context;
        await page.GotoAsync(server.EmptyPage);

        var cookies = await context.CookiesAsync();
        Assert.That(cookies.Count, Is.EqualTo(1));
        // Get storage state, this create internal page.
        await context.StorageStateAsync();

        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("pages").GetArrayLength(), Is.EqualTo(1));
    }

    [UpstreamTest("HA-041 should have connection details")]
    public async Task ShouldHaveConnectionDetails(ContextFactory contextFactory, Server server, string browserName, string platform, string mode, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        var log = await pageWithHar.GetLog();
        var serverIPAddress = log.GetProperty("entries")[0].GetProperty("serverIPAddress").GetString();
        var port = log.GetProperty("entries")[0].GetProperty("_serverPort").GetInt32();
        var securityDetails = log.GetProperty("entries")[0].GetProperty("_securityDetails");
        Assert.That(serverIPAddress, Does.Match(@"^127\.0\.0\.1|\[::1\]"));
        Assert.That(port, Is.EqualTo(server.Port));
        ExpectToEqual(securityDetails, new { });
    }

    [UpstreamTest("HA-042 should have security details")]
    [LocalFixme("harness: the HTTPS server of the harness negotiates TLS 1.2 on macOS, where the SslStream of .NET has no TLS 1.3 for a server: the protocol is 'TLS 1.2', upstream expects 'TLS 1.3' (only on darwin)")]
    public async Task ShouldHaveSecurityDetails(ContextFactory contextFactory, Server httpsServer, string browserName, string platform, string mode, string channel, bool isFrozenWebkit, TestInfo testInfo)
    {
        Info.Fixme(browserName == "webkit" && platform == "win32" && channel != "webkit-wsl");
        Info.Skip(isFrozenWebkit);

        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        var context = pageWithHar.Context;
        var apiHarPath = testInfo.OutputPath("api.har");
        await context.APIRequest.Tracing.StartHarAsync(apiHarPath);
        await page.GotoAsync(httpsServer.EmptyPage);
        await page.APIRequest.GetAsync(httpsServer.EmptyPage);
        await context.APIRequest.Tracing.StopHarAsync();
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries").GetArrayLength(), Is.EqualTo(1));
        var serverIPAddress = log.GetProperty("entries")[0].GetProperty("serverIPAddress").GetString();
        var port = log.GetProperty("entries")[0].GetProperty("_serverPort").GetInt32();
        var securityDetails = log.GetProperty("entries")[0].GetProperty("_securityDetails");
        Assert.That(serverIPAddress, Does.Match(@"^127\.0\.0\.1|\[::1\]"));
        Assert.That(port, Is.EqualTo(httpsServer.Port));
        if (browserName == "webkit")
        {
            ExpectToEqual(securityDetails, new { protocol = "TLS 1.3", subjectName = "playwright-test", validFrom = 1691708270, validTo = 2007068270 });
        }
        else
        {
            ExpectToEqual(securityDetails, new { issuer = "playwright-test", protocol = "TLS 1.3", subjectName = "playwright-test", validFrom = 1691708270, validTo = 2007068270 });
        }

        var apiLog = JsonDocument.Parse(File.ReadAllText(apiHarPath)).RootElement.GetProperty("log");
        Assert.That(apiLog.GetProperty("entries").GetArrayLength(), Is.EqualTo(1));
        ExpectToEqual(apiLog.GetProperty("entries")[0].GetProperty("_securityDetails"), new { issuer = "playwright-test", protocol = "TLSv1.3", subjectName = "playwright-test", validFrom = 1691708270, validTo = 2007068270 });
    }

    [UpstreamTest("HA-043 should have connection details for redirects")]
    public async Task ShouldHaveConnectionDetailsForRedirects(ContextFactory contextFactory, Server server, string browserName, string mode, TestInfo testInfo)
    {
        server.SetRedirect("/foo.html", "/empty.html");
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/foo.html");
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries").GetArrayLength(), Is.EqualTo(2));

        var detailsFoo = log.GetProperty("entries")[0];

        if (browserName == "webkit")
        {
            Assert.That(detailsFoo.TryGetProperty("serverIPAddress", out _), Is.False);
            Assert.That(detailsFoo.TryGetProperty("_serverPort", out _), Is.False);
        }
        else
        {
            Assert.That(detailsFoo.GetProperty("serverIPAddress").GetString(), Does.Match(@"^127\.0\.0\.1|\[::1\]"));
            Assert.That(detailsFoo.GetProperty("_serverPort").GetInt32(), Is.EqualTo(server.Port));
        }

        var detailsEmpty = log.GetProperty("entries")[1];
        Assert.That(detailsEmpty.GetProperty("serverIPAddress").GetString(), Does.Match(@"^127\.0\.0\.1|\[::1\]"));
        Assert.That(detailsEmpty.GetProperty("_serverPort").GetInt32(), Is.EqualTo(server.Port));
    }

    [UpstreamTest("HA-044 should have connection details for failed requests")]
    public async Task ShouldHaveConnectionDetailsForFailedRequests(ContextFactory contextFactory, Server server, string browserName, string platform, string mode, TestInfo testInfo)
    {
        server.SetRoute("/one-style.css", (_, res) =>
        {
            res.SetHeader("Content-Type", "text/css");
            res.Socket.Destroy();
        });
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/one-style.html");
        var log = await pageWithHar.GetLog();
        var serverIPAddress = log.GetProperty("entries")[0].GetProperty("serverIPAddress").GetString();
        var port = log.GetProperty("entries")[0].GetProperty("_serverPort").GetInt32();
        Assert.That(serverIPAddress, Does.Match(@"^127\.0\.0\.1|\[::1\]"));
        Assert.That(port, Is.EqualTo(server.Port));
    }

    [UpstreamTest("HA-045 should return server address directly from response")]
    public async Task ShouldReturnServerAddressDirectlyFromResponse(IPage page, Server server, string mode)
    {
        var response = await page.GotoAsync(server.EmptyPage);
        var serverAddr = await response.ServerAddrAsync();
        var ipAddress = serverAddr.IpAddress;
        var port = serverAddr.Port;
        Assert.That(ipAddress, Does.Match(@"^127\.0\.0\.1|\[::1\]"));
        Assert.That(port, Is.EqualTo(server.Port));
    }

    [UpstreamTest("HA-046 should return security details directly from response")]
    [LocalFixme("harness: the HTTPS server of the harness negotiates TLS 1.2 on macOS, where the SslStream of .NET has no TLS 1.3 for a server: the protocol is 'TLS 1.2', upstream expects 'TLS 1.3' (only on darwin)")]
    public async Task ShouldReturnSecurityDetailsDirectlyFromResponse(ContextFactory contextFactory, Server httpsServer, string browserName, string platform, bool isFrozenWebkit)
    {
        Info.Skip(isFrozenWebkit);

        var context = await contextFactory(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(httpsServer.EmptyPage);
        var securityDetails = await response.SecurityDetailsAsync();
        if (browserName == "webkit" && platform == "win32")
        {
            // WebKit on Windows does not surface the TLS protocol.
            // .NET: { ...securityDetails, protocol: undefined } is a copy without the protocol.
            ExpectToEqual(new ResponseSecurityDetailsResult { Issuer = securityDetails.Issuer, SubjectName = securityDetails.SubjectName, ValidFrom = securityDetails.ValidFrom, ValidTo = securityDetails.ValidTo }, new { subjectName = "playwright-test", validFrom = 1691708270, validTo = 2007068270 });
        }
        else if (browserName == "webkit")
        {
            ExpectToEqual(securityDetails, new { protocol = "TLS 1.3", subjectName = "playwright-test", validFrom = 1691708270, validTo = 2007068270 });
        }
        else
        {
            ExpectToEqual(securityDetails, new { issuer = "playwright-test", protocol = "TLS 1.3", subjectName = "playwright-test", validFrom = 1691708270, validTo = 2007068270 });
        }
    }

    [UpstreamTest("HA-047 should contain http2 for http2 requests")]
    public async Task ShouldContainHttp2ForHttp2Requests(ContextFactory contextFactory, TestInfo testInfo)
    {
        // .NET: createHttp2Server(await TestServer.certOptions()) with a 'stream' listener is a Kestrel server that speaks HTTP/2.
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrelHttpsConfiguration();
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(0, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
            listenOptions.UseHttps(Server.CertOptions());
        }));
        var server = builder.Build();
        server.Run(async stream =>
        {
            stream.Response.ContentType = "text/html; charset=utf-8";
            stream.Response.StatusCode = 200;
            await stream.Response.WriteAsync("<h1>Hello World</h1>");
        });
        await server.StartAsync();

        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync($"https://localhost:{new Uri(server.Urls.First()).Port}");
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries")[0].GetProperty("request").GetProperty("httpVersion").GetString(), Is.EqualTo("HTTP/2.0"));
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("httpVersion").GetString(), Is.EqualTo("HTTP/2.0"));
        Assert.That(log.GetProperty("entries")[0].GetProperty("response").GetProperty("content").GetProperty("text").GetString(), Is.EqualTo("<h1>Hello World</h1>"));
        await server.StopAsync();
    }

    [UpstreamTest("HA-048 should filter favicon and favicon redirects")]
    public async Task ShouldFilterFaviconAndFaviconRedirects(Server server, string browserName, bool headless, Func<string, string> asset, ContextFactory contextFactory, string channel, TestInfo testInfo)
    {
        Info.Skip(headless && browserName != "firefox", "headless browsers, except firefox, do not request favicons");
        Info.Skip(!headless && browserName == "webkit", "headed webkit does not have a favicon feature");

        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;

        // Browsers aggressively cache favicons, so force bust with the
        // `d` parameter to make iterating on this test more predictable and isolated.
        var favicon = "/no-cache-2/favicon.ico";
        var hashedFaviconUrl = "/favicon-hashed.ico";
        server.SetRedirect(favicon, hashedFaviconUrl);
        server.SetRoute(hashedFaviconUrl, (req, res) =>
        {
            server.ServeFile(req, res, asset("media-query-prefers-color-scheme.svg"));
        });

        server.SetRoute("/page.html", (_, res) =>
        {
            res.End($@"
      <!DOCTYPE html>
      <html>
        <head>
          <meta charset=""utf-8"">
          <link rel=""icon"" type=""image/svg+xml"" href=""{favicon}"">
          <title>SVG Favicon Test</title>
        </head>
        <body>
          favicons
        </body>
      </html>
");
        });

        await PromiseAll(
            server.WaitForRequest(favicon),
            server.WaitForRequest(hashedFaviconUrl),
            page.GotoAsync(server.Prefix + "/page.html"));

        await page.WaitForTimeoutAsync(500);
        // Text still being around ensures we haven't actually lost our browser to a crash.
        await page.WaitForSelectorAsync("text=favicons");

        // favicon and 302 redirects to favicons should be filtered out of request logs
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries").GetArrayLength(), Is.EqualTo(1));
        var entry = log.GetProperty("entries")[0];
        Assert.That(entry.GetProperty("request").GetProperty("url").GetString(), Is.EqualTo(server.Prefix + "/page.html"));
    }

    [UpstreamTest("HA-049 should have different hars for concurrent contexts")]
    public async Task ShouldHaveDifferentHarsForConcurrentContexts(ContextFactory contextFactory, TestInfo testInfo)
    {
        var session0 = await PageWithHar(contextFactory, testInfo, new() { OutputPath = "test-0.har" });
        await session0.Page.GotoAsync("data:text/html,<title>Zero</title>");
        await session0.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        var session1 = await PageWithHar(contextFactory, testInfo, new() { OutputPath = "test-1.har" });
        await session1.Page.GotoAsync("data:text/html,<title>One</title>");
        await session1.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Trigger flushing on the server and ensure they are not racing to same
        // location. NB: Run this test with --repeat-each 10.
        var (log0, log1) = await PromiseAll(
            session0.GetLog(),
            session1.GetLog());

        {
            Assert.That(log0.GetProperty("pages").GetArrayLength(), Is.EqualTo(1));
            var pageEntry = log0.GetProperty("pages")[0];
            Assert.That(pageEntry.GetProperty("title").GetString(), Is.EqualTo("Zero"));
        }

        {
            Assert.That(log1.GetProperty("pages").GetArrayLength(), Is.EqualTo(1));
            var pageEntry = log1.GetProperty("pages")[0];
            Assert.That(pageEntry.GetProperty("id").GetString(), Is.Not.EqualTo(log0.GetProperty("pages")[0].GetProperty("id").GetString()));
            Assert.That(pageEntry.GetProperty("title").GetString(), Is.EqualTo("One"));
        }
    }

    [UpstreamTest("HA-050 should exclude API request")]
    public async Task ShouldExcludeAPIRequest(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        await page.APIRequest.GetAsync(server.Prefix + "/simple.json");
        var log = await pageWithHar.GetLog();
        Assert.That(log.GetProperty("entries").EnumerateArray().Select(entry => entry.GetProperty("request").GetProperty("url").GetString()), Is.EqualTo(new[] { server.EmptyPage }));
    }

    [UpstreamTest("HA-051 should correctly record API request cookies with equals sign in value")]
    public async Task ShouldCorrectlyRecordAPIRequestCookiesWithEqualsSignInValue(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var context = await contextFactory();
        var harPath = testInfo.OutputPath("request.har");
        await context.APIRequest.Tracing.StartHarAsync(harPath);
        var url = server.Prefix + "/simple.json";
        await context.APIRequest.GetAsync(url, new()
        {
            Headers = new Dictionary<string, string> { ["cookie"] = "token=abc=xyz; other=val" },
        });
        await context.APIRequest.Tracing.StopHarAsync();
        await context.CloseAsync();
        var log = JsonDocument.Parse(File.ReadAllText(harPath)).RootElement.GetProperty("log");
        ExpectToEqual(log.GetProperty("entries")[0].GetProperty("request").GetProperty("cookies"), new[]
        {
            new { name = "token", value = "abc=xyz" },
            new { name = "other", value = "val" },
        });
    }

    [UpstreamTest("HA-052 should record nameless API request cookies")]
    public async Task ShouldRecordNamelessAPIRequestCookies(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var context = await contextFactory();
        var harPath = testInfo.OutputPath("request.har");
        await context.APIRequest.Tracing.StartHarAsync(harPath);
        await context.APIRequest.GetAsync(server.Prefix + "/simple.json", new()
        {
            Headers = new Dictionary<string, string> { ["cookie"] = "nameless; other=val" },
        });
        await context.APIRequest.Tracing.StopHarAsync();
        await context.CloseAsync();
        var log = JsonDocument.Parse(File.ReadAllText(harPath)).RootElement.GetProperty("log");
        ExpectToEqual(log.GetProperty("entries")[0].GetProperty("request").GetProperty("cookies"), new[]
        {
            new { name = string.Empty, value = "nameless" },
            new { name = "other", value = "val" },
        });
    }

    [UpstreamTest("HA-053 should respect minimal mode for API Requests")]
    public async Task ShouldRespectMinimalModeForAPIRequests(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var context = await contextFactory();
        var harPath = testInfo.OutputPath("request.har");
        await context.APIRequest.Tracing.StartHarAsync(harPath, new() { Mode = HarMode.Minimal });
        var url = server.Prefix + "/simple.json";
        await context.APIRequest.PostAsync(url, new()
        {
            Headers = new Dictionary<string, string> { ["cookie"] = "a=b; c=d" },
            DataObject = new { foo = "bar" },
        });
        await context.APIRequest.Tracing.StopHarAsync();
        await context.CloseAsync();
        var entries = JsonDocument.Parse(File.ReadAllText(harPath)).RootElement.GetProperty("log").GetProperty("entries");
        Assert.That(entries.GetArrayLength(), Is.EqualTo(1));
        var entry = entries[0];
        ExpectToEqual(entry.GetProperty("timings"), new { receive = -1, send = -1, wait = -1 });
        Assert.That(entry.TryGetProperty("serverIPAddress", out _), Is.False);
        Assert.That(entry.TryGetProperty("_serverPort", out _), Is.False);
        ExpectToEqual(entry.GetProperty("request").GetProperty("cookies"), Array.Empty<object>());
        Assert.That(entry.GetProperty("request").GetProperty("bodySize").GetInt32(), Is.EqualTo(-1));
        Assert.That(entry.GetProperty("response").GetProperty("bodySize").GetInt32(), Is.EqualTo(-1));
    }

    [UpstreamTest("HA-054 should include redirects from API request")]
    public async Task ShouldIncludeRedirectsFromAPIRequest(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        server.SetRedirect("/redirect-me", "/simple.json");
        var context = await contextFactory();
        var harPath = testInfo.OutputPath("request.har");
        await context.APIRequest.Tracing.StartHarAsync(harPath);
        await context.APIRequest.PostAsync(server.Prefix + "/redirect-me", new()
        {
            Headers = new Dictionary<string, string> { ["cookie"] = "a=b; c=d" },
            DataObject = new { foo = "bar" },
        });
        await context.APIRequest.Tracing.StopHarAsync();
        await context.CloseAsync();
        var log = JsonDocument.Parse(File.ReadAllText(harPath)).RootElement.GetProperty("log");
        Assert.That(log.GetProperty("entries").GetArrayLength(), Is.EqualTo(2));
        var redirect = log.GetProperty("entries")[0];
        var json = log.GetProperty("entries")[1];
        Assert.That(redirect.GetProperty("request").GetProperty("url").GetString(), Is.EqualTo(server.Prefix + "/redirect-me"));
        Assert.That(json.GetProperty("request").GetProperty("url").GetString(), Is.EqualTo(server.Prefix + "/simple.json"));
        Assert.That(redirect.GetProperty("response").GetProperty("bodySize").GetInt32(), Is.EqualTo(-1));
        Assert.That(redirect.GetProperty("response").GetProperty("content").GetProperty("size").GetInt32(), Is.EqualTo(-1));
        Assert.That(redirect.TryGetProperty("timings", out _), Is.True);
        Assert.That(json.TryGetProperty("timings", out _), Is.True);
    }

    [UpstreamTest("HA-055 should not hang on resources served from cache")]
    public async Task ShouldNotHangOnResourcesServedFromCache(ContextFactory contextFactory, Server server, string browserName, bool isBidi, TestInfo testInfo)
    {
        server.SetRoute("/one-style.css", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/css",
                ["Cache-Control"] = "public, max-age=10031518",
            });
            res.End("body { background: red }");
        });
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/har.html");
        await page.GotoAsync(server.Prefix + "/har.html");
        var log = await pageWithHar.GetLog();
        var entries = log.GetProperty("entries").EnumerateArray().Where(e => e.GetProperty("request").GetProperty("url").GetString().EndsWith("one-style.css", StringComparison.Ordinal)).ToArray();
        // In firefox no request events are fired for cached resources.
        if (browserName == "firefox" && !isBidi)
        {
            Assert.That(entries.Length, Is.EqualTo(1));
        }
        else
        {
            Assert.That(entries.Length, Is.EqualTo(2));
        }
    }

    [UpstreamTest("HA-056 should not hang on slow chunked response")]
    public async Task ShouldNotHangOnSlowChunkedResponse(string browserName, IBrowser browser, ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/html",
            });
            res.End(@"<script>
    let receivedFirstData = new Promise(f => {
      setTimeout(() =>  {
        var x = new XMLHttpRequest();
        x.open(""GET"", ""slow.txt"");
        x.onprogress = () => f();
        x.send();
      }, 0);
    });
    </script>");
        });
        server.SetRoute("/slow.txt", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/plain",
                ["Transfer-Encoding"] = "chunked",
                ["Content-length"] = "2023",
            });
            res.Write("begin");
        });
        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync("() => window.receivedFirstData");
        var log = await pageWithHar.GetLog();

        Assert.That(log.GetProperty("browser").GetProperty("name").GetString(), Is.EqualTo(browserName));
        Assert.That(log.GetProperty("browser").GetProperty("version").GetString(), Is.EqualTo(browser.Version));
    }

    [UpstreamTest("HA-057 should close the context when saving the har fails")]
    public async Task ShouldCloseTheContextWhenSavingTheHarFails(ContextFactory contextFactory, Server server, TestInfo testInfo)
    {
        var filePath = testInfo.OutputPath("not-a-directory");
        File.WriteAllText(filePath, "data");
        var context = await contextFactory(new() { RecordHarPath = Path.Combine(filePath, "test.har") });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var closed = Once<IBrowserContext>(h => context.Close += h, h => context.Close -= h);
        var error = await ThrowsAsync<PlaywrightException>(() => context.CloseAsync());
        Assert.That(error.Message, Does.Match("ENOTDIR|ENOENT|EEXIST"));
        await closed;
        await context.CloseAsync();
    }

    [UpstreamTest("HA-058 should support HAR larger than 512MB")]
    public async Task ShouldSupportHARLargerThan512MB(ContextFactory contextFactory, Server server, string browserName, TestInfo testInfo)
    {
        Info.Skip(browserName != "chromium", "serializer is browser-agnostic; one browser is enough");
        Info.Slow();

        var harPath = testInfo.OutputPath("test.har");
        var context = await contextFactory();
        await context.APIRequest.Tracing.StartHarAsync(harPath);

        // 30 x 20MB textual responses push the HAR JSON past V8's ~512MB max
        // string length. Each body still fits in a single string; only the
        // aggregate would overflow the previous tokens.join('').
        var body = new string('a', 20 * 1024 * 1024);
        server.SetRoute("/large", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/plain" });
            res.End(body);
        });
        for (var i = 0; i < 30; i++)
        {
            await context.APIRequest.GetAsync($"{server.Prefix}/large");
        }
        await context.APIRequest.Tracing.StopHarAsync();
        await context.CloseAsync();

        var stats = new FileInfo(harPath);
        Assert.That(stats.Length, Is.GreaterThan(512 * 1024 * 1024));

        // Reading the whole file as a string would re-hit V8's limit, so
        // sample the head and tail to verify structural sanity.
        using var fd = File.OpenRead(harPath);
        var head = new byte[64];
        fd.Seek(0, SeekOrigin.Begin);
        fd.ReadExactly(head, 0, 64);
        var tail = new byte[64];
        fd.Seek(stats.Length - 64, SeekOrigin.Begin);
        fd.ReadExactly(tail, 0, 64);
        fd.Close();
        Assert.That(Encoding.UTF8.GetString(head), Does.Match(@"^\{\s*""log""\s*:\s*\{"));
        Assert.That(Encoding.UTF8.GetString(tail), Does.Match(@"\}\s*\}\s*$"));
    }

    [UpstreamTest("HA-059 should record resource type")]
    public async Task ShouldRecordResourceType(ContextFactory contextFactory, Server server, Func<string, string> asset, TestInfo testInfo)
    {
        server.SetRoute("/resource-types.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End(@"
      <link rel=""stylesheet"" href=""/resource-type-stylesheet.css"">
      <script src=""/resource-type-script.js""></script>
      <img src=""/resource-type-image.png"">
    ");
        });
        server.SetRoute("/resource-type-stylesheet.css", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/css" });
            res.End(@"
      @font-face {
        font-family: 'iconfont';
        src: url('/resource-type-font.woff2') format('woff2');
      }
      body { font-family: 'iconfont'; }
    ");
        });
        server.SetRoute("/resource-type-font.woff2", (req, res) =>
        {
            server.ServeFile(req, res, asset("webfont/iconfont.woff2"));
        });
        server.SetRoute("/resource-type-script.js", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "application/javascript" });
            res.End("window.__loaded = true;");
        });
        server.SetRoute("/resource-type-image.png", (req, res) =>
        {
            server.ServeFile(req, res, asset("pptr.png"));
        });

        var pageWithHar = await PageWithHar(contextFactory, testInfo);
        var page = pageWithHar.Page;
        await page.GotoAsync(server.Prefix + "/resource-types.html");
        await page.EvaluateAsync("() => fetch('/resource-type-fetch').catch(() => {})");
        await page.EvaluateAsync(@"() => new Promise(resolve => {
    const xhr = new XMLHttpRequest();
    xhr.open('GET', '/resource-type-xhr');
    xhr.onloadend = () => resolve();
    xhr.send();
  })");
        var log = await pageWithHar.GetLog();

        var typeForURL = log.GetProperty("entries").EnumerateArray().Aggregate(new Dictionary<string, object>(), (accumulator, entry) =>
        {
            accumulator[entry.GetProperty("request").GetProperty("url").GetString()] = entry.GetProperty("_resourceType").GetString();
            return accumulator;
        });
        ExpectToEqual(typeForURL, ObjectContaining(new Dictionary<string, object>
        {
            [server.Prefix + "/resource-types.html"] = "document",
            [server.Prefix + "/resource-type-stylesheet.css"] = "stylesheet",
            [server.Prefix + "/resource-type-script.js"] = "script",
            [server.Prefix + "/resource-type-image.png"] = "image",
            [server.Prefix + "/resource-type-font.woff2"] = "font",
            [server.Prefix + "/resource-type-fetch"] = "fetch",
            [server.Prefix + "/resource-type-xhr"] = "xhr",
        }));
    }

    public class TracingStartHar
    {
        [UpstreamTest("HA-060 should record a HAR with options")]
        public async Task ShouldRecordAHARWithOptions(ContextFactory contextFactory, Server server, TestInfo testInfo)
        {
            var context = await contextFactory();
            var harPath = testInfo.OutputPath("tracing.har");
            await context.Tracing.StartHarAsync(harPath, new() { Mode = HarMode.Minimal, UrlFilter = "**/one-style.css" });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/one-style.html");
            await context.Tracing.StopHarAsync();
            await context.CloseAsync();

            var log = JsonDocument.Parse(File.ReadAllText(harPath)).RootElement.GetProperty("log");
            var urls = log.GetProperty("entries").EnumerateArray().Select(e => e.GetProperty("request").GetProperty("url").GetString());
            Assert.That(urls, Is.EqualTo(new[] { server.Prefix + "/one-style.css" }));
            // Minimal mode drops body sizes.
            Assert.That(log.GetProperty("entries")[0].GetProperty("request").GetProperty("bodySize").GetInt32(), Is.EqualTo(-1));
        }

        [UpstreamTest("HA-061 should record a HAR for a context APIRequestContext")]
        public async Task ShouldRecordAHARForAContextAPIRequestContext(ContextFactory contextFactory, Server server, TestInfo testInfo)
        {
            var context = await contextFactory();
            var harPath = testInfo.OutputPath("request.har");
            await context.APIRequest.Tracing.StartHarAsync(harPath);
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            var url = server.Prefix + "/simple.json";
            var response = await context.APIRequest.PostAsync(url, new()
            {
                Headers = new Dictionary<string, string> { ["cookie"] = "a=b; c=d" },
                DataObject = new { foo = "bar" },
            });
            var responseBody = await response.BodyAsync();
            await context.APIRequest.Tracing.StopHarAsync();
            await context.CloseAsync();

            var log = JsonDocument.Parse(File.ReadAllText(harPath)).RootElement.GetProperty("log");
            Assert.That(log.GetProperty("entries").GetArrayLength(), Is.EqualTo(1));
            var entry = log.GetProperty("entries")[0];
            Assert.That(entry.GetProperty("request").GetProperty("url").GetString(), Is.EqualTo(url));
            Assert.That(entry.GetProperty("request").GetProperty("method").GetString(), Is.EqualTo("POST"));
            Assert.That(entry.GetProperty("request").GetProperty("httpVersion").GetString(), Is.EqualTo("HTTP/1.1"));
            ExpectToEqual(entry.GetProperty("request").GetProperty("cookies"), new[]
            {
                new
                {
                    name = "a",
                    value = "b",
                },
                new
                {
                    name = "c",
                    value = "d",
                },
            });
            Assert.That(entry.GetProperty("request").GetProperty("headers").GetArrayLength(), Is.GreaterThan(1));
            Assert.That(entry.GetProperty("request").GetProperty("headers").EnumerateArray().FirstOrDefault(h => h.GetProperty("name").GetString().ToLowerInvariant() == "user-agent").ValueKind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(entry.GetProperty("request").GetProperty("headers").EnumerateArray().FirstOrDefault(h => h.GetProperty("name").GetString().ToLowerInvariant() == "content-type").GetProperty("value").GetString(), Is.EqualTo("application/json"));
            Assert.That(entry.GetProperty("request").GetProperty("headers").EnumerateArray().FirstOrDefault(h => h.GetProperty("name").GetString().ToLowerInvariant() == "content-length").GetProperty("value").GetString(), Is.EqualTo("13"));
            Assert.That(entry.GetProperty("request").GetProperty("bodySize").GetInt32(), Is.EqualTo(13));

            Assert.That(entry.GetProperty("response").GetProperty("status").GetInt32(), Is.EqualTo(200));
            Assert.That(entry.GetProperty("response").GetProperty("headers").EnumerateArray().FirstOrDefault(h => h.GetProperty("name").GetString().ToLowerInvariant() == "content-type").GetProperty("value").GetString(), Does.Contain("application/json"));
            Assert.That(entry.GetProperty("response").GetProperty("content").GetProperty("size").GetInt32(), Is.EqualTo(15));
            Assert.That(entry.GetProperty("response").GetProperty("content").GetProperty("text").GetString(), Is.EqualTo(Encoding.UTF8.GetString(responseBody)));
            Assert.That(entry.GetProperty("response").GetProperty("bodySize").GetInt32(), Is.EqualTo(15));

            Assert.That(entry.GetProperty("time").GetDouble(), Is.GreaterThan(0));
            ExpectToEqual(entry.GetProperty("timings"), ObjectContaining(new
            {
                blocked = -1,
                connect = AnyNumber,
                dns = AnyNumber,
                receive = AnyNumber,
                send = AnyNumber,
                ssl = AnyNumber,
                wait = AnyNumber,
            }));

            Assert.That(entry.TryGetProperty("serverIPAddress", out _), Is.True);
            Assert.That(entry.GetProperty("_serverPort").GetInt32(), Is.EqualTo(server.Port));
        }

        [UpstreamTest("HA-062 should include pages")]
        public async Task ShouldIncludePages(ContextFactory contextFactory, Server server, TestInfo testInfo)
        {
            var context = await contextFactory();
            var harPath = testInfo.OutputPath("tracing.har");
            await context.Tracing.StartHarAsync(harPath);
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/title.html");
            var page2 = await context.NewPageAsync();
            await page2.GotoAsync(server.Prefix + "/empty.html");
            await page.CloseAsync(); // Close the page before stopping - it should still be in the har.
            await context.Tracing.StopHarAsync();
            await context.CloseAsync();

            var log = JsonDocument.Parse(File.ReadAllText(harPath)).RootElement.GetProperty("log");
            Assert.That(log.GetProperty("pages").GetArrayLength(), Is.EqualTo(2));
            Assert.That(log.GetProperty("pages")[0].GetProperty("title").GetString(), Is.EqualTo("Woof-Woof"));
            Assert.That(log.GetProperty("pages")[1].GetProperty("title").GetString(), Is.EqualTo(string.Empty));
        }

        [UpstreamTest("HA-063 should record a zipped HAR for APIRequestContext")]
        public async Task ShouldRecordAZippedHARForAPIRequestContext(IPlaywright playwright, Server server, TestInfo testInfo)
        {
            var request = await playwright.APIRequest.NewContextAsync();
            var harPath = testInfo.OutputPath("tracing.har.zip");
            await request.Tracing.StartHarAsync(harPath, new() { Content = HarContentPolicy.Attach });
            await request.GetAsync(server.Prefix + "/simple.json");
            await request.Tracing.StopHarAsync();
            await request.DisposeAsync();

            var resources = ParseHar(harPath);
            var log = JsonDocument.Parse(Encoding.UTF8.GetString(resources["har.har"])).RootElement.GetProperty("log");
            Assert.That(log.GetProperty("entries").EnumerateArray().Any(e => e.GetProperty("request").GetProperty("url").GetString() == server.Prefix + "/simple.json"), Is.EqualTo(true));
        }

        [UpstreamTest("HA-064 should record correct cookie expires for APIRequestContext")]
        public async Task ShouldRecordCorrectCookieExpiresForAPIRequestContext(IPlaywright playwright, Server server, TestInfo testInfo)
        {
            server.SetRoute("/set-cookie", (req, res) =>
            {
                res.SetHeader("Set-Cookie", "name=value; Expires=Tue, 01 Jan 2030 00:00:00 GMT");
                res.End("hello");
            });
            var request = await playwright.APIRequest.NewContextAsync();
            var harPath = testInfo.OutputPath("api.har.zip");
            await request.Tracing.StartHarAsync(harPath, new() { Content = HarContentPolicy.Attach });
            await request.GetAsync(server.Prefix + "/set-cookie");
            await request.Tracing.StopHarAsync();
            await request.DisposeAsync();

            var resources = ParseHar(harPath);
            var log = JsonDocument.Parse(Encoding.UTF8.GetString(resources["har.har"])).RootElement.GetProperty("log");
            var entry = log.GetProperty("entries").EnumerateArray().First(e => e.GetProperty("request").GetProperty("url").GetString().EndsWith("/set-cookie", StringComparison.Ordinal));
            var cookie = entry.GetProperty("response").GetProperty("cookies").EnumerateArray().First(c => c.GetProperty("name").GetString() == "name");
            Assert.That(DateTimeOffset.Parse(cookie.GetProperty("expires").GetString(), CultureInfo.InvariantCulture).UtcDateTime.Year, Is.EqualTo(2030));
        }

        [UpstreamTest("HA-065 should record mixed-case request content-type for APIRequestContext")]
        public async Task ShouldRecordMixedCaseRequestContentTypeForAPIRequestContext(IPlaywright playwright, Server server, TestInfo testInfo)
        {
            server.SetRoute("/post", (req, res) => res.End("ok"));
            var request = await playwright.APIRequest.NewContextAsync();
            var harPath = testInfo.OutputPath("api-post.har.zip");
            await request.Tracing.StartHarAsync(harPath, new() { Content = HarContentPolicy.Attach });
            await request.PostAsync(server.Prefix + "/post", new() { Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }, DataByte = Encoding.UTF8.GetBytes("{\"a\":1}") });
            await request.Tracing.StopHarAsync();
            await request.DisposeAsync();

            var resources = ParseHar(harPath);
            var log = JsonDocument.Parse(Encoding.UTF8.GetString(resources["har.har"])).RootElement.GetProperty("log");
            var entry = log.GetProperty("entries").EnumerateArray().First(e => e.GetProperty("request").GetProperty("url").GetString().EndsWith("/post", StringComparison.Ordinal));
            Assert.That(entry.GetProperty("request").GetProperty("postData").GetProperty("mimeType").GetString(), Is.EqualTo("application/json"));
        }
    }
}
