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
public class BrowsercontextTimezoneIdTests : UpstreamBrowserTest
{
    [UpstreamTest("TZ-001 should work @smoke")]
    public async Task ShouldWork()
    {
        // Note: timezone names are rendered differently between browsers and platforms,
        // so we only check the GMT offset.
        const string func = "() => new Date(1479579154987).toString()";
        {
            var context = await Browser.NewContextAsync(new(ContextOptions()) { Locale = "en-US", TimezoneId = "America/Jamaica" });
            var page = await context.NewPageAsync();
            Assert.That(await page.EvaluateAsync<string>(func), Does.Contain("Sat Nov 19 2016 13:12:34 GMT-0500"));
            await context.CloseAsync();
        }
        {
            var context = await Browser.NewContextAsync(new(ContextOptions()) { Locale = "en-US", TimezoneId = "Pacific/Honolulu" });
            var page = await context.NewPageAsync();
            Assert.That(await page.EvaluateAsync<string>(func), Does.Contain("Sat Nov 19 2016 08:12:34 GMT-1000"));
            await context.CloseAsync();
        }
        {
            var context = await Browser.NewContextAsync(new(ContextOptions()) { Locale = "en-US", TimezoneId = BrowserName == "firefox" ? "America/Argentina/Buenos_Aires" : "America/Buenos_Aires" });
            var page = await context.NewPageAsync();
            Assert.That(await page.EvaluateAsync<string>(func), Does.Contain("Sat Nov 19 2016 15:12:34 GMT-0300"));
            await context.CloseAsync();
        }
        {
            var context = await Browser.NewContextAsync(new(ContextOptions()) { Locale = "en-US", TimezoneId = "Europe/Berlin" });
            var page = await context.NewPageAsync();
            Assert.That(await page.EvaluateAsync<string>(func), Does.Contain("Sat Nov 19 2016 19:12:34 GMT+0100"));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("TZ-002 should throw for invalid timezone IDs when creating pages")]
    public async Task ShouldThrowForInvalidTimezoneIDsWhenCreatingPages()
    {
        foreach (var timezoneId in new[] { "Foo/Bar", "Baz/Qux" })
        {
            if (IsBidi)
            {
                var error = await Catch(() => Browser.NewContextAsync(new(ContextOptions()) { TimezoneId = timezoneId }));
                if (BrowserName == "chromium")
                {
                    Assert.That(error.Message, Does.Contain($"Invalid timezone \"{timezoneId}\""));
                }
                else if (BrowserName == "firefox")
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
                    context = await Browser.NewContextAsync(new(ContextOptions()) { TimezoneId = timezoneId });
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
    public async Task ShouldWorkForMultiplePagesSharingSameProcess()
    {
        var context = await Browser.NewContextAsync(new(ContextOptions()) { TimezoneId = "Europe/Moscow" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => { window.open(url); }", Server.EmptyPage));
        (popup, _) = await PromiseAll(
            popup.WaitForPopupAsync(),
            popup.EvaluateAsync("url => { window.open(url); }", Server.EmptyPage));
        await context.CloseAsync();
    }

    [UpstreamTest("TZ-004 should not change default timezone in another context")]
    public async Task ShouldNotChangeDefaultTimezoneInAnotherContext()
    {
        async Task<string> getContextTimezone(IBrowserContext context)
        {
            var page = await context.NewPageAsync();
            return await page.EvaluateAsync<string>("() => Intl.DateTimeFormat().resolvedOptions().timeZone");
        }

        string defaultTimezone;
        {
            var context = await Browser.NewContextAsync(ContextOptions());
            defaultTimezone = await getContextTimezone(context);
            await context.CloseAsync();
        }
        var timezoneOverride = defaultTimezone == "Europe/Moscow" ? "America/Los_Angeles" : "Europe/Moscow";
        {
            var context = await Browser.NewContextAsync(new(ContextOptions()) { TimezoneId = timezoneOverride });
            Assert.That(await getContextTimezone(context), Is.EqualTo(timezoneOverride));
            await context.CloseAsync();
        }
        {
            var context = await Browser.NewContextAsync(ContextOptions());
            Assert.That(await getContextTimezone(context), Is.EqualTo(defaultTimezone));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("TZ-005 should affect Intl.DateTimeFormat().resolvedOptions().timeZone")]
    public async Task ShouldAffectIntlDateTimeFormatResolvedOptionsTimeZone()
    {
        var context = await Browser.NewContextAsync(new(ContextOptions()) { TimezoneId = "America/Jamaica" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(await page.EvaluateAsync<string>("() => (new Intl.DateTimeFormat()).resolvedOptions().timeZone"), Is.EqualTo("America/Jamaica"));
        await context.CloseAsync();
    }

    [UpstreamTest("TZ-006 should propagate timezone to workers")]
    public async Task ShouldPropagateTimezoneToWorkers()
    {
        var context = await Browser.NewContextAsync(new(ContextOptions()) { TimezoneId = "America/Jamaica" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        var (msg, _) = await PromiseAll(
            page.WaitForConsoleMessageAsync(),
            page.EvaluateAsync("() => new Worker(URL.createObjectURL(new Blob(['console.log(Intl.DateTimeFormat().resolvedOptions().timeZone)'], { type: 'application/javascript' })))"));
        Assert.That(msg.Text, Is.EqualTo("America/Jamaica"));
        await context.CloseAsync();
    }
}
