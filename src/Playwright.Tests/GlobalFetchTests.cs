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

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class GlobalFetchTests : PlaywrightTestEx
{
    [PlaywrightTest("global-fetch.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        var methodsToTest = new[] { "fetch", "delete", "get", "head", "patch", "post", "put" };
        var url = Server.Prefix + "/simple.json";
        foreach (var method in methodsToTest)
        {
            var response = await request.NameToMethod(method)(url, null);
            Assert.AreEqual(url, response.Url);
            Assert.AreEqual(200, response.Status);
            Assert.AreEqual("OK", response.StatusText);
            Assert.AreEqual(true, response.Ok);
            Assert.AreEqual("application/json; charset=utf-8", response.Headers.ToDictionary(x => x.Key, x => x.Value)["content-type"]);
            Assert.AreEqual(1, response.HeadersArray.Where(x => x.Name == "Content-Type" && x.Value == "application/json; charset=utf-8").Count());
            Assert.AreEqual(method == "head" ? "" : "{\"foo\": \"bar\"}\n", await response.TextAsync());
        }
    }

    [PlaywrightTest("global-fetch.spec.ts", "should dispose global request")]
    public async Task ShouldDisposeGlobalRequest()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        var response = await request.GetAsync(Server.Prefix + "/simple.json");
        var parsedJSON = await response.JsonAsync();
        Assert.AreEqual("bar", parsedJSON?.GetProperty("foo").GetString());
        await request.DisposeAsync();
        var exception = Assert.ThrowsAsync<PlaywrightException>(() => response.BodyAsync());
        Assert.AreEqual("Response has been disposed", exception.Message);
    }

    [PlaywrightTest("global-fetch.spec.ts", "support global userAgent option")]
    public async Task ShouldSupportGlobalUserAgentOption()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new() { UserAgent = "My Agent" });
        var (receivedUserAgent, response) = await TaskUtils.WhenAll(
           Server.WaitForRequest("/empty.html", request => request.Headers.ToDictionary(x => x.Key, x => x.Value)["User-Agent"]),
            request.GetAsync(Server.EmptyPage)
        );
        Assert.AreEqual(true, response.Ok);
        Assert.AreEqual(Server.EmptyPage, response.Url);
        Assert.AreEqual("My Agent", receivedUserAgent);
    }

    [PlaywrightTest("global-fetch.spec.ts", "should support global timeout option")]
    public async Task ShouldSupportGlobalTimeoutOption()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new() { Timeout = 1 });
        Server.SetRoute("/empty.html", async request => await Task.Delay(5_000));
        var exception = Assert.ThrowsAsync<PlaywrightException>(() => request.GetAsync(Server.EmptyPage));
        StringAssert.Contains("Request timed out after 1ms", exception.Message);
    }

    [PlaywrightTest("global-fetch.spec.ts", "should propagate extra http headers with redirects")]
    public async Task ShouldPropagateExtraHttpHeadersWithRedirects()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/simple.json");
        var request = await Playwright.APIRequest.NewContextAsync(new() { ExtraHTTPHeaders = new Dictionary<string, string>() { { "My-Secret", "Value" } } });
        var (req1MySecret, req2MySecret, req3MySecret, _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/a/redirect1", r => r.Headers["My-Secret"]),
            Server.WaitForRequest("/b/c/redirect2", r => r.Headers["My-Secret"]),
            Server.WaitForRequest("/simple.json", r => r.Headers["My-Secret"]),
            request.GetAsync(Server.Prefix + "/a/redirect1"));
        Assert.AreEqual("Value", req1MySecret);
        Assert.AreEqual("Value", req2MySecret);
        Assert.AreEqual("Value", req3MySecret);
    }

    [PlaywrightTest("global-fetch.spec.ts", "should support global httpCredentials option")]
    public async Task ShouldSupportGlobalHttpCredentialsOption()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        var request1 = await Playwright.APIRequest.NewContextAsync();
        var response1 = await request1.GetAsync(Server.EmptyPage);
        Assert.AreEqual(401, response1.Status);
        await request1.DisposeAsync();

        var request2 = await Playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass" } });
        var response2 = await request2.GetAsync(Server.EmptyPage);
        Assert.AreEqual(200, response2.Status);
        await request2.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "should return error with wrong credentials")]
    public async Task ShouldReturnErrorWithWrongCredentials()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        var request = await Playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "wrong" } });
        var response = await request.GetAsync(Server.EmptyPage);
        Assert.AreEqual(401, response.Status);
        await request.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "should use proxy")]
    [Ignore("Fetch API is using the CONNECT Http proxy server method all the time")]
    public async Task ShouldUseProxy()
    {
        Server.SetRoute("/target.html", ctx => ctx.Response.WriteAsync("<html><title>Served by the proxy</title></html>"));
        var request = await Playwright.APIRequest.NewContextAsync(new() { Proxy = new() { Server = $"127.0.0.1:{Server.Port}" } });
        var response = await request.GetAsync(Server.Prefix + "/target.html");
        StringAssert.Contains("Served by the proxy", await response.TextAsync());
        await request.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "should support global ignoreHTTPSErrors option")]
    public async Task ShouldSupportGlobalIgnoreHTTPSErrorsOption()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var response = await request.GetAsync(HttpsServer.EmptyPage);
        Assert.AreEqual(200, response.Status);
        await request.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "should propagate ignoreHTTPSErrors on redirects")]
    public async Task ShouldPropagateIgnoreHTTPSErrorsOnRedirect()
    {
        HttpsServer.SetRedirect("/redir", "/empty.html");
        var request = await Playwright.APIRequest.NewContextAsync();
        var response = await request.GetAsync(HttpsServer.Prefix + "/redir", new() { IgnoreHTTPSErrors = true });
        Assert.AreEqual(200, response.Status);
        await request.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "should resolve url relative to global baseURL option")]
    public async Task ShouldResolveUrlRelativeToGlobalBaseURLOption()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new() { BaseURL = Server.Prefix });
        var response = await request.GetAsync("/empty.html");
        Assert.AreEqual(Server.EmptyPage, response.Url);
        await request.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "should return empty body")]
    public async Task ShouldReturnEmptyBody()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        var response = await request.GetAsync(Server.EmptyPage);
        var body = await response.BodyAsync();
        Assert.AreEqual(0, body.Length);
        Assert.AreEqual("", await response.TextAsync());
        await request.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "serialize corectly")]
    public async Task ShouldSerializeCorrectly()
    {
        var testCases = new List<(string, (string, object), string)>(){
                ("object", (null, new {Foo="Bar" }), "{\u0022foo\u0022:\u0022Bar\u0022}"),
                ("array", (null, new object[]{"foo", "bar", 2021 }), "[\"foo\",\"bar\",2021]"),
                ("string", ("foo", null), "foo"),
                ("string (falsey)", (string.Empty, null), string.Empty),
                ("number", (null, 2021), "2021"),
                ("number (falsey)", (null, 0), "0"),
                ("boolean", (null, true), "true"),
                ("bool (false)", (null, false), "false"),
                ("null", (null, null), string.Empty),
                ("literal string undefined", ("undefined", null), "undefined"),
            };
        IAPIRequestContext request;
        IAPIResponse response;

        Server.SetRoute("/in-is-out", ctx => ctx.Request.Body.CopyToAsync(ctx.Response.Body));

        foreach (var (name, (valueString, valueObject), expected) in testCases)
        {
            request = await Playwright.APIRequest.NewContextAsync();

            response = await request.PostAsync(Server.Prefix + "/in-is-out", new() { DataObject = valueObject, DataString = valueString });
            Assert.AreEqual(expected, await response.TextAsync());

            var stringifiedValue = JsonSerializer.Serialize(string.IsNullOrEmpty(valueString) ? valueString : valueObject);
            var (serverRequestBody, _) = await TaskUtils.WhenAll(
                Server.WaitForRequest("/in-is-out", request =>
                {
                    using StreamReader reader = new(request.Body, Encoding.UTF8);
                    return reader.ReadToEndAsync().GetAwaiter().GetResult();
                }),
                request.PostAsync(Server.Prefix + "/in-is-out", new() { DataString = stringifiedValue, Headers = new Dictionary<string, string>() { ["Content-Type"] = "application/json" } })
            );
            Assert.AreEqual(stringifiedValue, serverRequestBody);

            await request.DisposeAsync();
        }
    }

    [PlaywrightTest("global-fetch.spec.ts", "should accept already serialized data as Buffer when content-type is application/json")]
    public async Task ShouldAcceptAlreadySerializedDataAsBufferWhenContentTypeIsJSON()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        var value = Encoding.UTF8.GetBytes("{\"foo\":\"Bar\"}");
        var (serverPostBody, _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", request =>
            {
                using StreamReader reader = new(request.Body);
                return reader.ReadToEndAsync().GetAwaiter().GetResult();
            }),
            request.PostAsync(Server.EmptyPage, new() { DataByte = value, Headers = new Dictionary<string, string>() { ["Content-Type"] = "application/json" } })
        );
        Assert.AreEqual(value, serverPostBody);
        await request.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "should have nice toString")]
    public async Task ShouldHaveANiceToString()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        var response = await request.PostAsync(Server.EmptyPage, new() { DataString = "My post data", Headers = new Dictionary<string, string>() { ["Content-Type"] = "application/json" } });
        var str = response.ToString();
        StringAssert.Contains("APIResponse: 200 OK", str);
        foreach (var header in response.HeadersArray)
        {
            StringAssert.Contains($"  {header.Name}: {header.Value}", str);
        }
        await response.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "should not fail on empty body with encoding")]
    public async Task ShouldNotFailOnEmptyBodyWithEncoding()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        foreach (var method in new[] { "head", "put" })
        {
            foreach (var encoding in new[] { "br", "gzip", "deflate" })
            {
                Server.SetRoute("/empty.html", (ctx) =>
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.Headers.Add("Content-Encoding", encoding);
                    ctx.Response.Headers.Add("Content-Type", "text/plain");
                    return Task.CompletedTask;
                });
                var response = await request.NameToMethod(method)(Server.EmptyPage, null);
                Assert.AreEqual(200, response.Status);
                Assert.AreEqual(0, (await response.BodyAsync()).Length);
            }
        }
        await request.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "should return body for failing requests")]
    public async Task ShouldReturnBodyForFailingRequests()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        foreach (var method in new[] { "head", "put", "trace" })
        {
            Server.SetRoute("/empty.html", async (ctx) =>
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Headers.Add("Content-Length", "10");
                ctx.Response.Headers.Add("Content-Type", "text/plain");
                await ctx.Response.WriteAsync("Not found.");
            });
            var response = await request.FetchAsync(Server.EmptyPage, new() { Method = method });
            Assert.AreEqual(404, response.Status);
            // HEAD response returns empty body in node http module.
            Assert.AreEqual(method == "head" ? "" : "Not found.", await response.TextAsync());
        }
        await request.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "should throw an error when maxRedirects is exceeded")]
    public async Task ShouldThrowAnErrorWhenMaxRedirectsIsExceeded()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/b/c/redirect3");
        Server.SetRedirect("/b/c/redirect3", "/b/c/redirect4");
        Server.SetRedirect("/b/c/redirect4", "/simple.json");

        var request = await Playwright.APIRequest.NewContextAsync();
        foreach (var method in new[] { "GET", "PUT", "POST", "OPTIONS", "HEAD", "PATCH" })
        {
            foreach (var maxRedirects in new[] { 1, 2, 3 })
            {
                var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => request.FetchAsync($"{Server.Prefix}/a/redirect1", new() { Method = method, MaxRedirects = maxRedirects }));
                StringAssert.Contains("Max redirect count exceeded", exception.Message);
            }
        }
        await request.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "should not follow redirects when maxRedirects is set to 0")]
    public async Task ShouldNotFollowRedirectsWhenMaxRedirectsIsSetTo0()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/simple.json");

        var request = await Playwright.APIRequest.NewContextAsync();
        foreach (var method in new[] { "GET", "PUT", "POST", "OPTIONS", "HEAD", "PATCH" })
        {
            var response = await request.FetchAsync($"{Server.Prefix}/a/redirect1", new() { Method = method, MaxRedirects = 0 });
            Assert.AreEqual("/b/c/redirect2", response.Headers["location"]);
            Assert.AreEqual(302, response.Status);
        }
        await request.DisposeAsync();
    }

    [PlaywrightTest("global-fetch.spec.ts", "should throw an error when maxRedirects is less than 0")]
    public async Task ShouldThrowAnErrorWhenMaxRedirectsIsLessThan0()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/simple.json");

        var request = await Playwright.APIRequest.NewContextAsync();
        foreach (var method in new[] { "GET", "PUT", "POST", "OPTIONS", "HEAD", "PATCH" })
        {
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => request.FetchAsync($"{Server.Prefix}/a/redirect1", new() { Method = method, MaxRedirects = -1 }));
            StringAssert.Contains("'maxRedirects' should be greater than or equal to '0'", exception.Message);
        }
        await request.DisposeAsync();
    }
}
