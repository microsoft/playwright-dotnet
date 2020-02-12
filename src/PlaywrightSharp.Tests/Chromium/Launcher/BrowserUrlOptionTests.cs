using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium.Launcher
{
    ///<playwright-file>chromium/launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.launch |browserURL| option</playwright-describe>
    public class BrowserUrlOptionTests : PlaywrightSharpBrowserContextBaseTest
    {
        /// <inheritdoc/>
        public BrowserUrlOptionTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch |browserURL| option</playwright-describe>
        ///<playwright-it>should be able to connect using browserUrl, with and without trailing slash</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipChromium: true)]
        public async Task ShouldBeAbleToConnectUsingBrowserURLWithAndWithoutTrailingSlash()
        {
            var options = TestConstants.DefaultBrowserOptions;
            var browserApp = await Playwright.LaunchBrowserAppAsync(options);
            string browserURL = GetBrowserUrl(browserApp.WebSocketEndpoint);

            var browser1 = await Playwright.ConnectAsync(new ConnectOptions { BrowserURL = browserURL });
            var page1 = await browser1.DefaultContext.NewPageAsync();
            Assert.Equal(56, await page1.EvaluateAsync<int>("7 * 8"));
            await browser1.DisconnectAsync();

            var browser2 = await Playwright.ConnectAsync(new ConnectOptions { BrowserURL = browserURL + "/" });
            var page2 = await browser2.DefaultContext.NewPageAsync();
            Assert.Equal(56, await page2.EvaluateAsync<int>("7 * 8"));
            await browser2.DisconnectAsync();
            await browserApp.CloseAsync();
        }

        ///<playwright-file>chromium/launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch |browserURL| option</playwright-describe>
        ///<playwright-it>should throw when using both browserWSEndpoint and browserURL</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipChromium: true)]
        public async Task ShouldThrowWhenUsingBothBrowserWSEndpointAndBrowserURL()
        {
            var options = TestConstants.DefaultBrowserOptions;
            var browserApp = await Playwright.LaunchBrowserAppAsync(options);
            string browserURL = GetBrowserUrl(browserApp.WebSocketEndpoint);

            await Assert.ThrowsAsync<PlaywrightSharpException>(() => Playwright.ConnectAsync(new ConnectOptions
            {
                BrowserURL = browserURL,
                WebSocketEndpoint = browserApp.WebSocketEndpoint
            }));

            await browserApp.CloseAsync();
        }

        ///<playwright-file>chromium/launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch |browserURL| option</playwright-describe>
        ///<playwright-it>should throw when trying to connect to non-existing browser</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipChromium: true)]
        public async Task ShouldThrowWhenTryingToConnectToNonExistingBrowser()
        {
            var options = TestConstants.DefaultBrowserOptions;
            var browserApp = await Playwright.LaunchBrowserAppAsync(options);
            string browserURL = GetBrowserUrl(browserApp.WebSocketEndpoint);

            await Assert.ThrowsAsync<PlaywrightSharpException>(() => Playwright.ConnectAsync(new ConnectOptions
            {
                BrowserURL = browserURL
            }));

            await browserApp.CloseAsync();
        }

        private string GetBrowserUrl(string wsEndpoint)
        {
            var regex = new Regex(@"ws:\/\/([0-9A-Za-z\.]*):(\d+)\/");
            string port = regex.Match(wsEndpoint).Captures[2].Value;
            return $"http://127.0.0.1:${port}";
        }
    }
}
