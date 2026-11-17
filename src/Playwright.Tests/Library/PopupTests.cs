/**
 * Copyright (c) Microsoft Corporation.
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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/popup.spec.ts.</summary>
public class PopupTests
{
    [UpstreamTest("PO-014 should inherit user agent from browser context @smoke")]
    public async Task ShouldInheritUserAgentFromBrowserContext(IBrowser browser, Server server, bool isFrozenWebkit, BrowserNewContextOptions contextOptions)
    {
        Info.Skip(isFrozenWebkit);

        var context = await browser.NewContextAsync(new(contextOptions)
        {
            UserAgent = "hey",
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a target=_blank rel=noopener href=\"/popup/popup.html\">link</a>");
        var requestPromise = server.WaitForRequest("/popup/popup.html");
        var popup = await PromiseAll(
            context.WaitForPageAsync(),
            page.ClickAsync("a"));
        await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        var userAgent = await popup.EvaluateAsync<string>("() => window['initialUserAgent']");
        var request = await requestPromise;
        await context.CloseAsync();
        Assert.That(userAgent, Is.EqualTo("hey"));
        Assert.That(request.Headers["user-agent"], Is.EqualTo("hey"));
    }

    [UpstreamTest("PO-015 should respect routes from browser context")]
    public async Task ShouldRespectRoutesFromBrowserContext(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a target=_blank rel=noopener href=\"empty.html\">link</a>");
        var intercepted = false;
        await context.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.ContinueAsync());
            intercepted = true;
        });
        await PromiseAll(
            context.WaitForPageAsync(),
            page.ClickAsync("a"));
        await context.CloseAsync();
        Assert.That(intercepted, Is.EqualTo(true));
    }

    [UpstreamTest("PO-016 should inherit extra headers from browser context")]
    public async Task ShouldInheritExtraHeadersFromBrowserContext(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            ExtraHTTPHeaders = new Dictionary<string, string> { ["foo"] = "bar" },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var requestPromise = server.WaitForRequest("/dummy.html");
        await page.EvaluateAsync("url => window['_popup'] = window.open(url)", server.Prefix + "/dummy.html");
        var request = await requestPromise;
        await context.CloseAsync();
        Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("PO-017 should inherit offline from browser context")]
    public async Task ShouldInheritOfflineFromBrowserContext(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        await context.SetOfflineAsync(true);
        var online = await page.EvaluateAsync<bool>(@"url => {
    const win = window.open(url);
    return win.navigator.onLine;
  }", server.Prefix + "/dummy.html");
        await context.CloseAsync();
        Assert.That(online, Is.EqualTo(false));
    }

    [UpstreamTest("PO-018 should inherit http credentials from browser context")]
    public async Task ShouldInheritHttpCredentialsFromBrowserContext(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/title.html", "user", "pass");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentials = new() { Username = "user", Password = "pass" },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window['_popup'] = window.open(url)", server.Prefix + "/title.html"));
        await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.That(await popup.TitleAsync(), Is.EqualTo("Woof-Woof"));
        await context.CloseAsync();
    }

    [UpstreamTest("PO-019 should inherit touch support from browser context")]
    public async Task ShouldInheritTouchSupportFromBrowserContext(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            ViewportSize = new() { Width = 400, Height = 500 },
            HasTouch = true,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var hasTouch = await page.EvaluateAsync<bool>(@"() => {
    const win = window.open('');
    return 'ontouchstart' in win;
  }");
        await context.CloseAsync();
        Assert.That(hasTouch, Is.EqualTo(true));
    }

    [UpstreamTest("PO-020 should inherit viewport size from browser context")]
    public async Task ShouldInheritViewportSizeFromBrowserContext(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            ViewportSize = new() { Width = 400, Height = 500 },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var size = await page.EvaluateAsync<object>(@"() => {
    const win = window.open('about:blank');
    return { width: win.innerWidth, height: win.innerHeight };
  }");
        await context.CloseAsync();
        ExpectToEqual(size, new { width = 400, height = 500 });
    }

    [UpstreamTest("PO-021 should use viewport size from window features")]
    public async Task ShouldUseViewportSizeFromWindowFeatures(IBrowser browser, Server server, string browserName, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            ViewportSize = new() { Width = 700, Height = 700 },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var (size, popup) = await PromiseAll(
            page.EvaluateAsync<object>(@"async () => {
      const win = window.open(window.location.href, 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=600,height=300,top=0,left=0');
      await new Promise(resolve => {
        const interval = window.builtins.setInterval(() => {
          if (win.innerWidth === 600 && win.innerHeight === 300) {
            window.builtins.clearInterval(interval);
            resolve();
          }
        }, 10);
      });
      return { width: win.innerWidth, height: win.innerHeight };
    }"),
            page.WaitForPopupAsync());
        await popup.SetViewportSizeAsync(500, 400);
        await popup.WaitForLoadStateAsync();
        var resized = await popup.EvaluateAsync<object>("() => ({ width: window.innerWidth, height: window.innerHeight })");
        await context.CloseAsync();
        ExpectToEqual(size, new { width = 600, height = 300 });
        ExpectToEqual(resized, new { width = 500, height = 400 });
    }

    [UpstreamTest("PO-022 should respect routes from browser context when using window.open")]
    public async Task ShouldRespectRoutesFromBrowserContextWhenUsingWindowOpen(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var intercepted = false;
        await context.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.ContinueAsync());
            intercepted = true;
        });
        await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window['__popup'] = window.open(url)", server.EmptyPage));
        Assert.That(intercepted, Is.EqualTo(true));
        await context.CloseAsync();
    }

    [UpstreamTest("PO-029 should load subresources of an about:blank popup when the opener has routes")]
    public async Task ShouldLoadSubresourcesOfAnAboutBlankPopupWhenTheOpenerHasRoutes(IBrowser browser, Server server, string browserName, BrowserNewContextOptions contextOptions)
    {
        Info.Fixme(browserName == "chromium", "Subresources of an about:blank popup are paused on the opener target and never continued");
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/*", route => route.ContinueAsync());
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window.open('').document.write(`<img src=\"${url}\">`)", server.Prefix + "/pptr.png"));
        await popup.WaitForFunctionAsync("() => document.querySelector('img')?.naturalWidth > 0");
        await context.CloseAsync();
    }

    [UpstreamTest("PO-023 BrowserContext.addInitScript should apply to an in-process popup")]
    public async Task BrowserContextAddInitScriptShouldApplyToAnInProcessPopup(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        await context.AddInitScriptAsync("(() => window['injected'] = 123)()");
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var injected = await page.EvaluateAsync<int>(@"() => {
    const win = window.open('about:blank');
    return win['injected'];
  }");
        await context.CloseAsync();
        Assert.That(injected, Is.EqualTo(123));
    }

    [UpstreamTest("PO-024 BrowserContext.addInitScript should apply to a cross-process popup")]
    public async Task BrowserContextAddInitScriptShouldApplyToACrossProcessPopup(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        await context.AddInitScriptAsync("(() => window['injected'] = 123)()");
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window.open(url)", server.CrossProcessPrefix + "/title.html"));
        Assert.That(await popup.EvaluateAsync<int>("injected"), Is.EqualTo(123));
        await popup.ReloadAsync();
        Assert.That(await popup.EvaluateAsync<int>("injected"), Is.EqualTo(123));
        await context.CloseAsync();
    }

    [UpstreamTest("PO-025 should expose function from browser context")]
    public async Task ShouldExposeFunctionFromBrowserContext(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var messages = new List<string>();
        await context.ExposeFunctionAsync("add", (int a, int b) =>
        {
            messages.Add("binding");
            return a + b;
        });
        var page = await context.NewPageAsync();
        context.Page += (_, _) => messages.Add("page");
        await page.GotoAsync(server.EmptyPage);
        var added = await page.EvaluateAsync<int>(@"async () => {
    const win = window.open('about:blank');
    return win['add'](9, 4);
  }");
        await context.CloseAsync();
        Assert.That(added, Is.EqualTo(13));
        Assert.That(string.Join("|", messages), Is.EqualTo("page|binding"));
    }

    [UpstreamTest("PO-026 should not dispatch binding on a closed page")]
    public async Task ShouldNotDispatchBindingOnAClosedPage(IBrowser browser, Server server, string browserName, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        bool? wasClosed = null;
        await context.ExposeBindingAsync("add", (BindingSource source, int a, int b) =>
        {
            wasClosed = source.Page.IsClosed;
            return a + b;
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync(@"async () => {
      const win = window.open('about:blank');
      win['add'](9, 4);
      win.close();
    }"));
        // Give it a chance to dispatch the binding on the closed page.
        await page.WaitForTimeoutAsync(1000);
        await context.CloseAsync();
        Assert.That(wasClosed, Is.Not.True);
    }

    [UpstreamTest("PO-027 should not throttle rAF in the opener page")]
    public async Task ShouldNotThrottleRAFInTheOpenerPage(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => { window.open('about:blank'); }"));
        await PromiseAll(
            WaitForRafs(page, 30),
            WaitForRafs(popup, 30));
    }

    [UpstreamTest("PO-028 should not throw when click closes popup")]
    public async Task ShouldNotThrowWhenClickClosesPopup(string browserName, IPage page, Server server)
    {
        Info.Fixme(browserName == "firefox", "locator.click: Target page, context or browser has been closed");

        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync(@"async browserName => {
      const w = window.open('about:blank');
      if (browserName === 'firefox')
        await new Promise(x => w.onload = x);
      w.document.body.innerHTML = `<button onclick=""window.close()"">close</button>`;
    }", browserName));
        await popup.GetByRole(AriaRole.Button).ClickAsync();
    }

    private static async Task WaitForRafs(IPage page, int count)
    {
        await page.EvaluateAsync(@"count => new Promise(resolve => {
    const onRaf = () => {
      --count;
      if (!count)
        resolve();
      else
        window.builtins.requestAnimationFrame(onRaf);
    };
    window.builtins.requestAnimationFrame(onRaf);
  })", count);
    }
}
