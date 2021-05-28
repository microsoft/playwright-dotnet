using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextLocaleTests : BrowserTestEx
    {
        [PlaywrightTest("browsercontext-locale.spec.ts", "should affect accept-language header")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAffectAcceptLanguageHeader()
        {
            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions { Locale = "fr-CH" });
            string acceptLanguage = string.Empty;
            var page = await context.NewPageAsync();
            var requestTask = HttpServer.Server.WaitForRequest("/empty.html", c => acceptLanguage = c.Headers["accept-language"]);

            await TaskUtils.WhenAll(
                requestTask,
                page.GotoAsync(TestConstants.EmptyPage));

            Assert.That(acceptLanguage, Does.StartWith("fr-CH"));
        }

        [PlaywrightTest("browsercontext-locale.spec.ts", "should affect navigator.language")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAffectNavigatorLanguage()
        {
            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "fr-CH"
            });
            var page = await context.NewPageAsync();
            Assert.AreEqual("fr-CH", await page.EvaluateAsync<string>("navigator.language"));
        }

        [PlaywrightTest("browsercontext-locale.spec.ts", "should format number")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFormatNumber()
        {
            await using (var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "en-US"
            }))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(TestConstants.EmptyPage);
                Assert.AreEqual("1,000,000.5", await page.EvaluateAsync<string>("() => (1000000.50).toLocaleString()"));
            }

            await using (var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "fr-CH"
            }))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(TestConstants.EmptyPage);
                string value = await page.EvaluateAsync<string>("() => (1000000.50).toLocaleString().replace(/\\s/g, ' ')");
                Assert.AreEqual("1 000 000,5", value);
            }
        }

        [PlaywrightTest("browsercontext-locale.spec.ts", "should format date")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFormatDate()
        {
            await using (var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "en-US",
                TimezoneId = "America/Los_Angeles",
            }))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(TestConstants.EmptyPage);
                Assert.AreEqual(
                    "Sat Nov 19 2016 10:12:34 GMT-0800 (Pacific Standard Time)",
                    await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"));
            }

            await using (var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "de-DE",
                TimezoneId = "Europe/Berlin",
            }))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(TestConstants.EmptyPage);
                Assert.AreEqual(
                    "Sat Nov 19 2016 19:12:34 GMT+0100 (Mitteleuropäische Normalzeit)",
                    await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"));
            }
        }

        [PlaywrightTest("browsercontext-locale.spec.ts", "should format number in popups")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFormatNumberInPopups()
        {
            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "fr-CH"
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);
            var popupTask = page.WaitForPopupAsync();

            await TaskUtils.WhenAll(
                popupTask,
                page.EvaluateAsync("url => window._popup = window.open(url)", TestConstants.ServerUrl + "/formatted-number.html"));

            var popup = popupTask.Result;
            await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.AreEqual("1 000 000,5", await popup.EvaluateAsync<string>("() => window.result"));
        }

        [PlaywrightTest("browsercontext-locale.spec.ts", "should affect navigator.language in popups")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAffectNavigatorLanguageInPopups()
        {
            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                Locale = "fr-CH"
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);
            var popupTask = page.WaitForPopupAsync();

            await TaskUtils.WhenAll(
                popupTask,
                page.EvaluateAsync("url => window._popup = window.open(url)", TestConstants.ServerUrl + "/formatted-number.html"));

            var popup = popupTask.Result;
            await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.AreEqual("fr-CH", await popup.EvaluateAsync<string>("() => window.initialNavigatorLanguage"));
        }
    }
}
