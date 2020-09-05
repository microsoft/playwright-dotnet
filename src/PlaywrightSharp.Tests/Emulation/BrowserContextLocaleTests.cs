using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Emulation
{
    ///<playwright-file>emulation.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext({locale})</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextLocaleTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextLocaleTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({locale})</playwright-describe>
        ///<playwright-it>should affect accept-language header</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({locale})</playwright-describe>
        ///<playwright-it>should affect navigator.language</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldAffectNavigatorLanguage()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Locale = "fr-CH"
            });
            var page = await context.NewPageAsync();
            Assert.Equal("fr-CH", await page.EvaluateAsync<string>("navigator.language"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({locale})</playwright-describe>
        ///<playwright-it>should format number</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({locale})</playwright-describe>
        ///<playwright-it>should format date</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({locale})</playwright-describe>
        ///<playwright-it>should format number in popups</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFormatNumberInPopups()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Locale = "fr-CH"
            });

            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);

            await TaskUtils.WhenAll(
                popupTask,
                page.EvaluateAsync("url => window._popup = window.open(url)", TestConstants.ServerUrl + "/formatted-number.html"));

            var popup = popupTask.Result.Page;
            await popup.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);
            Assert.Equal("1 000 000,5", await popup.EvaluateAsync<string>("() => window.result"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({locale})</playwright-describe>
        ///<playwright-it>should affect navigator.language in popups</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldAffectNavigatorLanguageInPopups()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Locale = "fr-CH"
            });

            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);

            await TaskUtils.WhenAll(
                popupTask,
                page.EvaluateAsync("url => window._popup = window.open(url)", TestConstants.ServerUrl + "/formatted-number.html"));

            var popup = popupTask.Result.Page;
            await popup.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);
            Assert.Equal("fr-CH", await popup.EvaluateAsync<string>("() => window.initialNavigatorLanguage"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({timezoneId})</playwright-describe>
        ///<playwright-it>should work for multiple pages sharing same process</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkForMultiplePagesSharingSameProcess()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { Locale = "ru-RU" });

            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            await TaskUtils.WhenAll(
                page.WaitForEvent<PopupEventArgs>(PageEvent.Popup),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

            await TaskUtils.WhenAll(
                page.WaitForEvent<PopupEventArgs>(PageEvent.Popup),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({timezoneId})</playwright-describe>
        ///<playwright-it>should be isolated between contexts</playwright-it>
        [Fact(Skip = "Flacky")]
        public async Task ShouldBeIsolatedBetweenContexts()
        {
            await using var context1 = await Browser.NewContextAsync(new BrowserContextOptions { Locale = "en-US" });
            var tasks = new List<Task>();

            for (int i = 0; i < 9; i++)
            {
                tasks.Add(context1.NewPageAsync());
            }
            await TaskUtils.WhenAll(tasks);

            await using var context2 = await Browser.NewContextAsync(new BrowserContextOptions { Locale = "ru-RU" });
            var page2 = await context2.NewPageAsync();

            string[] numbers = await TaskUtils.WhenAll(context1.Pages.Select(p => p.EvaluateAsync<string>("() => (1000000.50).toLocaleString()")));

            foreach (string number in numbers)
            {
                Assert.Equal("1,000,000.5", number);
            }

            Assert.Equal("1 000 000,5", await page2.EvaluateAsync<string>("() => (1000000.50).toLocaleString()"));
        }
    }
}
