using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Browser
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Browser.disconnect</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class DisconnectTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public DisconnectTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.disconnect</playwright-describe>
        ///<playwright-it>should reject navigation when browser closes</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRejectNavigationWhenBrowserCloses()
        {
            Server.SetRoute("/one-style.css", context => Task.Delay(10000));

            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());

            var remote = await BrowserType.ConnectAsync(browserServer.WSEndpoint);
            var page = await remote.NewPageAsync();
            var navigationTask = page.GoToAsync(TestConstants.ServerUrl + "/one-style.html", timeout: 60000);
            await Server.WaitForRequest("/one-style.css");
            await remote.CloseAsync();
            await Assert.ThrowsAsync<NavigationException>(() => navigationTask);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.disconnect</playwright-describe>
        ///<playwright-it>should reject waitForSelector when browser closes</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRejectWaitForSelectorWhenBrowserCloses()
        {
            Server.SetRoute("/empty.html", context => Task.Delay(10000));

            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            var remote = await BrowserType.ConnectAsync(browserServer.WSEndpoint);
            var page = await remote.NewPageAsync();
            var watchdog = page.WaitForSelectorAsync("div", WaitForState.Attached, 60000);

            await page.WaitForSelectorAsync("body", WaitForState.Attached);

            await remote.CloseAsync();
            await Assert.ThrowsAsync<TargetClosedException>(() => watchdog);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.disconnect</playwright-describe>
        ///<playwright-it>should throw if used after disconnect</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldThrowIfUsedAfterDisconnect()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            var remote = await BrowserType.ConnectAsync(browserServer.WSEndpoint);
            var page = await remote.NewPageAsync();
            await remote.CloseAsync();

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.EvaluateAsync("1 + 1"));
            Assert.Contains("has been closed", exception.Message);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.disconnect</playwright-describe>
        ///<playwright-it>should emit close events on pages and contexts</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldEmitCloseEventsOnPagesAndContexts()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            var remote = await BrowserType.ConnectAsync(browserServer.WSEndpoint);
            var context = await remote.NewContextAsync();
            var page = await context.NewPageAsync();
            var tcs = new TaskCompletionSource<bool>();
            page.Closed += (sender, e) => tcs.TrySetResult(true);

            await TaskUtils.WhenAll(remote.CloseAsync(), tcs.Task);
        }
    }
}
