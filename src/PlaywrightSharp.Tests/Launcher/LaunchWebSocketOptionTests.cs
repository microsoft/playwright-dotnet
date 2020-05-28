using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.launch |webSocket| option</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class LaunchWebSocketOptionTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public LaunchWebSocketOptionTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch |webSocket| option</playwright-describe>
        ///<playwright-it>should not have websocket by default</playwright-it>
        [Fact(Skip = "Websockets will be the default in Playwright sharp")]
        public void ShouldNotHaveWebsocketByDefault() { }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch |webSocket| option</playwright-describe>
        ///<playwright-it>should support the webSocket option</playwright-it>
        [Retry]
        public async Task ShouldSupportTheWebSocketOption()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            using var browserApp = await Playwright.LaunchBrowserAppAsync(options);
            using var browser = await Playwright.ConnectAsync(browserApp.ConnectOptions);
            Assert.Single(await browser.DefaultContext.GetPagesAsync());
            Assert.NotNull(browserApp.WebSocketEndpoint);
            var page = await browser.DefaultContext.NewPageAsync();
            Assert.Equal(121, await page.EvaluateAsync<int>("11 * 11"));
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch |webSocket| option</playwright-describe>
        ///<playwright-it>should fire "disconnected" when closing without webSocket</playwright-it>
        [Fact(Skip = "We don't support pipes yet")]
        public void ShouldFireDisconnectedWhenClosingWithoutWebSocket() { }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch |webSocket| option</playwright-describe>
        ///<playwright-it>should fire "disconnected" when closing with webSocket</playwright-it>
        [Retry]
        public async Task ShouldFireDisconnectedWhenClosingWithWebSocket()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            using var browserApp = await Playwright.LaunchBrowserAppAsync(options);
            using var browser = await Playwright.ConnectAsync(browserApp.ConnectOptions);
            var disconnectedTask = new TaskCompletionSource<bool>();
            browser.Disconnected += (sender, e) => disconnectedTask.TrySetResult(true);
            browserApp.Kill();
            await disconnectedTask.Task;
        }
    }
}
