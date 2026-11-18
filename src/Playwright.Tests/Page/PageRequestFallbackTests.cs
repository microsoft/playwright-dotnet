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

using System.Text;
using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-request-fallback.spec.ts.</summary>
public class PageRequestFallbackTests : UpstreamPageTest
{
    [UpstreamTest("RO-120 should work")]
    public async Task ShouldWork()
    {
        await Page.RouteAsync("**/*", route => route.FallbackAsync());
        await Page.GotoAsync(Server.EmptyPage);
    }

    [UpstreamTest("RO-121 should fall back")]
    public async Task ShouldFallBack()
    {
        var intercepted = new List<int>();
        await Page.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(1);
            IgnoreErrors(route.FallbackAsync());
        });
        await Page.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(2);
            IgnoreErrors(route.FallbackAsync());
        });
        await Page.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(3);
            IgnoreErrors(route.FallbackAsync());
        });
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 3, 2, 1 }));
    }

    [UpstreamTest("RO-122 should fall back async")]
    public async Task ShouldFallBackAsync()
    {
        var intercepted = new List<int>();
        await Page.RouteAsync("**/empty.html", async route =>
        {
            intercepted.Add(1);
            await Task.Delay(100);
            IgnoreErrors(route.FallbackAsync());
        });
        await Page.RouteAsync("**/empty.html", async route =>
        {
            intercepted.Add(2);
            await Task.Delay(100);
            IgnoreErrors(route.FallbackAsync());
        });
        await Page.RouteAsync("**/empty.html", async route =>
        {
            intercepted.Add(3);
            await Task.Delay(100);
            IgnoreErrors(route.FallbackAsync());
        });
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 3, 2, 1 }));
    }

    [UpstreamTest("RO-123 should not chain fulfill")]
    public async Task ShouldNotChainFulfill()
    {
        var failed = false;
        await Page.RouteAsync("**/empty.html", route =>
        {
            failed = true;
        });
        await Page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Status = 200, Body = "fulfilled" }));
        });
        await Page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FallbackAsync());
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        var body = await response.BodyAsync();
        Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo("fulfilled"));
        Assert.That(failed, Is.False);
    }

    [UpstreamTest("RO-124 should not chain abort")]
    public async Task ShouldNotChainAbort()
    {
        var failed = false;
        await Page.RouteAsync("**/empty.html", route =>
        {
            failed = true;
        });
        await Page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.AbortAsync());
        });
        await Page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FallbackAsync());
        });
        var e = await Catch(() => Page.GotoAsync(Server.EmptyPage));
        Assert.That(e, Is.Not.Null);
        Assert.That(failed, Is.False);
    }

    [UpstreamTest("RO-126 should chain once")]
    public async Task ShouldChainOnce()
    {
        await Page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Status = 200, Body = "fulfilled one" }));
        }, new() { Times = 1 });
        await Page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FallbackAsync());
        }, new() { Times = 1 });
        var response = await Page.GotoAsync(Server.EmptyPage);
        var body = await response.BodyAsync();
        Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo("fulfilled one"));
    }

    [UpstreamTest("RO-127 should amend HTTP headers")]
    public async Task ShouldAmendHTTPHeaders()
    {
        var values = new List<string>();
        await Page.RouteAsync("**/sleep.zzz", async route =>
        {
            values.Add(route.Request.Headers["foo"]);
            values.Add(await route.Request.HeaderValueAsync("FOO"));
            IgnoreErrors(route.ContinueAsync());
        });
        await Page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FallbackAsync(new() { Headers = new Dictionary<string, string>(route.Request.Headers) { ["FOO"] = "bar" } }));
        });
        await Page.GotoAsync(Server.EmptyPage);
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/sleep.zzz"),
            Page.EvaluateAsync("() => fetch('/sleep.zzz')"));
        values.Add(request.Headers["foo"]);
        Assert.That(values, Is.EqualTo(new[] { "bar", "bar", "bar" }));
    }

    [UpstreamTest("RO-128 should delete header with undefined value")]
    [LocalFixme("dotnet: route.FallbackAsync() keeps a null header value, and the continue that follows fails with \"headers[N].value: expected string, got undefined\"; the Node.js client leaves out a header whose value is undefined")]
    public async Task ShouldDeleteHeaderWithUndefinedValue()
    {
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        Server.SetRoute("/something", (request, response) =>
        {
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("done");
        });
        IRequest interceptedRequest = null;
        await Page.RouteAsync("**/*", route =>
        {
            interceptedRequest = route.Request;
            IgnoreErrors(route.ContinueAsync());
        });
        await Page.RouteAsync(Server.Prefix + "/something", async route =>
        {
            var headers = await route.Request.AllHeadersAsync();
            IgnoreErrors(route.FallbackAsync(new()
            {
                Headers = new Dictionary<string, string>(headers)
                {
                    ["foo"] = null,
                },
            }));
        });
        var (text, serverRequest) = await PromiseAll(
            Page.EvaluateAsync<string>(@"async url => {
      const data = await fetch(url, {
        headers: {
          foo: 'a',
          bar: 'b',
        }
      });
      return data.text();
    }", Server.Prefix + "/something"),
            Server.WaitForRequest("/something"));
        Assert.That(text, Is.EqualTo("done"));
        // .NET: a dictionary throws for a key that it does not have.
        Assert.That(interceptedRequest.Headers.GetValueOrDefault("foo"), Is.Null);
        Assert.That(interceptedRequest.Headers["bar"], Is.EqualTo("b"));
        Assert.That(serverRequest.Headers["foo"], Is.Null.Or.Empty);
        Assert.That(serverRequest.Headers["bar"], Is.EqualTo("b"));
    }

    [UpstreamTest("RO-129 should amend method")]
    public async Task ShouldAmendMethod()
    {
        var sRequest = Server.WaitForRequest("/sleep.zzz");
        await Page.GotoAsync(Server.EmptyPage);

        string method = null;
        await Page.RouteAsync("**/*", route =>
        {
            method = route.Request.Method;
            IgnoreErrors(route.ContinueAsync());
        });
        await Page.RouteAsync("**/*", route => route.FallbackAsync(new() { Method = "POST" }));

        var (request, _) = await PromiseAll(
            Page.WaitForRequestAsync("**/sleep.zzz"),
            Page.EvaluateAsync("() => fetch('/sleep.zzz')"));
        Assert.That(method, Is.EqualTo("POST"));
        Assert.That(request.Method, Is.EqualTo("POST"));
        Assert.That((await sRequest).Method, Is.EqualTo("POST"));
    }

    [UpstreamTest("RO-130 should override request url")]
    public async Task ShouldOverrideRequestUrl()
    {
        var serverRequest = Server.WaitForRequest("/global-var.html");

        string url = null;
        await Page.RouteAsync("**/global-var.html", route =>
        {
            url = route.Request.Url;
            IgnoreErrors(route.ContinueAsync());
        });

        await Page.RouteAsync("**/foo", route => route.FallbackAsync(new() { Url = Server.Prefix + "/global-var.html" }));

        var response = await Page.GotoAsync(Server.Prefix + "/foo");
        Assert.That(url, Is.EqualTo(Server.Prefix + "/global-var.html"));
        Assert.That(response.Request.Url, Is.EqualTo(Server.Prefix + "/global-var.html"));
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/global-var.html"));
        Assert.That(await Page.EvaluateAsync<int>("() => window['globalVar']"), Is.EqualTo(123));
        Assert.That((await serverRequest).Method, Is.EqualTo("GET"));
    }

    public class PostData : UpstreamPageTest
    {
        [UpstreamTest("RO-131 should amend post data")]
        public async Task ShouldAmendPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            string postData = null;
            await Page.RouteAsync("**/*", route =>
            {
                postData = route.Request.PostData;
                IgnoreErrors(route.ContinueAsync());
            });
            await Page.RouteAsync("**/*", route =>
            {
                // .NET: postData is a byte[].
                IgnoreErrors(route.FallbackAsync(new() { PostData = Encoding.UTF8.GetBytes("doggo") }));
            });
            var (serverRequest, _) = await PromiseAll(
                Server.WaitForRequest("/sleep.zzz"),
                Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })"));
            Assert.That(postData, Is.EqualTo("doggo"));
            Assert.That(Encoding.UTF8.GetString(await serverRequest.PostBody), Is.EqualTo("doggo"));
        }

        [UpstreamTest("RO-132 should amend binary post data")]
        public async Task ShouldAmendBinaryPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var arr = Enumerable.Range(0, 256).ToArray();
            byte[] postDataBuffer = null;
            await Page.RouteAsync("**/*", route =>
            {
                postDataBuffer = route.Request.PostDataBuffer;
                IgnoreErrors(route.ContinueAsync());
            });
            await Page.RouteAsync("**/*", route =>
            {
                IgnoreErrors(route.FallbackAsync(new() { PostData = arr.Select(value => (byte)value).ToArray() }));
            });
            var (serverRequest, _) = await PromiseAll(
                Server.WaitForRequest("/sleep.zzz"),
                Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })"));
            var buffer = await serverRequest.PostBody;
            Assert.That(postDataBuffer.Length, Is.EqualTo(arr.Length));
            Assert.That(buffer.Length, Is.EqualTo(arr.Length));
            for (var i = 0; i < arr.Length; ++i)
            {
                Assert.That(buffer[i], Is.EqualTo(arr[i]));
                Assert.That(postDataBuffer[i], Is.EqualTo(arr[i]));
            }
        }

        [UpstreamTest("RO-133 should amend json post data")]
        public async Task ShouldAmendJsonPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            JsonElement? postData = null;
            await Page.RouteAsync("**/*", route =>
            {
                postData = route.Request.PostDataJSON();
                IgnoreErrors(route.ContinueAsync());
            });
            await Page.RouteAsync("**/*", route =>
            {
                // .NET: postData is a byte[]: { foo: 'bar' } is the JSON that the Node.js client makes of it.
                IgnoreErrors(route.FallbackAsync(new() { PostData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { foo = "bar" })) }));
            });
            var (serverRequest, _) = await PromiseAll(
                Server.WaitForRequest("/sleep.zzz"),
                Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })"));
            ExpectToEqual(postData, new { foo = "bar" });
            Assert.That(Encoding.UTF8.GetString(await serverRequest.PostBody), Is.EqualTo("{\"foo\":\"bar\"}"));
        }
    }
}
