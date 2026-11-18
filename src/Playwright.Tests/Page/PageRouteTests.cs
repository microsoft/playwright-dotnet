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

using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-route.spec.ts.</summary>
public class PageRouteTests : UpstreamPageTest
{
    [UpstreamTest("RO-001 should intercept @smoke")]
    public async Task ShouldIntercept()
    {
        var intercepted = false;
        await Page.RouteAsync("**/empty.html", route =>
        {
            Assert.That(route.Request, Is.SameAs(route.Request));
            Assert.That(route.Request.Url, Does.Contain("empty.html"));
            Assert.That(route.Request.Headers["user-agent"], Is.Not.Empty);
            Assert.That(route.Request.Method, Is.EqualTo("GET"));
            Assert.That(route.Request.PostData, Is.Null);
            Assert.That(route.Request.IsNavigationRequest, Is.EqualTo(true));
            Assert.That(route.Request.ResourceType, Is.EqualTo("document"));
            Assert.That(route.Request.Frame == Page.MainFrame, Is.EqualTo(true));
            Assert.That(route.Request.Frame.Url, Is.EqualTo("about:blank"));
            IgnoreErrors(route.ContinueAsync());
            intercepted = true;
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(intercepted, Is.EqualTo(true));
    }

    [UpstreamTest("RO-002 should unroute")]
    public async Task ShouldUnroute()
    {
        var intercepted = new List<int>();
        await Page.RouteAsync("**/*", route =>
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
        Action<IRoute> handler4 = route =>
        {
            intercepted.Add(4);
            IgnoreErrors(route.FallbackAsync());
        };
        await Page.RouteAsync(new Regex("empty.html"), handler4);
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 4, 3, 2, 1 }));

        intercepted = new List<int>();
        await Page.UnrouteAsync(new Regex("empty.html"), handler4);
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 3, 2, 1 }));

        intercepted = new List<int>();
        await Page.UnrouteAsync("**/empty.html");
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 1 }));
    }

    [UpstreamTest("RO-003 should not support ? in glob pattern")]
    public async Task ShouldNotSupportInGlobPattern()
    {
        Server.SetRoute("/index", (req, res) => res.End("index-no-hello"));
        Server.SetRoute("/index123hello", (req, res) => res.End("index123hello"));
        Server.SetRoute("/index?hello", (req, res) => res.End("index?hello"));
        Server.SetRoute("/index1hello", (req, res) => res.End("index1hello"));

        await Page.RouteAsync("**/index?hello", async route =>
        {
            await route.FulfillAsync(new() { Body = "intercepted any character" });
        });

        await Page.RouteAsync("**/index\\?hello", async route =>
        {
            await route.FulfillAsync(new() { Body = "intercepted question mark" });
        });

        await Page.GotoAsync(Server.Prefix + "/index?hello");
        Assert.That(await Page.ContentAsync(), Does.Contain("intercepted question mark"));

        await Page.GotoAsync(Server.Prefix + "/index");
        Assert.That(await Page.ContentAsync(), Does.Contain("index-no-hello"));

        await Page.GotoAsync(Server.Prefix + "/index1hello");
        Assert.That(await Page.ContentAsync(), Does.Contain("index1hello"));

        await Page.GotoAsync(Server.Prefix + "/index123hello");
        Assert.That(await Page.ContentAsync(), Does.Contain("index123hello"));
    }

    [UpstreamTest("RO-004 should work when POST is redirected with 302")]
    public async Task ShouldWorkWhenPOSTIsRedirectedWith302()
    {
        Server.SetRedirect("/rredirect", "/empty.html");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/*", route => route.ContinueAsync());
        await Page.SetContentAsync(@"
    <form action='/rredirect' method='post'>
      <input type=""hidden"" id=""foo"" name=""foo"" value=""FOOBAR"">
    </form>
  ");
        await PromiseAll(
            Page.EvalOnSelectorAsync("form", "form => form.submit()"),
            Page.WaitForNavigationAsync());
    }

    // @see https://github.com/GoogleChrome/puppeteer/issues/3973
    [UpstreamTest("RO-005 should work when header manipulation headers with redirect")]
    public async Task ShouldWorkWhenHeaderManipulationHeadersWithRedirect()
    {
        Server.SetRedirect("/rrredirect", "/empty.html");
        await Page.RouteAsync("**/*", route =>
        {
            var headers = new Dictionary<string, string>(route.Request.Headers)
            {
                ["foo"] = "bar",
            };
            IgnoreErrors(route.ContinueAsync(new() { Headers = headers }));
        });
        await Page.GotoAsync(Server.Prefix + "/rrredirect");
    }

    // @see https://github.com/GoogleChrome/puppeteer/issues/4743
    [UpstreamTest("RO-006 should be able to remove headers")]
    public async Task ShouldBeAbleToRemoveHeaders()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/*", route =>
        {
            var headers = new Dictionary<string, string>(route.Request.Headers);
            headers.Remove("foo");
            IgnoreErrors(route.ContinueAsync(new() { Headers = headers }));
        });

        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/title.html"),
            Page.EvaluateAsync("url => fetch(url, { headers: { foo: 'bar' } })", Server.Prefix + "/title.html"));
        Assert.That(serverRequest.Headers["foo"], Is.Null);
    }

    [UpstreamTest("RO-007 should contain referer header")]
    public async Task ShouldContainRefererHeader()
    {
        var requests = new List<IRequest>();
        await Page.RouteAsync("**/*", route =>
        {
            requests.Add(route.Request);
            IgnoreErrors(route.ContinueAsync());
        });
        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.That(requests[1].Url, Does.Contain("/one-style.css"));
        Assert.That(requests[1].Headers["referer"], Does.Contain("/one-style.html"));
    }

    [UpstreamTest("RO-008 should properly return navigation response when URL has cookies")]
    public async Task ShouldProperlyReturnNavigationResponseWhenURLHasCookies()
    {
        Info.Skip(IsAndroid, "No isolated context");
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");

        // Setup cookie.
        await Page.GotoAsync(Server.EmptyPage);
        await Page.Context.AddCookiesAsync(new[] { new Cookie { Url = Server.EmptyPage, Name = "foo", Value = "bar" } });

        // Setup request interception.
        await Page.RouteAsync("**/*", route => route.ContinueAsync());
        var response = await Page.ReloadAsync();
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("RO-009 should not override cookie header")]
    public async Task ShouldNotOverrideCookieHeader()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync("() => document.cookie = 'original=value'");
        string cookieValueInRoute = null;
        await Page.RouteAsync("**", async route =>
        {
            var headers = await route.Request.AllHeadersAsync();
            cookieValueInRoute = headers.GetValueOrDefault("cookie");
            headers["cookie"] = "overridden=value";
            IgnoreErrors(route.ContinueAsync(new() { Headers = headers }));
        });
        var (serverReq, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            Page.GotoAsync(Server.EmptyPage));

        if (BrowserName != "webkit")
        {
            Assert.That(cookieValueInRoute, Is.EqualTo("original=value"));
        }
        Assert.That(serverReq.Headers["cookie"], Is.EqualTo("original=value"));
    }

    [UpstreamTest("RO-010 should show custom HTTP headers")]
    public async Task ShouldShowCustomHTTPHeaders()
    {
        await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["foo"] = "bar",
        });
        await Page.RouteAsync("**/*", route =>
        {
            Assert.That(route.Request.Headers["foo"], Is.EqualTo("bar"));
            IgnoreErrors(route.ContinueAsync());
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
    }

    // @see https://github.com/GoogleChrome/puppeteer/issues/4337
    [UpstreamTest("RO-011 should work with redirect inside sync XHR")]
    public async Task ShouldWorkWithRedirectInsideSyncXHR()
    {
        Info.Fixme(BrowserName == "webkit", "No Network.requestIntercepted for the request");
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRedirect("/logo.png", "/pptr.png");
        Task continuePromise = null;
        await Page.RouteAsync("**/*", route =>
        {
            continuePromise = route.ContinueAsync();
        });
        var status = await Page.EvaluateAsync<int>(@"async () => {
    const request = new XMLHttpRequest();
    request.open('GET', '/logo.png', false);  // `false` makes the request synchronous
    request.send(null);
    return request.status;
  }");
        Assert.That(status, Is.EqualTo(200));
        Assert.That(continuePromise, Is.Not.Null);
        await continuePromise;
    }

    [UpstreamTest("RO-012 should pause intercepted XHR until continue")]
    public async Task ShouldPauseInterceptedXHRUntilContinue()
    {
        Info.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");
        Info.Fixme(BrowserName == "webkit", "Redirected request is not paused in WebKit");

        await Page.GotoAsync(Server.EmptyPage);
        var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync("**/global-var.html", route => routePromise.TrySetResult(route));
        var xhrFinished = false;
        Page.RequestFinished += (_, request) =>
        {
            if (request.Url.EndsWith("/global-var.html", StringComparison.Ordinal))
            {
                xhrFinished = true;
            }
        };
        // The synchronous request blocks the page, so it is started from a timer.
        await Page.EvaluateAsync(@"() => {
    window.__statusPromise = new Promise(resolve => setTimeout(() => {
      const request = new XMLHttpRequest();
      request.open('GET', '/global-var.html', false);  // `false` makes the request synchronous
      request.send(null);
      resolve(request.status);
    }, 0));
  }");
        var route = await routePromise.Task;
        // Check that intercepted request is actually paused.
        await Task.Delay(500);
        Assert.That(xhrFinished, Is.EqualTo(false));
        await route.ContinueAsync();
        Assert.That(await Page.EvaluateAsync<int>("() => window.__statusPromise"), Is.EqualTo(200));
    }

    [UpstreamTest("RO-013 should pause intercepted fetch request until continue")]
    public async Task ShouldPauseInterceptedFetchRequestUntilContinue()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync("**/global-var.html", route => routePromise.TrySetResult(route));
        await Page.EvaluateAsync(@"() => {
    window.__fetchFinished = false;
    window.__statusPromise = fetch('/global-var.html').then(response => {
      window.__fetchFinished = true;
      return response.status;
    });
  }");
        var route = await routePromise.Task;
        // Check that intercepted request is actually paused.
        await Task.Delay(500);
        Assert.That(await Page.EvaluateAsync<bool>("() => window.__fetchFinished"), Is.EqualTo(false));
        await route.ContinueAsync();
        Assert.That(await Page.EvaluateAsync<int>("() => window.__statusPromise"), Is.EqualTo(200));
    }

    [UpstreamTest("RO-014 should work with custom referer headers")]
    public async Task ShouldWorkWithCustomRefererHeaders()
    {
        await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = Server.EmptyPage });
        await Page.RouteAsync("**/*", route =>
        {
            // See https://github.com/microsoft/playwright/issues/8999
            if (BrowserName == "chromium" && BrowserMajorVersion < 154)
            {
                Assert.That(route.Request.Headers["referer"], Is.EqualTo(Server.EmptyPage + ", " + Server.EmptyPage));
            }
            else
            {
                Assert.That(route.Request.Headers["referer"], Is.EqualTo(Server.EmptyPage));
            }
            IgnoreErrors(route.ContinueAsync());
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
    }

    [UpstreamTest("RO-015 should be abortable")]
    public async Task ShouldBeAbortable()
    {
        await Page.RouteAsync(new Regex("\\.css$"), route => route.AbortAsync());
        var failed = false;
        Page.RequestFailed += (_, request) =>
        {
            if (request.Url.Contains(".css", StringComparison.Ordinal))
            {
                failed = true;
            }
        };
        var response = await Page.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(response.Request.Failure, Is.Null);
        Assert.That(failed, Is.EqualTo(true));
    }

    [UpstreamTest("RO-016 should be abortable with custom error codes")]
    public async Task ShouldBeAbortableWithCustomErrorCodes()
    {
        await Page.RouteAsync("**/*", route => route.AbortAsync("internetdisconnected"));
        IRequest failedRequest = null;
        Page.RequestFailed += (_, request) => failedRequest = request;
        await Catch(() => Page.GotoAsync(Server.EmptyPage));
        Assert.That(failedRequest, Is.Not.Null);
        if (BrowserName == "webkit")
        {
            Assert.That(failedRequest.Failure, Is.EqualTo(IsMac && MacVersion < 11 ? "Request intercepted" : "Blocked by Web Inspector"));
        }
        else if (BrowserName == "firefox")
        {
            Assert.That(failedRequest.Failure, Is.EqualTo("NS_ERROR_OFFLINE"));
        }
        else
        {
            Assert.That(failedRequest.Failure, Is.EqualTo("net::ERR_INTERNET_DISCONNECTED"));
        }
    }

    [UpstreamTest("RO-017 should not throw if request was cancelled by the page")]
    public async Task ShouldNotThrowIfRequestWasCancelledByThePage()
    {
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
        await route.AbortAsync(); // Should not throw.
    }

    [UpstreamTest("RO-018 should send referer")]
    public async Task ShouldSendReferer()
    {
        await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["referer"] = "http://google.com/",
        });
        await Page.RouteAsync("**/*", route => route.ContinueAsync());
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/grid.html"),
            Page.GotoAsync(Server.Prefix + "/grid.html"));
        Assert.That(request.Headers["referer"], Is.EqualTo("http://google.com/"));
    }

    [UpstreamTest("RO-019 should fail navigation when aborting main resource")]
    public async Task ShouldFailNavigationWhenAbortingMainResource()
    {
        await Page.RouteAsync("**/*", route => route.AbortAsync());
        Exception error = null;
        error = await Catch(() => Page.GotoAsync(Server.EmptyPage));
        Assert.That(error, Is.Not.Null);
        if (BrowserName == "webkit")
        {
            Assert.That(error.Message, Does.Contain(IsMac && MacVersion < 11 ? "Request intercepted" : "Blocked by Web Inspector"));
        }
        else if (BrowserName == "firefox" && IsBidi)
        {
            Assert.That(error.Message, Does.Contain("NS_ERROR_ABORT"));
        }
        else if (BrowserName == "firefox")
        {
            Assert.That(error.Message, Does.Contain("NS_ERROR_FAILURE"));
        }
        else
        {
            Assert.That(error.Message, Does.Contain("net::ERR_FAILED"));
        }
    }

    [UpstreamTest("RO-020 should not work with redirects")]
    public async Task ShouldNotWorkWithRedirects()
    {
        var intercepted = new List<IRequest>();
        await Page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.ContinueAsync());
            intercepted.Add(route.Request);
        });
        Server.SetRedirect("/non-existing-page.html", "/non-existing-page-2.html");
        Server.SetRedirect("/non-existing-page-2.html", "/non-existing-page-3.html");
        Server.SetRedirect("/non-existing-page-3.html", "/non-existing-page-4.html");
        Server.SetRedirect("/non-existing-page-4.html", "/empty.html");

        var response = await Page.GotoAsync(Server.Prefix + "/non-existing-page.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.Url, Does.Contain("empty.html"));

        Assert.That(intercepted.Count, Is.EqualTo(1));
        Assert.That(intercepted[0].ResourceType, Is.EqualTo("document"));
        Assert.That(intercepted[0].IsNavigationRequest, Is.EqualTo(true));
        Assert.That(intercepted[0].Url, Does.Contain("/non-existing-page.html"));

        var chain = new List<IRequest>();
        for (var r = response.Request; r != null; r = r.RedirectedFrom)
        {
            chain.Add(r);
            Assert.That(r.IsNavigationRequest, Is.EqualTo(true));
        }
        Assert.That(chain.Count, Is.EqualTo(5));
        Assert.That(chain[0].Url, Does.Contain("/empty.html"));
        Assert.That(chain[1].Url, Does.Contain("/non-existing-page-4.html"));
        Assert.That(chain[2].Url, Does.Contain("/non-existing-page-3.html"));
        Assert.That(chain[3].Url, Does.Contain("/non-existing-page-2.html"));
        Assert.That(chain[4].Url, Does.Contain("/non-existing-page.html"));
        for (var i = 0; i < chain.Count; i++)
        {
            Assert.That(chain[i].RedirectedTo, Is.SameAs(i != 0 ? chain[i - 1] : null));
        }
    }

    [UpstreamTest("RO-021 should chain fallback w/ dynamic URL")]
    public async Task ShouldChainFallbackWDynamicURL()
    {
        var intercepted = new List<int>();
        await Page.RouteAsync("**/bar", route =>
        {
            intercepted.Add(1);
            IgnoreErrors(route.FallbackAsync(new() { Url = Server.EmptyPage }));
        });
        await Page.RouteAsync("**/foo", route =>
        {
            intercepted.Add(2);
            IgnoreErrors(route.FallbackAsync(new() { Url = "http://localhost/bar" }));
        });

        await Page.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(3);
            IgnoreErrors(route.FallbackAsync(new() { Url = "http://localhost/foo" }));
        });

        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { 3, 2, 1 }));
    }

    [UpstreamTest("RO-022 should work with redirects for subresources")]
    public async Task ShouldWorkWithRedirectsForSubresources()
    {
        var intercepted = new List<IRequest>();
        await Page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.ContinueAsync());
            intercepted.Add(route.Request);
        });
        Server.SetRedirect("/one-style.css", "/two-style.css");
        Server.SetRedirect("/two-style.css", "/three-style.css");
        Server.SetRedirect("/three-style.css", "/four-style.css");
        Server.SetRoute("/four-style.css", (req, res) => res.End("body {box-sizing: border-box; }"));

        var response = await Page.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.Url, Does.Contain("one-style.html"));

        Assert.That(intercepted.Count, Is.EqualTo(2));
        Assert.That(intercepted[0].ResourceType, Is.EqualTo("document"));
        Assert.That(intercepted[0].Url, Does.Contain("one-style.html"));

        var r = intercepted[1];
        foreach (var url in new[] { "/one-style.css", "/two-style.css", "/three-style.css", "/four-style.css" })
        {
            Assert.That(r.ResourceType, Is.EqualTo("stylesheet"));
            Assert.That(r.Url, Does.Contain(url));
            r = r.RedirectedTo;
        }
        Assert.That(r, Is.Null);
    }

    [UpstreamTest("RO-023 should work with equal requests")]
    public async Task ShouldWorkWithEqualRequests()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var responseCount = 1;
        Server.SetRoute("/zzz", (req, res) => res.End((responseCount++) * 11 + ""));

        var spinner = false;
        // Cancel 2nd request.
        await Page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(spinner ? route.AbortAsync() : route.ContinueAsync());
            spinner = !spinner;
        });
        var results = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            results.Add(await Page.EvaluateAsync<string>("() => fetch('/zzz').then(response => response.text()).catch(e => 'FAILED')"));
        }
        Assert.That(results, Is.EqualTo(new[] { "11", "FAILED", "22" }));
    }

    [UpstreamTest("RO-024 should navigate to dataURL and not fire dataURL requests")]
    public async Task ShouldNavigateToDataURLAndNotFireDataURLRequests()
    {
        var requests = new List<IRequest>();
        await Page.RouteAsync("**/*", route =>
        {
            requests.Add(route.Request);
            IgnoreErrors(route.ContinueAsync());
        });
        const string dataURL = "data:text/html,<div>yo</div>";
        var response = await Page.GotoAsync(dataURL);
        Assert.That(response, Is.Null);
        Assert.That(requests.Count, Is.EqualTo(0));
    }

    [UpstreamTest("RO-025 should be able to fetch dataURL and not fire dataURL requests")]
    public async Task ShouldBeAbleToFetchDataURLAndNotFireDataURLRequests()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var requests = new List<IRequest>();
        await Page.RouteAsync("**/*", route =>
        {
            requests.Add(route.Request);
            IgnoreErrors(route.ContinueAsync());
        });
        const string dataURL = "data:text/html,<div>yo</div>";
        var text = await Page.EvaluateAsync<string>("url => fetch(url).then(r => r.text())", dataURL);
        Assert.That(text, Is.EqualTo("<div>yo</div>"));
        Assert.That(requests.Count, Is.EqualTo(0));
    }

    [UpstreamTest("RO-026 should navigate to URL with hash and and fire requests without hash")]
    public async Task ShouldNavigateToURLWithHashAndAndFireRequestsWithoutHash()
    {
        var requests = new List<IRequest>();
        await Page.RouteAsync("**/*", route =>
        {
            requests.Add(route.Request);
            IgnoreErrors(route.ContinueAsync());
        });
        var response = await Page.GotoAsync(Server.EmptyPage + "#hash");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.Url, Is.EqualTo(Server.EmptyPage));
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(requests[0].Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("RO-027 should work with encoded server")]
    public async Task ShouldWorkWithEncodedServer()
    {
        // The requestWillBeSent will report encoded URL, whereas interception will
        // report URL as-is. @see crbug.com/759388
        await Page.RouteAsync("**/*", route => route.ContinueAsync());
        var response = await Page.GotoAsync(Server.Prefix + "/some nonexisting page");
        Assert.That(response.Status, Is.EqualTo(404));
    }

    [UpstreamTest("RO-028 should work with badly encoded server")]
    public async Task ShouldWorkWithBadlyEncodedServer()
    {
        Server.SetRoute("/malformed?rnd=%911", (req, res) => res.End());
        await Page.RouteAsync("**/*", route => route.ContinueAsync());
        var response = await Page.GotoAsync(Server.Prefix + "/malformed?rnd=%911");
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("RO-029 should work with encoded server - 2")]
    public async Task ShouldWorkWithEncodedServer2()
    {
        // The requestWillBeSent will report URL as-is, whereas interception will
        // report encoded URL for stylesheet. @see crbug.com/759388
        await Page.GotoAsync(Server.EmptyPage);
        var requests = new List<IRequest>();
        await Page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.ContinueAsync());
            requests.Add(route.Request);
        });
        await Page.SetContentAsync($"<link rel=\"stylesheet\" href=\"{Server.Prefix}/fonts?helvetica|arial\"/>");
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That((await requests[0].ResponseAsync()).Status, Is.EqualTo(404));
    }

    [UpstreamTest("RO-030 should not throw \"Invalid Interception Id\" if the request was cancelled")]
    public async Task ShouldNotThrowInvalidInterceptionIdIfTheRequestWasCancelled()
    {
        await Page.SetContentAsync("<iframe></iframe>");
        IRoute route = null;
        await Page.RouteAsync("**/*", r => route = r);
        IgnoreErrors(Page.EvalOnSelectorAsync("iframe", "(frame, url) => frame.src = url", Server.EmptyPage));
        // Wait for request interception.
        await Page.WaitForRequestAsync(_ => true);
        // Delete frame to cause request to be canceled.
        await Page.EvalOnSelectorAsync("iframe", "frame => frame.remove()");
        Exception error = null;
        error = await Catch(() => route.ContinueAsync());
        Assert.That(error, Is.Null);
    }

    [UpstreamTest("RO-031 should intercept main resource during cross-process navigation")]
    public async Task ShouldInterceptMainResourceDuringCrossProcessNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var intercepted = false;
        await Page.RouteAsync(Server.CrossProcessPrefix + "/empty.html", route =>
        {
            intercepted = true;
            IgnoreErrors(route.ContinueAsync());
        });
        var response = await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(intercepted, Is.EqualTo(true));
    }

    [UpstreamTest("RO-032 should fulfill with redirect status")]
    public async Task ShouldFulfillWithRedirectStatus()
    {
        Info.Fixme(BrowserName == "webkit", "in WebKit the redirects are handled by the network stack and we intercept before");

        await Page.GotoAsync(Server.Prefix + "/title.html");
        Server.SetRoute("/final", (req, res) => res.End("foo"));
        await Page.RouteAsync("**/*", async route =>
        {
            if (route.Request.Url != Server.Prefix + "/redirect_this")
            {
                await route.ContinueAsync();
                return;
            }
            await route.FulfillAsync(new()
            {
                Status = 301,
                Headers = new Dictionary<string, string>
                {
                    ["location"] = "/final",
                },
            });
        });

        var text = await Page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url);
    return data.text();
  }", Server.Prefix + "/redirect_this");
        Assert.That(text, Is.EqualTo("foo"));
    }

    [UpstreamTest("RO-033 should not fulfill with redirect status")]
    public async Task ShouldNotFulfillWithRedirectStatus()
    {
        Info.Skip(BrowserName != "webkit", "we should support fulfill with redirect in webkit and delete this test");

        await Page.GotoAsync(Server.Prefix + "/empty.html");

        var status = 0;
        // .NET: fulfill and reject are the two ends of one TaskCompletionSource.
        TaskCompletionSource<Exception> fulfill = null;
        await Page.RouteAsync("**/*", async route =>
        {
            if (route.Request.Url != Server.Prefix + "/redirect_this")
            {
                await route.ContinueAsync();
                return;
            }
            try
            {
                await route.FulfillAsync(new()
                {
                    Status = status,
                    Headers = new Dictionary<string, string>
                    {
                        ["location"] = "/empty.html",
                    },
                });
                fulfill.TrySetException(new Exception("fulfill didn't throw"));
            }
            catch (Exception e)
            {
                fulfill.TrySetResult(e);
            }
        });

        for (status = 300; status < 310; status++)
        {
            var (_, exception) = await PromiseAll(
                Page.EvaluateAsync("url => location.href = url", Server.Prefix + "/redirect_this"),
                (fulfill = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously)).Task);
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Message.Contains("Cannot fulfill with redirect status", StringComparison.Ordinal), Is.EqualTo(true));
        }
    }

    [UpstreamTest("RO-034 should support cors with GET")]
    public async Task ShouldSupportCorsWithGET()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/cars*", async route =>
        {
            var headers = new Dictionary<string, string> { ["access-control-allow-origin"] = route.Request.Url.EndsWith("allow", StringComparison.Ordinal) ? "*" : "none" };
            await route.FulfillAsync(new()
            {
                ContentType = "application/json",
                Headers = headers,
                Status = 200,
                Body = JsonSerializer.Serialize(new[] { "electric", "gas" }),
            });
        });
        {
            // Should succeed
            var resp = await Page.EvaluateAsync<string[]>(@"async () => {
      const response = await fetch('https://example.com/cars?allow', { mode: 'cors' });
      return response.json();
    }");
            Assert.That(resp, Is.EqualTo(new[] { "electric", "gas" }));
        }
        {
            // Should be rejected
            var error = await Catch(() => Page.EvaluateAsync(@"async () => {
      const response = await fetch('https://example.com/cars?reject', { mode: 'cors' });
      return response.json();
    }"));
            if (BrowserName == "chromium")
            {
                Assert.That(error.Message, Does.Contain("Failed"));
            }
            if (BrowserName == "webkit")
            {
                Assert.That(error.Message, Does.Contain("TypeError"));
            }
            if (BrowserName == "firefox")
            {
                Assert.That(error.Message, Does.Contain("NetworkError"));
            }
        }
    }

    [UpstreamTest("RO-035 should add Access-Control-Allow-Origin by default when fulfill")]
    public async Task ShouldAddAccessControlAllowOriginByDefaultWhenFulfill()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/cars", async route =>
        {
            await route.FulfillAsync(new()
            {
                ContentType = "application/json",
                Status = 200,
                Body = JsonSerializer.Serialize(new[] { "electric", "gas" }),
            });
        });

        var (result, response) = await PromiseAll(
            Page.EvaluateAsync<string[]>(@"async () => {
      const response = await fetch('https://example.com/cars', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        mode: 'cors',
        body: JSON.stringify({ 'number': 1 })
      });
      return response.json();
    }"),
            Page.WaitForResponseAsync("https://example.com/cars"));
        Assert.That(result, Is.EqualTo(new[] { "electric", "gas" }));
        Assert.That(await response.HeaderValueAsync("Access-Control-Allow-Origin"), Is.EqualTo(Server.Prefix));
    }

    [UpstreamTest("RO-036 should allow null origin for about:blank")]
    public async Task ShouldAllowNullOriginForAboutBlank()
    {
        await Page.RouteAsync("**/something", async route =>
        {
            await route.FulfillAsync(new()
            {
                ContentType = "text/plain",
                Status = 200,
                Body = "done",
            });
        });

        var (response, text) = await PromiseAll(
            Page.WaitForResponseAsync(Server.CrossProcessPrefix + "/something"),
            Page.EvaluateAsync<string>(@"async url => {
      const data = await fetch(url, {
        method: 'GET',
        headers: { 'X-PINGOTHER': 'pingpong' }
      });
      return data.text();
    }", Server.CrossProcessPrefix + "/something"));
        Assert.That(text, Is.EqualTo("done"));
        Assert.That(await response.HeaderValueAsync("Access-Control-Allow-Origin"), Is.EqualTo("null"));
    }

    [UpstreamTest("RO-037 should respect cors overrides")]
    public async Task ShouldRespectCorsOverrides()
    {
        Info.Fixme(IsAndroid, "no cors error in android emulator");
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/something", (request, response) =>
        {
            if (request.Method == "OPTIONS")
            {
                response.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Access-Control-Allow-Methods"] = "POST, GET, OPTIONS, DELETE",
                    ["Access-Control-Allow-Headers"] = "*",
                    ["Cache-Control"] = "no-cache",
                });
                response.End();
                return;
            }
            response.WriteHead(404, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("NOT FOUND");
        });
        // Fetch request should fail when CORS header doesn't include the origin.
        {
            await Page.RouteAsync("**/something", async route =>
            {
                await route.FulfillAsync(new()
                {
                    ContentType = "text/plain",
                    Status = 200,
                    Headers = new Dictionary<string, string> { ["Access-Control-Allow-Origin"] = "http://non-existent" },
                    Body = "done",
                });
            });

            var error = await Catch(() => Page.EvaluateAsync(@"async url => {
      const data = await fetch(url, {
        method: 'GET',
        headers: { 'X-PINGOTHER': 'pingpong' }
      });
      return data.text();
    }", Server.CrossProcessPrefix + "/something"));
            if (BrowserName == "chromium")
            {
                Assert.That(error.Message, Does.Contain("Failed to fetch"));
            }
            else if (BrowserName == "webkit")
            {
                Assert.That(error.Message, Does.Contain("Load failed"));
            }
            else if (BrowserName == "firefox")
            {
                Assert.That(error.Message, Does.Contain("NetworkError when attempting to fetch resource."));
            }
        }
    }

    [UpstreamTest("RO-038 should not auto-intercept non-preflight OPTIONS without network interception")]
    public async Task ShouldNotAutoInterceptNonPreflightOPTIONSWithoutNetworkInterception()
    {
        Info.Fixme(IsAndroid);

        await Page.GotoAsync(Server.EmptyPage);

        var requests = new List<string>();
        Server.SetRoute("/something", (request, response) =>
        {
            requests.Add(request.Method + ":" + request.Url);
            if (request.Method == "OPTIONS")
            {
                response.WriteHead(200, new()
                {
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Access-Control-Allow-Methods"] = "POST, GET, OPTIONS, DELETE",
                    ["Access-Control-Allow-Headers"] = "*",
                    ["Cache-Control"] = "no-cache",
                });
                response.End("Hello");
                return;
            }
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("World");
        });

        // Without interception.
        {
            requests = new List<string>();
            // .NET: the array is destructured by index.
            var texts = await Page.EvaluateAsync<string[]>(@"async url => {
      const response1 = await fetch(url, { method: 'OPTIONS' });
      const text1 = await response1.text();
      const response2 = await fetch(url, { method: 'GET' });
      const text2 = await response2.text();
      return [text1, text2];
    }", Server.CrossProcessPrefix + "/something");
            var text1 = texts[0];
            var text2 = texts[1];
            Assert.That(text1, Is.EqualTo("Hello"));
            Assert.That(text2, Is.EqualTo("World"));
            // Preflight for OPTIONS, then OPTIONS, then GET without preflight.
            Assert.That(requests, Is.EqualTo(new[] { "OPTIONS:/something", "OPTIONS:/something", "GET:/something" }));
        }
    }

    // Make sure this runs in a new context as preflight results could be cached.
    [UpstreamTest("RO-039 should not auto-intercept non-preflight OPTIONS with network interception")]
    public async Task ShouldNotAutoInterceptNonPreflightOPTIONSWithNetworkInterception()
    {
        Info.Fixme(IsAndroid);

        await Page.GotoAsync(Server.EmptyPage);

        var requests = new List<string>();
        Server.SetRoute("/something", (request, response) =>
        {
            requests.Add(request.Method + ":" + request.Url);
            if (request.Method == "OPTIONS")
            {
                response.WriteHead(200, new()
                {
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Access-Control-Allow-Methods"] = "POST, GET, OPTIONS, DELETE",
                    ["Access-Control-Allow-Headers"] = "*",
                    ["Cache-Control"] = "no-cache",
                });
                response.End("Hello");
                return;
            }
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("World");
        });

        // With interception.
        {
            await Page.RouteAsync("**/something", route => route.ContinueAsync());

            requests = new List<string>();
            // .NET: the array is destructured by index.
            var texts = await Page.EvaluateAsync<string[]>(@"async url => {
      const response1 = await fetch(url, { method: 'OPTIONS' });
      const text1 = await response1.text();
      const response2 = await fetch(url, { method: 'GET' });
      const text2 = await response2.text();
      return [text1, text2];
    }", Server.CrossProcessPrefix + "/something");
            var text1 = texts[0];
            var text2 = texts[1];
            Assert.That(text1, Is.EqualTo("Hello"));
            Assert.That(text2, Is.EqualTo("World"));
            // Preflight for OPTIONS is auto-fulfilled, then OPTIONS, then GET without preflight.
            if (BrowserName == "chromium" || IsBidi)
            {
                Assert.That(requests, Is.EqualTo(new[] { "OPTIONS:/something", "GET:/something" }));
            }
            else
            {
                Assert.That(requests, Is.EqualTo(new[] { "OPTIONS:/something", "OPTIONS:/something", "GET:/something" }));
            }
        }
    }

    [UpstreamTest("RO-040 should support cors with POST")]
    public async Task ShouldSupportCorsWithPOST()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/cars", async route =>
        {
            await route.FulfillAsync(new()
            {
                ContentType = "application/json",
                Headers = new Dictionary<string, string> { ["Access-Control-Allow-Origin"] = "*" },
                Status = 200,
                Body = JsonSerializer.Serialize(new[] { "electric", "gas" }),
            });
        });
        var resp = await Page.EvaluateAsync<string[]>(@"async () => {
    const response = await fetch('https://example.com/cars', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      mode: 'cors',
      body: JSON.stringify({ 'number': 1 })
    });
    return response.json();
  }");
        Assert.That(resp, Is.EqualTo(new[] { "electric", "gas" }));
    }

    [UpstreamTest("RO-041 should support cors with credentials")]
    public async Task ShouldSupportCorsWithCredentials()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/cars", async route =>
        {
            await route.FulfillAsync(new()
            {
                ContentType = "application/json",
                Headers = new Dictionary<string, string>
                {
                    ["Access-Control-Allow-Origin"] = Server.Prefix,
                    ["Access-Control-Allow-Credentials"] = "true",
                },
                Status = 200,
                Body = JsonSerializer.Serialize(new[] { "electric", "gas" }),
            });
        });
        var resp = await Page.EvaluateAsync<string[]>(@"async () => {
    const response = await fetch('https://example.com/cars', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      mode: 'cors',
      body: JSON.stringify({ 'number': 1 }),
      credentials: 'include'
    });
    return response.json();
  }");
        Assert.That(resp, Is.EqualTo(new[] { "electric", "gas" }));
    }

    [UpstreamTest("RO-042 should not abort preflighted cross-origin redirect when a route is registered")]
    public async Task ShouldNotAbortPreflightedCrossOriginRedirectWhenARouteIsRegistered()
    {
        Info.Skip(IsAndroid, "No cross-process on Android");

        Func<ServerRequest, ServerResponse, bool> allowCors = (request, response) =>
        {
            response.SetHeader("Access-Control-Allow-Origin", request.Headers["origin"] ?? "*");
            response.SetHeader("Access-Control-Allow-Credentials", "true");
            response.SetHeader("Access-Control-Allow-Headers", "x-app-version");
            if (request.Method != "OPTIONS")
            {
                return false;
            }
            response.WriteHead(204);
            response.End();
            return true;
        };
        Server.SetRoute("/download", (request, response) =>
        {
            if (allowCors(request, response))
            {
                return;
            }
            response.WriteHead(302, new() { ["location"] = $"{Server.Prefix}/file" });
            response.End();
        });
        Server.SetRoute("/file", (request, response) =>
        {
            if (allowCors(request, response))
            {
                return;
            }
            response.End("file contents");
        });

        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/never-matches", route => route.ContinueAsync());
        var result = await Page.EvaluateAsync<string>(@"async url => {
    const response = await fetch(url, { credentials: 'include', headers: { 'X-App-Version': '1.2.3' } });
    return `${response.status} ${await response.text()}`;
  }", Server.CrossProcessPrefix + "/download");
        Assert.That(result, Is.EqualTo("200 file contents"));
    }

    [UpstreamTest("RO-043 should reject cors with disallowed credentials")]
    public async Task ShouldRejectCorsWithDisallowedCredentials()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/cars", async route =>
        {
            await route.FulfillAsync(new()
            {
                ContentType = "application/json",
                Headers = new Dictionary<string, string>
                {
                    ["Access-Control-Allow-Origin"] = Server.Prefix,
                    // Should fail without this line below!
                    // 'Access-Control-Allow-Credentials': 'true'
                },
                Status = 200,
                Body = JsonSerializer.Serialize(new[] { "electric", "gas" }),
            });
        });
        Exception error = null;
        try
        {
            await Page.EvaluateAsync(@"async () => {
      const response = await fetch('https://example.com/cars', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        mode: 'cors',
        body: JSON.stringify({ 'number': 1 }),
        credentials: 'include'
      });
      return response.json();
    }");
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error, Is.Not.Null);
    }

    [UpstreamTest("RO-044 should support cors for different methods")]
    public async Task ShouldSupportCorsForDifferentMethods()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/cars", async route =>
        {
            await route.FulfillAsync(new()
            {
                ContentType = "application/json",
                Headers = new Dictionary<string, string> { ["Access-Control-Allow-Origin"] = "*" },
                Status = 200,
                Body = JsonSerializer.Serialize(new[] { route.Request.Method, "electric", "gas" }),
            });
        });
        // First POST
        {
            var resp = await Page.EvaluateAsync<string[]>(@"async () => {
      const response = await fetch('https://example.com/cars', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        mode: 'cors',
        body: JSON.stringify({ 'number': 1 })
      });
      return response.json();
    }");
            Assert.That(resp, Is.EqualTo(new[] { "POST", "electric", "gas" }));
        }
        // Then DELETE
        {
            var resp = await Page.EvaluateAsync<string[]>(@"async () => {
      const response = await fetch('https://example.com/cars', {
        method: 'DELETE',
        headers: {},
        mode: 'cors',
        body: ''
      });
      return response.json();
    }");
            Assert.That(resp, Is.EqualTo(new[] { "DELETE", "electric", "gas" }));
        }
    }

    [UpstreamTest("RO-045 should support the times parameter with route matching")]
    public async Task ShouldSupportTheTimesParameterWithRouteMatching()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Task<string> FetchBody()
        {
            return Page.EvaluateAsync<string>("() => fetch('/empty.html').then(response => response.text())");
        }

        var intercepted = new List<int>();
        await Page.RouteAsync("**/empty.html", route =>
        {
            intercepted.Add(1);
            IgnoreErrors(route.FulfillAsync(new() { Body = "first" }));
        }, new() { Times = 1 });
        Assert.That(await FetchBody(), Is.EqualTo("first"));
        await Page.RouteAsync("**/empty.html", route => route.FulfillAsync(new() { Body = "second" }), new() { Times = 1 });
        Assert.That(await FetchBody(), Is.EqualTo("second"));
        await FetchBody();
        await FetchBody();
        Assert.That(intercepted, Has.Count.EqualTo(1));
    }

    [UpstreamTest("RO-046 should work if handler with times parameter was removed from another handler")]
    public async Task ShouldWorkIfHandlerWithTimesParameterWasRemovedFromAnotherHandler()
    {
        var intercepted = new List<string>();
        Action<IRoute> handler = route =>
        {
            intercepted.Add("first");
            IgnoreErrors(route.ContinueAsync());
        };
        await Page.RouteAsync("**/*", handler, new() { Times = 1 });
        await Page.RouteAsync("**/*", async route =>
        {
            intercepted.Add("second");
            await Page.UnrouteAsync("**/*", handler);
            await route.FallbackAsync();
        });
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { "second" }));
        intercepted.Clear();
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(intercepted, Is.EqualTo(new[] { "second" }));
    }

    [UpstreamTest("RO-047 should support async handler w/ times")]
    public async Task ShouldSupportAsyncHandlerWTimes()
    {
        await Page.RouteAsync("**/empty.html", async route =>
        {
            await Task.Delay(100);
            await route.FulfillAsync(new()
            {
                Body = "<html>intercepted</html>",
                ContentType = "text/html",
            });
        }, new() { Times = 1 });
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.Locator("body")).ToHaveTextAsync("intercepted");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.Locator("body")).Not.ToHaveTextAsync("intercepted");
    }

    [UpstreamTest("RO-048 route abort with times: 1 should not affect second sequential fetch")]
    public async Task RouteAbortWithTimes1ShouldNotAffectSecondSequentialFetch()
    {
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 155, "Chromium drops a request that is intercepted while Fetch.disable is being processed; fix is not rolled yet");

        Server.SetRoute("/data", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/plain" });
            res.End("ok");
        });

        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/data", route => route.AbortAsync("timedout"), new() { Times = 1 });

        var results = await Page.EvaluateAsync<string[]>(@"async () => {
    async function fetchOrHung(url) {
      const timeout = new Promise(resolve => setTimeout(() => resolve('hung'), 3000));
      const request = fetch(url).then(r => String(r.status)).catch(() => 'aborted');
      return Promise.race([request, timeout]);
    }
    const first = await fetchOrHung('/data');
    const second = await fetchOrHung('/data');
    return [first, second];
  }");

        Assert.That(results, Is.EqualTo(new[] { "aborted", "200" }));
    }

    [UpstreamTest("RO-049 should contain raw request header")]
    public async Task ShouldContainRawRequestHeader()
    {
        Dictionary<string, string> headers = null;
        await Page.RouteAsync("**/*", async route =>
        {
            headers = await route.Request.AllHeadersAsync();
            IgnoreErrors(route.ContinueAsync());
        });
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        Assert.That(headers["accept"], Is.Not.Empty);
    }

    [UpstreamTest("RO-050 should contain sec-fetch headers in route")]
    public async Task ShouldContainSecFetchHeadersInRoute()
    {
        Info.Fixme(BrowserName == "chromium", "Fetch.requestPaused fires before Chromium attaches Fetch Metadata headers");

        Func<IEnumerable<KeyValuePair<string, string>>, Dictionary<string, string>> secFetch = headers => headers
            .Where(entry => entry.Key.ToLowerInvariant().StartsWith("sec-fetch-", StringComparison.Ordinal))
            .ToDictionary(entry => entry.Key, entry => entry.Value);
        var modes = new[] { "cors", "no-cors", "same-origin" };

        await Page.GotoAsync(Server.EmptyPage);

        var routed = new Dictionary<string, (Dictionary<string, string> headers, Dictionary<string, string> allHeaders)>();
        await Page.RouteAsync("**/probe-*", async route =>
        {
            var request = route.Request;
            var path = new Uri(request.Url).AbsolutePath;
            routed[path] = (headers: secFetch(request.Headers), allHeaders: secFetch(await request.AllHeadersAsync()));
            IgnoreErrors(route.ContinueAsync());
        });

        var (serverRequests, _) = await PromiseAll(
            PromiseAll(modes.Select(mode => Server.WaitForRequest($"/probe-{mode}"))),
            Page.EvaluateAsync(@"async modes => {
      for (const mode of modes)
        await (await fetch(`/probe-${mode}`, { mode: mode })).text();
    }", modes));

        for (var i = 0; i < modes.Length; i++)
        {
            var path = $"/probe-{modes[i]}";
            var onServer = secFetch(serverRequests[i].Headers);
            // The server does receive the browser-computed Fetch Metadata.
            Assert.That(onServer, Is.EqualTo(new Dictionary<string, string>
            {
                ["sec-fetch-site"] = "same-origin",
                ["sec-fetch-mode"] = modes[i],
                ["sec-fetch-dest"] = "empty",
            }), $"{path} on server");
            // The route handler should see the same headers.
            Assert.That(routed[path].headers, Is.EqualTo(onServer), $"{path} route.request().headers()");
            Assert.That(routed[path].allHeaders, Is.EqualTo(onServer), $"{path} route.request().allHeaders()");
        }
    }

    [UpstreamTest("RO-051 should contain raw response header")]
    public async Task ShouldContainRawResponseHeader()
    {
        IRequest request = null;
        await Page.RouteAsync("**/*", route =>
        {
            request = route.Request;
            IgnoreErrors(route.ContinueAsync());
        });
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        var response = await request.ResponseAsync();
        var headers = await response.AllHeadersAsync();
        Assert.That(headers["content-type"], Is.Not.Empty);
    }

    [UpstreamTest("RO-052 should contain raw response header after fulfill")]
    public async Task ShouldContainRawResponseHeaderAfterFulfill()
    {
        IRequest request = null;
        await Page.RouteAsync("**/*", async route =>
        {
            request = route.Request;
            await route.FulfillAsync(new()
            {
                Status = 200,
                Body = "Hello",
                ContentType = "text/html",
            });
        });
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        var response = await request.ResponseAsync();
        var headers = await response.AllHeadersAsync();
        Assert.That(headers["content-type"], Is.Not.Empty);
    }

    [UpstreamTest("RO-053 route.${method} should throw if called twice")]
    [TestCase("fulfill")]
    [TestCase("continue")]
    [TestCase("fallback")]
    [TestCase("abort")]
    public async Task RouteShouldThrowIfCalledTwice(string method)
    {
        var resolvePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync("**/*", route => resolvePromise.TrySetResult(route));
        IgnoreErrors(Page.GotoAsync(Server.Prefix + "/empty.html"));
        var route = await resolvePromise.Task;
        // .NET: route[method]()
        Task RouteMethod() => method switch
        {
            "fulfill" => route.FulfillAsync(),
            "continue" => route.ContinueAsync(),
            "fallback" => route.FallbackAsync(),
            _ => route.AbortAsync(),
        };
        await RouteMethod();
        var e = await Catch(() => RouteMethod());
        Assert.That(e.Message, Does.Contain("Route is already handled!"));
    }

    [UpstreamTest("RO-054 should intercept when postData is more than 1MB")]
    public async Task ShouldInterceptWhenPostDataIsMoreThan1MB()
    {
        Info.Fixme(IsAndroid);

        await Page.GotoAsync(Server.EmptyPage);
        var interceptionPromise = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var POST_BODY = new string('0', 2 * 1024 * 1024); // 2MB
        await Page.RouteAsync("**/404.html", async route =>
        {
            await route.AbortAsync();
            interceptionPromise.TrySetResult(route.Request.PostData);
        });
        await Page.EvaluateAsync(@"POST_BODY => fetch('/404.html', {
    method: 'POST',
    body: POST_BODY,
  }).catch(e => {})", POST_BODY);
        Assert.That(await interceptionPromise.Task, Is.EqualTo(POST_BODY));
    }

    [UpstreamTest("RO-055 should be able to intercept every navigation to a page controlled by service worker")]
    public async Task ShouldBeAbleToInterceptEveryNavigationToAPageControlledByServiceWorker()
    {
        Info.Skip(IsElectron);

        var interceptions = 0;
        var URL = Server.Prefix + "/serviceworkers/bug-33561/index.html";
        await Page.RouteAsync(URL, async route =>
        {
            ++interceptions;
            await route.ContinueAsync();
        });

        await Page.GotoAsync(URL);
        await Page.EvaluateAsync("() => window['activationPromise']");
        await Page.GotoAsync(URL);
        Assert.That(interceptions, Is.EqualTo(2));
    }

    [UpstreamTest("RO-202 should continue a request reported while interception was disabled")]
    public async Task ShouldContinueARequestReportedWhileInterceptionWasDisabled()
    {
        Info.Skip(IsElectron);

        await Page.GotoAsync(Server.Prefix + "/serviceworkers/slow-fallback/sw.html");
        await Page.EvaluateAsync("() => window['activationPromise']");

        Func<IRoute, Task> handler = route => route.FulfillAsync(new() { Body = "mocked" });
        await Page.RouteAsync("**/never-requested", handler);
        await Page.UnrouteAsync("**/never-requested");
        // The service worker holds the request, so it reaches the network
        // after interception is enabled again.
        var requestPromise = Page.WaitForRequestAsync("**/simple.json");
        var bodyPromise = Page.EvaluateAsync<string>("() => fetch('/simple.json').then(response => response.text())");
        await requestPromise;
        await Page.RouteAsync("**/never-requested", handler);
        Assert.That(await bodyPromise, Is.EqualTo("{\"foo\": \"bar\"}\n"));
    }
}
