/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Microsoft.Playwright.Tests;

public class BrowserContextLocaleTests : BrowserTestEx
{
    [PlaywrightTest("browsercontext-locale.spec.ts", "should affect accept-language header")]
    public async Task ShouldAffectAcceptLanguageHeader()
    {
        await using var context = await Browser.NewContextAsync(new() { Locale = "fr-FR" });
        string acceptLanguage = string.Empty;
        var page = await context.NewPageAsync();
        var requestTask = Server.WaitForRequest("/empty.html", c => acceptLanguage = c.Headers["accept-language"]);

        await TaskUtils.WhenAll(
            requestTask,
            page.GotoAsync(Server.EmptyPage));

        Assert.That(acceptLanguage, Does.StartWith("fr-FR"));
    }

    [PlaywrightTest("browsercontext-locale.spec.ts", "should affect navigator.language")]
    public async Task ShouldAffectNavigatorLanguage()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            Locale = "fr-FR"
        });
        var page = await context.NewPageAsync();
        Assert.AreEqual("fr-FR", await page.EvaluateAsync<string>("navigator.language"));
    }

    [PlaywrightTest("browsercontext-locale.spec.ts", "should format number")]
    public async Task ShouldFormatNumber()
    {
        await using (var context = await Browser.NewContextAsync(new()
        {
            Locale = "en-US"
        }))
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual("1,000,000.5", await page.EvaluateAsync<string>("() => (1000000.50).toLocaleString()"));
        }

        await using (var context = await Browser.NewContextAsync(new()
        {
            Locale = "fr-FR"
        }))
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            string value = await page.EvaluateAsync<string>("() => (1000000.50).toLocaleString().replace(/\\s/g, ' ')");
            Assert.AreEqual("1 000 000,5", value);
        }
    }

    [PlaywrightTest("browsercontext-locale.spec.ts", "should format date")]
    public async Task ShouldFormatDate()
    {
        await using (var context = await Browser.NewContextAsync(new()
        {
            Locale = "en-US",
            TimezoneId = "America/Los_Angeles",
        }))
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(
                "Sat Nov 19 2016 10:12:34 GMT-0800 (Pacific Standard Time)",
                await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"));
        }

        await using (var context = await Browser.NewContextAsync(new()
        {
            Locale = "de-DE",
            TimezoneId = "Europe/Berlin",
        }))
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(
                "Sat Nov 19 2016 19:12:34 GMT+0100 (Mitteleuropäische Normalzeit)",
                await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"));
        }
    }

    [PlaywrightTest("browsercontext-locale.spec.ts", "should format number in popups")]
    public async Task ShouldFormatNumberInPopups()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            Locale = "fr-FR"
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        var popupTask = page.WaitForPopupAsync();

        await TaskUtils.WhenAll(
            popupTask,
            page.EvaluateAsync("url => window._popup = window.open(url)", Server.Prefix + "/formatted-number.html"));

        var popup = popupTask.Result;
        await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.AreEqual("1 000 000,5", await popup.EvaluateAsync<string>("() => window.result"));
    }

    [PlaywrightTest("browsercontext-locale.spec.ts", "should affect navigator.language in popups")]
    public async Task ShouldAffectNavigatorLanguageInPopups()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            Locale = "fr-FR"
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        var popupTask = page.WaitForPopupAsync();

        await TaskUtils.WhenAll(
            popupTask,
            page.EvaluateAsync("url => window._popup = window.open(url)", Server.Prefix + "/formatted-number.html"));

        var popup = popupTask.Result;
        await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.AreEqual("fr-FR", await popup.EvaluateAsync<string>("() => window.initialNavigatorLanguage"));
    }
}
