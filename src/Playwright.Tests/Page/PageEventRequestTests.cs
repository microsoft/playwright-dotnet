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
public class PageEventRequestTests
{
    [UpstreamTest("NE-091 should fire for navigation requests")]
    public async Task ShouldFireForNavigationRequests(IPage page, Server server)
    {
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        await page.GotoAsync(server.EmptyPage);
        Assert.That(requests.Count, Is.EqualTo(1));
    }

    [UpstreamTest("NE-092 should fire for iframes")]
    public async Task ShouldFireForIframes(IPage page, Server server)
    {
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        await page.GotoAsync(server.EmptyPage);
        await AttachFrame(page, "frame1", server.EmptyPage);
        Assert.That(requests.Count, Is.EqualTo(2));
    }

    [UpstreamTest("NE-093 should fire for fetches")]
    public async Task ShouldFireForFetches(IPage page, Server server)
    {
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync("() => fetch('/empty.html')");
        Assert.That(requests.Count, Is.EqualTo(2));
    }

    [UpstreamTest("NE-094 should fire for fetches with keepalive: true")]
    public async Task ShouldFireForFetchesWithKeepaliveTrue(IPage page, Server server, string browserName)
    {
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync("() => fetch('/empty.html', { keepalive: true })");
        Assert.That(requests.Count, Is.EqualTo(2));
    }

    [UpstreamTest("NE-095 should report requests and responses handled by service worker")]
    public async Task ShouldReportRequestsAndResponsesHandledByServiceWorker(IPage page, Server server, bool isAndroid, bool isElectron)
    {
        It.Fixme(isAndroid);
        It.Fixme(isElectron);

        await page.GotoAsync(server.Prefix + "/serviceworkers/fetchdummy/sw.html");
        await page.EvaluateAsync("() => window['activationPromise']");
        var (request, swResponse) = await PromiseAll(
            page.WaitForRequestAsync(_ => true),
            page.EvaluateAsync<string>("() => window['fetchDummy']('foo')"));
        Assert.That(swResponse, Is.EqualTo("responseFromServiceWorker:foo"));
        Assert.That(request.Url, Is.EqualTo(server.Prefix + "/serviceworkers/fetchdummy/foo"));
        // .NET: expect(request.serviceWorker()).toBe(null) is left out: request.serviceWorker() is not in the .NET API.
        var response = await request.ResponseAsync();
        Assert.That(response.Url, Is.EqualTo(server.Prefix + "/serviceworkers/fetchdummy/foo"));
        Assert.That(await response.TextAsync(), Is.EqualTo("responseFromServiceWorker:foo"));
        Assert.That(response.FromServiceWorker, Is.EqualTo(true));

        var (failedRequest, _) = await PromiseAll(
            Once<IRequest>(h => page.RequestFailed += h, h => page.RequestFailed -= h),
            Catch(() => page.EvaluateAsync("() => window['fetchDummy']('error')")));
        Assert.That(failedRequest.Url, Is.EqualTo(server.Prefix + "/serviceworkers/fetchdummy/error"));
        Assert.That(failedRequest.Failure, Is.Not.Null);
        // .NET: expect(failedRequest.serviceWorker()).toBe(null) is left out: request.serviceWorker() is not in the .NET API.
        Assert.That(await failedRequest.ResponseAsync(), Is.Null);
    }

