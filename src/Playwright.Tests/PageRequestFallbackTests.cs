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

namespace Microsoft.Playwright.Tests;

public class PageRequestFallbackTests : PageTestEx
{
    [PlaywrightTest("page-request-fallback.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.RouteAsync("**/empty.html", (route) => route.FallbackAsync());
        await Page.GotoAsync(Server.EmptyPage);
    }

    [PlaywrightTest("page-request-fallback.spec.ts", "should fall back")]
    public async Task ShouldFallback()
    {
        var intercepted = new List<int>();
        await Page.RouteAsync("**/empty.html", (route) =>
        {
            intercepted.Add(1);
            route.FallbackAsync();
        });
        await Page.RouteAsync("**/empty.html", (route) =>
        {
            intercepted.Add(2);
            route.FallbackAsync();
        });
        await Page.RouteAsync("**/empty.html", (route) =>
        {
            intercepted.Add(3);
            route.FallbackAsync();
        });

        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(new List<int>() { 3, 2, 1 }, intercepted);
    }


    [PlaywrightTest("page-request-fallback.spec.ts", "should fall back async")]
    public async Task ShouldFallbackAsync()
    {
        var intercepted = new List<int>();
        await Page.RouteAsync("**/empty.html", async (route) =>
        {
            intercepted.Add(1);
            await Task.Delay(100);
            await route.FallbackAsync();
        });
        await Page.RouteAsync("**/empty.html", async (route) =>
        {
            intercepted.Add(2);
            await Task.Delay(100);
            await route.FallbackAsync();
        });
        await Page.RouteAsync("**/empty.html", async (route) =>
        {
            intercepted.Add(3);
            await Task.Delay(100);
            await route.FallbackAsync();
        });

        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(new List<int>() { 3, 2, 1 }, intercepted);
    }

    [PlaywrightTest("page-request-fallback.spec.ts", "should not chain fulfill")]
    public async Task ShouldNotChainFulfill()
    {
        var failed = false;
        await Page.RouteAsync("**/empty.html", (route) =>
        {
            failed = true;
        });
        await Page.RouteAsync("**/empty.html", (route) =>
        {
            route.FulfillAsync(new() { Status = 200, Body = "fulfilled" });
        });
        await Page.RouteAsync("**/empty.html", (route) =>
        {
            route.FallbackAsync();
        });

        var response = await Page.GotoAsync(Server.EmptyPage);
        var body = await response.BodyAsync();
        Assert.AreEqual(Encoding.UTF8.GetString(body), "fulfilled");
        Assert.IsFalse(failed);
    }

    [PlaywrightTest("page-request-fallback.spec.ts", "should not chain abort")]
    public async Task ShouldNotChainAbort()
    {
        var failed = false;
        await Page.RouteAsync("**/empty.html", (route) =>
        {
            failed = true;
        });
        await Page.RouteAsync("**/empty.html", (route) =>
        {
            route.AbortAsync();
        });
        await Page.RouteAsync("**/empty.html", (route) =>
        {
            route.FallbackAsync();
        });

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () =>
        {
            await Page.GotoAsync(Server.EmptyPage);
        });
        Assert.NotNull(exception);
        Assert.IsFalse(failed);
    }

    [PlaywrightTest("page-request-fallback.spec.ts", "should chain once")]
    public async Task ShouldChainOnce()
    {
        await Page.RouteAsync("**/empty.html", route =>
        {
            route.FulfillAsync(new() { Status = 200, Body = "fulfilled one" });
        }, new() { Times = 1 });
        await Page.RouteAsync("**/empty.html", route =>
        {
            route.FallbackAsync();
        }, new() { Times = 1 });

        var response = await Page.GotoAsync(Server.EmptyPage);
        var body = await response.BodyAsync();
        Assert.AreEqual(Encoding.UTF8.GetString(body), "fulfilled one");
    }

    [PlaywrightTest("page-request-fallback.spec.ts", "should amend HTTP headers")]
    public async Task ShouldAmendHttpHeaders()
    {
        List<string> values = new();
        await Page.RouteAsync("**/sleep.zzz", async (route) =>
        {
            values.Add(route.Request.Headers["foo"]);
            values.Add(await route.Request.HeaderValueAsync("FOO"));
            await route.ContinueAsync();
        });
        await Page.RouteAsync("**/*", route =>
        {
            var headers = route.Request.Headers.ToDictionary(x => x.Key, x => x.Value);
            headers["FOO"] = "bar";
            route.FallbackAsync(new() { Headers = headers });
        });
        await Page.GotoAsync(Server.EmptyPage);
        var (requestHeaders, _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/sleep.zzz", request => request.Headers.ToDictionary(x => x.Key, x => x.Value)),
            Page.EvaluateAsync("() => fetch('/sleep.zzz')"));
        values.Add(requestHeaders["FOO"][0]);
        Assert.AreEqual(new List<string>() { "bar", "bar", "bar" }, values);
    }

