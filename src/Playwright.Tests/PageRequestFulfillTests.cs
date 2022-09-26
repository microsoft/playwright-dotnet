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

using System.Net;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class RequestFulfillTests : PageTestEx
{
    [PlaywrightTest("page-request-fulfill.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new()
            {
                Status = (int)HttpStatusCode.Created,
                Headers = new Dictionary<string, string>
                {
                    ["foo"] = "bar"
                },
                ContentType = "text/html",
                Body = "Yo, page!",
            });
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.Created, response.Status);
#pragma warning disable 0612
        Assert.AreEqual("bar", response.Headers["foo"]);
#pragma warning restore 0612
        Assert.AreEqual("Yo, page!", await Page.EvaluateAsync<string>("() => document.body.textContent"));
    }

    /// <summary>
    /// In Playwright this method is called ShouldWorkWithStatusCode422.
    /// I found that status 422 is not available in all .NET runtime versions (see https://github.com/dotnet/core/blob/4c4642d548074b3fbfd425541a968aadd75fea99/release-notes/2.1/Preview/api-diff/preview2/2.1-preview2_System.Net.md)
    /// As the goal here is testing HTTP codes that are not in Chromium (see https://cs.chromium.org/chromium/src/net/http/http_status_code_list.h?sq=package:chromium) we will use code 426: Upgrade Required
    /// </summary>
    [PlaywrightTest("page-request-fulfill.spec.ts", "should work with status code 422")]
    public async Task ShouldWorkWithStatusCode422()
    {
        await Page.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new() { Status = (int)HttpStatusCode.UpgradeRequired, Body = "Yo, page!" });
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.UpgradeRequired, response.Status);
        Assert.AreEqual("Upgrade Required", response.StatusText);
        Assert.AreEqual("Yo, page!", await Page.EvaluateAsync<string>("() => document.body.textContent"));
    }

    [PlaywrightTest("page-request-fulfill.spec.ts", "should allow mocking binary responses")]
    [Ignore("We need screenshots for this")]
    public async Task ShouldAllowMockingBinaryResponses()
    {
        await Page.RouteAsync("**/*", (route) =>
        {
            byte[] imageBuffer = File.ReadAllBytes(TestUtils.GetAsset("pptr.png"));
            route.FulfillAsync(new()
            {
                ContentType = "image/png",
                BodyBytes = imageBuffer,
            });
        });
        await Page.EvaluateAsync(@"PREFIX => {
                const img = document.createElement('img');
                img.src = PREFIX + '/does-not-exist.png';
                document.body.appendChild(img);
                return new Promise(fulfill => img.onload = fulfill);
            }", Server.Prefix);
        var img = await Page.QuerySelectorAsync("img");
        Assert.True(ScreenshotHelper.PixelMatch("mock-binary-response.png", await img.ScreenshotAsync()));
    }

    [PlaywrightTest("page-request-fulfill.spec.ts", "should allow mocking svg with charset")]
    [Ignore("We need screenshots for this")]
    public void ShouldAllowMockingSvgWithCharset()
    {
    }

    [PlaywrightTest("page-request-fulfill.spec.ts", "should work with file path")]
    [Ignore("We need screenshots for this")]
    public async Task ShouldWorkWithFilePath()
    {
        await Page.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new()
            {
                ContentType = "shouldBeIgnored",
                Path = TestUtils.GetAsset("pptr.png"),
            });
        });

        await Page.EvaluateAsync(@"PREFIX => {
                const img = document.createElement('img');
                img.src = PREFIX + '/does-not-exist.png';
                document.body.appendChild(img);
                return new Promise(fulfill => img.onload = fulfill);
            }", Server.Prefix);
        var img = await Page.QuerySelectorAsync("img");
        Assert.True(ScreenshotHelper.PixelMatch("mock-binary-response.png", await img.ScreenshotAsync()));
    }

    [PlaywrightTest("page-request-fulfill.spec.ts", "should stringify intercepted request response headers")]
    public async Task ShouldStringifyInterceptedRequestResponseHeaders()
    {
        await Page.RouteAsync("**/*", (route) =>
        {
            route.FulfillAsync(new()
            {
                Status = (int)HttpStatusCode.OK,
                Headers = new Dictionary<string, string>
                {
                    ["foo"] = "true"
                },
                Body = "Yo, page!",
            });
        });

        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
#pragma warning disable 0612
        Assert.AreEqual("true", response.Headers["foo"]);
#pragma warning restore 0612
        Assert.AreEqual("Yo, page!", await Page.EvaluateAsync<string>("() => document.body.textContent"));
    }

    [PlaywrightTest("page-request-fulfill.spec.ts", "should not modify the headers sent to the server")]
    [Ignore("Flacky with the ASP.NET server")]
    public async Task ShouldNotModifyTheHeadersSentToTheServer()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var interceptedRequests = new List<Dictionary<string, string>>();

        await Page.GotoAsync(Server.Prefix + "/unused");

        Server.SetRoute("/something", ctx =>
        {
            var hh = new Dictionary<string, string>();
            foreach (var h in ctx.Request.Headers)
            {
                hh[h.Key] = h.Value;
            }
            interceptedRequests.Add(hh);
            ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
            return ctx.Response.WriteAsync("done");
        });

        string text = await Page.EvaluateAsync<string>(@"async url => {
                const data = await fetch(url);
                return data.text();
            }", Server.CrossProcessPrefix + "/something");

        Assert.AreEqual("done", text);

        IRequest playwrightRequest = null;

        await Page.RouteAsync(Server.CrossProcessPrefix + "/something", (route) =>
        {
            playwrightRequest = route.Request;
#pragma warning disable 0612
            route.ContinueAsync(new() { Headers = route.Request.Headers.ToDictionary(x => x.Key, x => x.Value) });
#pragma warning restore 0612
        });

        string textAfterRoute = await Page.EvaluateAsync<string>(@"async url => {
                const data = await fetch(url);
                return data.text();
            }", Server.CrossProcessPrefix + "/something");

        Assert.AreEqual("done", textAfterRoute);

        Assert.AreEqual(2, interceptedRequests.Count);
        Assert.AreEqual(interceptedRequests[1].OrderBy(kv => kv.Key), interceptedRequests[0].OrderBy(kv => kv.Key));
    }

    [PlaywrightTest("page-request-fulfill.spec.ts", "should include the origin header")]
    public async Task ShouldIncludeTheOriginHeader()
    {
        await Page.GotoAsync(Server.EmptyPage);
        IRequest interceptedRequest = null;

        await Page.RouteAsync(Server.CrossProcessPrefix + "/something", (route) =>
        {
            interceptedRequest = route.Request;
            route.FulfillAsync(new()
            {
                Headers = new Dictionary<string, string> { ["Access-Control-Allow-Origin"] = "*" },
                ContentType = "text/plain",
                Body = "done",
            });
        });

        string text = await Page.EvaluateAsync<string>(@"async url => {
                const data = await fetch(url);
                return data.text();
            }", Server.CrossProcessPrefix + "/something");

        Assert.AreEqual("done", text);
#pragma warning disable 0612
        Assert.AreEqual(Server.Prefix, interceptedRequest.Headers["origin"]);
#pragma warning restore 0612
    }

    [PlaywrightTest("page-request-fulfill.spec.ts", "should fulfill with global fetch result")]
    public async Task ShouldFulfillWithGlobalFetchResult()
    {
        await Page.RouteAsync("**/*", async (route) =>
        {
            var request = await Playwright.APIRequest.NewContextAsync();
            var response = await request.GetAsync(Server.Prefix + "/simple.json");
            await route.FulfillAsync(new() { Response = response });
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(response.Status, 200);
        Assert.AreEqual(await response.TextAsync(), "{\"foo\": \"bar\"}\n");
    }

    [PlaywrightTest("page-request-fulfill.spec.ts", "should fulfill with fetch result")]
    public async Task ShouldFulfillWithFetchResult()
    {
        await Page.RouteAsync("**/*", async (route) =>
        {
            var response = await Page.APIRequest.GetAsync(Server.Prefix + "/simple.json");
            await route.FulfillAsync(new() { Response = response });
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(response.Status, 200);
        Assert.AreEqual(await response.TextAsync(), "{\"foo\": \"bar\"}\n");
    }

    [PlaywrightTest("page-request-fulfill.spec.ts", "should fulfill with fetch result and overrides")]
    public async Task ShouldFulfillWithFetchResultAndOverrides()
    {
        await Page.RouteAsync("**/*", async (route) =>
        {
            var response = await Page.APIRequest.GetAsync(Server.Prefix + "/simple.json");
            await route.FulfillAsync(new() { Response = response, Status = 201, Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json", ["foo"] = "bar" } });
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(response.Status, 201);
        Assert.AreEqual((await response.AllHeadersAsync())["foo"], "bar");
        Assert.AreEqual(await response.TextAsync(), "{\"foo\": \"bar\"}\n");
    }

    [PlaywrightTest("page-request-fulfill.spec.ts", "should fetch original request and fulfill")]
    public async Task ShouldFetchOriginalRequestAndFulfill()
    {
        await Page.RouteAsync("**/*", async (route) =>
        {
            var response = await Page.APIRequest.FetchAsync(route.Request);
            await route.FulfillAsync(new() { Response = response });
        });
        var response = await Page.GotoAsync(Server.Prefix + "/title.html");
        Assert.AreEqual(response.Status, 200);
        Assert.AreEqual(await Page.TitleAsync(), "Woof-Woof");
    }

    [PlaywrightTest("page-request-fulfill.spec.ts", "should fulfill with multiple set-cookie")]
    public async Task ShouldFulfillWithMultipleSetCookies()
    {
        var cookies = new List<string>(){
                "a=b",
                "c=d",
            };
        await Page.RouteAsync("**/empty.html", async (route) =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                Headers = new Dictionary<string, string>
                {
                    ["X-Header-1"] = "v1",
                    ["Set-Cookie"] = string.Join("\n", cookies),
                    ["X-Header-2"] = "v2",
                },
                Body = ""
            });
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((await Page.EvaluateAsync<string[]>("() => document.cookie.split(';').map(s => s.trim()).sort()")), cookies);
        Assert.AreEqual(await response.HeaderValueAsync("X-Header-1"), "v1");
        Assert.AreEqual(await response.HeaderValueAsync("X-Header-2"), "v2");
    }

    [PlaywrightTest("page-request-fulfill.spec.ts", "should fulfill with fetch response that has multiple set-cookie")]
    public async Task ShouldFulfillWithFetchResponseThatHasMultipleSetCookie()
    {
        Server.SetRoute("/empty.html", context =>
        {
            context.Response.Headers.Add("Set-Cookie", new Extensions.Primitives.StringValues(new[] { "a=b", "c=d" }));
            context.Response.Headers.Add("Content-Type", "text/html");
            return Task.CompletedTask;
        });
        await Page.RouteAsync("**/empty.html", async (route) =>
        {
            var request = await Playwright.APIRequest.NewContextAsync();
            var response = await request.FetchAsync(route.Request);
            await route.FulfillAsync(new() { Response = response });
        });
        await Page.GotoAsync(Server.EmptyPage);
        var cookie = await Page.EvaluateAsync<string[]>("() => document.cookie.split(';').map(s => s.trim()).sort()");
        Assert.AreEqual(cookie, new[] { "a=b", "c=d" });
    }
}
