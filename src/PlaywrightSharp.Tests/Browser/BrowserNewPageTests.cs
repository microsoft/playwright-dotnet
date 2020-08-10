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
    public class BrowserNewPageTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public BrowserNewPageTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browser.spec.js</playwright-file>
        ///<playwright-describe>Browser.newPage</playwright-describe>
        ///<playwright-it>should create new page</playwright-it>
        [Fact]
        public async Task ShouldCreateNewPage()
        {
            await using var browser = await Playwright[TestConstants.Product].LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page1 = await browser.NewPageAsync();
            Assert.Single(browser.Contexts);

            var page2 = await browser.NewPageAsync();
            Assert.Equal(2, browser.Contexts.Length);

            await page1.CloseAsync();
            Assert.Single(browser.Contexts);

            await page2.CloseAsync();
        }

        ///<playwright-file>browser.spec.js</playwright-file>
        ///<playwright-describe>Browser.newPage</playwright-describe>
        ///<playwright-it>should throw upon second create new page</playwright-it>
        [Fact]
        public async Task ShouldThrowUponSecondCreateNewPage()
        {
            await using var browser = await Playwright[TestConstants.Product].LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page = await browser.NewPageAsync();
            var ex = await Assert.ThrowsAsync<PlaywrightSharpException>(() => page.Context.NewPageAsync());
            await page.CloseAsync();
            Assert.Contains("Please use Browser.NewContextAsync()", ex.Message);
        }
    }
}
