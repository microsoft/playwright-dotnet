using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Multiclient
{
    ///<playwright-file>multiclient.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>multiclient.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should work across sessions</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReturnAllOfThePages()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            await using var browser1 = await BrowserType.ConnectAsync(browserServer.WSEndpoint);
            Assert.Empty(browser1.Contexts);
            await browser1.NewContextAsync();
            Assert.Single(browser1.Contexts);

            await using var browser2 = await BrowserType.ConnectAsync(browserServer.WSEndpoint);
            Assert.Empty(browser2.Contexts);
            await browser2.NewContextAsync();
            Assert.Single(browser2.Contexts);

            Assert.Single(browser1.Contexts);
        }

    }
}
