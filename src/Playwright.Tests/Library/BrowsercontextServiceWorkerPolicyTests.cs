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
public class BrowsercontextServiceWorkerPolicyTests : UpstreamPageTest
{
    [UpstreamTest("WO-027 should allow service workers by default")]
    public async Task ShouldAllowServiceWorkersByDefault()
    {
        await Page.GotoAsync(Server.Prefix + "/serviceworkers/empty/sw.html");
        Assert.That(await Page.EvaluateAsync<object>("() => window['registrationPromise']"), Is.Not.Null);
    }

    [UpstreamTest("WO-028 should allow ${mode} service worker navigation while offline")]
    [TestCase("literal", "new Response('<h1>offline</h1>', { headers: { 'Content-Type': 'text/html' } })")]
    [TestCase("cache-only", "caches.match('/offline.html')")]
    [TestCase("network-fallback", "fetch(event.request).catch(() => caches.match('/offline.html'))")]
    [TestCase("preload-fallback", @"(async () => {
    try {
      return await event.preloadResponse || await fetch(event.request);
    } catch {
      return caches.match('/offline.html');
    }
  })()")]
    public async Task ShouldAllowServiceWorkerNavigationWhileOffline(string mode, string workerResponse)
    {
        Info.Skip(IsElectron);
        Info.Skip(IsFrozenWebkit, "Requires offline emulation in the network process.");
        Info.Fixme(BrowserName == "firefox" && (mode == "network-fallback" || mode == "preload-fallback"), "Firefox does not apply offline emulation to service worker requests.");

        var networkRequests = 0;
        Server.SetRoute("/target.html", (request, response) =>
        {
            ++networkRequests;
            response.SetHeader("Cache-Control", "no-store");
            response.End("online");
        });
        Server.SetRoute("/offline.html", (request, response) =>
        {
            response.SetHeader("Content-Type", "text/html");
            response.End("<h1>offline</h1>");
        });
        Server.SetRoute("/sw.js", (request, response) =>
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

        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"async () => {
      await navigator.serviceWorker.register('/sw.js');
      await navigator.serviceWorker.ready;
    }");
        await Page.WaitForFunctionAsync("() => !!navigator.serviceWorker.controller");
        await Context.SetOfflineAsync(true);

        var response = await Page.GotoAsync(Server.Prefix + "/target.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.FromServiceWorker, Is.EqualTo(true));
        await Expect(Page.Locator("h1")).ToHaveTextAsync("offline");
        Assert.That(await Page.EvaluateAsync<bool>("() => navigator.onLine"), Is.EqualTo(false));
        Assert.That(networkRequests, Is.EqualTo(0));

        var newPage = await Context.NewPageAsync();
        var newResponse = await newPage.GotoAsync(Server.Prefix + "/target.html");
        Assert.That(newResponse.Status, Is.EqualTo(200));
        Assert.That(newResponse.FromServiceWorker, Is.EqualTo(true));
        await Expect(newPage.Locator("h1")).ToHaveTextAsync("offline");
        Assert.That(await newPage.EvaluateAsync<bool>("() => navigator.onLine"), Is.EqualTo(false));
        Assert.That(networkRequests, Is.EqualTo(0));

        await Context.SetOfflineAsync(false);
        Assert.That(await Page.EvaluateAsync<string>("() => fetch('/target.html').then(response => response.text())"), Is.EqualTo("online"));
        Assert.That(networkRequests, Is.EqualTo(1));
    }

    public class Block : UpstreamPageTest
    {
        // it.use({ serviceWorkers: 'block' });
        public override BrowserNewContextOptions ContextOptions() => new(base.ContextOptions()) { ServiceWorkers = ServiceWorkerPolicy.Block };

        [UpstreamTest("WO-029 blocks service worker registration")]
        public async Task BlocksServiceWorkerRegistration()
        {
            await PromiseAll(
                Page.WaitForConsoleMessageAsync(new() { Predicate = evt => evt.Text == "Service Worker registration blocked by Playwright" }),
                Page.GotoAsync(Server.Prefix + "/serviceworkers/empty/sw.html"));
        }

        [UpstreamTest("WO-030 should not throw error on about:blank")]
        public async Task ShouldNotThrowErrorOnAboutBlank()
        {
            var errors = new List<string>();
            Page.PageError += (_, error) => errors.Add(error);
            await Page.GotoAsync("about:blank");
            Assert.That(errors, Is.EqualTo(Array.Empty<string>()));
        }

        [UpstreamTest("WO-031 should not throw error in opaque sandboxed iframe")]
        public async Task ShouldNotThrowErrorInOpaqueSandboxedIframe()
        {
            var errors = new List<string>();
            Page.PageError += (_, error) => errors.Add(error);
            await Page.GotoAsync(Server.EmptyPage);
            await Page.SetContentAsync("<iframe sandbox=\"allow-scripts\" srcdoc=\"<p>Preview</p>\"></iframe>");
            await Expect(Page.FrameLocator("iframe").Locator("p")).ToHaveTextAsync("Preview");
            Assert.That(errors, Is.EqualTo(Array.Empty<string>()));
        }
    }
}
