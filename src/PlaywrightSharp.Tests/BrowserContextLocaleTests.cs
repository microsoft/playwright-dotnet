using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextLocaleTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextLocaleTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-locale.spec.ts", "should affect accept-language header")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAffectAcceptLanguageHeader()
        {
            await using var context = await Browser.NewContextAsync(locale: "fr-CH");
            string acceptLanguage = string.Empty;
            var page = await context.NewPageAsync();
            var requestTask = Server.WaitForRequest("/empty.html", c => acceptLanguage = c.Headers["accept-language"]);

            await TaskUtils.WhenAll(
                requestTask,
                page.GoToAsync(TestConstants.EmptyPage));

            Assert.StartsWith("fr-CH", acceptLanguage);
        }

        [PlaywrightTest("browsercontext-locale.spec.ts", "should affect navigator.language")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAffectNavigatorLanguage()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Locale = "fr-CH"
            });
            var page = await context.NewPageAsync();
            Assert.Equal("fr-CH", await page.EvaluateAsync<string>("navigator.language"));
        }

        [PlaywrightTest("browsercontext-locale.spec.ts", "should format number")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFormatNumber()
        {
            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Locale = "en-US"
            }))
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                Assert.Equal("1,000,000.5", await page.EvaluateAsync<string>("() => (1000000.50).toLocaleString()"));
            }

            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Locale = "fr-CH"
            }))
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                string value = await page.EvaluateAsync<string>("() => (1000000.50).toLocaleString().replace(/\\s/g, ' ')");
                Assert.Equal("1 000 000,5", value);
            }
        }

        [PlaywrightTest("browsercontext-locale.spec.ts", "should format date")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFormatDate()
        {
            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Locale = "en-US",
                TimezoneId = "America/Los_Angeles",
            }))
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                Assert.Equal(
                    "Sat Nov 19 2016 10:12:34 GMT-0800 (Pacific Standard Time)",
                    await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"));
            }

            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Locale = "de-DE",
                TimezoneId = "Europe/Berlin",
            }))
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync(TestConstants.EmptyPage);
                Assert.Equal(
                    "Sat Nov 19 2016 19:12:34 GMT+0100 (Mitteleuropäische Normalzeit)",
                    await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"));
            }
        }

        [PlaywrightTest("browsercontext-locale.spec.ts", "should format number in popups")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFormatNumberInPopups()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Locale = "fr-CH"
            });

            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = page.WaitForEventAsync(PageEvent.Popup);

            await TaskUtils.WhenAll(
                popupTask,
                page.EvaluateAsync("url => window._popup = window.open(url)", TestConstants.ServerUrl + "/formatted-number.html"));

            var popup = popupTask.Result.Page;
            await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.Equal("1 000 000,5", await popup.EvaluateAsync<string>("() => window.result"));
        }

        [PlaywrightTest("browsercontext-locale.spec.ts", "should affect navigator.language in popups")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAffectNavigatorLanguageInPopups()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Locale = "fr-CH"
            });

            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = page.WaitForEventAsync(PageEvent.Popup);

            await TaskUtils.WhenAll(
                popupTask,
                page.EvaluateAsync("url => window._popup = window.open(url)", TestConstants.ServerUrl + "/formatted-number.html"));

            var popup = popupTask.Result.Page;
            await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.Equal("fr-CH", await popup.EvaluateAsync<string>("() => window.initialNavigatorLanguage"));
        }
    }
}
