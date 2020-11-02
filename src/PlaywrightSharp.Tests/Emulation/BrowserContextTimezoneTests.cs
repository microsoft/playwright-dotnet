using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Emulation
{
    ///<playwright-file>emulation.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext({timezoneId})</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextTimezoneTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextTimezoneTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({timezoneId})</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await using var browser = await Playwright[TestConstants.Product].LaunchAsync(TestConstants.GetDefaultBrowserOptions());

            const string func = "() => new Date(1479579154987).toString()";
            await using (var context = await browser.NewContextAsync(new BrowserContextOptions { TimezoneId = "America/Jamaica" }))
            {
                var page = await context.NewPageAsync();
                string result = await page.EvaluateAsync<string>(func);
                Assert.Equal(
                    "Sat Nov 19 2016 13:12:34 GMT-0500 (Eastern Standard Time)",
                    result);
            }

            await using (var context = await browser.NewContextAsync(new BrowserContextOptions { TimezoneId = "Pacific/Honolulu" }))
            {
                var page = await context.NewPageAsync();
                Assert.Equal(
                    "Sat Nov 19 2016 08:12:34 GMT-1000 (Hawaii-Aleutian Standard Time)",
                    await page.EvaluateAsync<string>(func));
            }

            await using (var context = await browser.NewContextAsync(new BrowserContextOptions { TimezoneId = "America/Buenos_Aires" }))
            {
                var page = await context.NewPageAsync();
                Assert.Equal(
                    "Sat Nov 19 2016 15:12:34 GMT-0300 (Argentina Standard Time)",
                    await page.EvaluateAsync<string>(func));
            }

            await using (var context = await browser.NewContextAsync(new BrowserContextOptions { TimezoneId = "Europe/Berlin" }))
            {
                var page = await context.NewPageAsync();
                Assert.Equal(
                    "Sat Nov 19 2016 19:12:34 GMT+0100 (Central European Standard Time)",
                    await page.EvaluateAsync<string>(func));
            }
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({timezoneId})</playwright-describe>
        ///<playwright-it>should throw for invalid timezone IDs</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldThrowForInvalidTimezoneId()
        {
            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions { TimezoneId = "Foo/Bar" }))
            {
                var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => context.NewPageAsync());
                Assert.Contains("Invalid timezone ID: Foo/Bar", exception.Message);
            }

            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions { TimezoneId = "Baz/Qux" }))
            {
                var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => context.NewPageAsync());
                Assert.Contains("Invalid timezone ID: Baz/Qux", exception.Message);
            }
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({timezoneId})</playwright-describe>
        ///<playwright-it>should work for multiple pages sharing same process</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkForMultiplePagesSharingSameProcess()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { TimezoneId = "Europe/Moscow" });

            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            await TaskUtils.WhenAll(
                page.WaitForEventAsync(PageEvent.Popup),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

            await TaskUtils.WhenAll(
                page.WaitForEventAsync(PageEvent.Popup),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));
        }
    }
}
