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
        Assert.AreEqual(response.Headers["foo"], "bar");
        Assert.AreEqual(response.Headers["content-type"], "text/plain");
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
            context.Response.Headers.Append("foo", "bar");
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
            Assert.AreEqual(response.Headers.Count, 1);
            Assert.AreEqual(response.Headers["content-type"], "text/plain");
        }
        else
        {
            Assert.AreEqual(response.Headers.Count, 0);
        }
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "should fulfill with any response")]
    public async Task ShouldFulfillWithAnyResponse()
    {
        Server.SetRoute("/sample", async context =>
        {
            context.Response.Headers.Append("foo", "bar");
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
        Assert.AreEqual(response.Headers["foo"], "bar");
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

    [PlaywrightTest("page-request-intercept.spec.ts", "should fulfill intercepted response using alias")]
    public async Task ShouldFulfillInterceptedResponseUsingAlias()
    {
        await Page.RouteAsync("**/*", async (route) =>
        {
            var response = await Page.APIRequest.FetchAsync(route.Request);
            await route.FulfillAsync(new() { Response = response });
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(200, response.Status);
        Assert.True(response.Headers["content-type"].Contains("text/html"));
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "should support timeout option in route.fetch")]
    public async Task ShouldSupportTimeoutOptionInRouteFetch()
    {
        Server.SetRoute("/slow", async context =>
        {
            await Task.Delay(10000);
        });
        await Page.RouteAsync("**/*", async (route) =>
        {
            var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => route.FetchAsync(new() { Timeout = 1000 }));
            Assert.True(error.Message.Contains("Request timed out after 1000ms"));
        });
        var error = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.GotoAsync(Server.Prefix + "/slow", new() { Timeout = 2000 }));
        Assert.True(error.Message.Contains("Timeout 2000ms exceeded"));
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "should not follow redirects when maxRedirects is set to 0 in route.fetch")]
    public async Task ShouldNotFollowRedirectsInRouteFetch()
    {
        Server.SetRedirect("/foo", "/empty.html");
        await Page.RouteAsync("**/*", async (route) =>
        {
            var response = await route.FetchAsync(new() { MaxRedirects = 0 });
            Assert.AreEqual("/empty.html", response.Headers["location"]);
            Assert.AreEqual(302, response.Status);
            await route.FulfillAsync(new() { Body = "hello" });
        });

        await Page.GotoAsync($"{Server.Prefix}/foo");
        var content = await Page.ContentAsync();
        Assert.True(content.Contains("hello"));
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "should intercept with url override")]
    public async Task ShouldInterceptWithUrlOverride()
    {
        await Page.RouteAsync("**/*.html", async (route) =>
        {
            var response = await route.FetchAsync(new() { Url = Server.Prefix + "/one-style.html" });
            await route.FulfillAsync(new() { Response = response });
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(200, response.Status);
        StringAssert.Contains("one-style.css", await response.TextAsync());
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "should intercept with post data override")]
    public async Task ShouldInterceptWithPostDataOverride()
    {
        var requestBodyPromise = Server.WaitForRequest("/empty.html", request =>
        {
            using StreamReader reader = new(request.Body, System.Text.Encoding.UTF8);
            return reader.ReadToEndAsync().GetAwaiter().GetResult();
        });
        await Page.RouteAsync("**/*.html", async (route) =>
        {
            var response = await Page.APIRequest.FetchAsync(route.Request, new()
            {
                DataObject = new Dictionary<string, object>() { { "foo", "bar" } }
            });
            await route.FulfillAsync(new() { Response = response });
        });
        await Page.GotoAsync(Server.EmptyPage);
        var requestBody = await requestBodyPromise;
        Assert.AreEqual(requestBody, JsonSerializer.Serialize(new Dictionary<string, object>() { { "foo", "bar" } }));
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "")]
    public async Task ShouldBeAbleToCallRouteFetchWithoutParameters()
    {
        await Page.RouteAsync("**/*", async (route) =>
        {
            var response = await route.FetchAsync();
            await route.FulfillAsync(new() { Response = response });
        });
        await Page.GotoAsync($"{Server.Prefix}/empty.html");
    }

    [PlaywrightTest("page-request-intercept.spec.ts", "should fulfill popup main request using alias")]
    public async Task ShouldFulfillPopupMainRequestUsingAlias()
    {
        await Page.Context.RouteAsync("**/*", async (route) =>
        {
            var response = await route.FetchAsync();
            await route.FulfillAsync(new() { Response = response, Body = "hello" });
        });
        await Page.SetContentAsync("<a target=_blank href=\"" + Server.EmptyPage + "\">click me</a>");
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.ClickAsync("a").ContinueWith(_ => Task.CompletedTask)
        );
        await Expect(popup.Locator("body")).ToHaveTextAsync("hello");
    }
}
