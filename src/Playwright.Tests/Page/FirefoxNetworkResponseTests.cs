/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
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

/// <summary>Port of tests/page/firefox-network-response.spec.ts.</summary>
public class FirefoxNetworkResponseTests
{
    [BeforeEach]
    public static void Skip(string browserName, bool isBidi) => It.Skip(browserName != "firefox" || isBidi, "Tests the Firefox network observer.");

    private static async Task ExpectHeaderFields(IResponse response, Header[] expected)
    {
        var headers = await response.HeadersArrayAsync();
        var allHeaders = await response.AllHeadersAsync();
        foreach (var name in new HashSet<string>(expected.Select(header => header.Name.ToLowerInvariant())))
        {
            var fields = expected.Where(header => header.Name.ToLowerInvariant() == name).ToArray();
            var values = fields.Select(header => header.Value).ToArray();
            ExpectToEqual(headers.Where(header => header.Name.ToLowerInvariant() == name), fields);
            Assert.That(await response.HeaderValuesAsync(name), Is.EqualTo(values));
            var combined = string.Join(name == "set-cookie" ? "\n" : ", ", values);
            Assert.That(await response.HeaderValueAsync(name), Is.EqualTo(combined));
            Assert.That(allHeaders[name], Is.EqualTo(combined));
        }
    }

