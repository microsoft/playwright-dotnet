/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-service-worker-policy.spec.ts.</summary>
public class BrowsercontextServiceWorkerPolicyTests
{
    [UpstreamTest("WO-027 should allow service workers by default")]
    public async Task ShouldAllowServiceWorkersByDefault(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/serviceworkers/empty/sw.html");
        Assert.That(await page.EvaluateAsync<object>("() => window['registrationPromise']"), Is.Not.Null);
    }

    [UpstreamTest("WO-028 should allow ${mode} service worker navigation while offline")]
    [Case("literal", "new Response('<h1>offline</h1>', { headers: { 'Content-Type': 'text/html' } })")]
    [Case("cache-only", "caches.match('/offline.html')")]
    [Case("network-fallback", "fetch(event.request).catch(() => caches.match('/offline.html'))")]
    [Case("preload-fallback", @"(async () => {
    try {
      return await event.preloadResponse || await fetch(event.request);
    } catch {
      return caches.match('/offline.html');
    }
  })()")]
    public async Task ShouldAllowServiceWorkerNavigationWhileOffline(string mode, string workerResponse, IBrowserContext context, IPage page, Server server, string browserName, bool isElectron, bool isFrozenWebkit)
    {
        Info.Skip(isElectron);
        Info.Skip(isFrozenWebkit, "Requires offline emulation in the network process.");
        Info.Fixme(browserName == "firefox" && (mode == "network-fallback" || mode == "preload-fallback"), "Firefox does not apply offline emulation to service worker requests.");

        var networkRequests = 0;
        server.SetRoute("/target.html", (request, response) =>
        {
            ++networkRequests;
            response.SetHeader("Cache-Control", "no-store");
            response.End("online");
        });
        server.SetRoute("/offline.html", (request, response) =>
        {
            response.SetHeader("Content-Type", "text/html");
            response.End("<h1>offline</h1>");
        });
        server.SetRoute("/sw.js", (request, response) =>
        {
            response.SetHeader("Content-Type", "application/javascript");
            response.End($@"
        self.addEventListener('install', event => event.waitUntil((async () => {{
          const cache = await caches.open('offline');
          await cache.add('/offline.html');
          await self.skipWaiting();
        }})()));
        self.addEventListener('activate', event => event.waitUntil((async () => {{
          {(mode == "preload-fallback" ? "await self.registration.navigationPreload.enable();" : "")}
          await self.clients.claim();
        }})()));
        self.addEventListener('fetch', event => {{
          if (event.request.mode === 'navigate')
            event.respondWith({workerResponse});
        }});
      ");
        });

        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"async () => {
      await navigator.serviceWorker.register('/sw.js');
      await navigator.serviceWorker.ready;
    }");
        await page.WaitForFunctionAsync("() => !!navigator.serviceWorker.controller");
        await context.SetOfflineAsync(true);

        var response = await page.GotoAsync(server.Prefix + "/target.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.FromServiceWorker, Is.EqualTo(true));
        await Expect(page.Locator("h1")).ToHaveTextAsync("offline");
        Assert.That(await page.EvaluateAsync<bool>("() => navigator.onLine"), Is.EqualTo(false));
        Assert.That(networkRequests, Is.EqualTo(0));

        var newPage = await context.NewPageAsync();
        var newResponse = await newPage.GotoAsync(server.Prefix + "/target.html");
        Assert.That(newResponse.Status, Is.EqualTo(200));
        Assert.That(newResponse.FromServiceWorker, Is.EqualTo(true));
        await Expect(newPage.Locator("h1")).ToHaveTextAsync("offline");
        Assert.That(await newPage.EvaluateAsync<bool>("() => navigator.onLine"), Is.EqualTo(false));
        Assert.That(networkRequests, Is.EqualTo(0));

        await context.SetOfflineAsync(false);
        Assert.That(await page.EvaluateAsync<string>("() => fetch('/target.html').then(response => response.text())"), Is.EqualTo("online"));
        Assert.That(networkRequests, Is.EqualTo(1));
    }

    public class Block
    {
        // it.use({ serviceWorkers: 'block' });
        [Fixture]
        public static BrowserNewContextOptions ContextOptions(BrowserNewContextOptions contextOptions) => new(contextOptions) { ServiceWorkers = ServiceWorkerPolicy.Block };

        [UpstreamTest("WO-029 blocks service worker registration")]
        public async Task BlocksServiceWorkerRegistration(IPage page, Server server)
        {
            await PromiseAll(
                page.WaitForConsoleMessageAsync(new() { Predicate = evt => evt.Text == "Service Worker registration blocked by Playwright" }),
                page.GotoAsync(server.Prefix + "/serviceworkers/empty/sw.html"));
        }

        [UpstreamTest("WO-030 should not throw error on about:blank")]
        public async Task ShouldNotThrowErrorOnAboutBlank(IPage page)
        {
            var errors = new List<string>();
            page.PageError += (_, error) => errors.Add(error);
            await page.GotoAsync("about:blank");
            Assert.That(errors, Is.EqualTo(Array.Empty<string>()));
        }

        [UpstreamTest("WO-031 should not throw error in opaque sandboxed iframe")]
        public async Task ShouldNotThrowErrorInOpaqueSandboxedIframe(IPage page, Server server)
        {
            var errors = new List<string>();
            page.PageError += (_, error) => errors.Add(error);
            await page.GotoAsync(server.EmptyPage);
            await page.SetContentAsync("<iframe sandbox=\"allow-scripts\" srcdoc=\"<p>Preview</p>\"></iframe>");
            await Expect(page.FrameLocator("iframe").Locator("p")).ToHaveTextAsync("Preview");
            Assert.That(errors, Is.EqualTo(Array.Empty<string>()));
        }
    }
}
