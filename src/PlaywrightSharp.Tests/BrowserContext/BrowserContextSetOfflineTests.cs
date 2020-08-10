using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>browsercontext.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext.setOffline</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextSetOfflineTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextSetOfflineTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext.setOffline</playwright-describe>
        ///<playwright-it>should work with initial option</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithInitialOption()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { Offline = true });
            var page = await context.NewPageAsync();
            await Assert.ThrowsAsync<PlaywrightSharpException>(() => page.GoToAsync(TestConstants.EmptyPage));
            await context.SetOfflineAsync(false);
            var response = await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext.setOffline</playwright-describe>
        ///<playwright-it>should emulate navigator.onLine</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldEmulateNavigatorOnLine()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            Assert.True(await page.EvaluateAsync<bool>("() => window.navigator.onLine"));
            await context.SetOfflineAsync(true);
            Assert.False(await page.EvaluateAsync<bool>("() => window.navigator.onLine"));
            await context.SetOfflineAsync(false);
            Assert.True(await page.EvaluateAsync<bool>("() => window.navigator.onLine"));
        }
    }
}
