using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Browser
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Browser.close</playwright-describe>
    public class CloseTests : PlaywrightSharpBrowserContextBaseTest
    {
        /// <inheritdoc/>
        public CloseTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.close</playwright-describe>
        ///<playwright-it>should terminate network waiters</playwright-it>
        [Fact]
        public async Task ShouldTerminateNetworkWaiters()
        {
            using var browserApp = await Playwright.LaunchBrowserAppAsync(TestConstants.DefaultBrowserOptions);
            using var remote = await Playwright.ConnectAsync(browserApp.ConnectOptions);

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
        [Fact]
        public async Task ShouldBeAbleToCloseRemoteBrowser()
        {
            using var browserApp = await Playwright.LaunchBrowserAppAsync(TestConstants.DefaultBrowserOptions);
            using var remote = await Playwright.ConnectAsync(browserApp.ConnectOptions);
            var closeTask = new TaskCompletionSource<bool>();

            browserApp.Closed += (sender, e) => closeTask.TrySetResult(true);

            await Task.WhenAll(remote.CloseAsync(), closeTask.Task);
        }
    }
}