    private static async Task<(IResponse Response, object Fetched)> FetchResponse(IPage page, string url)
    {
        var (response, fetched) = await PromiseAll(
            page.WaitForResponseAsync(url),
            page.EvaluateAsync<object>(@"async url => {
      const response = await fetch(url);
      return { status: response.status, body: await response.text() };
    }", url));
        return (response, fetched);
    }

    [UpstreamTest("FF-005 should preserve original response header fields for ${status} responses")]
    [Case(200)]
    [Case(302)]
    public async Task ShouldPreserveOriginalResponseHeaderFieldsForResponses(int status, IPage page, Server server)
    {
        var headers = new Header[]
        {
            new() { Name = "Date", Value = "Wed, 21 Oct 2037 07:28:00 GMT" },
            new() { Name = "X-Literal", Value = "first, \"second, third\"" },
            new() { Name = "X-Identical", Value = "same" },
            new() { Name = "X-Identical", Value = "same" },
            new() { Name = "X-Repeat", Value = "first, literal" },
            new() { Name = "x-repeat", Value = "second" },
            new() { Name = "Set-Cookie", Value = "a=b; Expires=Wed, 21 Oct 2037 07:28:00 GMT" },
            new() { Name = "Set-Cookie", Value = "c=d; Expires=Wed, 21 Oct 2037 07:28:00 GMT; Path=/" },
            new() { Name = "WWW-Authenticate", Value = "Digest realm=\"one, two\", qop=\"auth, auth-int\"" },
            new() { Name = "WWW-Authenticate", Value = "Basic realm=\"three, four\"" },
            new() { Name = "Proxy-Authenticate", Value = "Basic realm=\"five, six\"" },
            new() { Name = "Proxy-Authenticate", Value = "Basic realm=\"five, six\"" },
        };
        server.SetRoute("/headers", (request, response) =>
        {
            response.WriteHeadRaw(status, [
                "Content-Type", "text/html",
                .. status == 302 ? new[] { "Location", "/empty.html" } : [],
                .. headers.SelectMany(header => new[] { header.Name, header.Value }),
            ]);
            response.End("body");
        });
        var finalResponse = await page.GotoAsync(server.Prefix + "/headers");
        var response = status == 302 ? await finalResponse.Request.RedirectedFrom.ResponseAsync() : finalResponse;
        Assert.That(response.Status, Is.EqualTo(status));
        await ExpectHeaderFields(response, headers);
    }

    [UpstreamTest("FF-006 should preserve synthesized response header fields")]
    public async Task ShouldPreserveSynthesizedResponseHeaderFields(IPage page, Server server)
    {
        var cookies = new[]
        {
            "a=b; Expires=Wed, 21 Oct 2037 07:28:00 GMT",
            "c=d; Expires=Wed, 21 Oct 2037 07:28:00 GMT; Path=/",
        };
        await page.RouteAsync("**/headers", route => route.FulfillAsync(new()
        {
            ContentType = "text/html",
            Headers = new Dictionary<string, string>
            {
                ["X-Literal"] = "first, \"second, third\"",
                ["Date"] = "Wed, 21 Oct 2037 07:28:00 GMT",
                ["Set-Cookie"] = string.Join("\n", cookies),
                ["WWW-Authenticate"] = "Digest realm=\"one, two\", qop=\"auth, auth-int\"",
            },
            Body = "fulfilled",
        }));
        var response = await page.GotoAsync(server.Prefix + "/headers");
        Assert.That(await response.TextAsync(), Is.EqualTo("fulfilled"));
        await ExpectHeaderFields(response, [
            new() { Name = "x-literal", Value = "first, \"second, third\"" },
            new() { Name = "date", Value = "Wed, 21 Oct 2037 07:28:00 GMT" },
            .. cookies.Select(value => new Header { Name = "set-cookie", Value = value }),
            new() { Name = "www-authenticate", Value = "Digest realm=\"one, two\", qop=\"auth, auth-int\"" },
        ]);
    }

    [UpstreamTest("FF-007 should report current effective fields after cache revalidation")]
    public async Task ShouldReportCurrentEffectiveFieldsAfterCacheRevalidation(IPage page, Server server)
    {
        var validators = new List<string>();
        server.SetRoute("/cached-headers", (request, response) =>
        {
            validators.Add(request.Headers["if-none-match"]);
            if (validators.Count == 1)
            {
                response.WriteHead(200, new()
                {
                    ["Content-Type"] = "text/plain",
                    ["ETag"] = "\"v1\"",
                    ["Cache-Control"] = "max-age=0, must-revalidate",
                    ["X-Keep"] = new[] { "first, literal", "second" },
                    ["X-Change"] = new[] { "old, literal", "old-second" },
                    ["Set-Cookie"] = new[] { "old=value; Expires=Wed, 21 Oct 2037 07:28:00 GMT", "old2=value; Path=/" },
                    ["WWW-Authenticate"] = new[] { "Basic realm=\"old, realm\"", "Basic realm=\"old-second\"" },
                    ["Proxy-Authenticate"] = new[] { "Basic realm=\"old, proxy\"", "Basic realm=\"old-proxy-second\"" },
                });
                response.End("cached-body");
                return;
            }
            response.WriteHead(304, new()
            {
                ["Cache-Control"] = "max-age=3600",
                ["X-Change"] = new[] { "new, literal", "new-second" },
                ["Set-Cookie"] = new[] { "new=value; Expires=Wed, 21 Oct 2037 07:28:00 GMT", "new2=value; Path=/" },
                ["WWW-Authenticate"] = new[] { "Basic realm=\"new, realm\"", "Basic realm=\"new-second\"" },
                ["Proxy-Authenticate"] = new[] { "Basic realm=\"new, proxy\"", "Basic realm=\"new-proxy-second\"" },
            });
            response.End();
        });

        await page.GotoAsync(server.EmptyPage);
        var url = server.Prefix + "/cached-headers";
        var initial = await FetchResponse(page, url);
        var revalidated = await FetchResponse(page, url);
        var cached = await FetchResponse(page, url);

        Assert.That(validators, Is.EqualTo(new[] { null, "\"v1\"" }));
        foreach (var (_, fetched) in new[] { initial, revalidated, cached })
        {
            ExpectToEqual(fetched, new { status = 200, body = "cached-body" });
        }
        Assert.That(new[] { initial.Response.Status, revalidated.Response.Status, cached.Response.Status }, Is.EqualTo(new[] { 200, 304, 200 }));
        Assert.That(await initial.Response.HeaderValueAsync("x-change"), Is.EqualTo("old, literal, old-second"));
        Assert.That(await revalidated.Response.HeaderValuesAsync("x-keep"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await revalidated.Response.HeaderValuesAsync("x-change"), Is.EqualTo(new[] { "new, literal", "new-second" }));
        Assert.That(await revalidated.Response.HeaderValuesAsync("set-cookie"), Is.EqualTo(new[]
        {
            "new=value; Expires=Wed, 21 Oct 2037 07:28:00 GMT",
            "new2=value; Path=/",
        }));
        Assert.That(await revalidated.Response.HeaderValuesAsync("www-authenticate"), Is.EqualTo(new[] { "Basic realm=\"new, realm\"", "Basic realm=\"new-second\"" }));
        Assert.That(await revalidated.Response.HeaderValuesAsync("proxy-authenticate"), Is.EqualTo(new[] { "Basic realm=\"new, proxy\"", "Basic realm=\"new-proxy-second\"" }));
        Assert.That(await cached.Response.HeaderValuesAsync("x-keep"), Is.EqualTo(new[] { "first, literal, second" }));
        Assert.That(await cached.Response.HeaderValuesAsync("x-change"), Is.EqualTo(new[] { "new, literal, new-second" }));
        foreach (var name in new[] { "set-cookie", "www-authenticate", "proxy-authenticate" })
        {
            Assert.That(await cached.Response.HeaderValuesAsync(name), Is.EqualTo(Array.Empty<string>()));
        }
    }

    [UpstreamTest("FF-008 should report a standalone 304 response without waiting for a cache merge")]
    public async Task ShouldReportAStandalone304ResponseWithoutWaitingForACacheMerge(IPage page, Server server)
    {
        server.SetRoute("/standalone-304", (request, response) =>
        {
            response.WriteHead(304, new()
            {
                ["X-Literal"] = "standalone, value",
                ["Cache-Control"] = "no-store",
            });
            response.End();
        });
        await page.GotoAsync(server.EmptyPage);
        var (response, fetched) = await FetchResponse(page, server.Prefix + "/standalone-304");
        Assert.That(response.Status, Is.EqualTo(304));
        ExpectToEqual(fetched, new { status = 304, body = "" });
        Assert.That(await response.HeaderValuesAsync("x-literal"), Is.EqualTo(new[] { "standalone, value" }));
        Assert.That(await response.FinishedAsync(), Is.Null);
    }
}
