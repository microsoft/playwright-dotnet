using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>browser.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browser.spec.ts", "should create new page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCreateNewPage()
        {
            var browser = await Playwright[TestConstants.Product].LaunchDefaultAsync();
            var page1 = await browser.NewPageAsync();
            Assert.Single(browser.Contexts);

            var page2 = await browser.NewPageAsync();
            Assert.Equal(2, browser.Contexts.Count);

            await page1.CloseAsync();
            Assert.Single(browser.Contexts);

            await page2.CloseAsync();
        }

        [PlaywrightTest("browser.spec.ts", "should throw upon second create new page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowUponSecondCreateNewPage()
        {
            var page = await Browser.NewPageAsync();
            var ex = await Assert.ThrowsAsync<PlaywrightSharpException>(() => page.Context.NewPageAsync());
            await page.CloseAsync();
            Assert.Contains("Please use Browser.NewContextAsync()", ex.Message);
        }

        [PlaywrightTest("browser.spec.ts", "version should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public void VersionShouldWork()
        {
            string version = Browser.Version;

            if (TestConstants.IsChromium)
            {
                Assert.Matches(new Regex("\\d+\\.\\d+\\.\\d+\\.\\d+"), version);
            }
            else
            {
                Assert.Matches(new Regex("\\d+\\.\\d+"), version);
            }
        }
    }
}
