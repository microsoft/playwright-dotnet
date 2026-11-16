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
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/interception.spec.ts.</summary>
public class InterceptionTests
{
    [UpstreamTest("RO-151 should work with navigation @smoke")]
    public async Task ShouldWorkWithNavigation(IPage page, Server server)
    {
        var requests = new Dictionary<string, IRequest>();
        await page.RouteAsync("**/*", route =>
        {
            requests[route.Request.Url.Split('/').Last()] = route.Request;
            IgnoreErrors(route.ContinueAsync());
        });
        server.SetRedirect("/rrredirect", "/frames/one-frame.html");
        await page.GotoAsync(server.Prefix + "/rrredirect");
        Assert.That(requests["rrredirect"].IsNavigationRequest, Is.EqualTo(true));
        Assert.That(requests["frame.html"].IsNavigationRequest, Is.EqualTo(true));
        Assert.That(requests["script.js"].IsNavigationRequest, Is.EqualTo(false));
        Assert.That(requests["style.css"].IsNavigationRequest, Is.EqualTo(false));
    }

    [UpstreamTest("RO-152 should intercept after a service worker")]
    public async Task ShouldInterceptAfterAServiceWorker(IPage page, Server server, string browserName, bool isAndroid, bool isBidi, bool isFrozenWebkit)
    {
        It.Skip(isAndroid);

        await page.GotoAsync(server.Prefix + "/serviceworkers/fetchdummy/sw.html");
        await page.EvaluateAsync("() => window['activationPromise']");

        // Sanity check.
        var swResponse = await page.EvaluateAsync<string>("() => window['fetchDummy']('foo')");
        Assert.That(swResponse, Is.EqualTo("responseFromServiceWorker:foo"));

        await page.RouteAsync("**/foo", route =>
        {
            var slash = route.Request.Url.LastIndexOf('/');
            var name = route.Request.Url.Substring(slash + 1);
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/css",
                Body = "responseFromInterception:" + name,
            }));
        });

        // Page route is applied after service worker fetch event.
        var swResponse2 = await page.EvaluateAsync<string>("() => window['fetchDummy']('foo')");
        Assert.That(swResponse2, Is.EqualTo("responseFromServiceWorker:foo"));

        // Frozen WebKit predates the header preservation fix.
        if (!isFrozenWebkit)
        {
            var headers = new Dictionary<string, string>
            {
                ["cache-control"] = "max-age=12345",
                ["accept-language"] = "fr-CH",
                ["accept"] = "application/custom",
            };
            var receivedHeaders = await page.EvaluateAsync<object>(@"async headers => {
      const response = await fetch('headers', { headers });
      return response.json();
    }", headers);
            // .NET: expect(receivedHeaders).toMatchObject(headers)
            ExpectToEqual(receivedHeaders, ObjectContaining(headers));
        }

        // Page route is not applied to service worker initiated fetch.
        var nonInterceptedResponse = await page.EvaluateAsync<string>("() => window['fetchDummy']('passthrough')");
        Assert.That(nonInterceptedResponse, Is.EqualTo("FAILURE: Not Found"));

        // Firefox/Juggler does not want to fetch the redirect for some reason.
        if (browserName != "firefox" || isBidi)
        {
            // Page route is not applied to service worker initiated fetch with redirect.
            server.SetRedirect("/serviceworkers/fetchdummy/passthrough", "/simple.json");
            var redirectedResponse = await page.EvaluateAsync<string>("() => window['fetchDummy']('passthrough')");
            Assert.That(redirectedResponse, Is.EqualTo("{\"foo\": \"bar\"}\n"));
        }
    }

    [UpstreamTest("RO-155 should throw on page.route with invalid glob")]
    [Fixme("dotnet: page.RouteAsync(\"http://*/foo{\", handler) does not throw, the glob is not checked; upstream throws \"unmatched '{'\"")]
    public async Task ShouldThrowOnPageRouteWithInvalidGlob(IPage page)
    {
        var error = await ThrowsAsync<PlaywrightException>(() => page.RouteAsync("http://*/foo{", route => route.ContinueAsync()));
        Assert.That(error.Message, Does.Contain("unmatched '{'"));
    }

    [UpstreamTest("RO-156 should intercept by glob")]
    public async Task ShouldInterceptByGlob(IPage page, Server server, bool isAndroid)
    {
        It.Skip(isAndroid);

        await page.GotoAsync(server.EmptyPage);
        await page.RouteAsync("http://localhos**?*oo", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                Body = "intercepted",
            });
        });
        var result = await page.EvaluateAsync<string>("url => fetch(url).then(r => r.text())", server.Prefix + "/?foo");
        Assert.That(result, Is.EqualTo("intercepted"));
    }

    [UpstreamTest("RO-157 should intercept network activity from worker")]
    public async Task ShouldInterceptNetworkActivityFromWorker(IPage page, Server server, bool isAndroid)
    {
        It.Skip(isAndroid);

        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/data_for_worker", (req, res) => res.End("failed to intercept"));
        var url = server.Prefix + "/data_for_worker";
        await page.RouteAsync(url, route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                Body = "intercepted",
            }));
        });
        var (msg, _) = await PromiseAll(
            page.WaitForConsoleMessageAsync(),
            page.EvaluateAsync(@"url => new Worker(URL.createObjectURL(new Blob([`
      fetch(""${url}"").then(response => response.text()).then(console.log);
    `], { type: 'application/javascript' })))", url));
        Assert.That(msg.Text, Is.EqualTo("intercepted"));
    }

    [UpstreamTest("RO-158 should intercept worker requests when enabled after worker creation")]
    public async Task ShouldInterceptWorkerRequestsWhenEnabledAfterWorkerCreation(IPage page, Server server, bool isAndroid, string browserName, int browserMajorVersion)
    {
        It.Skip(isAndroid);
        It.Skip(browserName == "chromium" && browserMajorVersion < 130, "fixed in Chromium 130");

        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/data_for_worker", (req, res) => res.End("failed to intercept"));
        var url = server.Prefix + "/data_for_worker";
        await PromiseAll(
            page.WaitForWorkerAsync(),
            page.EvaluateAsync(@"url => {
      window.w = new Worker(URL.createObjectURL(new Blob([`
        onmessage = function(e) {
          fetch(""${url}"").then(response => response.text()).then(console.log);
        };
      `], { type: 'application/javascript' })));
    }", url));
        // Install the route **after** the worker has been created.
        await page.RouteAsync(url, route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                Body = "intercepted",
            }));
        });
        var (msg, _) = await PromiseAll(
            page.WaitForConsoleMessageAsync(),
            page.EvaluateAsync("() => window.w.postMessage('')"));
        Assert.That(msg.Text, Is.EqualTo("intercepted"));
    }

    [UpstreamTest("RO-159 should intercept network activity from worker 2")]
    public async Task ShouldInterceptNetworkActivityFromWorker2(IPage page, Server server, bool isAndroid)
    {
        It.Skip(isAndroid);

        var url = server.Prefix + "/worker/worker.js";
        await page.RouteAsync(url, route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                Body = "console.log(\"intercepted\");",
                ContentType = "application/javascript",
            }));
        });
        var (msg, _) = await PromiseAll(
            page.WaitForConsoleMessageAsync(),
            page.GotoAsync(server.Prefix + "/worker/worker.html"));
        Assert.That(msg.Text, Is.EqualTo("intercepted"));
    }

    [UpstreamTest("RO-162 should not break remote worker importScripts")]
    public async Task ShouldNotBreakRemoteWorkerImportScripts(IPage page, Server server)
    {
        await page.RouteAsync("**", async route =>
        {
            await route.ContinueAsync();
        });
        await page.GotoAsync(server.Prefix + "/worker/worker-http-import.html");
        await page.WaitForSelectorAsync("#status:has-text('finished')");
    }

    [UpstreamTest("RO-163 should disable memory cache when intercepting")]
    public async Task ShouldDisableMemoryCacheWhenIntercepting(IPage page, Server server)
    {
        var interceted = 0;
        await page.RouteAsync("**/page.html", route =>
        {
            ++interceted;
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = "success",
            }));
        });
        await page.GotoAsync(server.Prefix + "/page.html");
        Assert.That(await page.Locator("body").TextContentAsync(), Does.Contain("success"));
        await page.GotoAsync(server.EmptyPage);
        await Expect(page).ToHaveURLAsync(server.EmptyPage);
        Assert.That(interceted, Is.EqualTo(1));
        await page.GoBackAsync();
        await Expect(page).ToHaveURLAsync(server.Prefix + "/page.html");
        Assert.That(interceted, Is.EqualTo(2));
    }

    [UpstreamTest("RO-164 should intercept blob url requests")]
    public async Task ShouldInterceptBlobUrlRequests(IPage page, Server server, string browserName)
    {
        It.Skip(browserName != "webkit");
        await page.GotoAsync(server.EmptyPage);
        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                Body = "intercepted",
            }));
        });
        page.Console += (_, msg) =>
        {
            // console.log(msg.text())
        };
        var response = await page.EvaluateAsync<string>(@"async () => {
    const blobUrl = URL.createObjectURL(new Blob(['failed to intercept'], { type: 'text/plain' }));
    return await fetch(blobUrl).then(response => response.text());
  }");
        Assert.That(response, Is.EqualTo("intercepted"));
    }
}