    [PlaywrightTest("page-request-fallback.spec.ts", "should delete header with undefined value")]
    public async Task ShouldDeleteHeaderWithUndefinedValue()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/something", async context =>
        {
            context.Response.StatusCode = 200;
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes("done"));
            await context.Response.CompleteAsync();
        });
        IRequest interceptedRequest = null;
        await Page.RouteAsync("**/*", async (route) =>
        {
            interceptedRequest = route.Request;
            await route.ContinueAsync();
        });
        await Page.RouteAsync(Server.Prefix + "/something", async route =>
        {
            var headers = await route.Request.AllHeadersAsync();
            headers.Remove("foo");
            await route.FallbackAsync(new() { Headers = headers });
        });
        var (text, serverRequestHeaders) = await TaskUtils.WhenAll(
            Page.EvaluateAsync<string>(@"async url => {
                    const data = await fetch(url, {
                        headers: {
                        foo: 'a',
                        bar: 'b',
                        }
                    });
                    return data.text();
                }", Server.Prefix + "/something"),
            Server.WaitForRequest("/something", request => request.Headers.ToDictionary(x => x.Key, x => x.Value)));
        Assert.AreEqual("done", text);
        Assert.AreEqual(interceptedRequest.Headers.ContainsKey("foo"), false);
        Assert.AreEqual(interceptedRequest.Headers["bar"], "b");
        Assert.AreEqual(serverRequestHeaders.ContainsKey("foo"), false);
        Assert.AreEqual(serverRequestHeaders["bar"], "b");
    }

    [PlaywrightTest("page-request-fallback.spec.ts", "should amend method")]
    public async Task ShouldAmendMethod()
    {
        var sRequestMethod = Server.WaitForRequest("/sleep.zzz", request => request.Method);
        await Page.GotoAsync(Server.EmptyPage);

        string method = null;
        await Page.RouteAsync("**/*", async (route) =>
        {
            method = route.Request.Method;
            await route.ContinueAsync();
        });
        await Page.RouteAsync("**/*", route => route.FallbackAsync(new() { Method = "POST" }));

        await Page.EvaluateAsync("() => fetch('/sleep.zzz')");
        Assert.AreEqual("POST", method);
        Assert.AreEqual("POST", await sRequestMethod);
    }

    [PlaywrightTest("page-request-fallback.spec.ts", "should override request url")]
    public async Task ShouldOverrideRequestUrl()
    {
        var serverRequestMethod = Server.WaitForRequest("/global-var.html", request => request.Method);

        string url = null;
        await Page.RouteAsync("**/global-var.html", route =>
        {
            url = route.Request.Url;
            route.ContinueAsync();
        });

        await Page.RouteAsync("**/foo", route => route.FallbackAsync(new() { Url = Server.Prefix + "/global-var.html" }));

        var response = await Page.GotoAsync(Server.Prefix + "/foo");
        Assert.AreEqual(url, Server.Prefix + "/global-var.html");
        Assert.AreEqual(response.Request.Url, Server.Prefix + "/global-var.html");
        Assert.AreEqual(response.Url, Server.Prefix + "/global-var.html");
        Assert.AreEqual(await Page.EvaluateAsync<int>("() => window['globalVar']"), 123);
        Assert.AreEqual((await serverRequestMethod), "GET");
    }

    [PlaywrightTest("page-request-fallback.spec.ts", "should amend post data")]
    public async Task ShouldAmendPostData()
    {
        await Page.GotoAsync(Server.EmptyPage);
        string postData = null;
        await Page.RouteAsync("**/*", async (route) =>
        {
            postData = route.Request.PostData;
            await route.ContinueAsync();
        });
        await Page.RouteAsync("**/*", route => route.FallbackAsync(new() { PostData = Encoding.UTF8.GetBytes("doggo") }));
        var (serverRequestBody, _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/sleep.zzz", request =>
            {
                using StreamReader reader = new(request.Body, System.Text.Encoding.UTF8);
                return reader.ReadToEndAsync().GetAwaiter().GetResult();
            }),
            Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })"));
        Assert.AreEqual("doggo", postData);
        Assert.AreEqual("doggo", serverRequestBody);
    }
}
