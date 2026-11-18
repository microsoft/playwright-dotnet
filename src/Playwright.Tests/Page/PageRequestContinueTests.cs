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
public class PageRequestContinueTests : UpstreamPageTest
{
    [UpstreamTest("RO-057 should work")]
    public async Task ShouldWork()
    {
        await Page.RouteAsync("**/*", route => route.ContinueAsync());
        await Page.GotoAsync(Server.EmptyPage);
    }

    [UpstreamTest("RO-058 should amend HTTP headers")]
    public async Task ShouldAmendHTTPHeaders()
    {
        await Page.RouteAsync("**/*", route =>
        {
            var headers = new Dictionary<string, string>(route.Request.Headers)
            {
                ["FOO"] = "bar",
            };
            IgnoreErrors(route.ContinueAsync(new() { Headers = headers }));
        });
        await Page.GotoAsync(Server.EmptyPage);
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/sleep.zzz"),
            Page.EvaluateAsync("() => fetch('/sleep.zzz')"));
        Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("RO-059 should not allow to override unsafe HTTP headers")]
    public async Task ShouldNotAllowToOverrideUnsafeHTTPHeaders()
    {
        Info.Skip(IsAndroid);
        var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync("**/*", route => routePromise.TrySetResult(route));
        var serverRequestPromise = Server.WaitForRequest("/empty.html");
        IgnoreErrors(Page.GotoAsync(Server.EmptyPage));
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
        Assert.That(serverRequest.Headers["host"], Is.EqualTo(new Uri(Server.EmptyPage).Authority));
    }

    [UpstreamTest("RO-060 should delete header with undefined value")]
    public async Task ShouldDeleteHeaderWithUndefinedValue()
    {
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        Server.SetRoute("/something", (request, response) =>
        {
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("done");
        });
        IRequest interceptedRequest = null;
        await Page.RouteAsync(Server.Prefix + "/something", async route =>
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
        Assert.That(serverRequest.Headers["foo"], Is.Null.Or.Empty);
        Assert.That(serverRequest.Headers["bar"], Is.EqualTo("b"));
    }

    [UpstreamTest("RO-061 should amend method")]
    public async Task ShouldAmendMethod()
    {
        var sRequest = Server.WaitForRequest("/sleep.zzz");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/*", route => route.ContinueAsync(new() { Method = "POST" }));
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/sleep.zzz"),
            Page.EvaluateAsync("() => fetch('/sleep.zzz')"));
        Assert.That(request.Method, Is.EqualTo("POST"));
        Assert.That((await sRequest).Method, Is.EqualTo("POST"));
    }

    [UpstreamTest("RO-062 should override request url")]
    public async Task ShouldOverrideRequestUrl()
    {
        var serverRequest = Server.WaitForRequest("/global-var.html");
        await Page.RouteAsync("**/foo", route =>
        {
            IgnoreErrors(route.ContinueAsync(new() { Url = Server.Prefix + "/global-var.html" }));
        });
        var response = await Page.GotoAsync(Server.Prefix + "/foo");
        Assert.That(response.Request.Url, Is.EqualTo(Server.Prefix + "/global-var.html"));
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/global-var.html"));
        Assert.That(await Page.EvaluateAsync<int>("() => window['globalVar']"), Is.EqualTo(123));
        Assert.That((await serverRequest).Method, Is.EqualTo("GET"));
    }

    [UpstreamTest("RO-063 should not allow changing protocol when overriding url")]
    public async Task ShouldNotAllowChangingProtocolWhenOverridingUrl()
    {
        var errorPromise = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync("**/*", async route =>
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
        IgnoreErrors(Page.GotoAsync(Server.EmptyPage));
        var error = await errorPromise.Task;
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("New URL must have same protocol as overridden URL"));
    }

    [UpstreamTest("RO-064 should not throw if request was cancelled by the page")]
    public async Task ShouldNotThrowIfRequestWasCancelledByThePage()
    {
        Info.Fixme(IsAndroid, "net::ERR_CONNECTION_RESET");

        var interceptPromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync("**/data.json", route => interceptPromise.TrySetResult(route));
        await Page.GotoAsync(Server.EmptyPage);
        IgnoreErrors(Page.EvaluateAsync(@"url => {
    globalThis.controller = new AbortController();
    return fetch(url, { signal: globalThis.controller.signal });
  }", Server.Prefix + "/data.json"));
        var route = await interceptPromise.Task;
        var failurePromise = Once<IRequest>(h => Page.RequestFailed += h, h => Page.RequestFailed -= h);
        await Page.EvaluateAsync("() => globalThis.controller.abort()");
        var cancelledRequest = await failurePromise;
        Assert.That(cancelledRequest.Failure, Does.Match("cancelled|aborted").IgnoreCase);
        await route.ContinueAsync(); // Should not throw.
    }

    [UpstreamTest("RO-065 should override method along with url")]
    public async Task ShouldOverrideMethodAlongWithUrl()
    {
        var request = Server.WaitForRequest("/empty.html");
        await Page.RouteAsync("**/foo", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Url = Server.EmptyPage,
                Method = "POST",
            }));
        });
        await Page.GotoAsync(Server.Prefix + "/foo");
        Assert.That((await request).Method, Is.EqualTo("POST"));
    }

    [UpstreamTest("RO-066 should amend method on main request")]
    public async Task ShouldAmendMethodOnMainRequest()
    {
        var request = Server.WaitForRequest("/empty.html");
        await Page.RouteAsync("**/*", route => route.ContinueAsync(new() { Method = "POST" }));
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That((await request).Method, Is.EqualTo("POST"));
    }

    public class PostData : UpstreamPageTest
    {
        [UpstreamTest("RO-067 should amend post data")]
        public async Task ShouldAmendPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.RouteAsync("**/*", route =>
            {
                // .NET: postData is a byte[].
                IgnoreErrors(route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("doggo") }));
            });
            var (serverRequest, _) = await PromiseAll(
                Server.WaitForRequest("/sleep.zzz"),
                Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })"));
            Assert.That(Encoding.UTF8.GetString(await serverRequest.PostBody), Is.EqualTo("doggo"));
        }

        [UpstreamTest("RO-068 should compute content-length from post data")]
        public async Task ShouldComputeContentLengthFromPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var data = new string('a', 7500);
            await Page.RouteAsync("**/*", route =>
            {
                var headers = route.Request.Headers;
                headers["content-type"] = "application/json";
                // .NET: postData is a byte[].
                IgnoreErrors(route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes(data), Headers = headers }));
            });
            var (serverRequest, _) = await PromiseAll(
                Server.WaitForRequest("/sleep.zzz"),
                Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'PATCH', body: 'birdy' })"));
            Assert.That(Encoding.UTF8.GetString(await serverRequest.PostBody), Is.EqualTo(data));
            Assert.That(serverRequest.Headers["content-length"], Is.EqualTo(data.Length.ToString(CultureInfo.InvariantCulture)));
            Assert.That(serverRequest.Headers["content-type"], Is.EqualTo("application/json"));
        }

        [UpstreamTest("RO-069 should amend method and post data")]
        public async Task ShouldAmendMethodAndPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.RouteAsync("**/*", route =>
            {
                // .NET: postData is a byte[].
                IgnoreErrors(route.ContinueAsync(new() { Method = "POST", PostData = Encoding.UTF8.GetBytes("doggo") }));
            });
            var (serverRequest, _) = await PromiseAll(
                Server.WaitForRequest("/sleep.zzz"),
                Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'GET' })"));
            Assert.That(serverRequest.Method, Is.EqualTo("POST"));
            Assert.That(Encoding.UTF8.GetString(await serverRequest.PostBody), Is.EqualTo("doggo"));
        }

        [UpstreamTest("RO-070 should amend utf8 post data")]
        public async Task ShouldAmendUtf8PostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.RouteAsync("**/*", route =>
            {
                // .NET: postData is a byte[].
                IgnoreErrors(route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("пушкин") }));
            });
            var (serverRequest, _) = await PromiseAll(
                Server.WaitForRequest("/sleep.zzz"),
                Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })"));
            Assert.That(serverRequest.Method, Is.EqualTo("POST"));
            Assert.That(Encoding.UTF8.GetString(await serverRequest.PostBody), Is.EqualTo("пушкин"));
        }

        [UpstreamTest("RO-071 should amend longer post data")]
        public async Task ShouldAmendLongerPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.RouteAsync("**/*", route =>
            {
                // .NET: postData is a byte[].
                IgnoreErrors(route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("doggo-is-longer-than-birdy") }));
            });
            var (serverRequest, _) = await PromiseAll(
                Server.WaitForRequest("/sleep.zzz"),
                Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })"));
            Assert.That(serverRequest.Method, Is.EqualTo("POST"));
            Assert.That(Encoding.UTF8.GetString(await serverRequest.PostBody), Is.EqualTo("doggo-is-longer-than-birdy"));
        }

        [UpstreamTest("RO-072 should amend binary post data")]
        public async Task ShouldAmendBinaryPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var arr = Enumerable.Range(0, 256).ToArray();
            await Page.RouteAsync("**/*", route =>
            {
                IgnoreErrors(route.ContinueAsync(new() { PostData = arr.Select(value => (byte)value).ToArray() }));
            });
            var (serverRequest, _) = await PromiseAll(
                Server.WaitForRequest("/sleep.zzz"),
                Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })"));
            Assert.That(serverRequest.Method, Is.EqualTo("POST"));
            var buffer = await serverRequest.PostBody;
            Assert.That(buffer.Length, Is.EqualTo(arr.Length));
            for (var i = 0; i < arr.Length; ++i)
            {
                Assert.That(arr[i], Is.EqualTo(buffer[i]));
            }
        }

        [UpstreamTest("RO-073 should use content-type from original request")]
        public async Task ShouldUseContentTypeFromOriginalRequest()
        {
            await Page.GotoAsync(Server.EmptyPage);
            // .NET: postData is a byte[].
            await Page.RouteAsync($"{Server.Prefix}/title.html", route => route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("{\"b\":2}") }));
            var (request, _) = await PromiseAll(
                Server.WaitForRequest("/title.html"),
                Page.EvaluateAsync(@"async url => {
        await fetch(url, {
          method: 'POST',
          body: '{""a"":1}',
          headers: { 'content-type': 'application/json' },
        });
      }", $"{Server.Prefix}/title.html"));
            Assert.That(request.Headers["content-type"], Is.EqualTo("application/json"));
            Assert.That(Encoding.UTF8.GetString(await request.PostBody), Is.EqualTo("{\"b\":2}"));
        }
    }

    [UpstreamTest("RO-074 should work with Cross-Origin-Opener-Policy")]
    public async Task ShouldWorkWithCrossOriginOpenerPolicy()
    {
        Info.Fixme(BrowserName == "webkit", "https://github.com/microsoft/playwright/issues/8796");
        ServerHeaders serverHeaders = null;
        var serverRequests = new List<string>();
        Server.SetRoute("/empty.html", (req, res) =>
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
        await Page.RouteAsync("**/*", route =>
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
        Page.Request += (_, r) =>
        {
            events.Add("request");
            requests.Add(r);
        };
        Page.RequestFailed += (_, r) =>
        {
            events.Add("requestfailed");
            requests.Add(r);
        };
        Page.RequestFinished += (_, r) =>
        {
            events.Add("requestfinished");
            requests.Add(r);
        };
        Page.Response += (_, r) =>
        {
            events.Add("response");
            requests.Add(r.Request);
        };
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { Server.EmptyPage }));
        // There should be only one request to the server.
        if (BrowserName == "webkit")
        {
            Assert.That(serverRequests, Is.EqualTo(new[] { "/empty.html", "/empty.html" }));
        }
        else
        {
            Assert.That(serverRequests, Is.EqualTo(new[] { "/empty.html" }));
        }
        Assert.That(serverHeaders["foo"], Is.EqualTo("bar"));
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
        await response.FinishedAsync();
        Assert.That(events, Is.EqualTo(new[] { "request", "response", "requestfinished" }));
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(response.Request.Failure, Is.Null);
    }

    [UpstreamTest("RO-075 should not delete the origin header")]
    public async Task ShouldNotDeleteTheOriginHeader()
    {
        Info.Skip(IsAndroid, "No cross-process on Android");

        await Page.GotoAsync(Server.Prefix + "/empty.html");
        Server.SetRoute("/something", (request, response) =>
        {
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("done");
        });
        string interceptedOrigin = null;
        await Page.RouteAsync(Server.CrossProcessPrefix + "/something", async route =>
        {
            var headers = await route.Request.AllHeadersAsync();
            interceptedOrigin = headers["origin"];
            headers.Remove("origin");
            IgnoreErrors(route.ContinueAsync(new() { Headers = headers }));
        });

        var (text, serverRequest) = await PromiseAll(
            Page.EvaluateAsync<string>(@"async url => {
      const data = await fetch(url);
      return data.text();
    }", Server.CrossProcessPrefix + "/something"),
            Server.WaitForRequest("/something"));
        Assert.That(text, Is.EqualTo("done"));
        Assert.That(interceptedOrigin, Is.EqualTo(Server.Prefix));
        Assert.That(serverRequest.Headers["origin"], Is.EqualTo(Server.Prefix));
    }

    [UpstreamTest("RO-076 should continue preload link requests")]
    public async Task ShouldContinuePreloadLinkRequests()
    {
        var intercepted = false;
        await Page.RouteAsync("**/one-style.css", route =>
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
            Server.WaitForRequest("/one-style.css"),
            Page.GotoAsync(Server.Prefix + "/preload.html"));
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("value"));
        await Page.WaitForFunctionAsync("() => window.preloadedStyles");
        Assert.That(intercepted, Is.EqualTo(true));
        var color = await Page.EvaluateAsync<string>("() => window.getComputedStyle(document.body).backgroundColor");
        Assert.That(color, Is.EqualTo("rgb(255, 192, 203)"));
    }

    [UpstreamTest("RO-077 should respect set-cookie in redirect response")]
    public async Task ShouldRespectSetCookieInRedirectResponse()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a href=\"/set-cookie-redirect\">Set cookie</a>");
        Server.SetRoute("/set-cookie-redirect", (request, response) =>
        {
            response.WriteHead(302, new()
            {
                ["set-cookie"] = "foo=bar;  max-age=36000",
                ["location"] = "/empty.html",
            });
            response.End();
        });
        await Page.RouteAsync("**/set-cookie-redirect", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers),
            }));
        });
        var serverRequestPromise = Server.WaitForRequest("/empty.html");
        await Page.GotoAsync(Server.Prefix + "/set-cookie-redirect");
        var serverRequest = await serverRequestPromise;
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("foo=bar"));
        Assert.That(await Page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("foo=bar"));
    }

    [UpstreamTest("RO-078 continue should not propagate cookie override to redirects")]
    public async Task ContinueShouldNotPropagateCookieOverrideToRedirects()
    {
        Server.SetRoute("/set-cookie", (request, response) =>
        {
            response.WriteHead(200, new() { ["Set-Cookie"] = "foo=bar;" });
            response.End();
        });
        await Page.GotoAsync(Server.Prefix + "/set-cookie");
        Assert.That(await Page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("foo=bar"));
        Server.SetRedirect("/redirect", Server.Prefix + "/empty.html");
        await Page.RouteAsync("**/redirect", route =>
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
            Server.WaitForRequest("/empty.html"),
            Page.GotoAsync(Server.Prefix + "/redirect"));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("foo=bar"));
    }

    [UpstreamTest("RO-079 continue should not override cookie")]
    public async Task ContinueShouldNotOverrideCookie()
    {
        Server.SetRoute("/set-cookie", (request, response) =>
        {
            response.WriteHead(200, new() { ["Set-Cookie"] = "foo=bar;" });
            response.End();
        });
        await Page.GotoAsync(Server.Prefix + "/set-cookie");
        Assert.That(await Page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("foo=bar"));
        await Page.RouteAsync("**", route =>
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
            Server.WaitForRequest("/empty.html"),
            Page.GotoAsync(Server.EmptyPage));
        // Original cookie from the browser's cookie jar should be sent.
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("foo=bar"));
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("value"));
    }

    [UpstreamTest("RO-080 continue with headers should send fresh cookie from the browser cookie store")]
    public async Task ContinueWithHeadersShouldSendFreshCookieFromTheBrowserCookieStore()
    {
        Server.SetRoute("/set-cookie", (request, response) =>
        {
            response.WriteHead(200, new() { ["Set-Cookie"] = "foo=v1;" });
            response.End();
        });
        await Page.GotoAsync(Server.Prefix + "/set-cookie");
        Assert.That(await Page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("foo=v1"));

        await Page.RouteAsync("**/empty.html", async route =>
        {
            // Cookie store changes between interception and continuation.
            await Page.Context.AddCookiesAsync(new[] { new Cookie { Name = "foo", Value = "v2", Url = Server.Prefix } });
            await route.ContinueAsync(new() { Headers = route.Request.Headers });
        });

        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            Page.GotoAsync(Server.EmptyPage));
        // The fresh cookie from the browser cookie store should be sent, not the stale captured value.
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("foo=v2"));
    }

    [UpstreamTest("RO-081 continue with headers should send fresh cookie after a redirect")]
    public async Task ContinueWithHeadersShouldSendFreshCookieAfterARedirect()
    {
        Server.SetRoute("/set-cookie", (request, response) =>
        {
            response.WriteHead(200, new() { ["Set-Cookie"] = "foo=v1;" });
            response.End();
        });
        await Page.GotoAsync(Server.Prefix + "/set-cookie");
        Assert.That(await Page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("foo=v1"));

        // The redirect updates the cookie before bouncing to the final destination.
        Server.SetRoute("/redirect", (request, response) =>
        {
            response.WriteHead(302, new() { ["Set-Cookie"] = "foo=v2;", ["location"] = Server.Prefix + "/empty.html" });
            response.End();
        });

        await Page.RouteAsync("**/redirect", route =>
        {
            IgnoreErrors(route.ContinueAsync(new() { Headers = route.Request.Headers }));
        });

        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            Page.GotoAsync(Server.Prefix + "/redirect"));
        // The redirected request should carry the cookie updated by the redirect, not the stale captured value.
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("foo=v2"));
    }

    [UpstreamTest("RO-082 redirect after continue should be able to delete cookie")]
    public async Task RedirectAfterContinueShouldBeAbleToDeleteCookie()
    {
        Server.SetRoute("/set-cookie", (request, response) =>
        {
            response.WriteHead(200, new() { ["Set-Cookie"] = "foo=bar;" });
            response.End();
        });
        await Page.GotoAsync(Server.Prefix + "/set-cookie");
        Assert.That(await Page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("foo=bar"));

        Server.SetRoute("/delete-cookie", (request, response) =>
        {
            response.WriteHead(200, new() { ["Set-Cookie"] = "foo=bar; expires=Thu, 01 Jan 1970 00:00:00 GMT" });
            response.End();
        });
        Server.SetRedirect("/redirect", "/delete-cookie");
        await Page.RouteAsync("**/redirect", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers),
            }));
        });
        await Page.GotoAsync(Server.Prefix + "/redirect");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            Page.GotoAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.Null.Or.Empty);
    }

    [UpstreamTest("RO-083 continue should propagate headers to redirects")]
    public async Task ContinueShouldPropagateHeadersToRedirects()
    {
        Server.SetRedirect("/redirect", "/empty.html");
        await Page.RouteAsync("**/redirect", route =>
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
            Server.WaitForRequest("/empty.html"),
            Page.GotoAsync(Server.Prefix + "/redirect"));
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("value"));
    }

    [UpstreamTest("RO-084 continue should drop content-length on redirects")]
    public async Task ContinueShouldDropContentLengthOnRedirects()
    {
        await Page.GotoAsync(Server.EmptyPage);

        Server.SetRedirect("/redirect", "/empty.html");
        await Page.RouteAsync("**/redirect", route =>
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
            Server.WaitForRequest("/empty.html"),
            Page.EvaluateAsync("url => fetch(url, { method: 'POST', body: 'foo' })", Server.Prefix + "/redirect"));
        Assert.That(serverRequest.Method, Is.EqualTo("GET"));
        Assert.That(serverRequest.Headers["content-length"], Is.Null);
        Assert.That(serverRequest.Headers["content-type"], Is.Null);
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("value"));
    }

    [UpstreamTest("RO-085 redirected requests should report overridden headers")]
    public async Task RedirectedRequestsShouldReportOverriddenHeaders()
    {
        Server.SetRedirect("/redirect", "/empty.html");
        await Page.RouteAsync("**/redirect", route =>
        {
            var headers = route.Request.Headers;
            headers["custom"] = "value";
            IgnoreErrors(route.FallbackAsync(new() { Headers = headers }));
        });

        var (serverRequest, response) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            Page.GotoAsync(Server.Prefix + "/redirect"));
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("value"));
        Assert.That(response.Request.Url, Is.EqualTo(Server.EmptyPage));
        Assert.That(response.Request.Headers["custom"], Is.EqualTo("value"));
        Assert.That((await response.Request.AllHeadersAsync())["custom"], Is.EqualTo("value"));
    }

    [UpstreamTest("RO-086 continue should delete headers on redirects")]
    public async Task ContinueShouldDeleteHeadersOnRedirects()
    {
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        Server.SetRoute("/something", (request, response) =>
        {
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("done");
        });
        Server.SetRedirect("/redirect", "/something");
        await Page.RouteAsync("**/redirect", route =>
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
            Page.EvaluateAsync<string>(@"async url => {
      const data = await fetch(url, {
        headers: {
          foo: 'a',
        }
      });
      return data.text();
    }", Server.Prefix + "/redirect"),
            Server.WaitForRequest("/something"));
        Assert.That(text, Is.EqualTo("done"));
        Assert.That(serverRequest.Headers["foo"], Is.Null.Or.Empty);
    }

    [UpstreamTest("RO-087 propagate headers same origin redirect")]
    public async Task PropagateHeadersSameOriginRedirect()
    {
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        var serverRequestPromise = new TaskCompletionSource<ServerRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        Server.SetRoute("/something", (request, response) =>
        {
            if (request.Method == "OPTIONS")
            {
                response.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = Server.Prefix,
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
        Server.SetRedirect("/redirect", "/something");
        await Page.EvaluateAsync("() => document.cookie = 'a=b'");
        var text = await Page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url, {
      headers: {
        authorization: 'credentials',
        custom: 'foo'
      },
      credentials: 'include',
    });
    return data.text();
  }", Server.Prefix + "/redirect");
        Assert.That(text, Is.EqualTo("done"));
        var serverRequest = await serverRequestPromise.Task;
        Assert.That(serverRequest.Headers["authorization"], Is.EqualTo("credentials"));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("a=b"));
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("foo"));
    }

    [UpstreamTest("RO-088 propagate headers cross origin")]
    public async Task PropagateHeadersCrossOrigin()
    {
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        var serverRequestPromise = new TaskCompletionSource<ServerRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        Server.SetRoute("/something", (request, response) =>
        {
            if (request.Method == "OPTIONS")
            {
                response.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = Server.Prefix,
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
                ["Access-Control-Allow-Origin"] = Server.Prefix,
                ["Access-Control-Allow-Credentials"] = "true",
            });
            response.End("done");
        });
        var text = await Page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url, {
      headers: {
        authorization: 'credentials',
        custom: 'foo'
      },
      credentials: 'include',
    });
    return data.text();
  }", Server.CrossProcessPrefix + "/something");
        Assert.That(text, Is.EqualTo("done"));
        var serverRequest = await serverRequestPromise.Task;
        Assert.That(serverRequest.Headers["authorization"], Is.EqualTo("credentials"));
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("foo"));
    }

    [UpstreamTest("RO-089 propagate headers cross origin redirect")]
    public async Task PropagateHeadersCrossOriginRedirect()
    {
        Info.Fixme(IsAndroid, "receives authorization:credentials header");

        await Page.GotoAsync(Server.Prefix + "/empty.html");
        var serverRequestPromise = new TaskCompletionSource<ServerRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        Server.SetRoute("/something", (request, response) =>
        {
            if (request.Method == "OPTIONS")
            {
                response.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = Server.Prefix,
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
                ["Access-Control-Allow-Origin"] = Server.Prefix,
                ["Access-Control-Allow-Credentials"] = "true",
            });
            response.End("done");
        });
        Server.SetRoute("/redirect", (request, response) =>
        {
            response.WriteHead(301, new() { ["location"] = $"{Server.CrossProcessPrefix}/something" });
            response.End();
        });
        await Page.EvaluateAsync("() => document.cookie = 'a=b'");
        var text = await Page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url, {
      headers: {
        authorization: 'credentials',
        custom: 'foo'
      },
      credentials: 'include',
    });
    return data.text();
  }", Server.Prefix + "/redirect");
        Assert.That(text, Is.EqualTo("done"));
        var serverRequest = await serverRequestPromise.Task;
        // Authorization header not propagated to cross-origin redirect.
        Assert.That(serverRequest.Headers["authorization"], Is.Null.Or.Empty);
        Assert.That(serverRequest.Headers["cookie"], Is.Null.Or.Empty);
        Assert.That(serverRequest.Headers["custom"], Is.EqualTo("foo"));
    }

    [UpstreamTest("RO-090 propagate headers cross origin redirect after interception")]
    public async Task PropagateHeadersCrossOriginRedirectAfterInterception()
    {
        Info.Skip(IsAndroid, "No cross-process on Android");

        await Page.GotoAsync(Server.Prefix + "/empty.html");
        var serverRequestPromise = new TaskCompletionSource<ServerRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        Server.SetRoute("/something", (request, response) =>
        {
            if (request.Method == "OPTIONS")
            {
                response.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = Server.Prefix,
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
                ["Access-Control-Allow-Origin"] = Server.Prefix,
                ["Access-Control-Allow-Credentials"] = "true",
            });
            response.End("done");
        });
        Server.SetRoute("/redirect", (request, response) =>
        {
            response.WriteHead(301, new() { ["location"] = $"{Server.CrossProcessPrefix}/something" });
            response.End();
        });
        await Page.EvaluateAsync("() => document.cookie = 'a=b'");
        await Page.RouteAsync("**/redirect", async route =>
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
        var text = await Page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url, {
      headers: {
        authorization: 'none',
      },
      credentials: 'include',
    });
    return data.text();
  }", Server.Prefix + "/redirect");
        Assert.That(text, Is.EqualTo("done"));
        var serverRequest = await serverRequestPromise.Task;
        if (BrowserName == "webkit")
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
    public async Task ContinueShouldPassOn307CrossOriginRedirect()
    {
        Info.Skip(IsAndroid, "No cross-process on Android");

        Server.SetRoute("/final", (request, response) =>
        {
            response.WriteHead(200, new() { ["content-type"] = "text/html" });
            response.End("<!doctype html><title>final</title><p>ok</p>");
        });
        // Cross-origin 307 redirect that preserves the POST method.
        Server.SetRoute("/redirect307", (request, response) =>
        {
            response.WriteHead(307, new() { ["location"] = $"{Server.Prefix}/final" });
            response.End();
        });

        await Page.GotoAsync(Server.Prefix + "/empty.html");
        await Page.SetContentAsync($@"
    <form id=""f"" method=""POST"" action=""{Server.CrossProcessPrefix}/redirect307"">
      <input type=""submit"">
    </form>");

        await Page.RouteAsync("**/*", route => route.ContinueAsync());
        await PromiseAll(
            Page.WaitForURLAsync($"{Server.Prefix}/final"),
            Page.Locator("input").ClickAsync());
        await Expect(Page.Locator("p")).ToHaveTextAsync("ok");
    }

    [UpstreamTest("RO-092 should intercept css variable with background url")]
    public async Task ShouldInterceptCssVariableWithBackgroundUrl()
    {
        Server.SetRoute("/test.html", (request, response) =>
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
        await Page.RouteAsync(Server.Prefix + "/pptr.png", route =>
        {
            ++interceptedRequests;
            interceptPromise.TrySetResult();
            IgnoreErrors(route.ContinueAsync());
        });
        await Page.GotoAsync(Server.Prefix + "/test.html");
        Assert.That(await Page.Locator("div").TextContentAsync(), Is.EqualTo("Yo!"));
        await interceptPromise.Task;
        await Page.WaitForTimeoutAsync(1000);
        Assert.That(interceptedRequests, Is.EqualTo(1));
    }

    [UpstreamTest("RO-093 continue should not change multipart/form-data body")]
    public async Task ContinueShouldNotChangeMultipartFormDataBody()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/upload", (request, response) =>
        {
            response.WriteHead(200, new() { ["Content-Type"] = "text/plain" });
            response.End("done");
        });
        async Task<ServerRequest> sendFormData()
        {
            var reqPromise = Server.WaitForRequest("/upload");
            var status = await Page.EvaluateAsync<int>(@"async () => {
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
        await Page.RouteAsync("**/*", async route =>
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
    public async Task ShouldNotForwardHostHeaderOnCrossOriginRedirect()
    {
        const string redirectTargetPath = "/final";
        const string redirectSourcePath = "/redirect";

        string redirectedHost = null;
        Server.SetRoute(redirectTargetPath, (req, res) =>
        {
            redirectedHost = req.Headers["host"];
            res.End("OK");
        });

        string firstHost = null;
        Server.SetRoute(redirectSourcePath, (req, res) =>
        {
            firstHost = req.Headers["host"];
            res.WriteHead(302, new() { ["location"] = $"{Server.CrossProcessPrefix}{redirectTargetPath}" });
            res.End();
        });

        await Page.RouteAsync("**/*", async route =>
        {
            var headers = route.Request.Headers;
            if (BrowserName == "firefox")
            {
                Assert.That(headers, Does.ContainKey("host"));
            }
            else
            {
                Assert.That(headers, Does.Not.ContainKey("host"));
            }
            await route.ContinueAsync(new() { Headers = headers });
        });

        var response = await Page.GotoAsync(Server.Prefix + redirectSourcePath);
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(firstHost, Is.EqualTo(new Uri(Server.Prefix).Authority));
        Assert.That(redirectedHost, Is.EqualTo(new Uri(Server.CrossProcessPrefix).Authority));
    }

    [UpstreamTest("RO-095 postData should return empty string when overriding body with empty string")]
    public async Task PostDataShouldReturnEmptyStringWhenOverridingBodyWithEmptyString()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/*", route =>
        {
            // .NET: postData is a byte[].
            IgnoreErrors(route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("") }));
        });
        var (request, _) = await PromiseAll(
            Page.WaitForRequestAsync("**"),
            Page.EvaluateAsync("({ url }) => fetch(url, { method: 'POST', body: 'original' })", new { url = Server.Prefix + "/sleep.zzz" }));
        Assert.That(request.PostData, Is.EqualTo(""));
    }
}
