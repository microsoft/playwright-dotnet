using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Browser
{
    ///<playwright-file>browser.spec.js</playwright-file>
    ///<playwright-describe>Browser.newPage</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserNewPageTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserNewPageTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browser.spec.js</playwright-file>
        ///<playwright-describe>Browser.newPage</playwright-describe>
        ///<playwright-it>should create new page</playwright-it>
        [Retry]
        public async Task ShouldCreateNewPage()
        {
            var page1 = await Browser.NewPageAsync();
            Assert.Single(Browser.Contexts);

            var page2 = await Browser.NewPageAsync();
            Assert.Equal(2, Browser.Contexts.Length);

            await page1.CloseAsync();
            Assert.Single(Browser.Contexts);

            await page2.CloseAsync();
        }

        ///<playwright-file>browser.spec.js</playwright-file>
        ///<playwright-describe>Browser.newPage</playwright-describe>
        ///<playwright-it>should throw upon second create new page</playwright-it>
        [Retry]
        public async Task ShouldThrowUponSecondCreateNewPage()
        {
            var page = await Browser.NewPageAsync();
            var ex = await Assert.ThrowsAsync<PlaywrightSharpException>(() => page.BrowserContext.NewPageAsync());
            await page.CloseAsync();
            Assert.Contains("Please use Browser.NewContextAsync()", ex.Message);
        }
    }
}
