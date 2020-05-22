using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium.Launcher
{
    ///<playwright-file>chromium/launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.launch webSocket option</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class WebSocketOptionsTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public WebSocketOptionsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch webSocket option</playwright-describe>
        ///<playwright-it>should support the remote-debugging-port argument</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldSupportTheRemoteDebuggingPortArgument()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = new[] { "--remote-debugging-port=0" };

            var browserApp = await Playwright.LaunchBrowserAppAsync(options);

            var browser = await Playwright.ConnectAsync(browserApp.ConnectOptions);
            Assert.NotNull(browserApp.WebSocketEndpoint);
            var page = await browser.DefaultContext.NewPageAsync();
            Assert.Equal(121, await page.EvaluateAsync<int>("11 * 11"));
            await page.CloseAsync();
            await browserApp.CloseAsync();
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch webSocket option</playwright-describe>
        ///<playwright-it>should support the remote-debugging-pipe argument</playwright-it>
        [Fact(Skip = "We don't support pipes yet")]
        public void ShouldSupportTheRemoteDebuggingPipeArgument() { }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch webSocket option</playwright-describe>
        ///<playwright-it>should throw with remote-debugging-pipe argument and webSocket</playwright-it>
        [Fact(Skip = "We don't support pipes yet")]
        public void ShouldThrowWithRemoteDebuggingPipeArgumentAndWebSocket() { }
    }
}
