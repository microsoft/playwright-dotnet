using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Multiclient
{
    ///<playwright-file>multiclient.spec.js</playwright-file>
    ///<playwright-describe>browserType.connect</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserTypeConnectTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserTypeConnectTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>multiclient.spec.js</playwright-file>
        ///<playwright-describe>browserType.connect</playwright-describe>
        ///<playwright-it>should be able to connect multiple times to the same browser</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeAbleToConnectMultipleTimesToTheSameBrowser()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            var browser1 = await BrowserType.ConnectAsync(browserServer.WSEndpoint);
            await using var browser2 = await BrowserType.ConnectAsync(browserServer.WSEndpoint);

            var page1 = await browser1.NewPageAsync();
            Assert.Equal(56, await page1.EvaluateAsync<int>("() => 7 * 8"));

            await browser1.CloseAsync();

            var page2 = await browser2.NewPageAsync();
            Assert.Equal(56, await page2.EvaluateAsync<int>("() => 7 * 8"));
        }

        ///<playwright-file>multiclient.spec.js</playwright-file>
        ///<playwright-describe>browserType.connect</playwright-describe>
        ///<playwright-it>should not be able to close remote browser</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotBeAbleToCloseRemoteBrowser()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());

            await using (var remote = await BrowserType.ConnectAsync(browserServer.WSEndpoint))
            {
                await remote.NewPageAsync();
            }

            await using (var remote = await BrowserType.ConnectAsync(browserServer.WSEndpoint))
            {
                await remote.NewPageAsync();
            }
        }
    }
}
