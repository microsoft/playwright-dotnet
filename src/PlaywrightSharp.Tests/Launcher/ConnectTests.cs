using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.connect</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ConnectTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public ConnectTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.connect</playwright-describe>
        ///<playwright-it>should be able to reconnect to a browser</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeAbleToReconnectToADisconnectedBrowser()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            await using var browser = await BrowserType.ConnectAsync(new ConnectOptions { WSEndpoint = browserServer.WSEndpoint });
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            await browser.CloseAsync();

            await using var remote = await BrowserType.ConnectAsync(new ConnectOptions { WSEndpoint = browserServer.WSEndpoint });

            context = await remote.NewContextAsync();
            page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await remote.CloseAsync();
            await browserServer.CloseAsync();
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.connect</playwright-describe>
        ///<playwright-it>should handle exceptions during connect</playwright-it>
        [Fact(Skip = "We don't tests hooks")]
        public void ShouldHandleExceptionsDuringConnect()
        {
        }
    }
}
