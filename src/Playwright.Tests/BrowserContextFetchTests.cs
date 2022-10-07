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

using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class BrowserContextFetchTests : PageTestEx

{
    [PlaywrightTest("browsercontext-fetch.spec.ts", "get should work")]
    public async Task GetShouldWork()
    {
        var response = await Context.APIRequest.GetAsync(Server.Prefix + "/simple.json");
        Assert.AreEqual(Server.Prefix + "/simple.json", response.Url);
        Assert.AreEqual(200, response.Status);
        Assert.AreEqual("OK", response.StatusText);
        Assert.AreEqual(true, response.Ok);
        Assert.AreEqual("application/json; charset=utf-8", response.Headers.Where(v => v.Key == "content-type").First().Value);
        Assert.AreEqual("application/json; charset=utf-8", response.HeadersArray.Where(v => v.Name == "Content-Type").First().Value);
        Assert.AreEqual("{\"foo\": \"bar\"}\n", await response.TextAsync());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "fetch should work")]
    public async Task FetchShouldWork()
    {
        var response = await Context.APIRequest.FetchAsync(Server.Prefix + "/simple.json");
        Assert.AreEqual(Server.Prefix + "/simple.json", response.Url);
        Assert.AreEqual(200, response.Status);
        Assert.AreEqual("OK", response.StatusText);
        Assert.AreEqual(true, response.Ok);
        Assert.AreEqual("application/json; charset=utf-8", response.Headers.Where(v => v.Key == "content-type").First().Value);
        Assert.AreEqual("application/json; charset=utf-8", response.HeadersArray.Where(v => v.Name == "Content-Type").First().Value);
        Assert.AreEqual("{\"foo\": \"bar\"}\n", await response.TextAsync());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should throw on network error")]
    public async Task ShouldThrowOnNetworkErrors()
    {
        Server.SetRoute("/test", context =>
        {
            context.Abort();
            return Task.CompletedTask;
        });
        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Context.APIRequest.GetAsync(Server.Prefix + "/test"));
        StringAssert.Contains("socket hang up", error.Message);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should throw on network error after redirect")]
    public async Task ShouldThrowOnNetworkErrorsAfterRedirect()
    {
        Server.SetRedirect("/redirect", "/test");
        Server.SetRoute("/test", context =>
        {
            context.Abort();
            return Task.CompletedTask;
        });
        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Context.APIRequest.GetAsync(Server.Prefix + "/redirect"));
        StringAssert.Contains("socket hang up", error.Message);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should add session cookies to request")]
    public async Task ShouldAddSessionCookiesToRequest()
    {
        await Context.AddCookiesAsync(new[] { new Cookie()
            {
                Name="username",
                Value = "John Doe",
                Domain="localhost",
                Path = "/",
                Expires = -1,
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteAttribute.Lax,
            }});
        var (cookieHeader, _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/simple.json", request => request.Headers["Cookie"].First()),
            Context.APIRequest.FetchAsync(Server.Prefix + "/simple.json")
        );
        Assert.AreEqual("username=John Doe", cookieHeader);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should support queryParams")]
    public async Task ShouldSupportQueryParams()
    {
        var queryString = new QueryString();
        queryString.Add("p1", "v1");
        queryString.Add("парам2", "знач2");
        var requestOptions = new APIRequestContextOptions() { Params = new Dictionary<string, object>() { { "p1", "v1" }, { "парам2", "знач2" } } };
        await ForAllMethods(Context.APIRequest, async responseTask =>
        {
            var (receivedQueryString, _) = await TaskUtils.WhenAll(
                Server.WaitForRequest("/empty.html", request => request.Query),
                responseTask
            );
            Assert.AreEqual("v1", receivedQueryString["p1"].First());
            Assert.AreEqual("знач2", receivedQueryString["парам2"].First());
        }, Server.Prefix + "/empty.html?" + queryString.ToString(), requestOptions);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should support failOnStatusCode")]
    public async Task ShouldSupportFailOnStatusCode()
    {
        await ForAllMethods(Context.APIRequest, async responseTask =>
        {
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await responseTask);
            StringAssert.Contains("404 Not Found", exception.Message);
        }, Server.Prefix + "/does-not-exist.html", new() { FailOnStatusCode = true });
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should support ignoreHTTPSErrors option")]
    public async Task ShouldSupporIgnoreHTTPSErrorsOption()
    {
        await ForAllMethods(Context.APIRequest, async responseTask =>
        {
            var response = await responseTask;
            Assert.AreEqual(200, response.Status);
        }, HttpsServer.EmptyPage, new() { IgnoreHTTPSErrors = true });
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should not add context cookie if cookie header passed as a parameter")]
    public async Task ShouldNotAddContextCookieIfCookieHeaderIsPassedAsAParameter()
    {
        await Context.AddCookiesAsync(new[] { new Cookie()
            {
                Name="username",
                Value = "John Doe",
                Domain="localhost",
                Path = "/",
                Expires = -1,
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteAttribute.Lax,
            }});
        await ForAllMethods(Context.APIRequest, async responseTask =>
        {
            var (response, _) = await TaskUtils.WhenAll(
                Server.WaitForRequest("/empty.html", request => request.Headers["Cookie"].First()),
                responseTask
            );
            Assert.AreEqual("foo=bar", response);
        }, Server.EmptyPage, new() { Headers = new Dictionary<string, string>() { { "Cookie", "foo=bar" } } });
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should follow redirects")]
    public async Task ShouldFollowRedirects()
    {
        Server.SetRedirect("/redirect1", "/redirect2");
        Server.SetRedirect("/redirect2", "/simple.json");
        await Context.AddCookiesAsync(new[] { new Cookie()
            {
                Name="username",
                Value = "John Doe",
                Domain="localhost",
                Path = "/",
                Expires = -1,
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteAttribute.Lax,
            }});
        var ((requestURL, requestCookieHeader), response) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/simple.json", request => (request.Path, request.Headers["Cookie"].First())),
            Context.APIRequest.FetchAsync(Server.Prefix + "/redirect1")
        );
        Assert.AreEqual("username=John Doe", requestCookieHeader);
        Assert.AreEqual("/simple.json", requestURL.ToString());
        Assert.AreEqual("bar", (await response.JsonAsync())?.GetProperty("foo").GetString());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should add cookies from Set-Cookie header")]
    public async Task ShouldAddCookiesFromSetCookieHeader()
    {
        Server.SetRoute("/setcookie.html", context =>
        {
            context.Response.Headers["Set-Cookie"] = "first=";
            context.Response.StatusCode = 200;
            return Task.CompletedTask;
        });
        await Context.APIRequest.GetAsync(Server.Prefix + "/setcookie.html");
        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("first=", await Page.EvaluateAsync<string>("() => document.cookie"));
        var cookies = await Context.CookiesAsync();
        Assert.AreEqual(1, cookies.Count);
        Assert.AreEqual("first", cookies[0].Name);
        Assert.AreEqual("", cookies[0].Value);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should not lose body while handling Set-Cookie header")]
    public async Task ShouldNotLooseBodyWhileHandlingSetCookieHeader()
    {
        Server.SetRoute("/setcookie.html", async context =>
        {
            context.Response.Headers["Set-Cookie"] = new(new string[] { "session=value", "foo=bar; max-age=3600" });
            context.Response.StatusCode = 200;
            await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("text content"));
            await context.Response.CompleteAsync();
        });
        var response = await Context.APIRequest.GetAsync(Server.Prefix + "/setcookie.html");
        Assert.AreEqual("text content", await response.TextAsync());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should not lose body while handling Set-Cookie header")]
    public async Task ShouldHandleCookiesOnRedirects()
    {
        Server.SetRoute("/redirect1", context =>
        {
            context.Response.Headers["Set-Cookie"] = "r1=v1;SameSite=Lax";
            context.Response.Headers["Location"] = "/a/b/redirect2";
            context.Response.StatusCode = 302;
            return Task.CompletedTask;
        });
        Server.SetRoute("/a/b/redirect2", context =>
        {
            context.Response.Headers["Set-Cookie"] = "r2=v2;SameSite=Lax";
            context.Response.Headers["Location"] = "/title.html";
            context.Response.StatusCode = 302;
            return Task.CompletedTask;
        });
        {
            var (req1Cookie, req2Cookie, req3Cookie, _) = await TaskUtils.WhenAll(
                Server.WaitForRequest("/redirect1", request => request.Headers["Cookie"].FirstOrDefault()),
                Server.WaitForRequest("/a/b/redirect2", request => request.Headers["Cookie"].FirstOrDefault()),
                Server.WaitForRequest("/title.html", request => request.Headers["Cookie"].FirstOrDefault()),
                Context.APIRequest.GetAsync(Server.Prefix + "/redirect1")
            );
            Assert.AreEqual(req1Cookie, null);
            Assert.AreEqual("r1=v1", req2Cookie);
            Assert.AreEqual("r1=v1", req3Cookie);
        }
        {
            var (req1Cookie, req2Cookie, req3Cookie, _) = await TaskUtils.WhenAll(
                Server.WaitForRequest("/redirect1", request => request.Headers["Cookie"].FirstOrDefault()),
                Server.WaitForRequest("/a/b/redirect2", request => request.Headers["Cookie"].FirstOrDefault()),
                Server.WaitForRequest("/title.html", request => request.Headers["Cookie"].FirstOrDefault()),
                Context.APIRequest.GetAsync(Server.Prefix + "/redirect1")
            );
            Assert.AreEqual("r1=v1", req1Cookie);
            Assert.AreEqual(new string[] { "r1=v1", "r2=v2" }, req2Cookie.Split(';').Select(s => s.Trim()).OrderBy(s => s).ToArray());
            Assert.AreEqual(req3Cookie, "r1=v1");
        }
        var cookies = (await Context.CookiesAsync()).OrderBy(c => c.Name).ToList();
        Assert.AreEqual(2, cookies.Count);
        Assert.AreEqual("r1", cookies[0].Name);
        Assert.AreEqual("v1", cookies[0].Value);
        Assert.AreEqual("/", cookies[0].Path);
        Assert.AreEqual("r2", cookies[1].Name);
        Assert.AreEqual("v2", cookies[1].Value);
        Assert.AreEqual("/a/b", cookies[1].Path);
    }


    [PlaywrightTest("browsercontext-fetch.spec.ts", "should return raw headers")]
    public async Task ShouldReturnRawHeaders()
    {
        Server.SetRoute("/headers", context =>
        {
            context.Response.Headers["Name-A"] = new(new string[] { "v1", "v2", "v3" });
            context.Response.Headers["Name-B"] = new(new string[] { "v4" });
            context.Response.StatusCode = 200;
            return Task.CompletedTask;
        });
        var response = await Context.APIRequest.GetAsync(Server.Prefix + "/headers");
        Assert.AreEqual(200, response.Status);
        var headers = response.HeadersArray.Where(header => header.Name.StartsWith("name-", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.AreEqual(4, headers.Count);
        Assert.AreEqual("Name-A", headers[0].Name);
        Assert.AreEqual("v1", headers[0].Value);
        Assert.AreEqual("Name-A", headers[1].Name);
        Assert.AreEqual("v2", headers[1].Value);
        Assert.AreEqual("Name-A", headers[2].Name);
        Assert.AreEqual("v3", headers[2].Value);
        Assert.AreEqual("Name-B", headers[3].Name);
        Assert.AreEqual("v4", headers[3].Value);

        Assert.AreEqual("v1, v2, v3", response.Headers.First(header => header.Key == "name-a").Value);
        Assert.AreEqual("v4", response.Headers.First(header => header.Key == "name-b").Value);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should work with http credentials")]
    public async Task ShouldWorkWithHttpCredentials()
    {
        Server.SetAuth("/empty.html", "user", "pass");

        var (requestURL, response) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", request => request.Path.ToString()),
            Context.APIRequest.GetAsync(Server.Prefix + "/empty.html", new() { Headers = new Dictionary<string, string>() { { "Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("user:pass"))}" } } })
        );
        Assert.AreEqual(200, response.Status);
        Assert.AreEqual("/empty.html", requestURL);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "delete should support post data")]
    public async Task DeleteShouldSupportPostData()
    {
        var ((requestMethod, requestPath, requestBody), response) = await TaskUtils.WhenAll(
        Server.WaitForRequest("/simple.json", request =>
        {
            using StreamReader reader = new(request.Body, System.Text.Encoding.UTF8);
            return (request.Method, request.Path, reader.ReadToEndAsync().GetAwaiter().GetResult());
        }),
            Context.APIRequest.DeleteAsync(Server.Prefix + "/simple.json", new() { Method = "POST", DataString = "My request" })
        );
        Assert.AreEqual("DELETE", requestMethod);
        Assert.AreEqual("My request", requestBody);
        Assert.AreEqual(200, response.Status);
        Assert.AreEqual("/simple.json", requestPath.ToString());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "get should support post data")]
    public async Task GetShouldSupportPostData()
    {
        var ((requestMethod, requestPath, requestBody), response) = await TaskUtils.WhenAll(
        Server.WaitForRequest("/simple.json", request =>
        {
            using StreamReader reader = new(request.Body, System.Text.Encoding.UTF8);
            return (request.Method, request.Path, reader.ReadToEndAsync().GetAwaiter().GetResult());
        }),
            Context.APIRequest.GetAsync(Server.Prefix + "/simple.json", new() { Method = "POST", DataString = "My request" })
        );
        Assert.AreEqual("GET", requestMethod);
        Assert.AreEqual("My request", requestBody);
        Assert.AreEqual(200, response.Status);
        Assert.AreEqual("/simple.json", requestPath.ToString());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "head should support post data")]
    public async Task HeadShouldSupportPostData()
    {
        var ((requestMethod, requestPath, requestBody), response) = await TaskUtils.WhenAll(
        Server.WaitForRequest("/simple.json", request =>
        {
            using StreamReader reader = new(request.Body, System.Text.Encoding.UTF8);
            return (request.Method, request.Path, reader.ReadToEndAsync().GetAwaiter().GetResult());
        }),
            Context.APIRequest.HeadAsync(Server.Prefix + "/simple.json", new() { Method = "POST", DataString = "My request" })
        );
        Assert.AreEqual("HEAD", requestMethod);
        Assert.AreEqual("My request", requestBody);
        Assert.AreEqual(200, response.Status);
        Assert.AreEqual("/simple.json", requestPath.ToString());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "patch should support post data")]
    public async Task PatchShouldSupportPostData()
    {
        var ((requestMethod, requestPath, requestBody), response) = await TaskUtils.WhenAll(
        Server.WaitForRequest("/simple.json", request =>
        {
            using StreamReader reader = new(request.Body, System.Text.Encoding.UTF8);
            return (request.Method, request.Path, reader.ReadToEndAsync().GetAwaiter().GetResult());
        }),
            Context.APIRequest.PatchAsync(Server.Prefix + "/simple.json", new() { Method = "POST", DataString = "My request" })
        );
        Assert.AreEqual("PATCH", requestMethod);
        Assert.AreEqual("My request", requestBody);
        Assert.AreEqual(200, response.Status);
        Assert.AreEqual("/simple.json", requestPath.ToString());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "post should support post data")]
    public async Task PostShouldSupportPostData()
    {
        var ((requestMethod, requestPath, requestBody), response) = await TaskUtils.WhenAll(
        Server.WaitForRequest("/simple.json", request =>
        {
            using StreamReader reader = new(request.Body, System.Text.Encoding.UTF8);
            return (request.Method, request.Path, reader.ReadToEndAsync().GetAwaiter().GetResult());
        }),
            Context.APIRequest.PostAsync(Server.Prefix + "/simple.json", new() { Method = "POST", DataString = "My request" })
        );
        Assert.AreEqual("POST", requestMethod);
        Assert.AreEqual("My request", requestBody);
        Assert.AreEqual(200, response.Status);
        Assert.AreEqual("/simple.json", requestPath.ToString());
    }


    [PlaywrightTest("browsercontext-fetch.spec.ts", "put should support post data")]
    public async Task PutShouldSupportPostData()
    {
        var ((requestMethod, requestPath, requestBody), response) = await TaskUtils.WhenAll(
        Server.WaitForRequest("/simple.json", request =>
        {
            using StreamReader reader = new(request.Body, System.Text.Encoding.UTF8);
            return (request.Method, request.Path, reader.ReadToEndAsync().GetAwaiter().GetResult());
        }),
            Context.APIRequest.PutAsync(Server.Prefix + "/simple.json", new() { Method = "POST", DataString = "My request" })
        );
        Assert.AreEqual("PUT", requestMethod);
        Assert.AreEqual("My request", requestBody);
        Assert.AreEqual(200, response.Status);
        Assert.AreEqual("/simple.json", requestPath.ToString());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should add default headers")]
    public async Task ShouldAddDefaultHeaders()
    {
        var (requestHeaders, _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", request => request.Headers.ToDictionary(header => header.Key, header => header.Value)),
            Context.APIRequest.GetAsync(Server.Prefix + "/empty.html")
        );
        Assert.AreEqual("*/*", requestHeaders["Accept"].First());
        var userAgent = await Page.EvaluateAsync<string>("() => navigator.userAgent");
        Assert.AreEqual(userAgent, requestHeaders["User-Agent"].First());
        Assert.AreEqual("gzip,deflate,br", requestHeaders["Accept-Encoding"].First());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should send content-length")]
    public async Task ShouldSendContentLength()
    {
        byte[] bytes = new byte[256];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)i;
        }
        var ((requestHeaderContentLength, requestHeaderContentType), _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", request => (request.Headers["Content-Length"].FirstOrDefault(), request.Headers["Content-Type"].FirstOrDefault())),
            Context.APIRequest.PostAsync(Server.Prefix + "/empty.html", new() { DataByte = bytes })
        );
        Assert.AreEqual("256", requestHeaderContentLength);
        Assert.AreEqual("application/octet-stream", requestHeaderContentType);
    }


    [PlaywrightTest("browsercontext-fetch.spec.ts", "should add default headers to redirects")]
    public async Task ShouldAddDefaultHeadersToRedirects()
    {
        Server.SetRedirect("/redirect", "/empty.html");
        var (requestHeaders, _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/redirect", request => request.Headers.ToDictionary(header => header.Key, header => header.Value)),
            Context.APIRequest.GetAsync(Server.Prefix + "/redirect")
        );
        Assert.AreEqual("*/*", requestHeaders["Accept"].First());
        var userAgent = await Page.EvaluateAsync<string>("() => navigator.userAgent");
        Assert.AreEqual(userAgent, requestHeaders["User-Agent"].First());
        Assert.AreEqual("gzip,deflate,br", requestHeaders["Accept-Encoding"].First());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should allow to override default headers")]
    public async Task ShouldAllowToOverrideDefaultHeaders()
    {
        Server.SetRedirect("/redirect", "/empty.html");
        var (requestHeaders, _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/redirect", request => request.Headers.ToDictionary(header => header.Key, header => header.Value)),
            Context.APIRequest.GetAsync(Server.Prefix + "/redirect")
        );
        Assert.AreEqual("*/*", requestHeaders["Accept"].First());
        var userAgent = await Page.EvaluateAsync<string>("() => navigator.userAgent");
        Assert.AreEqual(userAgent, requestHeaders["User-Agent"].First());
        Assert.AreEqual("gzip,deflate,br", requestHeaders["Accept-Encoding"].First());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should propagate custom headers with redirects")]
    public async Task ShouldPropagateCustomHeadersWithRedirects()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/simple.json");
        var (req1Headers, req2Headers, req3Headers, _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/a/redirect1", request => request.Headers.ToDictionary(header => header.Key, header => header.Value)),
            Server.WaitForRequest("/b/c/redirect2", request => request.Headers.ToDictionary(header => header.Key, header => header.Value)),
            Server.WaitForRequest("/simple.json", request => request.Headers.ToDictionary(header => header.Key, header => header.Value)),
            Context.APIRequest.GetAsync(Server.Prefix + "/a/redirect1", new() { Headers = new Dictionary<string, string>() { { "foo", "bar" } } })
        );
        Assert.AreEqual("bar", req1Headers["foo"].First());
        Assert.AreEqual("bar", req2Headers["foo"].First());
        Assert.AreEqual("bar", req3Headers["foo"].First());
    }
    [PlaywrightTest("browsercontext-fetch.spec.ts", "should propagate extra http headers with redirect")]
    public async Task ShouldPropagateExtraHttpHeadersWithRedirects()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/simple.json");
        await Context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["My-Secret"] = "Value"
        });
        var (req1SecretHeader, req2SecretHeader, req3SecretHeader, _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/a/redirect1", request => request.Headers["my-secret"].First()),
            Server.WaitForRequest("/b/c/redirect2", request => request.Headers["my-secret"].First()),
            Server.WaitForRequest("/simple.json", request => request.Headers["my-secret"].First()),
            Context.APIRequest.GetAsync(Server.Prefix + "/a/redirect1")
        );
        Assert.AreEqual("Value", req1SecretHeader);
        Assert.AreEqual("Value", req2SecretHeader);
        Assert.AreEqual("Value", req3SecretHeader);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should throw on invalid header value")]
    public async Task ShouldThrowOnInvalidHeaderValue()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Context.APIRequest.GetAsync(Server.Prefix + "/a/redirect1", new() { Headers = new Dictionary<string, string>() { { "foo", "недопустимое значение" } } }));
        StringAssert.Contains("Invalid character in header content", exception.Message);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should throw on non-http(s) protocol")]
    public async Task ShouldThrowOnNonHttpsProtocol()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Context.APIRequest.GetAsync("data:text/plain,test"));
        StringAssert.Contains("Protocol \"data:\" not supported", exception.Message);

        exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Context.APIRequest.GetAsync("file:///tmp/foo"));
        StringAssert.Contains("Protocol \"file:\" not supported", exception.Message);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should support https")]
    public async Task ShouldSupportHttps()
    {
        var response = await Context.APIRequest.GetAsync(HttpsServer.EmptyPage, new() { IgnoreHTTPSErrors = true });
        Assert.AreEqual(200, response.Status);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should inherit ignoreHTTPSErrors from context")]
    public async Task ShouldInheritIgnoreHttpsErrorsFromContext()
    {
        var context = await Browser.NewContextAsync(new()
        {
            IgnoreHTTPSErrors = true
        });
        var response = await context.APIRequest.GetAsync(HttpsServer.EmptyPage);
        Assert.AreEqual(200, response.Status);
        await context.CloseAsync();
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should resolve url relative to baseURL")]
    public async Task ShouldResolveUrlRelativetoBaseUrl()
    {
        var context = await Browser.NewContextAsync(new()
        {
            BaseURL = Server.Prefix,
        });
        var response = await context.APIRequest.GetAsync("/empty.html");
        Assert.AreEqual(Server.EmptyPage, response.Url);
        await context.CloseAsync();
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should support timeout option")]
    public async Task ShouldSupportTimeoutOption()
    {
        Server.SetRoute("/slow", async ctx =>
        {
            await Task.Delay(5000);
        });
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Context.APIRequest.GetAsync(Server.Prefix + "/slow", new() { Timeout = 10 }));
        StringAssert.Contains("Request timed out after 10ms", exception.Message);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should dispose")]
    public async Task ShouldDispose()
    {
        var response = await Context.APIRequest.GetAsync(Server.Prefix + "/simple.json");
        Assert.AreEqual("bar", (await response.JsonAsync())?.GetProperty("foo").GetString());
        await response.DisposeAsync();
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => response.BodyAsync());
        StringAssert.Contains("Response has been disposed", exception.Message);
    }


    [PlaywrightTest("browsercontext-fetch.spec.ts", "should dispose when context closes")]
    public async Task ShouldDisposeWhenContextCloses()
    {
        var response = await Context.APIRequest.GetAsync(Server.Prefix + "/simple.json");
        Assert.AreEqual("bar", (await response.JsonAsync())?.GetProperty("foo").GetString());
        await Context.CloseAsync();
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => response.BodyAsync());
        StringAssert.Contains("Response has been disposed", exception.Message);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should override request parameters")]
    public async Task ShouldOverrideRequestParameters()
    {
        var (pageRequest, _) = await TaskUtils.WhenAll(
            Page.WaitForRequestAsync("**/*"),
            Page.GotoAsync(Server.EmptyPage)
        );
        var ((requestMethod, requestHeaders, requestBody), _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", request =>
            {
                using StreamReader reader = new(request.Body, System.Text.Encoding.UTF8);
                return (request.Method, request.Headers.ToDictionary(header => header.Key, header => header.Value), reader.ReadToEndAsync().GetAwaiter().GetResult());
            }),
            Context.APIRequest.FetchAsync(pageRequest, new()
            {
                Method = "POST",
                Headers = new Dictionary<string, string>(){
                        { "foo", "bar"}
                },
                DataString = "Data"
            })
        );
        Assert.AreEqual("POST", requestMethod);
        Assert.AreEqual("bar", requestHeaders["foo"].First());
        Assert.AreEqual("Data", requestBody);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should support application/x-www-form-urlencoded")]
    public async Task ShouldSupportApplicationXWwwFormUrlEncoded()
    {
        var formData = Context.APIRequest.CreateFormData();
        formData.Set("firstNam", "John");
        formData.Set("lastNam", "Doe");
        formData.Set("file", "f.js");
        var ((requestMethod, requestHeaders, requestForm), _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", request => (
                request.Method,
                request.Headers.ToDictionary(header => header.Key, header => header.Value),
                request.Form.ToDictionary(form => form.Key, form => form.Value)
            )),
            Context.APIRequest.PostAsync(Server.EmptyPage, new() { Form = formData })
        );
        Assert.AreEqual("POST", requestMethod);
        Assert.AreEqual("application/x-www-form-urlencoded", requestHeaders["Content-Type"].First());
        Assert.AreEqual("John", requestForm["firstNam"].First());
        Assert.AreEqual("Doe", requestForm["lastNam"].First());
        Assert.AreEqual("f.js", requestForm["file"].First());
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should encode to application/json by default")]
    public async Task ShouldEncodeToApplicationJsonByDefault()
    {
        var data = new Dictionary<string, object>() {
                { "firstNam", "John" },
                { "lastName", "Doe" },
                { "file", new Dictionary<string, object>() {
                    { "name", "f.js" },
                } }
            };
        var ((requestMethod, requestHeaders, requestBody), _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", request =>
            {
                using StreamReader reader = new(request.Body, System.Text.Encoding.UTF8);
                return
                (
                    request.Method,
                    request.Headers.ToDictionary(header => header.Key, header => header.Value),
                    reader.ReadToEndAsync().GetAwaiter().GetResult()
                );
            }),
            Context.APIRequest.PostAsync(Server.EmptyPage, new() { DataObject = data })
        );
        Assert.AreEqual("POST", requestMethod);
        Assert.AreEqual("application/json", requestHeaders["Content-Type"].First());
        var expectedBody = JsonSerializer.Serialize(data);
        Assert.AreEqual(expectedBody, requestBody);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should support multipart/form-data")]
    public async Task ShouldSupportMultipartFormData()
    {
        var file = new FilePayload()
        {
            Name = "f.js",
            MimeType = "text/javascript",
            Buffer = System.Text.Encoding.UTF8.GetBytes("var x = 10;\r\n;console.log(x);")
        };
        var multipart = Context.APIRequest.CreateFormData();
        multipart.Set("firstName", "John");
        multipart.Set("lastName", "Doe");
        multipart.Set("file", file);
        var ((responseForm, responseFormFiles, responseMethod, responseHeaders), response) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", request => (
                request.Form.ToDictionary(form => form.Key, form => form.Value),
                request.Form.Files.ToDictionary(file => file.Name, file =>
                {
                    using StreamReader reader = new(file.OpenReadStream(), System.Text.Encoding.UTF8);
                    return (fileName: file.FileName, contentType: file.ContentType, content: reader.ReadToEndAsync().GetAwaiter().GetResult());
                }),
                request.Method,
                request.Headers.ToDictionary(header => header.Key, header => header.Value)
            )),
            Context.APIRequest.PostAsync(Server.EmptyPage, new() { Multipart = multipart })
        );
        Assert.AreEqual("POST", responseMethod);
        StringAssert.Contains("multipart/form-data", responseHeaders["Content-Type"].First());
        Assert.AreEqual("John", responseForm["firstName"].First());
        Assert.AreEqual("Doe", responseForm["lastName"].First());
        Assert.AreEqual("file", responseFormFiles.Keys.First());
        Assert.AreEqual("f.js", responseFormFiles["file"].fileName);
        Assert.AreEqual("text/javascript", responseFormFiles["file"].contentType);
        Assert.AreEqual("var x = 10;\r\n;console.log(x);", responseFormFiles["file"].content);
        Assert.AreEqual(200, response.Status);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should serialize data to json regardless of content-type")]
    public async Task ShouldSerializeDatatoJsonRegardlessOfContentType()
    {
        var data = new Dictionary<string, object>()
            {
                { "firstName", "John" },
                { "lastName", "Doe" },
            };
        var ((requestMethod, requestHeaders, requestBody), _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", request =>
            {
                using StreamReader reader = new(request.Body, System.Text.Encoding.UTF8);
                return (request.Method, request.Headers.ToDictionary(header => header.Key, header => header.Value), reader.ReadToEndAsync().GetAwaiter().GetResult());
            }),
            Context.APIRequest.PostAsync(Server.EmptyPage, new() { DataObject = data, Headers = new Dictionary<string, string>() { { "content-type", "unknown" } } })
        );
        Assert.AreEqual("POST", requestMethod);
        Assert.AreEqual("unknown", requestHeaders["Content-Type"].First());
        var expect = JsonSerializer.Serialize(data);
        Assert.AreEqual(expect, requestBody);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "context request should export same storage state as context")]
    public async Task ContextRequestShouldExportSameStorageStateAsContext()
    {
        Server.SetRoute("/setcookie.html", context =>
        {
            context.Response.Headers["Set-Cookie"] = new(new string[] { "foobar42=kekstar42", "c=d" });
            return Task.CompletedTask;
        });
        await Context.APIRequest.GetAsync(Server.Prefix + "/setcookie.html");
        var contextState = await Context.StorageStateAsync();
        var requestState = await Context.APIRequest.StorageStateAsync();
        StringAssert.Contains("foobar42", contextState);
        StringAssert.Contains("kekstar42", contextState);
        Assert.AreEqual(contextState, requestState);
        var pageState = await Page.APIRequest.StorageStateAsync();
        Assert.AreEqual(contextState, pageState);
    }

    [PlaywrightTest("browsercontext-fetch.spec.ts", "should accept bool and numeric params")]
    public async Task ShouldAcceptBoolAndNumericParams()
    {
        var requestOptions = new APIRequestContextOptions()
        {
            Params = new Dictionary<string, object>()
                {
                    { "str", "s" },
                    { "num", 10 },
                    { "bool", true },
                    { "bool2", false },
                }
        };

        var (receivedQueryParams, _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", request => request.Query.ToDictionary(param => param.Key, param => param.Value)),
            Context.APIRequest.GetAsync(Server.EmptyPage, requestOptions)
        );
        Assert.AreEqual("s", receivedQueryParams["str"].First());
        Assert.AreEqual("10", receivedQueryParams["num"].First());
        Assert.AreEqual("True", receivedQueryParams["bool"].First());
        Assert.AreEqual("False", receivedQueryParams["bool2"].First());
    }

    private async Task ForAllMethods(IAPIRequestContext request, Func<Task<IAPIResponse>, Task> callback, string url, APIRequestContextOptions options = null)
    {
        var methodsToTest = new[] { "fetch", "delete", "get", "head", "patch", "post", "put" };
        foreach (var method in methodsToTest)
        {
            try
            {
                Task<IAPIResponse> responseTask = request.NameToMethod(method)(url, options);
                await callback(responseTask);
            }
            catch (Exception ex)
            {
                throw new TimeoutException($"{method} was failing:\n" + ex.Message, ex);
            }
        }
    }
}
