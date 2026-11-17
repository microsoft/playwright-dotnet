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
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-request-continue.spec.ts.</summary>
public class PageRequestContinueTests
{
    [UpstreamTest("RO-057 should work")]
    public async Task ShouldWork(IPage page, Server server)
    {
        await page.RouteAsync("**/*", route => route.ContinueAsync());
        await page.GotoAsync(server.EmptyPage);
    }

    [UpstreamTest("RO-058 should amend HTTP headers")]
    public async Task ShouldAmendHTTPHeaders(IPage page, Server server)
    {
        await page.RouteAsync("**/*", route =>
        {
            var headers = new Dictionary<string, string>(route.Request.Headers)
            {
                ["FOO"] = "bar",
            };
            IgnoreErrors(route.ContinueAsync(new() { Headers = headers }));
        });
        await page.GotoAsync(server.EmptyPage);
        var (request, _) = await PromiseAll(
            server.WaitForRequest("/sleep.zzz"),
            page.EvaluateAsync("() => fetch('/sleep.zzz')"));
        Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("RO-059 should not allow to override unsafe HTTP headers")]
    public async Task ShouldNotAllowToOverrideUnsafeHTTPHeaders(IPage page, Server server, string browserName, bool isAndroid, bool isElectron)
    {
        It.Skip(isAndroid);
        var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/*", route => routePromise.TrySetResult(route));
        var serverRequestPromise = server.WaitForRequest("/empty.html");
        IgnoreErrors(page.GotoAsync(server.EmptyPage));
        var route = await routePromise.Task;
        await route.ContinueAsync(new()
        {
            Headers = new Dictionary<string, string>(route.Request.Headers)
            {
                ["host"] = "bar",
                ["trailer"] = "baz",
            },
        });
        var serverRequest = await serverRequestPromise;
        Assert.That(serverRequest.Headers["trailer"], Is.Null);
        Assert.That(serverRequest.Headers["host"], Is.EqualTo(new Uri(server.EmptyPage).Authority));
    }

    [UpstreamTest("RO-060 should delete header with undefined value")]
    [Fixme("dotnet: route.ContinueAsync() with a null header value fails with \"headers[N].value: expected string, got undefined\"; the Node.js client leaves out a header whose value is undefined")]
    public async Task ShouldDeleteHeaderWithUndefinedValue(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.Prefix + "/empty.html");
        server.SetRoute("/something", (request, response) =>
        {
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("done");
        });
        IRequest interceptedRequest = null;
        await page.RouteAsync(server.Prefix + "/something", async route =>
        {
            interceptedRequest = route.Request;
            var headers = await route.Request.AllHeadersAsync();
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(headers)
                {
                    ["foo"] = null,
                },
            }));
        });

        var (text, serverRequest) = await PromiseAll(
            page.EvaluateAsync<string>(@"async url => {
      const data = await fetch(url, {
        headers: {
          foo: 'a',
          bar: 'b',
        }
      });
      return data.text();
    }", server.Prefix + "/something"),
            server.WaitForRequest("/something"));
        Assert.That(text, Is.EqualTo("done"));
        // .NET: a dictionary throws for a key that it does not have.
        Assert.That(interceptedRequest.Headers.GetValueOrDefault("foo"), Is.Null);
        Assert.That(serverRequest.Headers["foo"], Is.Null.Or.Empty);
        Assert.That(serverRequest.Headers["bar"], Is.EqualTo("b"));
    }

    [UpstreamTest("RO-061 should amend method")]
    public async Task ShouldAmendMethod(IPage page, Server server)
    {
        var sRequest = server.WaitForRequest("/sleep.zzz");
        await page.GotoAsync(server.EmptyPage);
        await page.RouteAsync("**/*", route => route.ContinueAsync(new() { Method = "POST" }));
        var (request, _) = await PromiseAll(
            server.WaitForRequest("/sleep.zzz"),
            page.EvaluateAsync("() => fetch('/sleep.zzz')"));
        Assert.That(request.Method, Is.EqualTo("POST"));
        Assert.That((await sRequest).Method, Is.EqualTo("POST"));
    }

    [UpstreamTest("RO-062 should override request url")]
    public async Task ShouldOverrideRequestUrl(IPage page, Server server)
    {
        var serverRequest = server.WaitForRequest("/global-var.html");
        await page.RouteAsync("**/foo", route =>
        {
            IgnoreErrors(route.ContinueAsync(new() { Url = server.Prefix + "/global-var.html" }));
        });
        var response = await page.GotoAsync(server.Prefix + "/foo");
        Assert.That(response.Request.Url, Is.EqualTo(server.Prefix + "/global-var.html"));
        Assert.That(response.Url, Is.EqualTo(server.Prefix + "/global-var.html"));
        Assert.That(await page.EvaluateAsync<int>("() => window['globalVar']"), Is.EqualTo(123));
        Assert.That((await serverRequest).Method, Is.EqualTo("GET"));
    }

