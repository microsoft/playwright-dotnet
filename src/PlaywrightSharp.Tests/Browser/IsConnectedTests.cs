using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Browser
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Browser.isConnected</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class IsConnectedTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public IsConnectedTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.isConnected</playwright-describe>
        ///<playwright-it>should set the browser connected state</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSetTheBrowserConnectedState()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            var remote = await BrowserType.ConnectAsync(browserServer.WSEndpoint);
            Assert.True(remote.IsConnected);
            await remote.CloseAsync();
            Assert.False(remote.IsConnected);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.isConnected</playwright-describe>
        ///<playwright-it>should throw when used after isConnected returns false</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldThrowWhenUsedAfterIsConnectedReturnsFalse()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            var remote = await BrowserType.ConnectAsync(browserServer.WSEndpoint);
            var page = await remote.NewPageAsync();
            var disconnectedTask = new TaskCompletionSource<bool>();
            remote.Disconnected += (sender, e) => disconnectedTask.TrySetResult(true);

            await TaskUtils.WhenAll(browserServer.CloseAsync(), disconnectedTask.Task);

            Assert.False(remote.IsConnected);

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.EvaluateAsync("1 + 1"));
            Assert.Contains("has been closed", exception.Message);
        }
    }
}
