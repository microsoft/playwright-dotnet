using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.launch |browserURL| option</playwright-describe>
    public class BrowserUrlOptionTests : PlaywrightSharpBrowserContextBaseTest
    {
        /// <inheritdoc/>
        public BrowserUrlOptionTests(ITestOutputHelper output) : base(output)
        {
        }
        /*
        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch |browserURL| option</playwright-describe>
        ///<playwright-it>should be able to connect using browserUrl, with and without trailing slash</playwright-it>
        [Fact]
        public async Task ShouldBeAbleToConnectUsingBrowserURLWithAndWithoutTrailingSlash()
        {
            var options = TestConstants.DefaultBrowserOptions();
            options.Args = new string[] { "--remote-debugging-port=21222" };
            var originalBrowser = await Playwright.LaunchAsync(options);
            var browserURL = "http://127.0.0.1:21222";

            var browser1 = await Playwright.ConnectAsync(new ConnectOptions { BrowserURL = browserURL });
            var page1 = await browser1.NewPageAsync();
            Assert.Equal(56, await page1.EvaluateExpressionAsync<int>("7 * 8"));
            browser1.Disconnect();

            var browser2 = await Playwright.ConnectAsync(new ConnectOptions { BrowserURL = browserURL + "/" });
            var page2 = await browser2.NewPageAsync();
            Assert.Equal(56, await page2.EvaluateExpressionAsync<int>("7 * 8"));
            browser2.Disconnect();
            await originalBrowser.CloseAsync();
        }

        [Fact]
        public async Task ShouldThrowWhenUsingBothBrowserWSEndpointAndBrowserURL()
        {
            var options = TestConstants.DefaultBrowserOptions();
            options.Args = new string[] { "--remote-debugging-port=21222" };
            var originalBrowser = await Playwright.LaunchAsync(options);
            var browserURL = "http://127.0.0.1:21222";

            await Assert.ThrowsAsync<PlaywrightException>(() => Playwright.ConnectAsync(new ConnectOptions
            {
                BrowserURL = browserURL,
                BrowserWSEndpoint = originalBrowser.WebSocketEndpoint
            }));

            await originalBrowser.CloseAsync();
        }

        [Fact]
        public async Task ShouldThrowWhenTryingToConnectToNonExistingBrowser()
        {
            var options = TestConstants.DefaultBrowserOptions();
            options.Args = new string[] { "--remote-debugging-port=21222" };
            var originalBrowser = await Playwright.LaunchAsync(options);
            var browserURL = "http://127.0.0.1:2122";

            await Assert.ThrowsAsync<ChromiumProcessException>(() => Playwright.ConnectAsync(new ConnectOptions
            {
                BrowserURL = browserURL
            }));

            await originalBrowser.CloseAsync();
        }*/
    }
}
