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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-event-request.spec.ts.</summary>
public class PageEventRequestTests : UpstreamPageTest
{
    [UpstreamTest("NE-091 should fire for navigation requests")]
    public async Task ShouldFireForNavigationRequests()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, request) => requests.Add(request);
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(requests.Count, Is.EqualTo(1));
    }

    [UpstreamTest("NE-092 should fire for iframes")]
    public async Task ShouldFireForIframes()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, request) => requests.Add(request);
        await Page.GotoAsync(Server.EmptyPage);
        await AttachFrame(Page, "frame1", Server.EmptyPage);
        Assert.That(requests.Count, Is.EqualTo(2));
    }

    [UpstreamTest("NE-093 should fire for fetches")]
    public async Task ShouldFireForFetches()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, request) => requests.Add(request);
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync("() => fetch('/empty.html')");
        Assert.That(requests.Count, Is.EqualTo(2));
    }

    [UpstreamTest("NE-094 should fire for fetches with keepalive: true")]
    public async Task ShouldFireForFetchesWithKeepaliveTrue()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, request) => requests.Add(request);
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync("() => fetch('/empty.html', { keepalive: true })");
        Assert.That(requests.Count, Is.EqualTo(2));
    }

    [UpstreamTest("NE-095 should report requests and responses handled by service worker")]
    public async Task ShouldReportRequestsAndResponsesHandledByServiceWorker()
    {
        Info.Fixme(IsAndroid);
        Info.Fixme(IsElectron);

        await Page.GotoAsync(Server.Prefix + "/serviceworkers/fetchdummy/sw.html");
        await Page.EvaluateAsync("() => window['activationPromise']");
        var (request, swResponse) = await PromiseAll(
            Page.WaitForRequestAsync(_ => true),
            Page.EvaluateAsync<string>("() => window['fetchDummy']('foo')"));
        Assert.That(swResponse, Is.EqualTo("responseFromServiceWorker:foo"));
        Assert.That(request.Url, Is.EqualTo(Server.Prefix + "/serviceworkers/fetchdummy/foo"));
        // .NET: expect(request.serviceWorker()).toBe(null) is left out: request.serviceWorker() is not in the .NET API.
        var response = await request.ResponseAsync();
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/serviceworkers/fetchdummy/foo"));
        Assert.That(await response.TextAsync(), Is.EqualTo("responseFromServiceWorker:foo"));
        Assert.That(response.FromServiceWorker, Is.EqualTo(true));

        var (failedRequest, _) = await PromiseAll(
            Once<IRequest>(h => Page.RequestFailed += h, h => Page.RequestFailed -= h),
            Catch(() => Page.EvaluateAsync("() => window['fetchDummy']('error')")));
        Assert.That(failedRequest.Url, Is.EqualTo(Server.Prefix + "/serviceworkers/fetchdummy/error"));
        Assert.That(failedRequest.Failure, Is.Not.Null);
        // .NET: expect(failedRequest.serviceWorker()).toBe(null) is left out: request.serviceWorker() is not in the .NET API.
        Assert.That(await failedRequest.ResponseAsync(), Is.Null);
    }

    [UpstreamTest("NE-096 should report requests and responses handled by service worker with routing")]
    public async Task ShouldReportRequestsAndResponsesHandledByServiceWorkerWithRouting()
    {
        Info.Fixme(IsAndroid);
        Info.Fixme(IsElectron);

        var interceptedUrls = new List<string>();
        await Page.RouteAsync("**/*", route =>
        {
            interceptedUrls.Add(route.Request.Url);
            IgnoreErrors(route.ContinueAsync());
        });
        await Page.GotoAsync(Server.Prefix + "/serviceworkers/fetchdummy/sw.html");
        await Page.EvaluateAsync("() => window['activationPromise']");
        var (swResponse, request) = await PromiseAll(
            Page.EvaluateAsync<string>("() => window['fetchDummy']('foo')"),
            Page.WaitForRequestAsync(_ => true));
        Assert.That(swResponse, Is.EqualTo("responseFromServiceWorker:foo"));
        Assert.That(request.Url, Is.EqualTo(Server.Prefix + "/serviceworkers/fetchdummy/foo"));
        // .NET: expect(request.serviceWorker()).toBe(null) is left out: request.serviceWorker() is not in the .NET API.
        var response = await request.ResponseAsync();
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/serviceworkers/fetchdummy/foo"));
        Assert.That(await response.TextAsync(), Is.EqualTo("responseFromServiceWorker:foo"));

        var (failedRequest, _) = await PromiseAll(
            Once<IRequest>(h => Page.RequestFailed += h, h => Page.RequestFailed -= h),
            Catch(() => Page.EvaluateAsync("() => window['fetchDummy']('error')")));
        Assert.That(failedRequest.Url, Is.EqualTo(Server.Prefix + "/serviceworkers/fetchdummy/error"));
        Assert.That(failedRequest.Failure, Is.Not.Null);
        // .NET: expect(failedRequest.serviceWorker()).toBe(null) is left out: request.serviceWorker() is not in the .NET API.
        Assert.That(await failedRequest.ResponseAsync(), Is.Null);

        var expectedUrls = new List<string> { Server.Prefix + "/serviceworkers/fetchdummy/sw.html" };
        if (BrowserName == "webkit")
        {
            expectedUrls.Add(Server.Prefix + "/serviceworkers/fetchdummy/sw.js");
        }
        Assert.That(interceptedUrls, Is.EqualTo(expectedUrls));
    }

    [UpstreamTest("NE-097 should report navigation requests and responses handled by service worker")]
    public async Task ShouldReportNavigationRequestsAndResponsesHandledByServiceWorker()
    {
        Info.Fixme(IsAndroid);

        await Page.GotoAsync(Server.Prefix + "/serviceworkers/stub/sw.html");
        await Page.EvaluateAsync("() => window['activationPromise']");

        var reloadResponse = await Page.ReloadAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window.fromSW"), Is.EqualTo(true));
        Assert.That(reloadResponse.Url, Is.EqualTo(Server.Prefix + "/serviceworkers/stub/sw.html"));
        await Page.EvaluateAsync("() => window['activationPromise']");

        if (BrowserName != "firefox")
        {
            // When SW fetch throws, Firefox does not fail the navigation,
            // but rather falls back to the real network.

            var (_, failedRequest) = await PromiseAll(
                Page.EvaluateAsync(@"() => {
        window.location.href = '/serviceworkers/stub/error.html';
      }"),
                Once<IRequest>(h => Page.RequestFailed += h, h => Page.RequestFailed -= h));
            Assert.That(failedRequest.Url, Is.EqualTo(Server.Prefix + "/serviceworkers/stub/error.html"));
            Assert.That(failedRequest.Failure, Does.Contain(BrowserName == "chromium" ? "net::ERR_FAILED" : "uh oh"));
            // .NET: expect(failedRequest.serviceWorker()).toBe(null) is left out: request.serviceWorker() is not in the .NET API.
            Assert.That(await failedRequest.ResponseAsync(), Is.Null);
        }
    }

    [UpstreamTest("NE-098 should report navigation requests and responses handled by service worker with routing")]
    public async Task ShouldReportNavigationRequestsAndResponsesHandledByServiceWorkerWithRouting()
    {
        Info.Fixme(IsAndroid);

        await Page.RouteAsync("**/*", route => route.ContinueAsync());
        await Page.GotoAsync(Server.Prefix + "/serviceworkers/stub/sw.html");
        await Page.EvaluateAsync("() => window['activationPromise']");

        var reloadResponse = await Page.ReloadAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window.fromSW"), Is.EqualTo(true));
        Assert.That(reloadResponse.Url, Is.EqualTo(Server.Prefix + "/serviceworkers/stub/sw.html"));
        await Page.EvaluateAsync("() => window['activationPromise']");

        if (BrowserName != "firefox")
        {
            // When SW fetch throws, Firefox does not fail the navigation,
            // but rather falls back to the real network.

            var (_, failedRequest) = await PromiseAll(
                Page.EvaluateAsync(@"() => {
        window.location.href = '/serviceworkers/stub/error.html';
        // eslint-disable-next-line
        undefined
      }"),
                Once<IRequest>(h => Page.RequestFailed += h, h => Page.RequestFailed -= h));
            Assert.That(failedRequest.Url, Is.EqualTo(Server.Prefix + "/serviceworkers/stub/error.html"));
            Assert.That(failedRequest.Failure, Does.Contain(BrowserName == "chromium" ? "net::ERR_FAILED" : "uh oh"));
            // .NET: expect(failedRequest.serviceWorker()).toBe(null) is left out: request.serviceWorker() is not in the .NET API.
            Assert.That(await failedRequest.ResponseAsync(), Is.Null);
        }
    }

    [UpstreamTest("NE-099 should return response body when Cross-Origin-Opener-Policy is set")]
    public async Task ShouldReturnResponseBodyWhenCrossOriginOpenerPolicyIsSet()
    {
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Cross-Origin-Opener-Policy", "same-origin");
            // Note: without 'onload', Firefox sometimes does not fire the load event
            // over the protocol. The reason is unclear.
            res.End(@"
      <div>Hello there!</div>
      <script>window.onload = () => console.log('onload')</script>
    ");
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
        await response.FinishedAsync();
        Assert.That(response.Request.Failure, Is.Null);
        Assert.That(await response.TextAsync(), Does.Contain("Hello there!"));
    }

    [UpstreamTest("NE-100 should fire requestfailed when intercepting race")]
    public async Task ShouldFireRequestfailedWhenInterceptingRace()
    {
        Info.Skip(BrowserName != "chromium", "This test is specifically testing Chromium race");

        var promise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        {
            var counter = 0;
            var failures = new HashSet<IRequest>();
            var alive = new HashSet<IRequest>();
            Page.Request += (_, request) =>
            {
                Assert.That(alive.Contains(request), Is.EqualTo(false));
                Assert.That(failures.Contains(request), Is.EqualTo(false));
                alive.Add(request);
            };
            Page.RequestFailed += (_, request) =>
            {
                Assert.That(failures.Contains(request), Is.EqualTo(false));
                Assert.That(alive.Contains(request), Is.EqualTo(true));
                alive.Remove(request);
                failures.Add(request);
                if (++counter == 10)
                {
                    promise.SetResult();
                }
            };
        }

        // Stall requests to make sure we don't get requestfinished.
        await Page.RouteAsync("**", route => { });

        await Page.SetContentAsync($@"
    <iframe src=""{Server.EmptyPage}""></iframe>
    <iframe src=""{Server.EmptyPage}""></iframe>
    <iframe src=""{Server.EmptyPage}""></iframe>
    <iframe src=""{Server.EmptyPage}""></iframe>
    <iframe src=""{Server.EmptyPage}""></iframe>
    <iframe src=""{Server.EmptyPage}""></iframe>
    <iframe src=""{Server.EmptyPage}""></iframe>
    <iframe src=""{Server.EmptyPage}""></iframe>
    <iframe src=""{Server.EmptyPage}""></iframe>
    <iframe src=""{Server.EmptyPage}""></iframe>
    <script>
      function abortAll() {{
        const frames = document.querySelectorAll(""iframe"");
        for (const frame of frames)
          frame.src = ""about:blank"";
      }}
      abortAll();
    </script>
  ");

        await promise.Task;
    }

    [UpstreamTest("NE-101 main resource xhr should have type xhr")]
    public async Task MainResourceXhrShouldHaveTypeXhr()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (request, _) = await PromiseAll(
            Page.WaitForRequestAsync(_ => true),
            Page.EvaluateAsync(@"() => {
      const x = new XMLHttpRequest();
      x.open('GET', location.href, false);
      x.send();
    }"));
        Assert.That(request.IsNavigationRequest, Is.EqualTo(false));
        Assert.That(request.ResourceType, Is.EqualTo("xhr"));
    }

    [UpstreamTest("NE-102 should finish 204 request")]
    public async Task ShouldFinish204Request()
    {
        Server.SetRoute("/204", (req, res) =>
        {
            res.WriteHead(204, new() { ["Content-type"] = "text/plain" });
            res.End();
        });
        await Page.GotoAsync(Server.EmptyPage);
        // .NET: page.waitForEvent('requestfailed', ...).then(() => 'requestfailed') and page.waitForEvent('requestfinished', ...).then(() => 'requestfinished') are local functions that are called in their place.
        async Task<string> RequestFailed()
        {
            await Once<IRequest>(h => Page.RequestFailed += h, h => Page.RequestFailed -= h, r => r.Url.EndsWith("/204", StringComparison.Ordinal));
            return "requestfailed";
        }
        async Task<string> RequestFinished()
        {
            await Page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url.EndsWith("/204", StringComparison.Ordinal) });
            return "requestfinished";
        }
        var reqPromise = Task.WhenAny(
            RequestFailed(),
            RequestFinished());
        IgnoreErrors(Page.EvaluateAsync("async url => { await fetch(url); }", Server.Prefix + "/204"));
        Assert.That(await await reqPromise, Is.EqualTo("requestfinished"));
    }

    [UpstreamTest("NE-103 should fail 204 navigation request")]
    public async Task ShouldFail204NavigationRequest()
    {
        Server.SetRoute("/204", (req, res) =>
        {
            res.StatusCode = 204;
            res.End();
        });
        await Page.GotoAsync(Server.EmptyPage);
        // .NET: page.waitForEvent('requestfailed', ...).then(() => 'requestfailed') and page.waitForEvent('requestfinished', ...).then(() => 'requestfinished') are local functions that are called in their place.
        async Task<string> RequestFailed()
        {
            await Once<IRequest>(h => Page.RequestFailed += h, h => Page.RequestFailed -= h, r => r.Url.EndsWith("/204", StringComparison.Ordinal));
            return "requestfailed";
        }
        async Task<string> RequestFinished()
        {
            await Page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url.EndsWith("/204", StringComparison.Ordinal) });
            return "requestfinished";
        }
        var reqPromise = Task.WhenAny(
            RequestFailed(),
            RequestFinished());
        await Page.SetContentAsync($"<a href=\"{Server.Prefix}/204\">link</a>");
        await Page.ClickAsync("a", new() { NoWaitAfter = true });
        Assert.That(await await reqPromise, Is.EqualTo("requestfailed"));
    }

    [UpstreamTest("NE-104 <picture> resource should have type image")]
    public async Task PictureResourceShouldHaveTypeImage()
    {
        var request = await PromiseAll(
            Page.WaitForRequestAsync(_ => true),
            Page.SetContentAsync(@"
      <picture>
        <source>
          <img src=""https://www.wikipedia.org/portal/wikipedia.org/assets/img/Wikipedia-logo-v2@2x.png"">
        </source>
      </picture>
    "));
        Assert.That(request.ResourceType, Is.EqualTo("image"));
    }

    // Chromium: requestWillBeSentEvent.frameId is undefined for OPTIONS.
    // WebKit: no requestWillBeSent event in the protocol for OPTIONS (at least on Mac).
    // Firefox: OPTIONS request can be dispatched.
    [UpstreamTest("NE-105 should not expose preflight OPTIONS request")]
    public async Task ShouldNotExposePreflightOPTIONSRequest()
    {
        var serverRequests = new List<string>();
        Server.SetRoute("/cors", (req, res) =>
        {
            serverRequests.Add($"{req.Method} {req.Url}");
            if (req.Method == "OPTIONS")
            {
                res.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Access-Control-Allow-Methods"] = "GET, POST, PUT, OPTIONS",
                    ["Access-Control-Allow-Headers"] = "*",
                });
                res.End();
                return;
            }
            res.WriteHead(200, new() { ["Content-type"] = "text/plain", ["Access-Control-Allow-Origin"] = "*" });
            res.End("Hello there!");
        });
        var clientRequests = new List<string>();
        Page.Request += (_, request) =>
        {
            clientRequests.Add($"{request.Method} {request.Url}");
        };
        // .NET: await page.evaluate(...).catch(() => {}) gives undefined when the call fails.
        string response = null;
        try
        {
            response = await Page.EvaluateAsync<string>(
                @"async url => {
    const response = await fetch(url, {
      method: 'POST',
      body: '',
      headers: {
        'Content-Type': 'application/json',
        'X-Custom-Header': 'test-value'
      }
    });
    return await response.text();
  }",
                Server.CrossProcessPrefix + "/cors");
        }
        catch (Exception)
        {
        }
        Assert.That(response, Is.EqualTo("Hello there!"));
        Assert.That(serverRequests, Is.EqualTo(new[]
        {
            "OPTIONS /cors",
            "POST /cors",
        }));
        Assert.That(clientRequests, Is.EqualTo(new[]
        {
            $"POST {Server.CrossProcessPrefix}/cors",
        }));
    }

    [UpstreamTest("NE-106 should not expose preflight OPTIONS request with network interception")]
    public async Task ShouldNotExposePreflightOPTIONSRequestWithNetworkInterception()
    {
        var serverRequests = new List<string>();
        Server.SetRoute("/cors", (req, res) =>
        {
            serverRequests.Add($"{req.Method} {req.Url}");
            if (req.Method == "OPTIONS")
            {
                res.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Access-Control-Allow-Methods"] = "GET, POST, PUT, OPTIONS",
                    ["Access-Control-Allow-Headers"] = "*",
                });
                res.End();
                return;
            }
            res.WriteHead(200, new() { ["Content-type"] = "text/plain", ["Access-Control-Allow-Origin"] = "*" });
            res.End("Hello there!");
        });
        await Page.RouteAsync("**/*", route => route.ContinueAsync());
        var clientRequests = new List<string>();
        Page.Request += (_, request) =>
        {
            clientRequests.Add($"{request.Method} {request.Url}");
        };
        // .NET: await page.evaluate(...).catch(() => {}) gives undefined when the call fails.
        string response = null;
        try
        {
            response = await Page.EvaluateAsync<string>(
                @"async url => {
    const response = await fetch(url, {
      method: 'POST',
      body: '',
      headers: {
        'Content-Type': 'application/json',
        'X-Custom-Header': 'test-value'
      }
    });
    return await response.text();
  }",
                Server.CrossProcessPrefix + "/cors");
        }
        catch (Exception)
        {
        }
        Assert.That(response, Is.EqualTo("Hello there!"));
        Assert.That(serverRequests, Is.EqualTo((string[])[
            .. (BrowserName == "chromium" || IsBidi ? [] : new[] { "OPTIONS /cors" }),
            "POST /cors",
        ]));
        Assert.That(clientRequests, Is.EqualTo(new[]
        {
            $"POST {Server.CrossProcessPrefix}/cors",
        }));
    }

    [UpstreamTest("NE-107 should return last requests")]
    public async Task ShouldReturnLastRequests()
    {
        await Page.GotoAsync(Server.Prefix + "/title.html");
        for (var i = 0; i < 200; ++i)
        {
            Server.SetRoute("/fetch?" + i, (req, res) => res.End("url:" + Server.Prefix + req.Url));
        }

        // #0 is the navigation request, so start with #1.
        for (var i = 0; i < 99; ++i)
        {
            await Page.EvaluateAsync("url => fetch(url)", Server.Prefix + "/fetch?" + i);
        }
        var first99Requests = (await Page.RequestsAsync()).ToList();
        first99Requests.RemoveAt(0);
        for (var i = 99; i < 199; ++i)
        {
            await Page.EvaluateAsync("url => fetch(url)", Server.Prefix + "/fetch?" + i);
        }
        var last100Requests = await Page.RequestsAsync();
        IRequest[] allRequests = [.. first99Requests, .. last100Requests];

        // All 199 requests are fully functional.
        var received = await PromiseAll(allRequests.Select(async request =>
        {
            var response = await request.ResponseAsync();
            return new { text = await response.TextAsync(), url = request.Url };
        }));
        var expected = new List<object>();
        for (var i = 0; i < 199; ++i)
        {
            var url = Server.Prefix + "/fetch?" + i;
            expected.Add(new { url, text = "url:" + url });
        }
        ExpectToEqual(received, expected);
    }
}
