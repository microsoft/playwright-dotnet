/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
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
using System.Web;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-fetch.spec.ts.</summary>
public class BrowsercontextFetchTests : UpstreamBrowserTest
{
    [SetUp]
    public void Skip() => Info.Skip(Mode != "default");

    // .NET: context.request[method](url, options).
    private static Task<IAPIResponse> CallMethod(IAPIRequestContext request, string method, string url, APIRequestContextOptions options) => method switch
    {
        "fetch" => request.FetchAsync(url, options),
        "delete" => request.DeleteAsync(url, options),
        "get" => request.GetAsync(url, options),
        "head" => request.HeadAsync(url, options),
        "patch" => request.PatchAsync(url, options),
        "post" => request.PostAsync(url, options),
        "put" => request.PutAsync(url, options),
        _ => throw new ArgumentException(method, nameof(method)),
    };

    // .NET: the text of a URLSearchParams, which is also url.search without the '?'.
    private static string SearchParamsToString(IEnumerable<KeyValuePair<string, object>> searchParams) =>
        string.Join("&", searchParams.Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString((string)pair.Value)));

    [UpstreamTest("FE-088 get should work @smoke")]
    public async Task GetShouldWork()
    {
        var context = await ContextFixture();
        var response = await context.APIRequest.GetAsync(Server.Prefix + "/simple.json");
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/simple.json"));
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.StatusText, Is.EqualTo("OK"));
        Assert.That(response.Ok, Is.True);
        Assert.That(response.Headers["content-type"], Is.EqualTo("application/json; charset=utf-8"));
        Assert.That(response.HeadersArray, Has.Some.Matches<Header>(header => DeepEquals(header, new { name = "Content-Type", value = "application/json; charset=utf-8" })));
        Assert.That(await response.TextAsync(), Is.EqualTo("{\"foo\": \"bar\"}\n"));
    }

    [UpstreamTest("FE-089 fetch should work")]
    public async Task FetchShouldWork()
    {
        var context = await ContextFixture();
        var response = await context.APIRequest.FetchAsync(Server.Prefix + "/simple.json");
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/simple.json"));
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.StatusText, Is.EqualTo("OK"));
        Assert.That(response.Ok, Is.True);
        Assert.That(response.Headers["content-type"], Is.EqualTo("application/json; charset=utf-8"));
        Assert.That(response.HeadersArray, Has.Some.Matches<Header>(header => DeepEquals(header, new { name = "Content-Type", value = "application/json; charset=utf-8" })));
        Assert.That(await response.TextAsync(), Is.EqualTo("{\"foo\": \"bar\"}\n"));
    }

    [UpstreamTest("FE-090 should return timing")]
    [LocalFixme("dotnet: RequestTimingResult.StartTime is a float, which holds a time in milliseconds since the epoch in steps of 131072 only: it is up to 65 seconds away from the time of the request, upstream expects it within 5 seconds of Date.now(). The test passes by chance when the request starts within 5 seconds of such a step, about one run in 13")]
    public async Task ShouldReturnTiming()
    {
        var context = await ContextFixture();
        // Create a fresh server to guarantee a new connection, because keep-alive
        // sockets from other tests do not have dns/connect timings.
        // .NET: http.createServer((req, res) => res.end('Hello')) that listens on the port 0 is a Server with a route.
        var httpServer = Server.Create(Env.AssetsDir, 0);
        httpServer.SetRoute("/", (req, res) => res.End("Hello"));
        var port = httpServer.Port;
        try
        {
            var response = await context.APIRequest.GetAsync($"http://localhost:{port}/");
            Assert.That(response.Ok, Is.True);
            var timing = response.Timing;
            Assert.That(timing.StartTime, Is.EqualTo((double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).Within(0.5 * Math.Pow(10, 4)));
            Assert.That(timing.DomainLookupStart, Is.EqualTo(0d));
            Assert.That(timing.DomainLookupEnd, Is.GreaterThanOrEqualTo(timing.DomainLookupStart));
            Assert.That(timing.ConnectStart, Is.EqualTo(timing.DomainLookupEnd));
            Assert.That(timing.SecureConnectionStart, Is.EqualTo(-1d));
            Assert.That(timing.ConnectEnd, Is.GreaterThanOrEqualTo(timing.ConnectStart));
            Assert.That(timing.RequestStart, Is.EqualTo(timing.ConnectEnd));
            Assert.That(timing.ResponseStart, Is.GreaterThanOrEqualTo(timing.RequestStart));
            Assert.That(timing.ResponseEnd, Is.GreaterThanOrEqualTo(timing.ResponseStart));
            Assert.That(timing.ResponseEnd, Is.LessThan(60_000d));
        }
        finally
        {
            await httpServer.StopAsync();
        }
    }

    [UpstreamTest("FE-091 should return timing for https")]
    [LocalFixme("dotnet: RequestTimingResult.StartTime is a float, which holds a time in milliseconds since the epoch in steps of 131072 only: it is up to 65 seconds away from the time of the request, upstream expects it within 5 seconds of Date.now(). The test passes by chance when the request starts within 5 seconds of such a step, about one run in 13")]
    public async Task ShouldReturnTimingForHttps()
    {
        var context = await ContextFixture();
        // Create a fresh server to guarantee a new connection, because keep-alive
        // sockets from other tests do not have dns/connect timings.
        // .NET: https.createServer(await TestServer.certOptions(), (req, res) => res.end('Hello')) that listens on the port 0 is a Server with a route.
        var httpsServer = Server.CreateHttps(Env.AssetsDir, 0);
        httpsServer.SetRoute("/", (req, res) => res.End("Hello"));
        var port = httpsServer.Port;
        try
        {
            var response = await context.APIRequest.GetAsync($"https://localhost:{port}/", new() { IgnoreHTTPSErrors = true });
            Assert.That(response.Ok, Is.True);
            var timing = response.Timing;
            Assert.That(timing.StartTime, Is.EqualTo((double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).Within(0.5 * Math.Pow(10, 4)));
            Assert.That(timing.DomainLookupStart, Is.EqualTo(0d));
            Assert.That(timing.DomainLookupEnd, Is.GreaterThanOrEqualTo(timing.DomainLookupStart));
            Assert.That(timing.ConnectStart, Is.EqualTo(timing.DomainLookupEnd));
            Assert.That(timing.SecureConnectionStart, Is.GreaterThanOrEqualTo(timing.ConnectStart));
            Assert.That(timing.ConnectEnd, Is.GreaterThanOrEqualTo(timing.SecureConnectionStart));
            Assert.That(timing.RequestStart, Is.EqualTo(timing.ConnectEnd));
            Assert.That(timing.ResponseStart, Is.GreaterThanOrEqualTo(timing.RequestStart));
            Assert.That(timing.ResponseEnd, Is.GreaterThanOrEqualTo(timing.ResponseStart));
        }
        finally
        {
            await httpsServer.StopAsync();
        }
    }

    [UpstreamTest("FE-092 should throw on network error")]
    [LocalFixme("dotnet: the message is 'socket hang up (ECONNRESET)', without the 'apiRequestContext.get: ' prefix of the call")]
    public async Task ShouldThrowOnNetworkError()
    {
        var context = await ContextFixture();
        Server.SetRoute("/test", (req, res) =>
        {
            req.Socket.Destroy();
        });
        var error = await Catch(() => context.APIRequest.GetAsync(Server.Prefix + "/test"));
        Assert.That(error.Message, Does.Contain("apiRequestContext.get: socket hang up"));
    }

    [UpstreamTest("FE-093 should throw on network error after redirect")]
    [LocalFixme("dotnet: the message is 'socket hang up (ECONNRESET)', without the 'apiRequestContext.get: ' prefix of the call")]
    public async Task ShouldThrowOnNetworkErrorAfterRedirect()
    {
        var context = await ContextFixture();
        Server.SetRedirect("/redirect", "/test");
        Server.SetRoute("/test", (req, res) =>
        {
            req.Socket.Destroy();
        });
        var error = await Catch(() => context.APIRequest.GetAsync(Server.Prefix + "/redirect"));
        Assert.That(error.Message, Does.Contain("apiRequestContext.get: socket hang up"));
    }

    [UpstreamTest("FE-094 should throw on network error when sending body")]
    [LocalFixme("dotnet: the message is 'aborted', without the 'apiRequestContext.get: ' prefix of the call")]
    public async Task ShouldThrowOnNetworkErrorWhenSendingBody()
    {
        var context = await ContextFixture();
        Server.SetRoute("/test", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["content-length"] = 4096,
                ["content-type"] = "text/html",
            });
            res.Write("<title>A");
            res.Uncork();
            req.Socket.Destroy();
        });
        var error = await Catch(() => context.APIRequest.GetAsync(Server.Prefix + "/test"));
        Assert.That(error.Message, Does.Contain("apiRequestContext.get: aborted"));
    }

    [UpstreamTest("FE-095 should throw on network error when sending body after redirect")]
    [LocalFixme("dotnet: the message is 'aborted', without the 'apiRequestContext.get: ' prefix of the call")]
    public async Task ShouldThrowOnNetworkErrorWhenSendingBodyAfterRedirect()
    {
        var context = await ContextFixture();
        Server.SetRedirect("/redirect", "/test");
        Server.SetRoute("/test", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["content-length"] = 4096,
                ["content-type"] = "text/html",
            });
            res.Write("<title>A");
            res.Uncork();
            req.Socket.Destroy();
        });
        var error = await Catch(() => context.APIRequest.GetAsync(Server.Prefix + "/redirect"));
        Assert.That(error.Message, Does.Contain("apiRequestContext.get: aborted"));
    }

    [UpstreamTest("FE-098 ${method} should support params passed as object")]
    [TestCase("fetch")]
    [TestCase("delete")]
    [TestCase("get")]
    [TestCase("head")]
    [TestCase("patch")]
    [TestCase("post")]
    [TestCase("put")]
    public async Task ShouldSupportParamsPassedAsObject(string method)
    {
        var context = await ContextFixture();
        var url = new Uri(Server.EmptyPage);
        // .NET: url.searchParams.set(name, value) adds to a list of pairs.
        var searchParams = new List<KeyValuePair<string, object>>();
        searchParams.Add(new("param1", "value1"));
        searchParams.Add(new("парам2", "знач2"));

        var (request, response) = await PromiseAll(
            // .NET: url.pathname + url.search.
            Server.WaitForRequest(url.AbsolutePath + "?" + SearchParamsToString(searchParams)),
            CallMethod(context.APIRequest, method, Server.EmptyPage, new()
            {
                Params = new Dictionary<string, object>
                {
                    ["param1"] = "value1",
                    ["парам2"] = "знач2",
                },
            }));

        var requestParams = HttpUtility.ParseQueryString(request.Url.Substring(request.Url.IndexOf('?')));
        Assert.That(requestParams.Get("param1"), Is.EqualTo("value1"));
        Assert.That(requestParams.Get("парам2"), Is.EqualTo("знач2"));

        var responseParams = HttpUtility.ParseQueryString(new Uri(response.Url).Query);
        Assert.That(responseParams.Get("param1"), Is.EqualTo("value1"));
        Assert.That(responseParams.Get("парам2"), Is.EqualTo("знач2"));
    }

    [UpstreamTest("FE-099 ${method} should support params passed as URLSearchParams")]
    [LocalFixme("dotnet: Params with the same name twice throws 'ArgumentException: An item with the same key has already been added. Key: param1': APIRequestContext.FetchAsync makes a dictionary of the params; upstream sends param1=value1&param1=value2")]
    [TestCase("fetch")]
    [TestCase("delete")]
    [TestCase("get")]
    [TestCase("head")]
    [TestCase("patch")]
    [TestCase("post")]
    [TestCase("put")]
    public async Task ShouldSupportParamsPassedAsURLSearchParams(string method)
    {
        var context = await ContextFixture();
        var url = new Uri(Server.EmptyPage);
        // .NET: a URLSearchParams is a list of pairs.
        var searchParams = new List<KeyValuePair<string, object>>();
        searchParams.Add(new("param1", "value1"));
        searchParams.Add(new("param1", "value2"));
        searchParams.Add(new("парам2", "знач2"));

        var (request, response) = await PromiseAll(
            Server.WaitForRequest(url.AbsolutePath + "?" + SearchParamsToString(searchParams)),
            CallMethod(context.APIRequest, method, Server.EmptyPage, new() { Params = searchParams }));

        var requestParams = HttpUtility.ParseQueryString(request.Url.Substring(request.Url.IndexOf('?')));
        Assert.That(requestParams.GetValues("param1"), Is.EqualTo(new[] { "value1", "value2" }));
        Assert.That(requestParams.Get("парам2"), Is.EqualTo("знач2"));

        var responseParams = HttpUtility.ParseQueryString(new Uri(response.Url).Query);
        Assert.That(responseParams.GetValues("param1"), Is.EqualTo(new[] { "value1", "value2" }));
        Assert.That(responseParams.Get("парам2"), Is.EqualTo("знач2"));
    }

    [UpstreamTest("FE-100 ${method} should support params passed as string")]
    [TestCase("fetch")]
    [TestCase("delete")]
    [TestCase("get")]
    [TestCase("head")]
    [TestCase("patch")]
    [TestCase("post")]
    [TestCase("put")]
    public async Task ShouldSupportParamsPassedAsString(string method)
    {
        var context = await ContextFixture();
        var url = new Uri(Server.EmptyPage);
        var @params = "?param1=value1&param1=value2&парам2=знач2";

        var (request, response) = await PromiseAll(
#pragma warning disable SYSLIB0013
            // .NET: encodeURI(params).
            Server.WaitForRequest(url.AbsolutePath + Uri.EscapeUriString(@params)),
#pragma warning restore SYSLIB0013
            CallMethod(context.APIRequest, method, Server.EmptyPage, new() { ParamsString = @params }));

        var requestParams = HttpUtility.ParseQueryString(request.Url.Substring(request.Url.IndexOf('?')));
        Assert.That(requestParams.GetValues("param1"), Is.EqualTo(new[] { "value1", "value2" }));
        Assert.That(requestParams.Get("парам2"), Is.EqualTo("знач2"));

        var responseParams = HttpUtility.ParseQueryString(new Uri(response.Url).Query);
        Assert.That(responseParams.GetValues("param1"), Is.EqualTo(new[] { "value1", "value2" }));
        Assert.That(responseParams.Get("парам2"), Is.EqualTo("знач2"));
    }

    [UpstreamTest("FE-101 ${method} should append string and URLSearchParams params to the URL query")]
    [TestCase("fetch")]
    [TestCase("delete")]
    [TestCase("get")]
    [TestCase("head")]
    [TestCase("patch")]
    [TestCase("post")]
    [TestCase("put")]
    public async Task ShouldAppendStringAndURLSearchParamsParamsToTheURLQuery(string method)
    {
        var context = await ContextFixture();
        // .NET: a string goes to ParamsString and a URLSearchParams to Params, so the loop is over the options.
        foreach (var options in new APIRequestContextOptions[] { new() { ParamsString = "q=cat" }, new() { ParamsString = "?q=cat" }, new() { Params = new KeyValuePair<string, object>[] { new("q", "cat") } } })
        {
            var (request, _) = await PromiseAll(
                Server.WaitForRequest("/empty.html?page=2&q=cat"),
                CallMethod(context.APIRequest, method, Server.EmptyPage + "?page=2", options));
            Assert.That(request.Url, Is.EqualTo("/empty.html?page=2&q=cat"));
        }
    }

    [UpstreamTest("FE-102 ${method} should support failOnStatusCode")]
    [TestCase("fetch")]
    [TestCase("delete")]
    [TestCase("get")]
    [TestCase("head")]
    [TestCase("patch")]
    [TestCase("post")]
    [TestCase("put")]
    public async Task ShouldSupportFailOnStatusCode(string method)
    {
        var context = await ContextFixture();
        var error = await Catch(() => CallMethod(context.APIRequest, method, Server.Prefix + "/does-not-exist.html", new()
        {
            FailOnStatusCode = true,
        }));
        Assert.That(error.Message, Does.Contain("404 Not Found"));
        if (method != "head")
        {
            Assert.That(error.Message, Does.Contain("Response text:\nFile not found:"));
        }
    }

    [UpstreamTest("FE-103 ${method}should support ignoreHTTPSErrors option")]
    [TestCase("fetch")]
    [TestCase("delete")]
    [TestCase("get")]
    [TestCase("head")]
    [TestCase("patch")]
    [TestCase("post")]
    [TestCase("put")]
    public async Task ShouldSupportIgnoreHTTPSErrorsOption(string method)
    {
        var context = await ContextFixture();
        var response = await CallMethod(context.APIRequest, method, HttpsServer.EmptyPage, new() { IgnoreHTTPSErrors = true });
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("FE-106 should follow redirects correctly when Location header contains UTF-8 characters")]
    public async Task ShouldFollowRedirectsCorrectlyWhenLocationHeaderContainsUTF8Characters()
    {
        var context = await ContextFixture();
        Server.SetRoute("/redirect", (req, res) =>
        {
            // Node.js only allows US-ASCII, so we can't send invalid headers directly. Sending it as a raw response instead.
            res.Socket.Write("HTTP/1.1 301 Moved Permanently\r\n");
            res.Socket.Write($"Location: {Server.Prefix}/empty.html?message=マスクПривет\r\n");
            res.Socket.Write("\r\n");
            res.Socket.Uncork();
            res.Socket.End();
        });
        var response = await context.APIRequest.GetAsync(Server.Prefix + "/redirect");
        // .NET: new URLSearchParams({ message: 'マスクПривет' }) is a list of pairs.
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/empty.html?" + SearchParamsToString(new KeyValuePair<string, object>[] { new("message", "マスクПривет") })));
    }

    [UpstreamTest("FE-107 should add cookies from Set-Cookie header")]
    public async Task ShouldAddCookiesFromSetCookieHeader()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "session=value", "foo=bar; max-age=3600" });
            res.End();
        });
        await context.APIRequest.GetAsync(Server.Prefix + "/setcookie.html");
        var cookies = await context.CookiesAsync();
        // .NET: toEqual() of two Sets is Is.EquivalentTo, with the comparison of toEqual().
        Assert.That(cookies.Select(c => new { name = c.Name, value = c.Value }), Is.EquivalentTo(new object[]
        {
            new
            {
                name = "session",
                value = "value",
            },
            new
            {
                name = "foo",
                value = "bar",
            },
        }).Using<object>(DeepEquals));
        await page.GotoAsync(Server.EmptyPage);
        Assert.That((await page.EvaluateAsync<string>("() => document.cookie")).Split(';').Select(s => s.Trim()).Order(StringComparer.Ordinal), Is.EqualTo(new[] { "foo=bar", "session=value" }));
    }

    [UpstreamTest("FE-108 page.request.addCookies should add cookies to the browser context")]
    public async Task PageRequestAddCookiesShouldAddCookiesToTheBrowserContext()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await page.APIRequest.AddCookiesAsync(new[]
        {
            new Cookie { Name = "a", Value = "b", Url = Server.EmptyPage },
            new Cookie { Name = "c", Value = "d", Domain = "localhost", Path = "/", Expires = (float)((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d) + 3600) },
        });
        // .NET: sort((a, b) => a.name.localeCompare(b.name)) is a stable sort with the comparison of a culture.
        ExpectToEqual((await context.CookiesAsync()).Select(c => new { name = c.Name, value = c.Value }).OrderBy(a => a.name, StringComparer.InvariantCulture), new[]
        {
            new { name = "a", value = "b" },
            new { name = "c", value = "d" },
        });
        var (req, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            context.APIRequest.GetAsync(Server.EmptyPage));
        Assert.That(req.Headers["cookie"].Split(';').Select(s => s.Trim()).Order(StringComparer.Ordinal), Is.EqualTo(new[] { "a=b", "c=d" }));
        await page.GotoAsync(Server.EmptyPage);
        Assert.That((await page.EvaluateAsync<string>("() => document.cookie")).Split(';').Select(s => s.Trim()).Order(StringComparer.Ordinal), Is.EqualTo(new[] { "a=b", "c=d" }));
    }

    [UpstreamTest("FE-109 page.request.cookies should return browser context cookies")]
    public async Task PageRequestCookiesShouldReturnBrowserContextCookies()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await context.AddCookiesAsync(new[]
        {
            new Cookie { Name = "a", Value = "b", Url = Server.EmptyPage },
            new Cookie { Name = "c", Value = "d", Domain = "example.com", Path = "/" },
        });
        Assert.That((await page.APIRequest.CookiesAsync()).Select(c => c.Name).Order(StringComparer.Ordinal), Is.EqualTo(new[] { "a", "c" }));
        Assert.That((await page.APIRequest.CookiesAsync(Server.EmptyPage)).Select(c => c.Name), Is.EqualTo(new[] { "a" }));
        ExpectToEqual(await page.APIRequest.CookiesAsync(Server.EmptyPage), await context.CookiesAsync(Server.EmptyPage));
    }

    [UpstreamTest("FE-110 page.request.clearCookies should clear browser context cookies")]
    public async Task PageRequestClearCookiesShouldClearBrowserContextCookies()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await context.AddCookiesAsync(new[]
        {
            new Cookie { Name = "a", Value = "b", Url = Server.EmptyPage },
            new Cookie { Name = "c", Value = "d", Url = Server.EmptyPage },
        });
        await page.APIRequest.ClearCookiesAsync(new() { Name = "a" });
        Assert.That((await context.CookiesAsync()).Select(c => c.Name), Is.EqualTo(new[] { "c" }));
        await page.APIRequest.ClearCookiesAsync();
        Assert.That(await context.CookiesAsync(), Is.Empty);
        var (req, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            page.APIRequest.GetAsync(Server.EmptyPage));
        Assert.That(req.Headers["cookie"], Is.Null);
    }

    [UpstreamTest("FE-111 should preserve cookie order from Set-Cookie header")]
    public async Task ShouldPreserveCookieOrderFromSetCookieHeader()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "cookie.0=foo", "cookie.1=bar" });
            res.End();
        });
        await page.APIRequest.GetAsync(Server.Prefix + "/setcookie.html");
        var cookies = await context.CookiesAsync();
        ExpectToEqual(cookies.Select(c => new { name = c.Name, value = c.Value }), new[]
        {
            new
            {
                name = "cookie.0",
                value = "foo",
            },
            new
            {
                name = "cookie.1",
                value = "bar",
            },
        });
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("cookie.0=foo; cookie.1=bar"));
        var requestPromise = Server.WaitForRequest("/empty.html");
        await page.APIRequest.GetAsync(Server.EmptyPage);
        var request = await requestPromise;
        Assert.That(request.Headers["cookie"], Is.EqualTo("cookie.0=foo; cookie.1=bar"));
    }

    [UpstreamTest("FE-112 should support cookie with empty value")]
    public async Task ShouldSupportCookieWithEmptyValue()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "first=" });
            res.End();
        });
        await context.APIRequest.GetAsync(Server.Prefix + "/setcookie.html");
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("first="));
        var cookies = await context.CookiesAsync();
        ExpectToEqual(cookies.Select(c => new { name = c.Name, value = c.Value }), new[]
        {
            new
            {
                name = "first",
                value = "",
            },
        });
    }

    [UpstreamTest("FE-113 should not lose body while handling Set-Cookie header")]
    public async Task ShouldNotLoseBodyWhileHandlingSetCookieHeader()
    {
        var context = await ContextFixture();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "session=value", "foo=bar; max-age=3600" });
            res.End("text content");
        });
        var response = await context.APIRequest.GetAsync(Server.Prefix + "/setcookie.html");
        Assert.That(await response.TextAsync(), Is.EqualTo("text content"));
    }

    [UpstreamTest("FE-114 should remove cookie with negative max-age")]
    public async Task ShouldRemoveCookieWithNegativeMaxAge()
    {
        var page = await PageFixture();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=v; max-age=100000", "b=v; max-age=100000", "c=v" });
            res.End();
        });
        Server.SetRoute("/removecookie.html", (req, res) =>
        {
            var maxAge = -2 * DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            res.SetHeader("Set-Cookie", new[] { $"a=v; max-age={maxAge}", "b=v; max-age=-1" });
            res.End();
        });
        await page.APIRequest.GetAsync($"{Server.Prefix}/setcookie.html");
        await page.APIRequest.GetAsync($"{Server.Prefix}/removecookie.html");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            page.APIRequest.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("c=v"));
    }

    [UpstreamTest("FE-115 should remove cookie with expires far in the past")]
    public async Task ShouldRemoveCookieWithExpiresFarInThePast()
    {
        var page = await PageFixture();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=v; max-age=1000000" });
            res.End();
        });
        Server.SetRoute("/removecookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=v; expires=Wed, 01 Jan 1000 00:00:00 GMT" });
            res.End();
        });
        await page.APIRequest.GetAsync($"{Server.Prefix}/setcookie.html");
        await page.APIRequest.GetAsync($"{Server.Prefix}/removecookie.html");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            page.APIRequest.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.Null.Or.Empty);
    }

    [UpstreamTest("FE-116 should handle cookies on redirects")]
    public async Task ShouldHandleCookiesOnRedirects()
    {
        var context = await ContextFixture();
        Server.SetRoute("/redirect1", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "r1=v1;SameSite=Lax");
            res.WriteHead(301, new() { ["location"] = "/a/b/redirect2" });
            res.End();
        });
        Server.SetRoute("/a/b/redirect2", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "r2=v2;SameSite=Lax");
            res.WriteHead(302, new() { ["location"] = "/title.html" });
            res.End();
        });
        {
            var (req1, req2, req3, _) = await PromiseAll(
                Server.WaitForRequest("/redirect1"),
                Server.WaitForRequest("/a/b/redirect2"),
                Server.WaitForRequest("/title.html"),
                context.APIRequest.GetAsync($"{Server.Prefix}/redirect1"));
            Assert.That(req1.Headers["cookie"], Is.Null.Or.Empty);
            Assert.That(req2.Headers["cookie"], Is.EqualTo("r1=v1"));
            Assert.That(req3.Headers["cookie"], Is.EqualTo("r1=v1"));
        }
        {
            var (req1, req2, req3, _) = await PromiseAll(
                Server.WaitForRequest("/redirect1"),
                Server.WaitForRequest("/a/b/redirect2"),
                Server.WaitForRequest("/title.html"),
                context.APIRequest.GetAsync($"{Server.Prefix}/redirect1"));
            Assert.That(req1.Headers["cookie"], Is.EqualTo("r1=v1"));
            Assert.That(req2.Headers["cookie"].Split(';').Select(s => s.Trim()).Order(StringComparer.Ordinal), Is.EqualTo(new[] { "r1=v1", "r2=v2" }));
            Assert.That(req3.Headers["cookie"], Is.EqualTo("r1=v1"));
        }
        var cookies = await context.CookiesAsync();
        // .NET: toEqual() of two Sets is Is.EquivalentTo, with the comparison of toEqual().
        Assert.That(cookies, Is.EquivalentTo(new object[]
        {
            new
            {
                sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Lax",
                name = "r2",
                value = "v2",
                domain = Server.Hostname,
                path = "/a/b",
                expires = -1,
                httpOnly = false,
                secure = false,
            },
            new
            {
                sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Lax",
                name = "r1",
                value = "v1",
                domain = Server.Hostname,
                path = "/",
                expires = -1,
                httpOnly = false,
                secure = false,
            },
        }).Using<object>(DeepEquals));
    }

    [UpstreamTest("FE-117 should return raw headers")]
    public async Task ShouldReturnRawHeaders()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        Server.SetRoute("/headers", (req, res) =>
        {
            // Headers array is only supported since Node v14.14.0 so we write directly to the socket.
            // res.writeHead(200, ['name-a', 'v1','name-b', 'v4','Name-a', 'v2', 'name-A', 'v3']);
            var conn = res.Connection;
            conn.Write("HTTP/1.1 200 OK\r\n");
            conn.Write("Name-A: v1\r\n");
            conn.Write("name-b: v4\r\n");
            conn.Write("Name-a: v2\r\n");
            conn.Write("name-A: v3\r\n");
            conn.Write("\r\n");
            conn.Uncork();
            conn.End();
        });
        var response = await context.APIRequest.GetAsync($"{Server.Prefix}/headers");
        Assert.That(response.Status, Is.EqualTo(200));
        var headers = response.HeadersArray.Where(header => header.Name.ToLowerInvariant().Contains("name-", StringComparison.Ordinal));
        ExpectToEqual(headers, new[] { new { name = "Name-A", value = "v1" }, new { name = "name-b", value = "v4" }, new { name = "Name-a", value = "v2" }, new { name = "name-A", value = "v3" } });
        // Comma separated values, this matches Response.headers()
        Assert.That(response.Headers["name-a"], Is.EqualTo("v1, v2, v3"));
        Assert.That(response.Headers["name-b"], Is.EqualTo("v4"));
    }

    [UpstreamTest("FE-118 should work with http credentials")]
    public async Task ShouldWorkWithHttpCredentials()
    {
        var context = await ContextFixture();
        Server.SetAuth("/empty.html", "user", "pass");

        var (request, response) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            context.APIRequest.GetAsync(Server.EmptyPage, new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass")),
                },
            }));
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(request.Url, Is.EqualTo("/empty.html"));
    }

    [UpstreamTest("FE-121 should support multiple httpCredentials")]
    public async Task ShouldSupportMultipleHttpCredentials()
    {
        Server.SetAuth("/empty.html", "user1", "pass1");
        var context = await ContextFactory(new()
        {
            HttpCredentialsList = new[]
            {
                new HttpCredentials { Username = "user1", Password = "pass1", Origin = Server.Prefix },
                new HttpCredentials { Username = "user2", Password = "pass2", Origin = Server.CrossProcessPrefix },
            },
        });
        var response1 = await context.APIRequest.GetAsync(Server.EmptyPage);
        Assert.That(response1.Status, Is.EqualTo(200));
        // Wrong credentials are picked for the other origin.
        var response2 = await context.APIRequest.GetAsync(Server.CrossProcessPrefix + "/empty.html");
        Assert.That(response2.Status, Is.EqualTo(401));
    }

    [UpstreamTest("FE-122 should support HTTPCredentials.send for newContext")]
    public async Task ShouldSupportHTTPCredentialsSendForNewContext()
    {
        var context = await ContextFactory(new()
        {
            HttpCredentials = new() { Username = "user", Password = "pass", Origin = Server.Prefix.ToUpperInvariant(), Send = HttpCredentialsSend.Always },
        });
        {
            var (serverRequest, response) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                context.APIRequest.GetAsync(Server.EmptyPage));
            Assert.That(serverRequest.Headers["authorization"], Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"))));
            Assert.That(response.Status, Is.EqualTo(200));
        }
        {
            var (serverRequest, response) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                context.APIRequest.GetAsync(Server.CrossProcessPrefix + "/empty.html"));
            // Not sent to another origin.
            Assert.That(serverRequest.Headers["authorization"], Is.Null);
            Assert.That(response.Status, Is.EqualTo(200));
        }
    }

    [UpstreamTest("FE-123 should support HTTPCredentials.send for browser.newPage")]
    public async Task ShouldSupportHTTPCredentialsSendForBrowserNewPage()
    {
        var page = await Browser.NewPageAsync(new(NewPageOptions())
        {
            HttpCredentials = new() { Username = "user", Password = "pass", Origin = Server.Prefix.ToUpperInvariant(), Send = HttpCredentialsSend.Always },
        });
        {
            var (serverRequest, response) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                page.APIRequest.GetAsync(Server.EmptyPage));
            Assert.That(serverRequest.Headers["authorization"], Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"))));
            Assert.That(response.Status, Is.EqualTo(200));
        }
        {
            var (serverRequest, response) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                page.APIRequest.GetAsync(Server.CrossProcessPrefix + "/empty.html"));
            // Not sent to another origin.
            Assert.That(serverRequest.Headers["authorization"], Is.Null);
            Assert.That(response.Status, Is.EqualTo(200));
        }
        await page.CloseAsync();
    }

    [UpstreamTest("FE-124 delete should support post data")]
    public async Task DeleteShouldSupportPostData()
    {
        var context = await ContextFixture();
        var (request, response) = await PromiseAll(
            Server.WaitForRequest("/simple.json"),
            context.APIRequest.DeleteAsync($"{Server.Prefix}/simple.json", new()
            {
                DataString = "My request",
            }));
        Assert.That(request.Method, Is.EqualTo("DELETE"));
        Assert.That(Encoding.UTF8.GetString(await request.PostBody), Is.EqualTo("My request"));
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(request.Url, Is.EqualTo("/simple.json"));
    }

    [UpstreamTest("FE-125 get should support post data")]
    public async Task GetShouldSupportPostData()
    {
        var context = await ContextFixture();
        var (request, response) = await PromiseAll(
            Server.WaitForRequest("/simple.json"),
            context.APIRequest.GetAsync($"{Server.Prefix}/simple.json", new()
            {
                DataString = "My request",
            }));
        Assert.That(request.Method, Is.EqualTo("GET"));
        Assert.That(Encoding.UTF8.GetString(await request.PostBody), Is.EqualTo("My request"));
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(request.Url, Is.EqualTo("/simple.json"));
    }

    [UpstreamTest("FE-126 head should support post data")]
    public async Task HeadShouldSupportPostData()
    {
        var context = await ContextFixture();
        var (request, response) = await PromiseAll(
            Server.WaitForRequest("/simple.json"),
            context.APIRequest.HeadAsync($"{Server.Prefix}/simple.json", new()
            {
                DataString = "My request",
            }));
        Assert.That(request.Method, Is.EqualTo("HEAD"));
        Assert.That(Encoding.UTF8.GetString(await request.PostBody), Is.EqualTo("My request"));
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(request.Url, Is.EqualTo("/simple.json"));
    }

    [UpstreamTest("FE-127 patch should support post data")]
    public async Task PatchShouldSupportPostData()
    {
        var context = await ContextFixture();
        var (request, response) = await PromiseAll(
            Server.WaitForRequest("/simple.json"),
            context.APIRequest.PatchAsync($"{Server.Prefix}/simple.json", new()
            {
                DataString = "My request",
            }));
        Assert.That(request.Method, Is.EqualTo("PATCH"));
        Assert.That(Encoding.UTF8.GetString(await request.PostBody), Is.EqualTo("My request"));
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(request.Url, Is.EqualTo("/simple.json"));
    }

    [UpstreamTest("FE-128 post should support post data")]
    public async Task PostShouldSupportPostData()
    {
        var context = await ContextFixture();
        var (request, response) = await PromiseAll(
            Server.WaitForRequest("/simple.json"),
            context.APIRequest.PostAsync($"{Server.Prefix}/simple.json", new()
            {
                DataString = "My request",
            }));
        Assert.That(request.Method, Is.EqualTo("POST"));
        Assert.That(Encoding.UTF8.GetString(await request.PostBody), Is.EqualTo("My request"));
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(request.Url, Is.EqualTo("/simple.json"));
    }

    [UpstreamTest("FE-129 put should support post data")]
    public async Task PutShouldSupportPostData()
    {
        var context = await ContextFixture();
        var (request, response) = await PromiseAll(
            Server.WaitForRequest("/simple.json"),
            context.APIRequest.PutAsync($"{Server.Prefix}/simple.json", new()
            {
                DataString = "My request",
            }));
        Assert.That(request.Method, Is.EqualTo("PUT"));
        Assert.That(Encoding.UTF8.GetString(await request.PostBody), Is.EqualTo("My request"));
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(request.Url, Is.EqualTo("/simple.json"));
    }

    [UpstreamTest("FE-130 should add default headers")]
    public async Task ShouldAddDefaultHeaders()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            context.APIRequest.GetAsync(Server.EmptyPage));
        Assert.That(request.Headers["accept"], Is.EqualTo("*/*"));
        var userAgent = await page.EvaluateAsync<string>("() => navigator.userAgent");
        Assert.That(request.Headers["user-agent"], Is.EqualTo(userAgent));
        Assert.That(request.Headers["accept-encoding"], Is.EqualTo("gzip,deflate,br"));
    }

    [UpstreamTest("FE-131 should send content-length")]
    public async Task ShouldSendContentLength()
    {
        var context = await ContextFixture();
        var bytes = new List<byte>();
        for (var i = 0; i < 256; i++)
        {
            bytes.Add((byte)i);
        }
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            context.APIRequest.PostAsync(Server.EmptyPage, new()
            {
                DataByte = bytes.ToArray(),
            }));
        Assert.That(request.Headers["content-length"], Is.EqualTo("256"));
        Assert.That(request.Headers["content-type"], Is.EqualTo("application/octet-stream"));
    }

    [UpstreamTest("FE-132 should add default headers to redirects")]
    public async Task ShouldAddDefaultHeadersToRedirects()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        Server.SetRedirect("/redirect", "/empty.html");
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            context.APIRequest.GetAsync($"{Server.Prefix}/redirect"));
        Assert.That(request.Headers["accept"], Is.EqualTo("*/*"));
        var userAgent = await page.EvaluateAsync<string>("() => navigator.userAgent");
        Assert.That(request.Headers["user-agent"], Is.EqualTo(userAgent));
        Assert.That(request.Headers["accept-encoding"], Is.EqualTo("gzip,deflate,br"));
    }

    [UpstreamTest("FE-133 should allow to override default headers")]
    public async Task ShouldAllowToOverrideDefaultHeaders()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            context.APIRequest.GetAsync(Server.EmptyPage, new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["User-Agent"] = "Playwright",
                    ["Accept"] = "text/html",
                    ["Accept-Encoding"] = "br",
                },
            }));
        Assert.That(request.Headers["accept"], Is.EqualTo("text/html"));
        Assert.That(request.Headers["user-agent"], Is.EqualTo("Playwright"));
        Assert.That(request.Headers["accept-encoding"], Is.EqualTo("br"));
    }

    [UpstreamTest("FE-134 should propagate custom headers with redirects")]
    public async Task ShouldPropagateCustomHeadersWithRedirects()
    {
        var context = await ContextFixture();
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/simple.json");
        var (req1, req2, req3, _) = await PromiseAll(
            Server.WaitForRequest("/a/redirect1"),
            Server.WaitForRequest("/b/c/redirect2"),
            Server.WaitForRequest("/simple.json"),
            context.APIRequest.GetAsync($"{Server.Prefix}/a/redirect1", new() { Headers = new Dictionary<string, string> { ["foo"] = "bar" } }));
        Assert.That(req1.Headers["foo"], Is.EqualTo("bar"));
        Assert.That(req2.Headers["foo"], Is.EqualTo("bar"));
        Assert.That(req3.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("FE-135 should propagate extra http headers with redirects")]
    public async Task ShouldPropagateExtraHttpHeadersWithRedirects()
    {
        var context = await ContextFixture();
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/simple.json");
        await context.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["My-Secret"] = "Value" });
        var (req1, req2, req3, _) = await PromiseAll(
            Server.WaitForRequest("/a/redirect1"),
            Server.WaitForRequest("/b/c/redirect2"),
            Server.WaitForRequest("/simple.json"),
            context.APIRequest.GetAsync($"{Server.Prefix}/a/redirect1"));
        Assert.That(req1.Headers["my-secret"], Is.EqualTo("Value"));
        Assert.That(req2.Headers["my-secret"], Is.EqualTo("Value"));
        Assert.That(req3.Headers["my-secret"], Is.EqualTo("Value"));
    }

    [UpstreamTest("FE-136 should throw on invalid header value")]
    public async Task ShouldThrowOnInvalidHeaderValue()
    {
        var context = await ContextFixture();
        var error = await Catch(() => context.APIRequest.GetAsync($"{Server.Prefix}/a/redirect1", new()
        {
            Headers = new Dictionary<string, string>
            {
                ["foo"] = "недопустимое значение",
            },
        }));
        Assert.That(error.Message, Does.Contain("Invalid character in header content"));
    }

    [UpstreamTest("FE-137 should throw on non-http(s) protocol")]
    public async Task ShouldThrowOnNonHttpSProtocol()
    {
        var context = await ContextFixture();
        var error1 = await Catch(() => context.APIRequest.GetAsync("data:text/plain,test"));
        Assert.That(error1.Message, Does.Contain("Protocol \"data:\" not supported"));
        var error2 = await Catch(() => context.APIRequest.GetAsync("file:///tmp/foo"));
        Assert.That(error2.Message, Does.Contain("Protocol \"file:\" not supported"));
    }

    [UpstreamTest("FE-139 should inherit ignoreHTTPSErrors from context")]
    public async Task ShouldInheritIgnoreHTTPSErrorsFromContext()
    {
        var context = await ContextFactory(new(ContextOptions()) { IgnoreHTTPSErrors = true });
        var response = await context.APIRequest.GetAsync(HttpsServer.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("FE-140 should resolve url relative to baseURL")]
    public async Task ShouldResolveUrlRelativeToBaseURL()
    {
        var context = await ContextFactory(new(ContextOptions())
        {
            BaseURL = Server.Prefix,
        });
        var response = await context.APIRequest.GetAsync("/empty.html");
        Assert.That(response.Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("FE-141 should support gzip compression")]
    public async Task ShouldSupportGzipCompression()
    {
        var context = await ContextFixture();
        Server.SetRoute("/compressed", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Encoding"] = "gzip",
                ["Content-Type"] = "text/plain",
            });

            // .NET: zlib.createGzip() with pipeline(gzip, res, err => { if (err) console.log(`Server error: ${err}`); }) is a GZipStream whose output is written to the response.
            using var compressed = new MemoryStream();
            using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, true))
            {
                gzip.Write(Encoding.UTF8.GetBytes("Hello, world!"));
            }
            res.Write(compressed.ToArray());
            res.End();
        });

        var response = await context.APIRequest.GetAsync(Server.Prefix + "/compressed");
        Assert.That(await response.TextAsync(), Is.EqualTo("Hello, world!"));
    }

    [UpstreamTest("FE-142 should support case-insensitive content-encoding")]
    public async Task ShouldSupportCaseInsensitiveContentEncoding()
    {
        var context = await ContextFixture();
        Server.SetRoute("/compressed-uppercase", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Encoding"] = "GZIP",
                ["Content-Type"] = "text/plain",
            });

            // .NET: zlib.createGzip() with pipeline(gzip, res, err => { if (err) console.log(`Server error: ${err}`); }) is a GZipStream whose output is written to the response.
            using var compressed = new MemoryStream();
            using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, true))
            {
                gzip.Write(Encoding.UTF8.GetBytes("Hello, world!"));
            }
            res.Write(compressed.ToArray());
            res.End();
        });

        var response = await context.APIRequest.GetAsync(Server.Prefix + "/compressed-uppercase");
        Assert.That(await response.TextAsync(), Is.EqualTo("Hello, world!"));
    }

    [UpstreamTest("FE-143 should throw informative error on corrupted gzip body")]
    public async Task ShouldThrowInformativeErrorOnCorruptedGzipBody()
    {
        var context = await ContextFixture();
        Server.SetRoute("/corrupted", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Encoding"] = "gzip",
                ["Content-Type"] = "text/plain",
            });
            res.Write("Hello, world!");
            res.End();
        });

        var error = await Catch(() => context.APIRequest.GetAsync(Server.Prefix + "/corrupted"));
        Assert.That(error.Message, Does.Contain("failed to decompress 'gzip' encoding"));
    }

    [UpstreamTest("FE-144 should support brotli compression")]
    public async Task ShouldSupportBrotliCompression()
    {
        var context = await ContextFixture();
        Server.SetRoute("/compressed", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Encoding"] = "br",
                ["Content-Type"] = "text/plain",
            });

            // .NET: zlib.createBrotliCompress() with pipeline(brotli, res, err => { if (err) console.log(`Server error: ${err}`); }) is a BrotliStream whose output is written to the response.
            using var compressed = new MemoryStream();
            using (var brotli = new BrotliStream(compressed, CompressionLevel.Optimal, true))
            {
                brotli.Write(Encoding.UTF8.GetBytes("Hello, world!"));
            }
            res.Write(compressed.ToArray());
            res.End();
        });

        var response = await context.APIRequest.GetAsync(Server.Prefix + "/compressed");
        Assert.That(await response.TextAsync(), Is.EqualTo("Hello, world!"));
    }

    [UpstreamTest("FE-145 should throw informative error on corrupted brotli body")]
    public async Task ShouldThrowInformativeErrorOnCorruptedBrotliBody()
    {
        var context = await ContextFixture();
        Server.SetRoute("/corrupted", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Encoding"] = "br",
                ["Content-Type"] = "text/plain",
            });
            res.Write("Hello, world!");
            res.End();
        });

        var error = await Catch(() => context.APIRequest.GetAsync(Server.Prefix + "/corrupted"));
        Assert.That(error.Message, Does.Contain("failed to decompress 'br' encoding"));
    }

    [UpstreamTest("FE-146 should support deflate compression")]
    public async Task ShouldSupportDeflateCompression()
    {
        var context = await ContextFixture();
        Server.SetRoute("/compressed", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Encoding"] = "deflate",
                ["Content-Type"] = "text/plain",
            });

            // .NET: zlib.createDeflate() with pipeline(deflate, res, err => { if (err) console.log(`Server error: ${err}`); }) is a ZLibStream whose output is written to the response.
            using var compressed = new MemoryStream();
            using (var deflate = new ZLibStream(compressed, CompressionLevel.Optimal, true))
            {
                deflate.Write(Encoding.UTF8.GetBytes("Hello, world!"));
            }
            res.Write(compressed.ToArray());
            res.End();
        });

        var response = await context.APIRequest.GetAsync(Server.Prefix + "/compressed");
        Assert.That(await response.TextAsync(), Is.EqualTo("Hello, world!"));
    }

    [UpstreamTest("FE-147 should throw informative error on corrupted deflate body")]
    public async Task ShouldThrowInformativeErrorOnCorruptedDeflateBody()
    {
        var context = await ContextFixture();
        Server.SetRoute("/corrupted", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Encoding"] = "deflate",
                ["Content-Type"] = "text/plain",
            });
            res.Write("Hello, world!");
            res.End();
        });

        var error = await Catch(() => context.APIRequest.GetAsync(Server.Prefix + "/corrupted"));
        Assert.That(error.Message, Does.Contain("failed to decompress 'deflate' encoding"));
    }

    [UpstreamTest("FE-148 should support timeout option")]
    [LocalFixme("dotnet: the message is 'Timeout 10ms exceeded.', without the 'apiRequestContext.get: ' prefix of the call")]
    public async Task ShouldSupportTimeoutOption()
    {
        var context = await ContextFixture();
        Server.SetRoute("/slow", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["content-length"] = 4096,
                ["content-type"] = "text/html",
            });
        });

        var error = await Catch(() => context.APIRequest.GetAsync(Server.Prefix + "/slow", new() { Timeout = 10 }));
        Assert.That(error.Message, Does.Contain("apiRequestContext.get: Timeout 10ms exceeded"));
    }

    [UpstreamTest("FE-149 should support a timeout of 0")]
    public async Task ShouldSupportATimeoutOf0()
    {
        var context = await ContextFixture();
        Server.SetRoute("/slow", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["content-length"] = 4,
                ["content-type"] = "text/html",
            });
            SetTimeout(
                () =>
                {
                    res.End("done");
                },
                50);
        });

        var response = await context.APIRequest.GetAsync(Server.Prefix + "/slow", new()
        {
            Timeout = 0,
        });
        Assert.That(await response.TextAsync(), Is.EqualTo("done"));
    }

    [UpstreamTest("FE-150 should respect timeout after redirects")]
    [LocalFixme("dotnet: the message is 'Timeout 100ms exceeded.', without the 'apiRequestContext.get: ' prefix of the call")]
    public async Task ShouldRespectTimeoutAfterRedirects()
    {
        var context = await ContextFixture();
        Server.SetRedirect("/redirect", "/slow");
        Server.SetRoute("/slow", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["content-length"] = 4096,
                ["content-type"] = "text/html",
            });
        });

        context.SetDefaultTimeout(100);
        var error = await Catch(() => context.APIRequest.GetAsync(Server.Prefix + "/redirect"));
        Assert.That(error.Message, Does.Contain("apiRequestContext.get: Timeout 100ms exceeded"));
    }

    [UpstreamTest("FE-151 should not hang on a brotli encoded Range request")]
    public async Task ShouldNotHangOnABrotliEncodedRangeRequest()
    {
        var context = await ContextFixture();
        Info.Skip(NodeVersion.Major < 18);

        // .NET: zlib.brotliCompressSync(Buffer.from('A')) is a BrotliStream.
        byte[] encodedRequestPayload;
        using (var stream = new MemoryStream())
        {
            using (var brotli = new BrotliStream(stream, CompressionLevel.Optimal))
            {
                brotli.Write(Encoding.UTF8.GetBytes("A"));
            }
            encodedRequestPayload = stream.ToArray();
        }
        Server.SetRoute("/brotli", (req, res) =>
        {
            res.WriteHead(206, new()
            {
                ["Content-Type"] = "text/plain",
                ["content-length"] = 1,
                ["Content-Encoding"] = "br",
                ["content-range"] = $"bytes 0-2/{encodedRequestPayload.Length}",
                ["Accept-Ranges"] = "bytes",
            });
            res.Write(encodedRequestPayload[..2]);
        });

        var error = await ThrowsAsync<PlaywrightException>(() => context.APIRequest.GetAsync(Server.Prefix + "/brotli", new()
        {
            Headers = new Dictionary<string, string>
            {
                ["range"] = "bytes=0-2",
            },
        }));
        Assert.That(error.Message, Does.Match("Parse Error: Expected HTTP"));
    }

    [UpstreamTest("FE-152 should dispose")]
    public async Task ShouldDispose()
    {
        var context = await ContextFixture();
        var response = await context.APIRequest.GetAsync(Server.Prefix + "/simple.json");
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
        await response.DisposeAsync();
        var error = await Catch(() => response.BodyAsync());
        Assert.That(error.Message, Does.Contain("Response has been disposed"));
    }

    [UpstreamTest("FE-153 should dispose when context closes")]
    public async Task ShouldDisposeWhenContextCloses()
    {
        var context = await ContextFixture();
        var response = await context.APIRequest.GetAsync(Server.Prefix + "/simple.json");
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
        await context.CloseAsync();
        var error = await Catch(() => response.BodyAsync());
        Assert.That(error.Message, Does.Contain("Response has been disposed"));
    }

    [UpstreamTest("FE-154 should override request parameters")]
    public async Task ShouldOverrideRequestParameters()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        var (pageReq, _) = await PromiseAll(
            page.WaitForRequestAsync("**/*"),
            page.GotoAsync(Server.EmptyPage));
        var (req, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            context.APIRequest.FetchAsync(pageReq, new()
            {
                Method = "POST",
                Headers = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                },
                DataString = "data",
            }));
        Assert.That(req.Method, Is.EqualTo("POST"));
        Assert.That(req.Headers["foo"], Is.EqualTo("bar"));
        Assert.That(Encoding.UTF8.GetString(await req.PostBody), Is.EqualTo("data"));
    }

    [UpstreamTest("FE-155 should support application/x-www-form-urlencoded")]
    public async Task ShouldSupportApplicationXWwwFormUrlencoded()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        // .NET: the object of the form option is a FormData.
        var form = context.APIRequest.CreateFormData();
        form.Set("firstName", "John");
        form.Set("lastName", "Doe");
        form.Set("file", "f.js");
        var (req, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            context.APIRequest.PostAsync(Server.EmptyPage, new()
            {
                Form = form,
            }));
        Assert.That(req.Method, Is.EqualTo("POST"));
        Assert.That(req.Headers["content-type"], Is.EqualTo("application/x-www-form-urlencoded"));
        var body = Encoding.UTF8.GetString(await req.PostBody);
        var @params = HttpUtility.ParseQueryString(body);
        Assert.That(req.Headers["content-length"], Is.EqualTo(@params.ToString().Length.ToString(CultureInfo.InvariantCulture)));
        Assert.That(@params.Get("firstName"), Is.EqualTo("John"));
        Assert.That(@params.Get("lastName"), Is.EqualTo("Doe"));
        Assert.That(@params.Get("file"), Is.EqualTo("f.js"));
    }

    [UpstreamTest("FE-156 should support application/x-www-form-urlencoded with param lists")]
    public async Task ShouldSupportApplicationXWwwFormUrlencodedWithParamLists()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        var form = context.APIRequest.CreateFormData();
        form.Append("foo", "1");
        form.Append("foo", "2");
        var (req, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            context.APIRequest.PostAsync(Server.EmptyPage, new() { Form = form }));
        Assert.That(req.Method, Is.EqualTo("POST"));
        Assert.That(req.Headers["content-type"], Is.EqualTo("application/x-www-form-urlencoded"));
        var body = Encoding.UTF8.GetString(await req.PostBody);
        var @params = HttpUtility.ParseQueryString(body);
        Assert.That(req.Headers["content-length"], Is.EqualTo(@params.ToString().Length.ToString(CultureInfo.InvariantCulture)));
        Assert.That(@params.GetValues("foo"), Is.EqualTo(new[] { "1", "2" }));
    }

    [UpstreamTest("FE-157 should encode to application/json by default")]
    public async Task ShouldEncodeToApplicationJsonByDefault()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        var data = new
        {
            firstName = "John",
            lastName = "Doe",
            file = new
            {
                name = "f.js",
            },
        };
        var (req, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            context.APIRequest.PostAsync(Server.EmptyPage, new() { DataObject = data }));
        Assert.That(req.Method, Is.EqualTo("POST"));
        Assert.That(req.Headers["content-type"], Is.EqualTo("application/json"));
        var body = Encoding.UTF8.GetString(await req.PostBody);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        ExpectToEqual(json, data);
    }

    [UpstreamTest("FE-158 should support multipart/form-data")]
    public async Task ShouldSupportMultipartFormData()
    {
        var context = await ContextFixture();
        var formReceived = new TaskCompletionSource<(Exception Error, FormidableMap<string> Fields, FormidableMap<FormidableFile> Files, ServerRequest ServerRequest)>(TaskCreationOptions.RunContinuationsAsynchronously);
        // .NET: serverRequest2, error2, fields2 and files2 are serverRequest, error, fields and files, which the test declares again below.
        Server.SetRoute("/empty.html", async (serverRequest2, res) =>
        {
            var form = new IncomingForm();
            var (error2, fields2, files2) = await form.Parse(serverRequest2);
            Server.ServeFile(serverRequest2, res);
            formReceived.TrySetResult((error2, fields2, files2, serverRequest2));
        });

        var file = new FilePayload
        {
            Name = "f.js",
            MimeType = "text/javascript",
            Buffer = Encoding.UTF8.GetBytes("var x = 10;\r\n;console.log(x);"),
        };
        // .NET: the object of the multipart option is a FormData.
        var multipart = context.APIRequest.CreateFormData();
        multipart.Set("firstName", "John");
        multipart.Set("middleName", "");
        multipart.Set("lastName", "Doe");
        multipart.Set("file", file);
        var ((error, fields, files, serverRequest), response) = await PromiseAll(
            formReceived.Task,
            context.APIRequest.PostAsync(Server.EmptyPage, new()
            {
                Multipart = multipart,
            }));
        Assert.That(error, Is.Null);
        Assert.That(serverRequest.Method, Is.EqualTo("POST"));
        Assert.That(serverRequest.Headers["content-type"], Does.Contain("multipart/form-data"));
        Assert.That(fields["firstName"], Is.EqualTo("John"));
        Assert.That(fields["middleName"], Is.EqualTo(""));
        Assert.That(fields["lastName"], Is.EqualTo("Doe"));
        Assert.That(files["file"].OriginalFilename, Is.EqualTo(file.Name));
        Assert.That(files["file"].Mimetype, Is.EqualTo(file.MimeType));
        Assert.That(File.ReadAllText(files["file"].Filepath), Is.EqualTo(Encoding.UTF8.GetString(file.Buffer)));
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("FE-159 should escape quotes and newlines in multipart field and file names")]
    public async Task ShouldEscapeQuotesAndNewlinesInMultipartFieldAndFileNames()
    {
        var context = await ContextFixture();
        var bodyReceived = new TaskCompletionSource<(string ContentType, byte[] Body)>(TaskCreationOptions.RunContinuationsAsynchronously);
        Server.SetRoute("/empty.html", async (serverRequest, res) =>
        {
            // .NET: for await (const chunk of serverRequest) chunks.push(chunk) is PostBody.
            var chunks = await serverRequest.PostBody;
            Server.ServeFile(serverRequest, res);
            bodyReceived.TrySetResult((serverRequest.Headers["content-type"], chunks));
        });
        // .NET: the object of the multipart option is a FormData.
        var multipart = context.APIRequest.CreateFormData();
        multipart.Set("say\"hi\nthere", "v1");
        multipart.Set("file", new FilePayload { Name = "report \"final\"\r\n.txt", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("hello") });
        var ((contentType, body), _) = await PromiseAll(
            bodyReceived.Task,
            context.APIRequest.PostAsync(Server.EmptyPage, new()
            {
                Multipart = multipart,
            }));
        var headers = Encoding.UTF8.GetString(body).Split("\r\n").Where(line => line.StartsWith("content-disposition", StringComparison.Ordinal));
        Assert.That(headers, Is.EqualTo(new[]
        {
            "content-disposition: form-data; name=\"say%22hi%0D%0Athere\"",
            "content-disposition: form-data; name=\"file\"; filename=\"report %22final%22%0D%0A.txt\"",
        }));
        // Node's standard multipart parser expects that escaping.
        // .NET: new Request('http://localhost/', { method: 'POST', headers: { 'content-type': contentType }, body: new Uint8Array(body) }).formData() is the parser of the harness.
        var (_, fields, files) = await new IncomingForm().Parse(contentType, body);
        Assert.That(fields["say\"hi\r\nthere"], Is.EqualTo("v1"));
        Assert.That(File.ReadAllText(files["file"].Filepath), Is.EqualTo("hello"));
    }

    [UpstreamTest("FE-161 should support multipart/form-data and keep the order")]
    public async Task ShouldSupportMultipartFormDataAndKeepTheOrder()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        // .NET: the object of the multipart option is a FormData, and Object.keys(given) is the list of the names that the test sets.
        var given = context.APIRequest.CreateFormData();
        given.Set("firstName", "John");
        given.Set("lastName", "Doe");
        given.Set("age", 27);
        given.Set("foo", "bar");
        var givenKeys = new[] { "firstName", "lastName", "age", "foo" };
        var formReceived = new TaskCompletionSource<(Exception Error, FormidableMap<string> Fields)>(TaskCreationOptions.RunContinuationsAsynchronously);
        // .NET: error2 and fields2 are error and fields, which the test declares again below.
        Server.SetRoute("/empty.html", async (serverRequest, res) =>
        {
            var form = new IncomingForm();
            var (error2, fields2, files) = await form.Parse(serverRequest);
            Server.ServeFile(serverRequest, res);
            formReceived.TrySetResult((error2, fields2));
        });
        var ((error, fields), response) = await PromiseAll(
            formReceived.Task,
            context.APIRequest.PostAsync(Server.EmptyPage, new()
            {
                Multipart = given,
            }));
        Assert.That(error, Is.Null);
        var actualKeys = fields.Keys;
        Assert.That(actualKeys, Is.EqualTo(givenKeys));
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("FE-162 should support repeating names in multipart/form-data")]
    public async Task ShouldSupportRepeatingNamesInMultipartFormData()
    {
        var context = await ContextFixture();
        Info.Skip(NodeVersion.Major < 20, "File is not available in Node.js < 20. FormData is not available in Node.js < 18");
        var postBodyPromise = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        Server.SetRoute("/empty.html", async (req, res) =>
        {
            postBodyPromise.TrySetResult(Encoding.UTF8.GetString(await req.PostBody));
            res.WriteHead(200, new()
            {
                ["content-type"] = "text/plain",
            });
            res.End("OK.");
        });
        var formData = context.APIRequest.CreateFormData();
        formData.Set("name", "John");
        formData.Append("name", "Doe");
        // .NET: a File is a FilePayload.
        formData.Append("file", new FilePayload { Name = "f1.js", MimeType = "text/javascript", Buffer = Encoding.UTF8.GetBytes("var x = 10;\r\n;console.log(x);") });
        // .NET: the file name that append() takes as the third argument is the name of the FilePayload.
        formData.Append("file", new FilePayload { Name = "custom_f2.txt", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("hello") });
        // .NET: a Blob is a FilePayload with the name that FormData gives a Blob, "blob".
        formData.Append("file", new FilePayload { Name = "blob", MimeType = "text/plain", Buffer = Encoding.UTF8.GetBytes("boo") });
        var (postBody, response) = await PromiseAll(
            postBodyPromise.Task,
            context.APIRequest.PostAsync(Server.EmptyPage, new()
            {
                Multipart = formData,
            }));
        Assert.That(postBody, Does.Contain("content-disposition: form-data; name=\"name\"\r\n\r\nJohn"));
        Assert.That(postBody, Does.Contain("content-disposition: form-data; name=\"name\"\r\n\r\nDoe"));
        Assert.That(postBody, Does.Contain("content-disposition: form-data; name=\"file\"; filename=\"f1.js\"\r\ncontent-type: text/javascript\r\n\r\nvar x = 10;\r\n;console.log(x);"));
        Assert.That(postBody, Does.Contain("content-disposition: form-data; name=\"file\"; filename=\"custom_f2.txt\"\r\ncontent-type: text/plain\r\n\r\nhello"));
        Assert.That(postBody, Does.Contain("content-disposition: form-data; name=\"file\"; filename=\"blob\"\r\ncontent-type: text/plain\r\n\r\nboo"));
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("FE-163 should serialize data to json regardless of content-type")]
    public async Task ShouldSerializeDataToJsonRegardlessOfContentType()
    {
        var context = await ContextFixture();
        var data = new
        {
            firstName = "John",
            lastName = "Doe",
        };
        var (req, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            context.APIRequest.PostAsync(Server.EmptyPage, new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["content-type"] = "unknown",
                },
                DataObject = data,
            }));
        Assert.That(req.Method, Is.EqualTo("POST"));
        Assert.That(req.Headers["content-type"], Is.EqualTo("unknown"));
        var body = Encoding.UTF8.GetString(await req.PostBody);
        Assert.That(body, Is.EqualTo(JsonSerializer.Serialize(data)));
    }

    [UpstreamTest("FE-165 context request should export same storage state as context")]
    public async Task ContextRequestShouldExportSameStorageStateAsContext()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=b", "c=d" });
            res.End();
        });
        await context.APIRequest.GetAsync(Server.Prefix + "/setcookie.html");
        // .NET: StorageStateAsync gives the JSON text of the storage state.
        var contextState = JsonSerializer.Deserialize<JsonElement>(await context.StorageStateAsync());
        Assert.That(contextState.GetProperty("cookies").GetArrayLength(), Is.EqualTo(2));
        var requestState = JsonSerializer.Deserialize<JsonElement>(await context.APIRequest.StorageStateAsync());
        ExpectToEqual(requestState, contextState);
        var pageState = JsonSerializer.Deserialize<JsonElement>(await page.APIRequest.StorageStateAsync());
        ExpectToEqual(pageState, contextState);
    }

    [UpstreamTest("FE-166 should send secure cookie over http for localhost")]
    public async Task ShouldSendSecureCookieOverHttpForLocalhost()
    {
        var page = await PageFixture();
        Info.Skip(Channel == "webkit-wsl");
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=v; secure" });
            res.End();
        });
        await page.APIRequest.GetAsync($"{Server.Prefix}/setcookie.html");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            page.APIRequest.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("a=v"));
    }

    [UpstreamTest("FE-167 should accept bool and numeric params and filter out undefined")]
    [LocalFixme("dotnet: a null value in Params throws NullReferenceException: APIRequestContext.FetchAsync calls ToString() on each value, which also makes 'True' and 'False' of the bools; upstream leaves the param out and sends bool=true&bool2=false")]
    public async Task ShouldAcceptBoolAndNumericParamsAndFilterOutUndefined()
    {
        var page = await PageFixture();
        ServerRequest request = null;
        var url = new Uri(Server.EmptyPage);
        // .NET: url.searchParams.set(name, value) adds to a list of pairs.
        var searchParams = new List<KeyValuePair<string, object>>();
        searchParams.Add(new("str", "s"));
        searchParams.Add(new("num", "10"));
        searchParams.Add(new("bool", "true"));
        searchParams.Add(new("bool2", "false"));
        // .NET: url.pathname + url.search.
        Server.SetRoute(url.AbsolutePath + "?" + SearchParamsToString(searchParams), (req, res) =>
        {
            request = req;
            Server.ServeFile(req, res);
        });
        await page.APIRequest.GetAsync(Server.EmptyPage, new()
        {
            Params = new Dictionary<string, object>
            {
                ["str"] = "s",
                ["num"] = 10,
                ["bool"] = true,
                ["bool2"] = false,
                ["none"] = null,
            },
        });
        var @params = HttpUtility.ParseQueryString(request.Url.Substring(request.Url.IndexOf('?')));
        Assert.That(@params.Get("str"), Is.EqualTo("s"));
        Assert.That(@params.Get("num"), Is.EqualTo("10"));
        Assert.That(@params.Get("bool"), Is.EqualTo("true"));
        Assert.That(@params.Get("bool2"), Is.EqualTo("false"));
        Assert.That(@params.AllKeys.Contains("none"), Is.EqualTo(false));
    }

    [UpstreamTest("FE-168 should abort requests when browser context closes")]
    public async Task ShouldAbortRequestsWhenBrowserContextCloses()
    {
        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Server.SetRoute("/empty.html", (req, res) =>
        {
            req.Socket.OnClose(() => connectionClosed.TrySetResult());
        });
        var context = await ContextFactory();
        async Task Then()
        {
            await Server.WaitForRequest("/empty.html");
            await context.CloseAsync();
        }
        var (error, _) = await PromiseAll(
            Catch(() => context.APIRequest.GetAsync(Server.EmptyPage)),
            Catch(() => context.APIRequest.PostAsync(Server.EmptyPage)),
            Then());
        Assert.That(error, Is.InstanceOf<Exception>());
        Assert.That(error.Message, Does.Match("Request context disposed|Target page, context or browser has been closed"));
        await connectionClosed.Task;
    }

    [UpstreamTest("FE-169 should work with connectOverCDP")]
    public async Task ShouldWorkWithConnectOverCDP()
    {
        Info.Skip(BrowserName != "chromium");
        var port = 9339 + Info.WorkerIndex;
        var browserServer = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Args = ["--remote-debugging-port=" + port],
        });
        try
        {
            var cdpBrowser = await BrowserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var context = cdpBrowser.Contexts[0];
            var response = await context.APIRequest.GetAsync(Server.Prefix + "/simple.json");
            Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/simple.json"));
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(await response.TextAsync(), Is.EqualTo("{\"foo\": \"bar\"}\n"));
        }
        finally
        {
            await browserServer.CloseAsync();
        }
    }

    [UpstreamTest("FE-170 should support SameSite cookie attribute over https")]
    public async Task ShouldSupportSameSiteCookieAttributeOverHttps()
    {
        // Cookies with SameSite=None must also specify the Secure attribute. WebKit navigation
        // to HTTP url will fail if the response contains a cookie with Secure attribute, so
        // we do HTTPS navigation.
        var context = await ContextFactory(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        // .NET: sameSite of a cookie is an enum; the names of its values are the texts 'None', 'Lax' and 'Strict'.
        foreach (var value in new[] { SameSiteAttribute.None, SameSiteAttribute.Lax, SameSiteAttribute.Strict })
        {
            await Info.Step($"SameSite={value}", async () =>
            {
                HttpsServer.SetRoute("/empty.html", (req, res) =>
                {
                    res.SetHeader("Set-Cookie", $"SID=2022; Path=/; Secure; SameSite={value}");
                    res.End();
                });
                await page.APIRequest.GetAsync(HttpsServer.EmptyPage);
                var cookie = (await page.Context.CookiesAsync()).FirstOrDefault();
                if (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl")
                {
                    Assert.That(cookie.SameSite, Is.EqualTo(SameSiteAttribute.None));
                }
                else
                {
                    Assert.That(cookie.SameSite, Is.EqualTo(value));
                }
            });
        }
    }

    [UpstreamTest("FE-171 should set domain=localhost cookie")]
    public async Task ShouldSetDomainLocalhostCookie()
    {
        var context = await ContextFixture();
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", $"name=val; Domain={Server.Hostname}; Path=/;");
            res.End();
        });
        await context.APIRequest.GetAsync(Server.EmptyPage);
        var cookie = (await context.CookiesAsync()).FirstOrDefault();
        Assert.That(cookie, Is.Not.Null);
        Assert.That(cookie.Name, Is.EqualTo("name"));
        Assert.That(cookie.Value, Is.EqualTo("val"));
    }

    [UpstreamTest("FE-172 fetch should not throw on long set-cookie value")]
    public async Task FetchShouldNotThrowOnLongSetCookieValue()
    {
        var context = await ContextFixture();
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { $"foo={new string('a', 4100)}; path=/;", "bar=val" });
            res.End();
        });
        await context.APIRequest.GetAsync(Server.EmptyPage, new() { Timeout = 5000 });
        var cookies = await context.CookiesAsync();
        Assert.That(cookies.Select(c => c.Name), Does.Contain("bar"));
    }

    [UpstreamTest("FE-173 should support set-cookie with SameSite and without Secure attribute over HTTP")]
    public async Task ShouldSupportSetCookieWithSameSiteAndWithoutSecureAttributeOverHTTP()
    {
        var page = await PageFixture();
        // .NET: sameSite of a cookie is an enum; the names of its values are the texts 'None', 'Lax' and 'Strict'.
        foreach (var value in new[] { SameSiteAttribute.None, SameSiteAttribute.Lax, SameSiteAttribute.Strict })
        {
            await Info.Step($"SameSite={value}", async () =>
            {
                Server.SetRoute("/empty.html", (req, res) =>
                {
                    res.SetHeader("Set-Cookie", $"SID=2022; Path=/; SameSite={value}");
                    res.End();
                });
                await page.APIRequest.GetAsync(Server.EmptyPage);
                var cookie = (await page.Context.CookiesAsync()).FirstOrDefault();
                if ((BrowserName == "chromium" || IsBidi) && value == SameSiteAttribute.None)
                {
                    Assert.That(cookie, Is.Null);
                }
                else if (BrowserName == "webkit" && (IsLinux || Channel == "webkit-wsl") && value == SameSiteAttribute.None)
                {
                    Assert.That(cookie, Is.Null);
                }
                else if (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl")
                {
                    Assert.That(cookie.SameSite, Is.EqualTo(SameSiteAttribute.None));
                }
                else
                {
                    Assert.That(cookie.SameSite, Is.EqualTo(value));
                }
            });
        }
    }

    [UpstreamTest("FE-174 should update host header on redirect")]
    public async Task ShouldUpdateHostHeaderOnRedirect()
    {
        var context = await ContextFixture();
        var redirectCount = 0;
        Server.SetRoute("/redirect", (req, res) =>
        {
            redirectCount++;
            var path = (req.Headers["host"] == new Uri(Server.Prefix).Authority) ? "/redirect" : "/test";
            res.WriteHead(302, new()
            {
                ["host"] = new Uri(Server.CrossProcessPrefix).Authority,
                ["location"] = Server.CrossProcessPrefix + path,
            });
            res.End();
        });
        Server.SetRoute("/test", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["content-type"] = "text/plain",
            });
            res.End("Hello!");
        });
        var reqPromise = Server.WaitForRequest("/test");
        var response = await context.APIRequest.GetAsync(Server.Prefix + "/redirect", new()
        {
            Headers = new Dictionary<string, string> { ["HosT"] = new Uri(Server.Prefix).Authority },
        });
        Assert.That(redirectCount, Is.EqualTo(2));
        await Expect(response).ToBeOKAsync();
        Assert.That(await response.TextAsync(), Is.EqualTo("Hello!"));

        Assert.That((await reqPromise).Headers["host"], Is.EqualTo(new Uri(Server.CrossProcessPrefix).Authority));
    }

    [UpstreamTest("FE-175 should not work after dispose")]
    public async Task ShouldNotWorkAfterDispose()
    {
        var context = await ContextFixture();
        await context.APIRequest.DisposeAsync();
        Assert.That((await Catch(() => context.APIRequest.GetAsync(Server.EmptyPage))).Message, Does.Contain(TargetClosedErrorMessage));
    }

    [UpstreamTest("FE-176 should not work after context dispose")]
    public async Task ShouldNotWorkAfterContextDispose()
    {
        var context = await ContextFixture();
        await context.CloseAsync(new() { Reason = "Test ended." });
        Assert.That((await Catch(() => context.APIRequest.GetAsync(Server.EmptyPage))).Message, Does.Contain("Test ended."));
    }

    [UpstreamTest("FE-177 should retry on ECONNRESET")]
    public async Task ShouldRetryOnECONNRESET()
    {
        var context = await ContextFixture();
        var requestCount = 0;
        Server.SetRoute("/test", (req, res) =>
        {
            if (requestCount++ < 3)
            {
                req.Socket.Destroy();
                return;
            }
            res.WriteHead(200, new() { ["content-type"] = "text/plain" });
            res.End("Hello!");
        });
        var response = await context.APIRequest.GetAsync(Server.Prefix + "/test", new() { MaxRetries = 3 });
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await response.TextAsync(), Is.EqualTo("Hello!"));
        Assert.That(requestCount, Is.EqualTo(4));
    }

    [UpstreamTest("FE-178 should retry ECONNRESET on compressed response")]
    public async Task ShouldRetryECONNRESETOnCompressedResponse()
    {
        var context = await ContextFixture();
        var requestCount = 0;
        Server.SetRoute("/test-gzip", (req, res) =>
        {
            if (requestCount++ < 2)
            {
                req.Socket.Destroy();
                return;
            }
            res.WriteHead(200, new()
            {
                ["Content-Encoding"] = "gzip",
                ["Content-Type"] = "text/plain",
            });
            // .NET: zlib.createGzip() with pipeline(gzipStream, res, err => { if (err) console.log(`Server error: ${err}`); }) is a GZipStream whose output is written to the response.
            using var compressed = new MemoryStream();
            using (var gzipStream = new GZipStream(compressed, CompressionLevel.Optimal, true))
            {
                gzipStream.Write(Encoding.UTF8.GetBytes("compressed-retry-ok"));
            }
            res.Write(compressed.ToArray());
            res.End();
        });
        var response = await context.APIRequest.GetAsync(Server.Prefix + "/test-gzip", new() { MaxRetries = 3 });
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await response.TextAsync(), Is.EqualTo("compressed-retry-ok"));
        Assert.That(requestCount, Is.EqualTo(3));
    }

    [UpstreamTest("FE-179 should retry ECONNRESET mid-stream during gzip decompression")]
    public async Task ShouldRetryECONNRESETMidStreamDuringGzipDecompression()
    {
        var context = await ContextFixture();
        var requestCount = 0;
        Server.SetRoute("/test-gzip-midstream", (req, res) =>
        {
            requestCount++;
            if (requestCount <= 2)
            {
                // Send response headers to make client enter the decompression pipeline,
                // then destroy the socket. This exercises the fix: without it, the
                // pipeline error callback wraps the error, stripping .code for retry.
                res.WriteHead(200, new()
                {
                    ["Content-Encoding"] = "gzip",
                    ["Content-Type"] = "text/plain",
                });
                res.FlushHeaders();
                req.Socket.Destroy();
                return;
            }
            res.WriteHead(200, new()
            {
                ["Content-Encoding"] = "gzip",
                ["Content-Type"] = "text/plain",
            });
            // .NET: zlib.createGzip() with pipeline(gzipStream, res, err => { if (err) console.log(`Server error: ${err}`); }) is a GZipStream whose output is written to the response.
            using var compressed = new MemoryStream();
            using (var gzipStream = new GZipStream(compressed, CompressionLevel.Optimal, true))
            {
                gzipStream.Write(Encoding.UTF8.GetBytes("midstream-retry-ok"));
            }
            res.Write(compressed.ToArray());
            res.End();
        });
        var response = await context.APIRequest.GetAsync(Server.Prefix + "/test-gzip-midstream", new() { MaxRetries = 3 });
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await response.TextAsync(), Is.EqualTo("midstream-retry-ok"));
        Assert.That(requestCount, Is.EqualTo(3));
    }
}
