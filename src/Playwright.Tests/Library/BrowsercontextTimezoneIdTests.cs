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

/// <summary>Port of tests/library/browsercontext-timezone-id.spec.ts.</summary>
public class BrowsercontextTimezoneIdTests
{
    [UpstreamTest("TZ-001 should work @smoke")]
    public async Task ShouldWork(IBrowser browser, string browserName, BrowserNewContextOptions contextOptions)
    {
        // Note: timezone names are rendered differently between browsers and platforms,
        // so we only check the GMT offset.
        const string func = "() => new Date(1479579154987).toString()";
        {
            var context = await browser.NewContextAsync(new(contextOptions) { Locale = "en-US", TimezoneId = "America/Jamaica" });
            var page = await context.NewPageAsync();
            Assert.That(await page.EvaluateAsync<string>(func), Does.Contain("Sat Nov 19 2016 13:12:34 GMT-0500"));
            await context.CloseAsync();
        }
        {
            var context = await browser.NewContextAsync(new(contextOptions) { Locale = "en-US", TimezoneId = "Pacific/Honolulu" });
            var page = await context.NewPageAsync();
            Assert.That(await page.EvaluateAsync<string>(func), Does.Contain("Sat Nov 19 2016 08:12:34 GMT-1000"));
            await context.CloseAsync();
        }
        {
            var context = await browser.NewContextAsync(new(contextOptions) { Locale = "en-US", TimezoneId = browserName == "firefox" ? "America/Argentina/Buenos_Aires" : "America/Buenos_Aires" });
            var page = await context.NewPageAsync();
            Assert.That(await page.EvaluateAsync<string>(func), Does.Contain("Sat Nov 19 2016 15:12:34 GMT-0300"));
            await context.CloseAsync();
        }
        {
            var context = await browser.NewContextAsync(new(contextOptions) { Locale = "en-US", TimezoneId = "Europe/Berlin" });
            var page = await context.NewPageAsync();
            Assert.That(await page.EvaluateAsync<string>(func), Does.Contain("Sat Nov 19 2016 19:12:34 GMT+0100"));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("TZ-002 should throw for invalid timezone IDs when creating pages")]
    public async Task ShouldThrowForInvalidTimezoneIDsWhenCreatingPages(IBrowser browser, string channel, string browserName, bool isBidi, BrowserNewContextOptions contextOptions)
    {
        foreach (var timezoneId in new[] { "Foo/Bar", "Baz/Qux" })
        {
            if (isBidi)
            {
                var error = await Catch(() => browser.NewContextAsync(new(contextOptions) { TimezoneId = timezoneId }));
                if (browserName == "chromium")
                {
                    Assert.That(error.Message, Does.Contain($"Invalid timezone \"{timezoneId}\""));
                }
                else if (browserName == "firefox")
                {
                    Assert.That(error.Message, Does.Contain($"Expected \"timezone\" to be a valid timezone ID (e.g., \"Europe/Berlin\") or a valid timezone offset (e.g., \"+01:00\"), got {timezoneId}"));
                }
            }
            else
            {
                Exception error = null;
                IBrowserContext context = null;
                try
                {
                    context = await browser.NewContextAsync(new(contextOptions) { TimezoneId = timezoneId });
                    await context.NewPageAsync();
                }
                catch (Exception e)
                {
                    error = e;
                }
                Assert.That(error.Message, Does.Contain($"Invalid timezone ID: {timezoneId}"));
                if (context != null)
                {
                    await context.CloseAsync();
                }
            }
        }
    }

    [UpstreamTest("TZ-003 should work for multiple pages sharing same process")]
    public async Task ShouldWorkForMultiplePagesSharingSameProcess(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { TimezoneId = "Europe/Moscow" });
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

    [UpstreamTest("TZ-004 should not change default timezone in another context")]
    public async Task ShouldNotChangeDefaultTimezoneInAnotherContext(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        async Task<string> getContextTimezone(IBrowserContext context)
        {
            var page = await context.NewPageAsync();
            return await page.EvaluateAsync<string>("() => Intl.DateTimeFormat().resolvedOptions().timeZone");
        }

        string defaultTimezone;
        {
            var context = await browser.NewContextAsync(contextOptions);
            defaultTimezone = await getContextTimezone(context);
            await context.CloseAsync();
        }
        var timezoneOverride = defaultTimezone == "Europe/Moscow" ? "America/Los_Angeles" : "Europe/Moscow";
        {
            var context = await browser.NewContextAsync(new(contextOptions) { TimezoneId = timezoneOverride });
            Assert.That(await getContextTimezone(context), Is.EqualTo(timezoneOverride));
            await context.CloseAsync();
        }
        {
            var context = await browser.NewContextAsync(contextOptions);
            Assert.That(await getContextTimezone(context), Is.EqualTo(defaultTimezone));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("TZ-005 should affect Intl.DateTimeFormat().resolvedOptions().timeZone")]
    public async Task ShouldAffectIntlDateTimeFormatResolvedOptionsTimeZone(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { TimezoneId = "America/Jamaica" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.EvaluateAsync<string>("() => (new Intl.DateTimeFormat()).resolvedOptions().timeZone"), Is.EqualTo("America/Jamaica"));
        await context.CloseAsync();
    }

    [UpstreamTest("TZ-006 should propagate timezone to workers")]
    public async Task ShouldPropagateTimezoneToWorkers(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { TimezoneId = "America/Jamaica" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var (msg, _) = await PromiseAll(
            page.WaitForConsoleMessageAsync(),
            page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(Intl.DateTimeFormat().resolvedOptions().timeZone)'], { type: 'application/javascript' })))"));
        Assert.That(msg.Text, Is.EqualTo("America/Jamaica"));
        await context.CloseAsync();
    }
}