    [UpstreamTest("NE-096 should report requests and responses handled by service worker with routing")]
    public async Task ShouldReportRequestsAndResponsesHandledByServiceWorkerWithRouting(IPage page, Server server, bool isAndroid, bool isElectron, string mode, string browserName, string platform)
    {
        It.Fixme(isAndroid);
        It.Fixme(isElectron);

        var interceptedUrls = new List<string>();
        await page.RouteAsync("**/*", route =>
        {
            interceptedUrls.Add(route.Request.Url);
            IgnoreErrors(route.ContinueAsync());
        });
        await page.GotoAsync(server.Prefix + "/serviceworkers/fetchdummy/sw.html");
        await page.EvaluateAsync("() => window['activationPromise']");
        var (swResponse, request) = await PromiseAll(
            page.EvaluateAsync<string>("() => window['fetchDummy']('foo')"),
            page.WaitForRequestAsync(_ => true));
        Assert.That(swResponse, Is.EqualTo("responseFromServiceWorker:foo"));
        Assert.That(request.Url, Is.EqualTo(server.Prefix + "/serviceworkers/fetchdummy/foo"));
        // .NET: expect(request.serviceWorker()).toBe(null) is left out: request.serviceWorker() is not in the .NET API.
        var response = await request.ResponseAsync();
        Assert.That(response.Url, Is.EqualTo(server.Prefix + "/serviceworkers/fetchdummy/foo"));
        Assert.That(await response.TextAsync(), Is.EqualTo("responseFromServiceWorker:foo"));

        var (failedRequest, _) = await PromiseAll(
            Once<IRequest>(h => page.RequestFailed += h, h => page.RequestFailed -= h),
            Catch(() => page.EvaluateAsync("() => window['fetchDummy']('error')")));
        Assert.That(failedRequest.Url, Is.EqualTo(server.Prefix + "/serviceworkers/fetchdummy/error"));
        Assert.That(failedRequest.Failure, Is.Not.Null);
        // .NET: expect(failedRequest.serviceWorker()).toBe(null) is left out: request.serviceWorker() is not in the .NET API.
        Assert.That(await failedRequest.ResponseAsync(), Is.Null);

        var expectedUrls = new List<string> { server.Prefix + "/serviceworkers/fetchdummy/sw.html" };
        if (browserName == "webkit")
        {
            expectedUrls.Add(server.Prefix + "/serviceworkers/fetchdummy/sw.js");
        }
        Assert.That(interceptedUrls, Is.EqualTo(expectedUrls));
    }

    [UpstreamTest("NE-097 should report navigation requests and responses handled by service worker")]
    public async Task ShouldReportNavigationRequestsAndResponsesHandledByServiceWorker(IPage page, Server server, bool isAndroid, string browserName)
    {
        It.Fixme(isAndroid);

        await page.GotoAsync(server.Prefix + "/serviceworkers/stub/sw.html");
        await page.EvaluateAsync("() => window['activationPromise']");

        var reloadResponse = await page.ReloadAsync();
        Assert.That(await page.EvaluateAsync<bool>("window.fromSW"), Is.EqualTo(true));
        Assert.That(reloadResponse.Url, Is.EqualTo(server.Prefix + "/serviceworkers/stub/sw.html"));
        await page.EvaluateAsync("() => window['activationPromise']");

        if (browserName != "firefox")
        {
            // When SW fetch throws, Firefox does not fail the navigation,
            // but rather falls back to the real network.

            var (_, failedRequest) = await PromiseAll(
                page.EvaluateAsync(@"() => {
        window.location.href = '/serviceworkers/stub/error.html';
      }"),
                Once<IRequest>(h => page.RequestFailed += h, h => page.RequestFailed -= h));
            Assert.That(failedRequest.Url, Is.EqualTo(server.Prefix + "/serviceworkers/stub/error.html"));
            Assert.That(failedRequest.Failure, Does.Contain(browserName == "chromium" ? "net::ERR_FAILED" : "uh oh"));
            // .NET: expect(failedRequest.serviceWorker()).toBe(null) is left out: request.serviceWorker() is not in the .NET API.
            Assert.That(await failedRequest.ResponseAsync(), Is.Null);
        }
    }

    [UpstreamTest("NE-098 should report navigation requests and responses handled by service worker with routing")]
    public async Task ShouldReportNavigationRequestsAndResponsesHandledByServiceWorkerWithRouting(IPage page, Server server, bool isAndroid, string browserName)
    {
        It.Fixme(isAndroid);

        await page.RouteAsync("**/*", route => route.ContinueAsync());
        await page.GotoAsync(server.Prefix + "/serviceworkers/stub/sw.html");
        await page.EvaluateAsync("() => window['activationPromise']");

        var reloadResponse = await page.ReloadAsync();
        Assert.That(await page.EvaluateAsync<bool>("window.fromSW"), Is.EqualTo(true));
        Assert.That(reloadResponse.Url, Is.EqualTo(server.Prefix + "/serviceworkers/stub/sw.html"));
        await page.EvaluateAsync("() => window['activationPromise']");

        if (browserName != "firefox")
        {
            // When SW fetch throws, Firefox does not fail the navigation,
            // but rather falls back to the real network.

            var (_, failedRequest) = await PromiseAll(
                page.EvaluateAsync(@"() => {
        window.location.href = '/serviceworkers/stub/error.html';
        // eslint-disable-next-line
        undefined
      }"),
                Once<IRequest>(h => page.RequestFailed += h, h => page.RequestFailed -= h));
            Assert.That(failedRequest.Url, Is.EqualTo(server.Prefix + "/serviceworkers/stub/error.html"));
            Assert.That(failedRequest.Failure, Does.Contain(browserName == "chromium" ? "net::ERR_FAILED" : "uh oh"));
            // .NET: expect(failedRequest.serviceWorker()).toBe(null) is left out: request.serviceWorker() is not in the .NET API.
            Assert.That(await failedRequest.ResponseAsync(), Is.Null);
        }
    }

