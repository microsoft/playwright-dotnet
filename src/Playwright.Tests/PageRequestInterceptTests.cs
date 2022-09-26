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

using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class PageRequestInterceptTests : PageTestEx
{
    [PlaywrightTest("page-request-intercept.spec.ts", "should fulfill intercepted response")]
    public async Task ShouldFulfillInterceptedResponse()
    {
        await Page.RouteAsync("**/*", async (route) =>
        {
            var response = await Page.APIRequest.FetchAsync(route.Request);
            await route.FulfillAsync(new()
            {
                Response = response,
                Status = 201,
                Headers = new Dictionary<string, string> {
                        { "foo", "bar" },
                },
                ContentType = "text/plain",
                Body = "Yo, page!",
            });
        });
        var response = await Page.GotoAsync(Server.Prefix + "/empty.html");
        Assert.AreEqual(201, response.Status);
#pragma warning disable 0612
        Assert.AreEqual(response.Headers["foo"], "bar");
        Assert.AreEqual(response.Headers["content-type"], "text/plain");
#pragma warning restore 0612
        Assert.AreEqual(await Page.EvaluateAsync<string>("() => document.body.textContent"), "Yo, page!");
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "should fulfill response with empty body")]
    public async Task ShouldFulfillResponseWithEmptyBody()
    {
        await Page.RouteAsync("**/*", async (route) =>
        {
            var response = await Page.APIRequest.FetchAsync(route.Request);
            await route.FulfillAsync(new()
            {
                Response = response,
                Status = 201,
                Body = "",
                Headers = new Dictionary<string, string> {
                        { "Content-Length", "0" },
                },
            });
        });
        var response = await Page.GotoAsync(Server.Prefix + "/title.html");
        Assert.AreEqual(201, response.Status);
        Assert.AreEqual(await response.TextAsync(), "");
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "should override with defaults when intercepted response not provided")]
    public async Task ShouldOverrideWithDefaultsWhenInterceptedResponseNotProvided()
    {
        Server.SetRoute("/empty.html", async context =>
        {
            context.Response.Headers.Add("foo", "bar");
            await context.Response.WriteAsync("my content");
        });
        await Page.RouteAsync("**/*", async (route) =>
        {
            await Page.APIRequest.FetchAsync(route.Request);
            await route.FulfillAsync(new()
            {
                Status = 201,
            });
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(201, response.Status);
        Assert.AreEqual(await response.TextAsync(), "");
        if (TestConstants.IsWebKit)
        {
#pragma warning disable 0612
            Assert.AreEqual(response.Headers.Count, 1);
            Assert.AreEqual(response.Headers["content-type"], "text/plain");
#pragma warning restore 0612
        }
        else
        {
#pragma warning disable 0612
            Assert.AreEqual(response.Headers.Count, 0);
#pragma warning restore 0612
        }
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "should fulfill with any response")]
    public async Task ShouldFulfillWithAnyResponse()
    {
        Server.SetRoute("/sample", async context =>
        {
            context.Response.Headers.Add("foo", "bar");
            await context.Response.WriteAsync("Woo-hoo");
        });
        var sampleResponse = await Page.APIRequest.GetAsync(Server.Prefix + "/sample");

        await Page.RouteAsync("**/*", async (route) =>
        {
            await route.FulfillAsync(new()
            {
                Response = sampleResponse,
                Status = 201,
                ContentType = "text/plain",
            });
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(201, response.Status);
        Assert.AreEqual(await response.TextAsync(), "Woo-hoo");
#pragma warning disable 0612
        Assert.AreEqual(response.Headers["foo"], "bar");
#pragma warning restore 0612
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "should support fulfill after intercept")]
    public async Task ShouldSupportFulfillAfterIntercept()
    {
        var requestPromise = Server.WaitForRequest("/title.html", context => context.Path);
        await Page.RouteAsync("**", async (route) =>
        {
            var response = await Page.APIRequest.FetchAsync(route.Request);
            await route.FulfillAsync(new() { Response = response });
        });
        var response = await Page.GotoAsync(Server.Prefix + "/title.html");
        var requestPath = await requestPromise;
        Assert.AreEqual("/title.html", requestPath.ToString());
        Assert.AreEqual(await response.TextAsync(), await File.ReadAllTextAsync(TestUtils.GetAsset("title.html")));
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "should give access to the intercepted response")]
    public async Task ShouldGiveAccessToTheInterceptedResponse()
    {
        await Page.GotoAsync(Server.EmptyPage);

        var routeT = new TaskCompletionSource<IRoute>();
        await Page.RouteAsync("**/title.html", (route) => routeT.SetResult(route));

        var evalTask = Page.EvaluateAsync("url => fetch(url)", Server.Prefix + "/title.html");

        var route = await routeT.Task;
        var response = await Page.APIRequest.FetchAsync(route.Request);

        Assert.AreEqual(response.Status, 200);
        Assert.AreEqual(response.StatusText, "OK");
        Assert.True(response.Ok);
        Assert.True(response.Url.EndsWith("/title.html"));
        Assert.AreEqual(response.Headers["content-type"], "text/html; charset=utf-8");
        Assert.AreEqual(response.HeadersArray.Where(x => x.Name.Equals("content-type", StringComparison.OrdinalIgnoreCase)).First().Value, "text/html; charset=utf-8");

        await Task.WhenAll(
           evalTask,
            route.FulfillAsync(new()
            {
                Response = response,
            }));
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "should give access to the intercepted response body")]
    public async Task ShouldGiveAccessToTheInterceptedResponseBody()
    {
        await Page.GotoAsync(Server.EmptyPage);

        var routeT = new TaskCompletionSource<IRoute>();
        await Page.RouteAsync("**/simple.json", (route) => routeT.SetResult(route));

        var evalTask = Page.EvaluateAsync("url => fetch(url)", Server.Prefix + "/simple.json");

        var route = await routeT.Task;
        var response = await Page.APIRequest.FetchAsync(route.Request);

        Assert.AreEqual(await response.TextAsync(), "{\"foo\": \"bar\"}\n");

        await Task.WhenAll(
           evalTask,
            route.FulfillAsync(new()
            {
                Response = response,
            }));
    }
}
