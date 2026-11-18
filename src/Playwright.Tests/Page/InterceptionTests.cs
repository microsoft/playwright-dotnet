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
public class InterceptionTests : UpstreamPageTest
{
    [UpstreamTest("RO-151 should work with navigation @smoke")]
    public async Task ShouldWorkWithNavigation()
    {
        var requests = new Dictionary<string, IRequest>();
        await Page.RouteAsync("**/*", route =>
        {
            requests[route.Request.Url.Split('/').Last()] = route.Request;
            IgnoreErrors(route.ContinueAsync());
        });
        Server.SetRedirect("/rrredirect", "/frames/one-frame.html");
        await Page.GotoAsync(Server.Prefix + "/rrredirect");
        Assert.That(requests["rrredirect"].IsNavigationRequest, Is.EqualTo(true));
        Assert.That(requests["frame.html"].IsNavigationRequest, Is.EqualTo(true));
        Assert.That(requests["script.js"].IsNavigationRequest, Is.EqualTo(false));
        Assert.That(requests["style.css"].IsNavigationRequest, Is.EqualTo(false));
    }

    [UpstreamTest("RO-152 should intercept after a service worker")]
    public async Task ShouldInterceptAfterAServiceWorker()
    {
        Info.Skip(IsAndroid);

        await Page.GotoAsync(Server.Prefix + "/serviceworkers/fetchdummy/sw.html");
        await Page.EvaluateAsync("() => window['activationPromise']");

        // Sanity check.
        var swResponse = await Page.EvaluateAsync<string>("() => window['fetchDummy']('foo')");
        Assert.That(swResponse, Is.EqualTo("responseFromServiceWorker:foo"));

        await Page.RouteAsync("**/foo", route =>
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
        var swResponse2 = await Page.EvaluateAsync<string>("() => window['fetchDummy']('foo')");
        Assert.That(swResponse2, Is.EqualTo("responseFromServiceWorker:foo"));

        // Frozen WebKit predates the header preservation fix.
        if (!IsFrozenWebkit)
        {
            var headers = new Dictionary<string, string>
            {
                ["cache-control"] = "max-age=12345",
                ["accept-language"] = "fr-CH",
                ["accept"] = "application/custom",
            };
            var receivedHeaders = await Page.EvaluateAsync<object>(@"async headers => {
      const response = await fetch('headers', { headers });
      return response.json();
    }", headers);
            // .NET: expect(receivedHeaders).toMatchObject(headers)
            ExpectToEqual(receivedHeaders, ObjectContaining(headers));
        }

        // Page route is not applied to service worker initiated fetch.
        var nonInterceptedResponse = await Page.EvaluateAsync<string>("() => window['fetchDummy']('passthrough')");
        Assert.That(nonInterceptedResponse, Is.EqualTo("FAILURE: Not Found"));

        // Firefox/Juggler does not want to fetch the redirect for some reason.
        if (BrowserName != "firefox" || IsBidi)
        {
            // Page route is not applied to service worker initiated fetch with redirect.
            Server.SetRedirect("/serviceworkers/fetchdummy/passthrough", "/simple.json");
            var redirectedResponse = await Page.EvaluateAsync<string>("() => window['fetchDummy']('passthrough')");
            Assert.That(redirectedResponse, Is.EqualTo("{\"foo\": \"bar\"}\n"));
        }
    }

    [UpstreamTest("RO-155 should throw on page.route with invalid glob")]
    [LocalFixme("dotnet: page.RouteAsync(\"http://*/foo{\", handler) does not throw, the glob is not checked; upstream throws \"unmatched '{'\"")]
    public async Task ShouldThrowOnPageRouteWithInvalidGlob()
    {
        var error = await ThrowsAsync<PlaywrightException>(() => Page.RouteAsync("http://*/foo{", route => route.ContinueAsync()));
        Assert.That(error.Message, Does.Contain("unmatched '{'"));
    }

    [UpstreamTest("RO-156 should intercept by glob")]
    public async Task ShouldInterceptByGlob()
    {
        Info.Skip(IsAndroid);

        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("http://localhos**?*oo", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                Body = "intercepted",
            });
        });
        var result = await Page.EvaluateAsync<string>("url => fetch(url).then(r => r.text())", Server.Prefix + "/?foo");
        Assert.That(result, Is.EqualTo("intercepted"));
    }

    [UpstreamTest("RO-157 should intercept network activity from worker")]
    public async Task ShouldInterceptNetworkActivityFromWorker()
    {
        Info.Skip(IsAndroid);

        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/data_for_worker", (req, res) => res.End("failed to intercept"));
        var url = Server.Prefix + "/data_for_worker";
        await Page.RouteAsync(url, route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                Body = "intercepted",
            }));
        });
        var (msg, _) = await PromiseAll(
            Page.WaitForConsoleMessageAsync(),
            Page.EvaluateAsync(@"url => new Worker(URL.createObjectURL(new Blob([`
      fetch(""${url}"").then(response => response.text()).then(console.log);
    `], { type: 'application/javascript' })))", url));
        Assert.That(msg.Text, Is.EqualTo("intercepted"));
    }

    [UpstreamTest("RO-158 should intercept worker requests when enabled after worker creation")]
    public async Task ShouldInterceptWorkerRequestsWhenEnabledAfterWorkerCreation()
    {
        Info.Skip(IsAndroid);
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 130, "fixed in Chromium 130");

        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/data_for_worker", (req, res) => res.End("failed to intercept"));
        var url = Server.Prefix + "/data_for_worker";
        await PromiseAll(
            Page.WaitForWorkerAsync(),
            Page.EvaluateAsync(@"url => {
      window.w = new Worker(URL.createObjectURL(new Blob([`
        onmessage = function(e) {
          fetch(""${url}"").then(response => response.text()).then(console.log);
        };
      `], { type: 'application/javascript' })));
    }", url));
        // Install the route **after** the worker has been created.
        await Page.RouteAsync(url, route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                Body = "intercepted",
            }));
        });
        var (msg, _) = await PromiseAll(
            Page.WaitForConsoleMessageAsync(),
            Page.EvaluateAsync("() => window.w.postMessage('')"));
        Assert.That(msg.Text, Is.EqualTo("intercepted"));
    }

    [UpstreamTest("RO-159 should intercept network activity from worker 2")]
    public async Task ShouldInterceptNetworkActivityFromWorker2()
    {
        Info.Skip(IsAndroid);

        var url = Server.Prefix + "/worker/worker.js";
        await Page.RouteAsync(url, route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                Body = "console.log(\"intercepted\");",
                ContentType = "application/javascript",
            }));
        });
        var (msg, _) = await PromiseAll(
            Page.WaitForConsoleMessageAsync(),
            Page.GotoAsync(Server.Prefix + "/worker/worker.html"));
        Assert.That(msg.Text, Is.EqualTo("intercepted"));
    }

    [UpstreamTest("RO-162 should not break remote worker importScripts")]
    public async Task ShouldNotBreakRemoteWorkerImportScripts()
    {
        await Page.RouteAsync("**", async route =>
        {
            await route.ContinueAsync();
        });
        await Page.GotoAsync(Server.Prefix + "/worker/worker-http-import.html");
        await Page.WaitForSelectorAsync("#status:has-text('finished')");
    }

    [UpstreamTest("RO-163 should disable memory cache when intercepting")]
    public async Task ShouldDisableMemoryCacheWhenIntercepting()
    {
        var interceted = 0;
        await Page.RouteAsync("**/page.html", route =>
        {
            ++interceted;
            IgnoreErrors(route.FulfillAsync(new()
            {
                Body = "success",
            }));
        });
        await Page.GotoAsync(Server.Prefix + "/page.html");
        Assert.That(await Page.Locator("body").TextContentAsync(), Does.Contain("success"));
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page).ToHaveURLAsync(Server.EmptyPage);
        Assert.That(interceted, Is.EqualTo(1));
        await Page.GoBackAsync();
        await Expect(Page).ToHaveURLAsync(Server.Prefix + "/page.html");
        Assert.That(interceted, Is.EqualTo(2));
    }

    [UpstreamTest("RO-164 should intercept blob url requests")]
    public async Task ShouldInterceptBlobUrlRequests()
    {
        Info.Skip(BrowserName != "webkit");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                Body = "intercepted",
            }));
        });
        Page.Console += (_, msg) =>
        {
            // console.log(msg.text())
        };
        var response = await Page.EvaluateAsync<string>(@"async () => {
    const blobUrl = URL.createObjectURL(new Blob(['failed to intercept'], { type: 'text/plain' }));
    return await fetch(blobUrl).then(response => response.text());
  }");
        Assert.That(response, Is.EqualTo("intercepted"));
    }
}
