using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Browser
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Browser.isConnected</playwright-describe>
    public class IsConnectedTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public IsConnectedTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.isConnected</playwright-describe>
        ///<playwright-it>should set the browser connected state</playwright-it>
        [Fact]
        public async Task ShouldSetTheBrowserConnectedState()
        {
            var browserApp = await Playwright.LaunchBrowserAppAsync(TestConstants.GetDefaultBrowserOptions());
            var remote = await Playwright.ConnectAsync(new ConnectOptions
            {
                BrowserWSEndpoint = browserApp.WebSocketEndpoint
            });
            Assert.True(remote.IsConnected);
            await remote.DisconnectAsync();
            Assert.False(remote.IsConnected);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.isConnected</playwright-describe>
        ///<playwright-it>should throw when used after isConnected returns false</playwright-it>
        [Fact]
        public async Task ShouldThrowWhenUsedAfterIsConnectedReturnsFalse()
        {
            var browserApp = await Playwright.LaunchBrowserAppAsync(TestConstants.GetDefaultBrowserOptions());
            var remote = await Playwright.ConnectAsync(new ConnectOptions
            {
                BrowserWSEndpoint = browserApp.WebSocketEndpoint
            });
            var page = await remote.DefaultContext.NewPageAsync();
            var disconnectedTask = new TaskCompletionSource<bool>();
            remote.Disconnected += (sender, e) => disconnectedTask.TrySetResult(true);

            await Task.WhenAll(browserApp.CloseAsync(), disconnectedTask.Task);

            Assert.False(remote.IsConnected);

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.EvaluateAsync("1 + 1"));
            Assert.Contains("has been closed", exception.Message);
        }
    }
}
