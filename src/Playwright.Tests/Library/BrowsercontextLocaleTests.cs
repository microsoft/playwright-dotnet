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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-locale.spec.ts.</summary>
public class BrowsercontextLocaleTests
{
    [UpstreamTest("LC-001 should affect accept-language header @smoke")]
    public async Task ShouldAffectAcceptLanguageHeader(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { Locale = "fr-CH" });
        var page = await context.NewPageAsync();
        var (request, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.EmptyPage));
        Assert.That(request.Headers["accept-language"].Substring(0, 5), Is.EqualTo("fr-CH"));
        await context.CloseAsync();
    }

    [UpstreamTest("LC-002 should affect navigator.language")]
    public async Task ShouldAffectNavigatorLanguage(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { Locale = "fr-FR" });
        var page = await context.NewPageAsync();
        Assert.That(await page.EvaluateAsync<string>("() => navigator.language"), Is.EqualTo("fr-FR"));
        await context.CloseAsync();
    }

    [UpstreamTest("LC-003 should format number")]
    public async Task ShouldFormatNumber(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        {
            var context = await browser.NewContextAsync(new(contextOptions) { Locale = "en-US" });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            Assert.That(await page.EvaluateAsync<string>("() => (1000000.50).toLocaleString()"), Is.EqualTo("1,000,000.5"));
            await context.CloseAsync();
        }
        {
            var context = await browser.NewContextAsync(new(contextOptions) { Locale = "fr-FR" });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            Assert.That(await page.EvaluateAsync<string>(@"() => (1000000.50).toLocaleString().replace(/\s/g, ' ')"), Is.EqualTo("1 000 000,5"));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("LC-004 should format date")]
    public async Task ShouldFormatDate(IBrowser browser, Server server, string browserName, BrowserNewContextOptions contextOptions)
    {
        {
            var context = await browser.NewContextAsync(new(contextOptions) { Locale = "en-US", TimezoneId = "America/Los_Angeles" });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            var formatted = "Sat Nov 19 2016 10:12:34 GMT-0800 (Pacific Standard Time)";
            Assert.That(await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"), Is.EqualTo(formatted));
            await context.CloseAsync();
        }
        {
            var context = await browser.NewContextAsync(new(contextOptions) { Locale = "de-DE", TimezoneId = "Europe/Berlin" });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            var formatted = "Sat Nov 19 2016 19:12:34 GMT+0100 (Mitteleuropäische Normalzeit)";
            Assert.That(await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"), Is.EqualTo(formatted));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("LC-005 should format number in popups")]
    public async Task ShouldFormatNumberInPopups(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { Locale = "fr-FR" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);

        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window.open(url)", server.Prefix + "/formatted-number.html"));
        await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        var result = await popup.EvaluateAsync<string>("window[\"result\"]");
        Assert.That(result, Is.EqualTo("1 000 000,5"));
        await context.CloseAsync();
    }

    [UpstreamTest("LC-006 should affect navigator.language in popups")]
    public async Task ShouldAffectNavigatorLanguageInPopups(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { Locale = "fr-FR" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window.open(url)", server.Prefix + "/formatted-number.html"));
        await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        var result = await popup.EvaluateAsync<string>("window.initialNavigatorLanguage");
        Assert.That(result, Is.EqualTo("fr-FR"));
        await context.CloseAsync();
    }

    [UpstreamTest("LC-007 should work for multiple pages sharing same process")]
    public async Task ShouldWorkForMultiplePagesSharingSameProcess(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { Locale = "ru-RU" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => { window.open(url); }", server.EmptyPage));
        (popup, _) = await PromiseAll(
            popup.WaitForPopupAsync(),
            popup.EvaluateAsync("url => { window.open(url); }", server.EmptyPage));
        await context.CloseAsync();
    }

    [UpstreamTest("LC-008 should be isolated between contexts")]
    public async Task ShouldBeIsolatedBetweenContexts(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        Info.Slow();

        var context1 = await browser.NewContextAsync(new(contextOptions) { Locale = "en-US" });
        var promises = new List<Task<IPage>>();
        // By default firefox limits number of child web processes to 8.
        for (var i = 0; i < 8; i++)
        {
            promises.Add(context1.NewPageAsync());
        }
        await PromiseAll(promises);

        var context2 = await browser.NewContextAsync(new(contextOptions) { Locale = "ru-RU" });
        var page2 = await context2.NewPageAsync();

        const string localeNumber = "() => (1000000.50).toLocaleString()";
        var numbers = await PromiseAll(context1.Pages.Select(page => page.EvaluateAsync<string>(localeNumber)));

        foreach (var value in numbers)
        {
            Assert.That(value, Is.EqualTo("1,000,000.5"));
        }
        Assert.That(await page2.EvaluateAsync<string>(localeNumber), Is.EqualTo("1 000 000,5"));

        await PromiseAll(
            context1.CloseAsync(),
            context2.CloseAsync());
    }

    [UpstreamTest("LC-009 should not change default locale in another context")]
    public async Task ShouldNotChangeDefaultLocaleInAnotherContext(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        async Task<string> getContextLocale(IBrowserContext context)
        {
            var page = await context.NewPageAsync();
            return await page.EvaluateAsync<string>("() => (new Intl.NumberFormat()).resolvedOptions().locale");
        }

        string defaultLocale;
        {
            var context = await browser.NewContextAsync(contextOptions);
            defaultLocale = await getContextLocale(context);
            await context.CloseAsync();
        }
        var localeOverride = defaultLocale == "es-MX" ? "de-DE" : "es-MX";
        {
            var context = await browser.NewContextAsync(new(contextOptions) { Locale = localeOverride });
            Assert.That(await getContextLocale(context), Is.EqualTo(localeOverride));
            await context.CloseAsync();
        }
        {
            var context = await browser.NewContextAsync(contextOptions);
            Assert.That(await getContextLocale(context), Is.EqualTo(defaultLocale));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("LC-010 should propagate locale to workers")]
    public async Task ShouldPropagateLocaleToWorkers(IBrowser browser, string browserName, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { Locale = "ru-RU" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        // .NET: Promise.all of four promises is a PromiseAll of the first three, inside a PromiseAll with the fourth.
        var ((msg, languages, request), _) = await PromiseAll(
            PromiseAll(
                page.WaitForConsoleMessageAsync(new() { Predicate = e => e.Text.StartsWith("locale:", StringComparison.Ordinal) }),
                page.WaitForConsoleMessageAsync(new() { Predicate = e => e.Text.StartsWith("languages:", StringComparison.Ordinal) }),
                server.WaitForRequest("/title.html")),
            page.EvaluateAsync(@"url => new Worker(URL.createObjectURL(new Blob([`
      console.log(""locale:"" + Intl.NumberFormat().resolvedOptions().locale);
      console.log(""languages:"" + navigator.languages);
      fetch(${JSON.stringify(url)});
    `], { type: 'application/javascript' })))", server.Prefix + "/title.html"));
        if (browserName == "webkit")
        {
            // Webkit on Ubuntu is "ru-RU", and on other platforms is "ru"
            Assert.That(msg.Text, Does.Contain("locale:ru"));
            Assert.That(languages.Text, Does.Contain("languages:ru"));
        }
        else
        {
            Assert.That(msg.Text, Is.EqualTo("locale:ru-RU"));
            Assert.That(languages.Text, Is.EqualTo("languages:ru-RU"));
        }
        Assert.That(request.Headers["accept-language"], Does.Contain("ru-RU"));
        await context.CloseAsync();
    }

    [UpstreamTest("LC-011 should affect Intl.DateTimeFormat().resolvedOptions().locale")]
    public async Task ShouldAffectIntlDateTimeFormatResolvedOptionsLocale(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { Locale = "en-GB" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.EvaluateAsync<string>("() => (new Intl.DateTimeFormat()).resolvedOptions().locale"), Is.EqualTo("en-GB"));
        await context.CloseAsync();
    }

    [UpstreamTest("LC-012 should send user Accept-Language header")]
    public async Task ShouldSendUserAcceptLanguageHeader(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { Locale = "en-GB" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        {
            var reqPromise = server.WaitForRequest("/empty.html");
            await page.EvaluateAsync(@"async url => {
      await fetch(url, {
        headers: {
          'Content-Type': 'application/json',
          'Accept-Language': 'de'
        },
      });
    }", server.EmptyPage);
            var req = await reqPromise;
            Assert.That(req.Headers["accept-language"], Is.EqualTo("de"));
        }
        {
            var reqPromise = server.WaitForRequest("/empty.html");
            await page.EvaluateAsync(@"async url => {
      await fetch(url, {
        headers: {
          'Content-Type': 'application/json'
        },
      });
    }", server.EmptyPage);
            var req = await reqPromise;
            Assert.That(req.Headers["accept-language"], Does.Contain("en-GB"));
        }
        await context.CloseAsync();
    }

    [UpstreamTest("LC-013 should send Accept-Language header on WebSocket handshake")]
    public async Task ShouldSendAcceptLanguageHeaderOnWebSocketHandshake(IBrowser browser, Server server, string browserName, int browserMajorVersion, BrowserNewContextOptions contextOptions)
    {
        Info.Fixme(browserName == "chromium" && browserMajorVersion < 151, "Chromium before 151 sends the browser Accept-Language instead of the emulated locale on WebSocket handshake");

        var context = await browser.NewContextAsync(new(contextOptions) { Locale = "en-GB" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var reqPromise = server.WaitForWebSocketConnectionRequest();
        await page.EvaluateAsync("port => { new WebSocket(`ws://localhost:${port}/ws`); }", server.Port);
        var req = await reqPromise;
        Assert.That(req.Headers["accept-language"], Does.Contain("en-GB"));
        await context.CloseAsync();
    }

    [UpstreamTest("LC-014 should send Accept-Language header on WebSocket handshake from a worker")]
    public async Task ShouldSendAcceptLanguageHeaderOnWebSocketHandshakeFromAWorker(IBrowser browser, Server server, string browserName, int browserMajorVersion, BrowserNewContextOptions contextOptions)
    {
        Info.Fixme(browserName == "chromium" && browserMajorVersion < 151, "Chromium before 151 sends the browser Accept-Language instead of the emulated locale on WebSocket handshake");

        var context = await browser.NewContextAsync(new(contextOptions) { Locale = "en-GB" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var reqPromise = server.WaitForWebSocketConnectionRequest();
        await page.EvaluateAsync(@"port => {
    const code = `new WebSocket(${JSON.stringify(`ws://localhost:${port}/ws`)});`;
    new Worker(URL.createObjectURL(new Blob([code], { type: 'text/javascript' })));
  }", server.Port);
        var req = await reqPromise;
        Assert.That(req.Headers["accept-language"], Does.Contain("en-GB"));
        await context.CloseAsync();
    }
}