    [UpstreamTest("RO-063 should not allow changing protocol when overriding url")]
    public async Task ShouldNotAllowChangingProtocolWhenOverridingUrl(IPage page, Server server)
    {
        var errorPromise = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/*", async route =>
        {
            try
            {
                await route.ContinueAsync(new() { Url = "file:///tmp/foo" });
                errorPromise.TrySetResult(null);
            }
            catch (Exception e)
            {
                errorPromise.TrySetResult(e);
            }
        });
        IgnoreErrors(page.GotoAsync(server.EmptyPage));
        var error = await errorPromise.Task;
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("New URL must have same protocol as overridden URL"));
    }

    [UpstreamTest("RO-064 should not throw if request was cancelled by the page")]
    public async Task ShouldNotThrowIfRequestWasCancelledByThePage(IPage page, Server server, bool isAndroid)
    {
        It.Fixme(isAndroid, "net::ERR_CONNECTION_RESET");

        var interceptPromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/data.json", route => interceptPromise.TrySetResult(route));
        await page.GotoAsync(server.EmptyPage);
        IgnoreErrors(page.EvaluateAsync(@"url => {
    globalThis.controller = new AbortController();
    return fetch(url, { signal: globalThis.controller.signal });
  }", server.Prefix + "/data.json"));
        var route = await interceptPromise.Task;
        var failurePromise = Once<IRequest>(h => page.RequestFailed += h, h => page.RequestFailed -= h);
        await page.EvaluateAsync("() => globalThis.controller.abort()");
        var cancelledRequest = await failurePromise;
        Assert.That(cancelledRequest.Failure, Does.Match("cancelled|aborted").IgnoreCase);
        await route.ContinueAsync(); // Should not throw.
    }

    [UpstreamTest("RO-065 should override method along with url")]
    public async Task ShouldOverrideMethodAlongWithUrl(IPage page, Server server)
    {
        var request = server.WaitForRequest("/empty.html");
        await page.RouteAsync("**/foo", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Url = server.EmptyPage,
                Method = "POST",
            }));
        });
        await page.GotoAsync(server.Prefix + "/foo");
        Assert.That((await request).Method, Is.EqualTo("POST"));
    }

    [UpstreamTest("RO-066 should amend method on main request")]
    public async Task ShouldAmendMethodOnMainRequest(IPage page, Server server)
    {
        var request = server.WaitForRequest("/empty.html");
        await page.RouteAsync("**/*", route => route.ContinueAsync(new() { Method = "POST" }));
        await page.GotoAsync(server.EmptyPage);
        Assert.That((await request).Method, Is.EqualTo("POST"));
    }

    public class PostData
    {
        [UpstreamTest("RO-067 should amend post data")]
        public async Task ShouldAmendPostData(IPage page, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            await page.RouteAsync("**/*", route =>
            {
                // .NET: postData is a byte[].
                IgnoreErrors(route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("doggo") }));
            });
            var (serverRequest, _) = await PromiseAll(
                server.WaitForRequest("/sleep.zzz"),
                page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })"));
            Assert.That(Encoding.UTF8.GetString(await serverRequest.PostBody), Is.EqualTo("doggo"));
        }

        [UpstreamTest("RO-068 should compute content-length from post data")]
        public async Task ShouldComputeContentLengthFromPostData(IPage page, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            var data = new string('a', 7500);
            await page.RouteAsync("**/*", route =>
            {
                var headers = route.Request.Headers;
                headers["content-type"] = "application/json";
                // .NET: postData is a byte[].
                IgnoreErrors(route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes(data), Headers = headers }));
            });
            var (serverRequest, _) = await PromiseAll(
                server.WaitForRequest("/sleep.zzz"),
                page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'PATCH', body: 'birdy' })"));
            Assert.That(Encoding.UTF8.GetString(await serverRequest.PostBody), Is.EqualTo(data));
            Assert.That(serverRequest.Headers["content-length"], Is.EqualTo(data.Length.ToString(CultureInfo.InvariantCulture)));
            Assert.That(serverRequest.Headers["content-type"], Is.EqualTo("application/json"));
        }

        [UpstreamTest("RO-069 should amend method and post data")]
        public async Task ShouldAmendMethodAndPostData(IPage page, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            await page.RouteAsync("**/*", route =>
            {
                // .NET: postData is a byte[].
                IgnoreErrors(route.ContinueAsync(new() { Method = "POST", PostData = Encoding.UTF8.GetBytes("doggo") }));
            });
            var (serverRequest, _) = await PromiseAll(
                server.WaitForRequest("/sleep.zzz"),
                page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'GET' })"));
            Assert.That(serverRequest.Method, Is.EqualTo("POST"));
            Assert.That(Encoding.UTF8.GetString(await serverRequest.PostBody), Is.EqualTo("doggo"));
        }

        [UpstreamTest("RO-070 should amend utf8 post data")]
        public async Task ShouldAmendUtf8PostData(IPage page, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            await page.RouteAsync("**/*", route =>
            {
                // .NET: postData is a byte[].
                IgnoreErrors(route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("пушкин") }));
            });
            var (serverRequest, _) = await PromiseAll(
                server.WaitForRequest("/sleep.zzz"),
                page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })"));
            Assert.That(serverRequest.Method, Is.EqualTo("POST"));
            Assert.That(Encoding.UTF8.GetString(await serverRequest.PostBody), Is.EqualTo("пушкин"));
        }

        [UpstreamTest("RO-071 should amend longer post data")]
        public async Task ShouldAmendLongerPostData(IPage page, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            await page.RouteAsync("**/*", route =>
            {
                // .NET: postData is a byte[].
                IgnoreErrors(route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("doggo-is-longer-than-birdy") }));
            });
            var (serverRequest, _) = await PromiseAll(
                server.WaitForRequest("/sleep.zzz"),
                page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })"));
            Assert.That(serverRequest.Method, Is.EqualTo("POST"));
            Assert.That(Encoding.UTF8.GetString(await serverRequest.PostBody), Is.EqualTo("doggo-is-longer-than-birdy"));
        }

        [UpstreamTest("RO-072 should amend binary post data")]
        public async Task ShouldAmendBinaryPostData(IPage page, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            var arr = Enumerable.Range(0, 256).ToArray();
            await page.RouteAsync("**/*", route =>
            {
                IgnoreErrors(route.ContinueAsync(new() { PostData = arr.Select(value => (byte)value).ToArray() }));
            });
            var (serverRequest, _) = await PromiseAll(
                server.WaitForRequest("/sleep.zzz"),
                page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })"));
            Assert.That(serverRequest.Method, Is.EqualTo("POST"));
            var buffer = await serverRequest.PostBody;
            Assert.That(buffer.Length, Is.EqualTo(arr.Length));
            for (var i = 0; i < arr.Length; ++i)
            {
                Assert.That(arr[i], Is.EqualTo(buffer[i]));
            }
        }

        [UpstreamTest("RO-073 should use content-type from original request")]
        public async Task ShouldUseContentTypeFromOriginalRequest(IPage page, Server server, string browserName)
        {
            await page.GotoAsync(server.EmptyPage);
            // .NET: postData is a byte[].
            await page.RouteAsync($"{server.Prefix}/title.html", route => route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("{\"b\":2}") }));
            var (request, _) = await PromiseAll(
                server.WaitForRequest("/title.html"),
                page.EvaluateAsync(@"async url => {
        await fetch(url, {
          method: 'POST',
          body: '{""a"":1}',
          headers: { 'content-type': 'application/json' },
        });
      }", $"{server.Prefix}/title.html"));
            Assert.That(request.Headers["content-type"], Is.EqualTo("application/json"));
            Assert.That(Encoding.UTF8.GetString(await request.PostBody), Is.EqualTo("{\"b\":2}"));
        }
    }

    [UpstreamTest("RO-074 should work with Cross-Origin-Opener-Policy")]
    public async Task ShouldWorkWithCrossOriginOpenerPolicy(IPage page, Server server, string browserName)
    {
        It.Fixme(browserName == "webkit", "https://github.com/microsoft/playwright/issues/8796");
        ServerHeaders serverHeaders = null;
        var serverRequests = new List<string>();
        server.SetRoute("/empty.html", (req, res) =>
        {
            serverRequests.Add(req.Url);
            serverHeaders ??= req.Headers;
            res.SetHeader("Cross-Origin-Opener-Policy", "same-origin");
            // Note: without 'onload', Firefox sometimes does not fire the load event
            // over the protocol. The reason is unclear.
            res.End(@"
      <div>Hello there!</div>
      <script>window.onload = () => console.log('onload')</script>
    ");
        });

        var intercepted = new List<string>();
        await page.RouteAsync("**/*", route =>
        {
            intercepted.Add(route.Request.Url);
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                },
            }));
        });
        var requests = new HashSet<IRequest>();
        var events = new List<string>();
        page.Request += (_, r) =>
        {
            events.Add("request");
            requests.Add(r);
        };
        page.RequestFailed += (_, r) =>
        {
            events.Add("requestfailed");
            requests.Add(r);
        };
        page.RequestFinished += (_, r) =>
        {
            events.Add("requestfinished");
            requests.Add(r);
        };
        page.Response += (_, r) =>
        {
            events.Add("response");
            requests.Add(r.Request);
        };
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { server.EmptyPage }));
        // There should be only one request to the server.
        if (browserName == "webkit")
        {
            Assert.That(serverRequests, Is.EqualTo(new[] { "/empty.html", "/empty.html" }));
        }
        else
        {
            Assert.That(serverRequests, Is.EqualTo(new[] { "/empty.html" }));
        }
        Assert.That(serverHeaders["foo"], Is.EqualTo("bar"));
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
        await response.FinishedAsync();
        Assert.That(events, Is.EqualTo(new[] { "request", "response", "requestfinished" }));
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(response.Request.Failure, Is.Null);
    }

    [UpstreamTest("RO-075 should not delete the origin header")]
    public async Task ShouldNotDeleteTheOriginHeader(IPage page, Server server, bool isAndroid)
    {
        It.Skip(isAndroid, "No cross-process on Android");

        await page.GotoAsync(server.Prefix + "/empty.html");
        server.SetRoute("/something", (request, response) =>
        {
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("done");
        });
        string interceptedOrigin = null;
        await page.RouteAsync(server.CrossProcessPrefix + "/something", async route =>
        {
            var headers = await route.Request.AllHeadersAsync();
            interceptedOrigin = headers["origin"];
            headers.Remove("origin");
            IgnoreErrors(route.ContinueAsync(new() { Headers = headers }));
        });

        var (text, serverRequest) = await PromiseAll(
            page.EvaluateAsync<string>(@"async url => {
      const data = await fetch(url);
      return data.text();
    }", server.CrossProcessPrefix + "/something"),
            server.WaitForRequest("/something"));
        Assert.That(text, Is.EqualTo("done"));
        Assert.That(interceptedOrigin, Is.EqualTo(server.Prefix));
        Assert.That(serverRequest.Headers["origin"], Is.EqualTo(server.Prefix));
    }

    [UpstreamTest("RO-076 should continue preload link requests")]
    public async Task ShouldContinuePreloadLinkRequests(IPage page, Server server)
    {
        var intercepted = false;
        await page.RouteAsync("**/one-style.css", route =>
        {
            intercepted = true;
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers)
                {
                    ["custom"] = "value",
                },
            }));
        });
        var (serverRequest, _) = await PromiseAll(
            server.WaitForRequest("/one-style.css"),
            page.GotoAsync(server.Prefix + "/preload.html"));
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("value"));
        await page.WaitForFunctionAsync("() => window.preloadedStyles");
        Assert.That(intercepted, Is.EqualTo(true));
        var color = await page.EvaluateAsync<string>("() => window.getComputedStyle(document.body).backgroundColor");
        Assert.That(color, Is.EqualTo("rgb(255, 192, 203)"));
    }

    [UpstreamTest("RO-077 should respect set-cookie in redirect response")]
    public async Task ShouldRespectSetCookieInRedirectResponse(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a href=\"/set-cookie-redirect\">Set cookie</a>");
        server.SetRoute("/set-cookie-redirect", (request, response) =>
        {
            response.WriteHead(302, new()
            {
                ["set-cookie"] = "foo=bar;  max-age=36000",
                ["location"] = "/empty.html",
            });
            response.End();
        });
        await page.RouteAsync("**/set-cookie-redirect", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers),
            }));
        });
        var serverRequestPromise = server.WaitForRequest("/empty.html");
        await page.GotoAsync(server.Prefix + "/set-cookie-redirect");
        var serverRequest = await serverRequestPromise;
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("foo=bar"));
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("foo=bar"));
    }

    [UpstreamTest("RO-078 continue should not propagate cookie override to redirects")]
    public async Task ContinueShouldNotPropagateCookieOverrideToRedirects(IPage page, Server server, string browserName)
    {
        server.SetRoute("/set-cookie", (request, response) =>
        {
            response.WriteHead(200, new() { ["Set-Cookie"] = "foo=bar;" });
            response.End();
        });
        await page.GotoAsync(server.Prefix + "/set-cookie");
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("foo=bar"));
        server.SetRedirect("/redirect", server.Prefix + "/empty.html");
        await page.RouteAsync("**/redirect", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers)
                {
                    ["cookie"] = "override",
                },
            }));
        });
        var (serverRequest, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.Prefix + "/redirect"));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("foo=bar"));
    }

    [UpstreamTest("RO-079 continue should not override cookie")]
    public async Task ContinueShouldNotOverrideCookie(IPage page, Server server)
    {
        server.SetRoute("/set-cookie", (request, response) =>
        {
            response.WriteHead(200, new() { ["Set-Cookie"] = "foo=bar;" });
            response.End();
        });
        await page.GotoAsync(server.Prefix + "/set-cookie");
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("foo=bar"));
        await page.RouteAsync("**", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers)
                {
                    ["cookie"] = "override",
                    ["custom"] = "value",
                },
            }));
        });
        var (serverRequest, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.EmptyPage));
        // Original cookie from the browser's cookie jar should be sent.
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("foo=bar"));
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("value"));
    }

    [UpstreamTest("RO-080 continue with headers should send fresh cookie from the browser cookie store")]
    public async Task ContinueWithHeadersShouldSendFreshCookieFromTheBrowserCookieStore(IPage page, Server server)
    {
        server.SetRoute("/set-cookie", (request, response) =>
        {
            response.WriteHead(200, new() { ["Set-Cookie"] = "foo=v1;" });
            response.End();
        });
        await page.GotoAsync(server.Prefix + "/set-cookie");
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("foo=v1"));

        await page.RouteAsync("**/empty.html", async route =>
        {
            // Cookie store changes between interception and continuation.
            await page.Context.AddCookiesAsync(new[] { new Cookie { Name = "foo", Value = "v2", Url = server.Prefix } });
            await route.ContinueAsync(new() { Headers = route.Request.Headers });
        });

        var (serverRequest, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.EmptyPage));
        // The fresh cookie from the browser cookie store should be sent, not the stale captured value.
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("foo=v2"));
    }

    [UpstreamTest("RO-081 continue with headers should send fresh cookie after a redirect")]
    public async Task ContinueWithHeadersShouldSendFreshCookieAfterARedirect(IPage page, Server server)
    {
        server.SetRoute("/set-cookie", (request, response) =>
        {
            response.WriteHead(200, new() { ["Set-Cookie"] = "foo=v1;" });
            response.End();
        });
        await page.GotoAsync(server.Prefix + "/set-cookie");
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("foo=v1"));

        // The redirect updates the cookie before bouncing to the final destination.
        server.SetRoute("/redirect", (request, response) =>
        {
            response.WriteHead(302, new() { ["Set-Cookie"] = "foo=v2;", ["location"] = server.Prefix + "/empty.html" });
            response.End();
        });

        await page.RouteAsync("**/redirect", route =>
        {
            IgnoreErrors(route.ContinueAsync(new() { Headers = route.Request.Headers }));
        });

        var (serverRequest, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.Prefix + "/redirect"));
        // The redirected request should carry the cookie updated by the redirect, not the stale captured value.
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("foo=v2"));
    }

    [UpstreamTest("RO-082 redirect after continue should be able to delete cookie")]
    public async Task RedirectAfterContinueShouldBeAbleToDeleteCookie(IPage page, Server server)
    {
        server.SetRoute("/set-cookie", (request, response) =>
        {
            response.WriteHead(200, new() { ["Set-Cookie"] = "foo=bar;" });
            response.End();
        });
        await page.GotoAsync(server.Prefix + "/set-cookie");
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("foo=bar"));

        server.SetRoute("/delete-cookie", (request, response) =>
        {
            response.WriteHead(200, new() { ["Set-Cookie"] = "foo=bar; expires=Thu, 01 Jan 1970 00:00:00 GMT" });
            response.End();
        });
        server.SetRedirect("/redirect", "/delete-cookie");
        await page.RouteAsync("**/redirect", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers),
            }));
        });
        await page.GotoAsync(server.Prefix + "/redirect");
        var (serverRequest, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.Null.Or.Empty);
    }

    [UpstreamTest("RO-083 continue should propagate headers to redirects")]
    public async Task ContinueShouldPropagateHeadersToRedirects(IPage page, Server server)
    {
        server.SetRedirect("/redirect", "/empty.html");
        await page.RouteAsync("**/redirect", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers)
                {
                    ["custom"] = "value",
                },
            }));
        });
        var (serverRequest, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.Prefix + "/redirect"));
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("value"));
    }

    [UpstreamTest("RO-084 continue should drop content-length on redirects")]
    public async Task ContinueShouldDropContentLengthOnRedirects(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);

        server.SetRedirect("/redirect", "/empty.html");
        await page.RouteAsync("**/redirect", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers)
                {
                    ["custom"] = "value",
                },
            }));
        });
        var (serverRequest, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.EvaluateAsync("url => fetch(url, { method: 'POST', body: 'foo' })", server.Prefix + "/redirect"));
        Assert.That(serverRequest.Method, Is.EqualTo("GET"));
        Assert.That(serverRequest.Headers["content-length"], Is.Null);
        Assert.That(serverRequest.Headers["content-type"], Is.Null);
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("value"));
    }

    [UpstreamTest("RO-085 redirected requests should report overridden headers")]
    public async Task RedirectedRequestsShouldReportOverriddenHeaders(IPage page, Server server)
    {
        server.SetRedirect("/redirect", "/empty.html");
        await page.RouteAsync("**/redirect", route =>
        {
            var headers = route.Request.Headers;
            headers["custom"] = "value";
            IgnoreErrors(route.FallbackAsync(new() { Headers = headers }));
        });

        var (serverRequest, response) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.Prefix + "/redirect"));
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("value"));
        Assert.That(response.Request.Url, Is.EqualTo(server.EmptyPage));
        Assert.That(response.Request.Headers["custom"], Is.EqualTo("value"));
        Assert.That((await response.Request.AllHeadersAsync())["custom"], Is.EqualTo("value"));
    }

    [UpstreamTest("RO-086 continue should delete headers on redirects")]
    [Fixme("dotnet: route.ContinueAsync() with a null header value fails with \"headers[N].value: expected string, got undefined\"; the Node.js client leaves out a header whose value is undefined")]
    public async Task ContinueShouldDeleteHeadersOnRedirects(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/empty.html");
        server.SetRoute("/something", (request, response) =>
        {
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("done");
        });
        server.SetRedirect("/redirect", "/something");
        await page.RouteAsync("**/redirect", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers)
                {
                    ["foo"] = null,
                },
            }));
        });
        var (text, serverRequest) = await PromiseAll(
            page.EvaluateAsync<string>(@"async url => {
      const data = await fetch(url, {
        headers: {
          foo: 'a',
        }
      });
      return data.text();
    }", server.Prefix + "/redirect"),
            server.WaitForRequest("/something"));
        Assert.That(text, Is.EqualTo("done"));
        Assert.That(serverRequest.Headers["foo"], Is.Null.Or.Empty);
    }

    [UpstreamTest("RO-087 propagate headers same origin redirect")]
    public async Task PropagateHeadersSameOriginRedirect(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/empty.html");
        var serverRequestPromise = new TaskCompletionSource<ServerRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/something", (request, response) =>
        {
            if (request.Method == "OPTIONS")
            {
                response.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = server.Prefix,
                    ["Access-Control-Allow-Credentials"] = "true",
                    ["Access-Control-Allow-Methods"] = "POST, GET, OPTIONS, DELETE",
                    ["Access-Control-Allow-Headers"] = "authorization,cookie,custom",
                });
                response.End();
                return;
            }
            serverRequestPromise.TrySetResult(request);
            response.WriteHead(200, new());
            response.End("done");
        });
        server.SetRedirect("/redirect", "/something");
        await page.EvaluateAsync("() => document.cookie = 'a=b'");
        var text = await page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url, {
      headers: {
        authorization: 'credentials',
        custom: 'foo'
      },
      credentials: 'include',
    });
    return data.text();
  }", server.Prefix + "/redirect");
        Assert.That(text, Is.EqualTo("done"));
        var serverRequest = await serverRequestPromise.Task;
        Assert.That(serverRequest.Headers["authorization"], Is.EqualTo("credentials"));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("a=b"));
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("foo"));
    }

    [UpstreamTest("RO-088 propagate headers cross origin")]
    public async Task PropagateHeadersCrossOrigin(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/empty.html");
        var serverRequestPromise = new TaskCompletionSource<ServerRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/something", (request, response) =>
        {
            if (request.Method == "OPTIONS")
            {
                response.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = server.Prefix,
                    ["Access-Control-Allow-Credentials"] = "true",
                    ["Access-Control-Allow-Methods"] = "POST, GET, OPTIONS, DELETE",
                    ["Access-Control-Allow-Headers"] = "authorization,custom",
                });
                response.End();
                return;
            }
            serverRequestPromise.TrySetResult(request);
            response.WriteHead(200, new()
            {
                ["Access-Control-Allow-Origin"] = server.Prefix,
                ["Access-Control-Allow-Credentials"] = "true",
            });
            response.End("done");
        });
        var text = await page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url, {
      headers: {
        authorization: 'credentials',
        custom: 'foo'
      },
      credentials: 'include',
    });
    return data.text();
  }", server.CrossProcessPrefix + "/something");
        Assert.That(text, Is.EqualTo("done"));
        var serverRequest = await serverRequestPromise.Task;
        Assert.That(serverRequest.Headers["authorization"], Is.EqualTo("credentials"));
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("foo"));
    }

    [UpstreamTest("RO-089 propagate headers cross origin redirect")]
    public async Task PropagateHeadersCrossOriginRedirect(IPage page, Server server, bool isAndroid)
    {
        It.Fixme(isAndroid, "receives authorization:credentials header");

        await page.GotoAsync(server.Prefix + "/empty.html");
        var serverRequestPromise = new TaskCompletionSource<ServerRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/something", (request, response) =>
        {
            if (request.Method == "OPTIONS")
            {
                response.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = server.Prefix,
                    ["Access-Control-Allow-Credentials"] = "true",
                    ["Access-Control-Allow-Methods"] = "POST, GET, OPTIONS, DELETE",
                    ["Access-Control-Allow-Headers"] = "authorization,cookie,custom",
                });
                response.End();
                return;
            }
            serverRequestPromise.TrySetResult(request);
            response.WriteHead(200, new()
            {
                ["Access-Control-Allow-Origin"] = server.Prefix,
                ["Access-Control-Allow-Credentials"] = "true",
            });
            response.End("done");
        });
        server.SetRoute("/redirect", (request, response) =>
        {
            response.WriteHead(301, new() { ["location"] = $"{server.CrossProcessPrefix}/something" });
            response.End();
        });
        await page.EvaluateAsync("() => document.cookie = 'a=b'");
        var text = await page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url, {
      headers: {
        authorization: 'credentials',
        custom: 'foo'
      },
      credentials: 'include',
    });
    return data.text();
  }", server.Prefix + "/redirect");
        Assert.That(text, Is.EqualTo("done"));
        var serverRequest = await serverRequestPromise.Task;
        // Authorization header not propagated to cross-origin redirect.
        Assert.That(serverRequest.Headers["authorization"], Is.Null.Or.Empty);
        Assert.That(serverRequest.Headers["cookie"], Is.Null.Or.Empty);
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("foo"));
    }

    [UpstreamTest("RO-090 propagate headers cross origin redirect after interception")]
    public async Task PropagateHeadersCrossOriginRedirectAfterInterception(IPage page, Server server, string browserName, bool isAndroid)
    {
        It.Skip(isAndroid, "No cross-process on Android");

        await page.GotoAsync(server.Prefix + "/empty.html");
        var serverRequestPromise = new TaskCompletionSource<ServerRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/something", (request, response) =>
        {
            if (request.Method == "OPTIONS")
            {
                response.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = server.Prefix,
                    ["Access-Control-Allow-Credentials"] = "true",
                    ["Access-Control-Allow-Methods"] = "POST, GET, OPTIONS, DELETE",
                    ["Access-Control-Allow-Headers"] = "authorization,cookie,custom",
                });
                response.End();
                return;
            }
            serverRequestPromise.TrySetResult(request);
            response.WriteHead(200, new()
            {
                ["Access-Control-Allow-Origin"] = server.Prefix,
                ["Access-Control-Allow-Credentials"] = "true",
            });
            response.End("done");
        });
        server.SetRoute("/redirect", (request, response) =>
        {
            response.WriteHead(301, new() { ["location"] = $"{server.CrossProcessPrefix}/something" });
            response.End();
        });
        await page.EvaluateAsync("() => document.cookie = 'a=b'");
        await page.RouteAsync("**/redirect", async route =>
        {
            await route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers)
                {
                    ["authorization"] = "credentials",
                    ["custom"] = "foo",
                },
            });
        });
        var text = await page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url, {
      headers: {
        authorization: 'none',
      },
      credentials: 'include',
    });
    return data.text();
  }", server.Prefix + "/redirect");
        Assert.That(text, Is.EqualTo("done"));
        var serverRequest = await serverRequestPromise.Task;
        if (browserName == "webkit")
        {
            Assert.That(serverRequest.Headers["authorization"], Is.Null.Or.Empty);
        }
        else
        {
            Assert.That(serverRequest.Headers["authorization"], Is.EqualTo("credentials"));
        }
        Assert.That(serverRequest.Headers["cookie"], Is.Null.Or.Empty);
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("foo"));
    }

    [UpstreamTest("RO-091 continue should pass on 307 cross-origin redirect")]
    public async Task ContinueShouldPassOn307CrossOriginRedirect(IPage page, Server server, bool isAndroid)
    {
        It.Skip(isAndroid, "No cross-process on Android");

        server.SetRoute("/final", (request, response) =>
        {
            response.WriteHead(200, new() { ["content-type"] = "text/html" });
            response.End("<!doctype html><title>final</title><p>ok</p>");
        });
        // Cross-origin 307 redirect that preserves the POST method.
        server.SetRoute("/redirect307", (request, response) =>
        {
            response.WriteHead(307, new() { ["location"] = $"{server.Prefix}/final" });
            response.End();
        });

        await page.GotoAsync(server.Prefix + "/empty.html");
        await page.SetContentAsync($@"
    <form id=""f"" method=""POST"" action=""{server.CrossProcessPrefix}/redirect307"">
      <input type=""submit"">
    </form>");

        await page.RouteAsync("**/*", route => route.ContinueAsync());
        await PromiseAll(
            page.WaitForURLAsync($"{server.Prefix}/final"),
            page.Locator("input").ClickAsync());
        await Expect(page.Locator("p")).ToHaveTextAsync("ok");
    }

    [UpstreamTest("RO-092 should intercept css variable with background url")]
    public async Task ShouldInterceptCssVariableWithBackgroundUrl(IPage page, Server server)
    {
        server.SetRoute("/test.html", (request, response) =>
        {
            response.SetHeader("Content-Type", "text/html");
            response.End(@"
    <style>
      @keyframes JNDzq {
        0% { background-position: 0 0 }
        to { background-position: 100 0 }
      }
      div {
        --background: url(/pptr.png);
        background-image: var(--background);
        animation: JNDzq 1s linear infinite;
      }
    </style>
    <div>Yo!</div>");
        });
        var interceptPromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var interceptedRequests = 0;
        await page.RouteAsync(server.Prefix + "/pptr.png", route =>
        {
            ++interceptedRequests;
            interceptPromise.TrySetResult();
            IgnoreErrors(route.ContinueAsync());
        });
        await page.GotoAsync(server.Prefix + "/test.html");
        Assert.That(await page.Locator("div").TextContentAsync(), Is.EqualTo("Yo!"));
        await interceptPromise.Task;
        await page.WaitForTimeoutAsync(1000);
        Assert.That(interceptedRequests, Is.EqualTo(1));
    }

    [UpstreamTest("RO-093 continue should not change multipart/form-data body")]
    public async Task ContinueShouldNotChangeMultipartFormDataBody(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/upload", (request, response) =>
        {
            response.WriteHead(200, new() { ["Content-Type"] = "text/plain" });
            response.End("done");
        });
        async Task<ServerRequest> sendFormData()
        {
            var reqPromise = server.WaitForRequest("/upload");
            var status = await page.EvaluateAsync<int>(@"async () => {
      const newFile = new File(['file content'], 'file.txt');
      const formData = new FormData();
      formData.append('file', newFile);
      const response = await fetch('/upload', {
        method: 'POST',
        credentials: 'include',
        body: formData,
      });
      return response.status;
    }");
            var req = await reqPromise;
            Assert.That(status, Is.EqualTo(200));
            return req;
        }
        var reqBefore = await sendFormData();
        await page.RouteAsync("**/*", async route =>
        {
            await route.ContinueAsync();
        });
        var reqAfter = await sendFormData();
        var fileContent = string.Join("\r\n", new[]
        {
            "Content-Disposition: form-data; name=\"file\"; filename=\"file.txt\"",
            "Content-Type: application/octet-stream",
            "",
            "file content",
            "------",
        });
        Assert.That(Encoding.UTF8.GetString(await reqBefore.PostBody), Does.Contain(fileContent));
        Assert.That(Encoding.UTF8.GetString(await reqAfter.PostBody), Does.Contain(fileContent));
    }

    [UpstreamTest("RO-094 should not forward Host header on cross-origin redirect")]
    public async Task ShouldNotForwardHostHeaderOnCrossOriginRedirect(IPage page, Server server, string browserName)
    {
        const string redirectTargetPath = "/final";
        const string redirectSourcePath = "/redirect";

        string redirectedHost = null;
        server.SetRoute(redirectTargetPath, (req, res) =>
        {
            redirectedHost = req.Headers["host"];
            res.End("OK");
        });

        string firstHost = null;
        server.SetRoute(redirectSourcePath, (req, res) =>
        {
            firstHost = req.Headers["host"];
            res.WriteHead(302, new() { ["location"] = $"{server.CrossProcessPrefix}{redirectTargetPath}" });
            res.End();
        });

        await page.RouteAsync("**/*", async route =>
        {
            var headers = route.Request.Headers;
            if (browserName == "firefox")
            {
                Assert.That(headers, Does.ContainKey("host"));
            }
            else
            {
                Assert.That(headers, Does.Not.ContainKey("host"));
            }
            await route.ContinueAsync(new() { Headers = headers });
        });

        var response = await page.GotoAsync(server.Prefix + redirectSourcePath);
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(firstHost, Is.EqualTo(new Uri(server.Prefix).Authority));
        Assert.That(redirectedHost, Is.EqualTo(new Uri(server.CrossProcessPrefix).Authority));
    }

    [UpstreamTest("RO-095 postData should return empty string when overriding body with empty string")]
    public async Task PostDataShouldReturnEmptyStringWhenOverridingBodyWithEmptyString(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.RouteAsync("**/*", route =>
        {
            // .NET: postData is a byte[].
            IgnoreErrors(route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("") }));
        });
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync("**"),
            page.EvaluateAsync("({ url }) => fetch(url, { method: 'POST', body: 'original' })", new { url = server.Prefix + "/sleep.zzz" }));
        Assert.That(request.PostData, Is.EqualTo(""));
    }
}
