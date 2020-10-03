using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Browser
{

    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Browser.close</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class CloseTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public CloseTests(ITestOutputHelper output) : base(output)
        {
        }

        /*
        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.close</playwright-describe>
        ///<playwright-it>should terminate network waiters</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldTerminateNetworkWaiters()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            await using var remote = await BrowserType.ConnectAsync(browserServer.WSEndpoint);

            var newPage = await remote.NewPageAsync();
            var requestTask = newPage.WaitForRequestAsync(TestConstants.EmptyPage);
            var responseTask = newPage.WaitForResponseAsync(TestConstants.EmptyPage);

            await browserServer.CloseAsync();

            var exception = await Assert.ThrowsAsync<TargetClosedException>(() => requestTask);
            Assert.Contains("Page closed", exception.Message);
            Assert.DoesNotContain("Timeout", exception.Message);

            exception = await Assert.ThrowsAsync<TargetClosedException>(() => responseTask);
            Assert.Contains("Page closed", exception.Message);
            Assert.DoesNotContain("Timeout", exception.Message);
        }
        */

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.close</playwright-describe>
        ///<playwright-it>should fire close event for all contexts</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFireCloseEventForAllContexts()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var context = await browser.NewContextAsync();
            var closeTask = new TaskCompletionSource<bool>();

            context.Close += (sender, e) => closeTask.TrySetResult(true);

            await TaskUtils.WhenAll(browser.CloseAsync(), closeTask.Task);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.close</playwright-describe>
        ///<playwright-it>should be callable twice</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeCallableTwice()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            await TaskUtils.WhenAll(browser.CloseAsync(), browser.CloseAsync());
            await browser.CloseAsync();
        }
    }
}
