using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Browser
{
    /*
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Browser.close</playwright-describe>
    [Collection(TestConstants.TestFixtureCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class CloseTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public CloseTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.close</playwright-describe>
        ///<playwright-it>should terminate network waiters</playwright-it>
        [Retry]
        public async Task ShouldTerminateNetworkWaiters()
        {
            using var browserApp = await BrowserType.LaunchBrowserAppAsync(TestConstants.GetDefaultBrowserOptions());
            await using var remote = await BrowserType.ConnectAsync(browserApp.ConnectOptions);

            var newPage = await remote.DefaultContext.NewPageAsync();
            var requestTask = newPage.WaitForRequestAsync(TestConstants.EmptyPage);
            var responseTask = newPage.WaitForResponseAsync(TestConstants.EmptyPage);

            await browserApp.CloseAsync();

            var exception = await Assert.ThrowsAsync<TargetClosedException>(() => requestTask);
            Assert.Contains("Target closed", exception.Message);
            Assert.DoesNotContain("Timeout", exception.Message);

            exception = await Assert.ThrowsAsync<TargetClosedException>(() => responseTask);
            Assert.Contains("Target closed", exception.Message);
            Assert.DoesNotContain("Timeout", exception.Message);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.close</playwright-describe>
        ///<playwright-it>should be able to close remote browser</playwright-it>
        [Retry]
        public async Task ShouldBeAbleToCloseRemoteBrowser()
        {
            using var browserApp = await BrowserType.LaunchBrowserAppAsync(TestConstants.GetDefaultBrowserOptions());
            using var remote = await BrowserType.ConnectAsync(browserApp.ConnectOptions);
            var closeTask = new TaskCompletionSource<bool>();

            browserApp.Closed += (sender, e) => closeTask.TrySetResult(true);

            await Task.WhenAll(remote.CloseAsync(), closeTask.Task);
        }
    }
    */
}