    [UpstreamTest("NE-099 should return response body when Cross-Origin-Opener-Policy is set")]
    public async Task ShouldReturnResponseBodyWhenCrossOriginOpenerPolicyIsSet(IPage page, Server server, string browserName)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Cross-Origin-Opener-Policy", "same-origin");
            // Note: without 'onload', Firefox sometimes does not fire the load event
            // over the protocol. The reason is unclear.
            res.End(@"
      <div>Hello there!</div>
      <script>window.onload = () => console.log('onload')</script>
    ");
        });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
        await response.FinishedAsync();
        Assert.That(response.Request.Failure, Is.Null);
        Assert.That(await response.TextAsync(), Does.Contain("Hello there!"));
    }

    [UpstreamTest("NE-100 should fire requestfailed when intercepting race")]
    public async Task ShouldFireRequestfailedWhenInterceptingRace(IPage page, Server server, string browserName)
    {
        It.Skip(browserName != "chromium", "This test is specifically testing Chromium race");

        var promise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        {
            var counter = 0;
            var failures = new HashSet<IRequest>();
            var alive = new HashSet<IRequest>();
            page.Request += (_, request) =>
            {
                Assert.That(alive.Contains(request), Is.EqualTo(false));
                Assert.That(failures.Contains(request), Is.EqualTo(false));
                alive.Add(request);
            };
            page.RequestFailed += (_, request) =>
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
        await page.RouteAsync("**", route => { });

        await page.SetContentAsync($@"
    <iframe src=""{server.EmptyPage}""></iframe>
    <iframe src=""{server.EmptyPage}""></iframe>
    <iframe src=""{server.EmptyPage}""></iframe>
    <iframe src=""{server.EmptyPage}""></iframe>
    <iframe src=""{server.EmptyPage}""></iframe>
    <iframe src=""{server.EmptyPage}""></iframe>
    <iframe src=""{server.EmptyPage}""></iframe>
    <iframe src=""{server.EmptyPage}""></iframe>
    <iframe src=""{server.EmptyPage}""></iframe>
    <iframe src=""{server.EmptyPage}""></iframe>
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
    public async Task MainResourceXhrShouldHaveTypeXhr(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync(_ => true),
            page.EvaluateAsync(@"() => {
      const x = new XMLHttpRequest();
      x.open('GET', location.href, false);
      x.send();
    }"));
        Assert.That(request.IsNavigationRequest, Is.EqualTo(false));
        Assert.That(request.ResourceType, Is.EqualTo("xhr"));
    }

    [UpstreamTest("NE-102 should finish 204 request")]
    public async Task ShouldFinish204Request(IPage page, Server server)
    {
        server.SetRoute("/204", (req, res) =>
        {
            res.WriteHead(204, new() { ["Content-type"] = "text/plain" });
            res.End();
        });
        await page.GotoAsync(server.EmptyPage);
        // .NET: page.waitForEvent('requestfailed', ...).then(() => 'requestfailed') and page.waitForEvent('requestfinished', ...).then(() => 'requestfinished') are local functions that are called in their place.
        async Task<string> RequestFailed()
        {
            await Once<IRequest>(h => page.RequestFailed += h, h => page.RequestFailed -= h, r => r.Url.EndsWith("/204", StringComparison.Ordinal));
            return "requestfailed";
        }
        async Task<string> RequestFinished()
        {
            await page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url.EndsWith("/204", StringComparison.Ordinal) });
            return "requestfinished";
        }
        var reqPromise = Task.WhenAny(
            RequestFailed(),
            RequestFinished());
        IgnoreErrors(page.EvaluateAsync("async url => { await fetch(url); }", server.Prefix + "/204"));
        Assert.That(await await reqPromise, Is.EqualTo("requestfinished"));
    }

    [UpstreamTest("NE-103 should fail 204 navigation request")]
    public async Task ShouldFail204NavigationRequest(IPage page, Server server)
    {
        server.SetRoute("/204", (req, res) =>
        {
            res.StatusCode = 204;
            res.End();
        });
        await page.GotoAsync(server.EmptyPage);
        // .NET: page.waitForEvent('requestfailed', ...).then(() => 'requestfailed') and page.waitForEvent('requestfinished', ...).then(() => 'requestfinished') are local functions that are called in their place.
        async Task<string> RequestFailed()
        {
            await Once<IRequest>(h => page.RequestFailed += h, h => page.RequestFailed -= h, r => r.Url.EndsWith("/204", StringComparison.Ordinal));
            return "requestfailed";
        }
        async Task<string> RequestFinished()
        {
            await page.WaitForRequestFinishedAsync(new() { Predicate = r => r.Url.EndsWith("/204", StringComparison.Ordinal) });
            return "requestfinished";
        }
        var reqPromise = Task.WhenAny(
            RequestFailed(),
            RequestFinished());
        await page.SetContentAsync($"<a href=\"{server.Prefix}/204\">link</a>");
        await page.ClickAsync("a", new() { NoWaitAfter = true });
        Assert.That(await await reqPromise, Is.EqualTo("requestfailed"));
    }

    [UpstreamTest("NE-104 <picture> resource should have type image")]
    public async Task PictureResourceShouldHaveTypeImage(IPage page)
    {
        var request = await PromiseAll(
            page.WaitForRequestAsync(_ => true),
            page.SetContentAsync(@"
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
    public async Task ShouldNotExposePreflightOPTIONSRequest(IPage page, Server server, string browserName)
    {
        var serverRequests = new List<string>();
        server.SetRoute("/cors", (req, res) =>
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
        page.Request += (_, request) =>
        {
            clientRequests.Add($"{request.Method} {request.Url}");
        };
        // .NET: await page.evaluate(...).catch(() => {}) gives undefined when the call fails.
        string response = null;
        try
        {
            response = await page.EvaluateAsync<string>(
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
                server.CrossProcessPrefix + "/cors");
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
            $"POST {server.CrossProcessPrefix}/cors",
        }));
    }

    [UpstreamTest("NE-106 should not expose preflight OPTIONS request with network interception")]
    public async Task ShouldNotExposePreflightOPTIONSRequestWithNetworkInterception(IPage page, Server server, string browserName, bool isBidi)
    {
        var serverRequests = new List<string>();
        server.SetRoute("/cors", (req, res) =>
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
        await page.RouteAsync("**/*", route => route.ContinueAsync());
        var clientRequests = new List<string>();
        page.Request += (_, request) =>
        {
            clientRequests.Add($"{request.Method} {request.Url}");
        };
        // .NET: await page.evaluate(...).catch(() => {}) gives undefined when the call fails.
        string response = null;
        try
        {
            response = await page.EvaluateAsync<string>(
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
                server.CrossProcessPrefix + "/cors");
        }
        catch (Exception)
        {
        }
        Assert.That(response, Is.EqualTo("Hello there!"));
        Assert.That(serverRequests, Is.EqualTo((string[])[
            .. (browserName == "chromium" || isBidi ? [] : new[] { "OPTIONS /cors" }),
            "POST /cors",
        ]));
        Assert.That(clientRequests, Is.EqualTo(new[]
        {
            $"POST {server.CrossProcessPrefix}/cors",
        }));
    }

    [UpstreamTest("NE-107 should return last requests")]
    public async Task ShouldReturnLastRequests(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/title.html");
        for (var i = 0; i < 200; ++i)
        {
            server.SetRoute("/fetch?" + i, (req, res) => res.End("url:" + server.Prefix + req.Url));
        }

        // #0 is the navigation request, so start with #1.
        for (var i = 0; i < 99; ++i)
        {
            await page.EvaluateAsync("url => fetch(url)", server.Prefix + "/fetch?" + i);
        }
        var first99Requests = (await page.RequestsAsync()).ToList();
        first99Requests.RemoveAt(0);
        for (var i = 99; i < 199; ++i)
        {
            await page.EvaluateAsync("url => fetch(url)", server.Prefix + "/fetch?" + i);
        }
        var last100Requests = await page.RequestsAsync();
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
            var url = server.Prefix + "/fetch?" + i;
            expected.Add(new { url, text = "url:" + url });
        }
        ExpectToEqual(received, expected);
    }
}
