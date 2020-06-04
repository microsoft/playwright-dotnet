using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Browser
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Browser.disconnect</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class DisconnectTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public DisconnectTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.disconnect</playwright-describe>
        ///<playwright-it>should reject navigation when browser closes</playwright-it>
        [Retry]
        public async Task ShouldRejectNavigationWhenBrowserCloses()
        {
            Server.SetRoute("/one-style.css", context => Task.Delay(10000));

            using var browserApp = await Playwright.LaunchBrowserAppAsync(TestConstants.GetDefaultBrowserOptions());

            var remote = await Playwright.ConnectAsync(browserApp.ConnectOptions);
            var page = await remote.DefaultContext.NewPageAsync();
            var navigationTask = page.GoToAsync(TestConstants.ServerUrl + "/one-style.html", new GoToOptions
            {
                Timeout = 60000
            });
            await Server.WaitForRequest("/one-style.css");
            await remote.DisconnectAsync();
            await Assert.ThrowsAsync<NavigationException>(() => navigationTask);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.disconnect</playwright-describe>
        ///<playwright-it>should reject waitForSelector when browser closes</playwright-it>
        [Retry]
        public async Task ShouldRejectWaitForSelectorWhenBrowserCloses()
        {
            Server.SetRoute("/empty.html", context => Task.Delay(10000));

            using var browserApp = await Playwright.LaunchBrowserAppAsync(TestConstants.GetDefaultBrowserOptions());
            var remote = await Playwright.ConnectAsync(browserApp.ConnectOptions);
            var page = await remote.DefaultContext.NewPageAsync();
            var watchdog = page.WaitForSelectorAsync("div", new WaitForSelectorOptions { Timeout = 60000 });

            await page.WaitForSelectorAsync("body");

            await remote.DisconnectAsync();
            await Assert.ThrowsAsync<TargetClosedException>(() => watchdog);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.disconnect</playwright-describe>
        ///<playwright-it>should throw if used after disconnect</playwright-it>
        [Retry]
        public async Task ShouldThrowIfUsedAfterDisconnect()
        {
            using var browserApp = await Playwright.LaunchBrowserAppAsync(TestConstants.GetDefaultBrowserOptions());
            var remote = await Playwright.ConnectAsync(browserApp.ConnectOptions);
            var page = await remote.DefaultContext.NewPageAsync();
            await remote.DisconnectAsync();

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.EvaluateAsync("1 + 1"));
            Assert.Contains("has been closed", exception.Message);
        }
    }
}
