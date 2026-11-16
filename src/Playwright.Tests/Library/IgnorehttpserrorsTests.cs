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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/ignorehttpserrors.spec.ts.</summary>
public class IgnorehttpserrorsTests
{
    [UpstreamTest("NE-136 should work @smoke")]
    public async Task ShouldWork(IBrowser browser, Server httpsServer, BrowserNewContextOptions contextOptions)
    {
        Exception error = null;
        var context = await browser.NewContextAsync(new(contextOptions) { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        IResponse response = null;
        error = await Catch(async () => response = await page.GotoAsync(httpsServer.EmptyPage));
        Assert.That(error, Is.Null);
        Assert.That(response.Ok, Is.EqualTo(true));
        await context.CloseAsync();
    }

    [UpstreamTest("NE-137 should isolate contexts")]
    public async Task ShouldIsolateContexts(IBrowser browser, Server httpsServer, BrowserNewContextOptions contextOptions)
    {
        {
            Exception error = null;
            var context = await browser.NewContextAsync(new(contextOptions) { IgnoreHTTPSErrors = true });
            var page = await context.NewPageAsync();
            IResponse response = null;
            error = await Catch(async () => response = await page.GotoAsync(httpsServer.EmptyPage));
            Assert.That(error, Is.Null);
            Assert.That(response.Ok, Is.EqualTo(true));
            await context.CloseAsync();
        }
        {
            Exception error = null;
            var context = await browser.NewContextAsync(contextOptions);
            var page = await context.NewPageAsync();
            error = await Catch(() => page.GotoAsync(httpsServer.EmptyPage));
            Assert.That(error, Is.Not.Null);
            await context.CloseAsync();
        }
    }

    [UpstreamTest("NE-138 should isolated contexts that share network process")]
    public async Task ShouldIsolatedContextsThatShareNetworkProcess(IBrowser browser, Server httpsServer, string browserName, bool isLinux, BrowserNewContextOptions contextOptions)
    {
        It.Fixme(browserName == "webkit" && isLinux, "See https://bugs.webkit.org/show_bug.cgi?id=293148");
        {
            var context = await browser.NewContextAsync(new(contextOptions) { IgnoreHTTPSErrors = true });
            var page = await context.NewPageAsync();
            var response = await page.GotoAsync(httpsServer.EmptyPage);
            Assert.That(response.Ok, Is.EqualTo(true));
            // Closing the context will remove WebsiteDataStore and stop the network process
            // which will make the test pass.
            // await context.close();
        }
        {
            var context = await browser.NewContextAsync(contextOptions);
            var page = await context.NewPageAsync();
            Exception error = null;
            error = await Catch(() => page.GotoAsync(httpsServer.EmptyPage));
            Assert.That(error, Is.Not.Null, "A TLS error expected, but the request succeeded.");
            await context.CloseAsync();
        }
    }

    [UpstreamTest("NE-139 should work with mixed content")]
    public async Task ShouldWorkWithMixedContent(IBrowser browser, Server server, Server httpsServer, BrowserNewContextOptions contextOptions)
    {
        httpsServer.SetRoute("/mixedcontent.html", (req, res) =>
        {
            res.End($"<iframe src={server.EmptyPage}></iframe>");
        });
        var context = await browser.NewContextAsync(new(contextOptions) { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(httpsServer.Prefix + "/mixedcontent.html", new() { WaitUntil = WaitUntilState.Load });
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        // Make sure blocked iframe has functional execution context
        // @see https://github.com/GoogleChrome/puppeteer/issues/2709
        Assert.That(await page.Frames[0].EvaluateAsync<int>("1 + 2"), Is.EqualTo(3));
        Assert.That(await page.Frames[1].EvaluateAsync<int>("2 + 3"), Is.EqualTo(5));
        await context.CloseAsync();
    }

    [UpstreamTest("NE-140 should work with WebSocket")]
    public async Task ShouldWorkWithWebSocket(IBrowser browser, Server httpsServer, BrowserNewContextOptions contextOptions)
    {
        httpsServer.SendOnWebSocketConnection("incoming");
        var context = await browser.NewContextAsync(new(contextOptions) { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        var value = await page.EvaluateAsync<string>(@"endpoint => {
    let cb;
    const result = new Promise(f => cb = f);
    const ws = new WebSocket(endpoint);
    ws.addEventListener('message', data => { ws.close(); cb(data.data); });
    ws.addEventListener('error', error => cb('Error'));
    return result;
  }", new Regex("https").Replace(httpsServer.Prefix, "wss", 1) + "/ws");
        Assert.That(value, Is.EqualTo("incoming"));
        await context.CloseAsync();
    }

    [UpstreamTest("NE-141 should fail with WebSocket if not ignored")]
    public async Task ShouldFailWithWebSocketIfNotIgnored(IBrowser browser, Server httpsServer, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        var value = await page.EvaluateAsync<string>(@"endpoint => {
    let cb;
    const result = new Promise(f => cb = f);
    const ws = new WebSocket(endpoint);
    ws.addEventListener('message', data => { ws.close(); cb(data.data); });
    ws.addEventListener('error', error => cb('Error'));
    return result;
  }", new Regex("https").Replace(httpsServer.Prefix, "wss", 1) + "/ws");
        Assert.That(value, Is.EqualTo("Error"));
        await context.CloseAsync();
    }

    [UpstreamTest("NE-142 serviceWorker should intercept document request")]
    public async Task ServiceWorkerShouldInterceptDocumentRequest(IBrowser browser, Server httpsServer, string browserName, BrowserNewContextOptions contextOptions)
    {
        It.Fixme(browserName == "chromium", "Failed to register a ServiceWorker: An SSL certificate error occurred when fetching the script.");
        var context = await browser.NewContextAsync(new(contextOptions) { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        await context.RouteAsync("**/*", route => route.ContinueAsync());
        httpsServer.SetRoute("/sw.js", (req, res) =>
        {
            res.SetHeader("Content-Type", "application/javascript");
            res.End(@"
      self.addEventListener('fetch', event => {
        event.respondWith(new Response('intercepted'));
      });
      self.addEventListener('activate', event => {
        event.waitUntil(clients.claim());
      });
    ");
        });
        await page.GotoAsync(httpsServer.EmptyPage);
        await page.EvaluateAsync(@"async () => {
    const waitForControllerChange = new Promise(resolve => navigator.serviceWorker.oncontrollerchange = resolve);
    await navigator.serviceWorker.register('/sw.js');
    await waitForControllerChange;
  }");
        await page.ReloadAsync();
        Assert.That(await page.TextContentAsync("body"), Is.EqualTo("intercepted"));
        await context.CloseAsync();
    }
}
