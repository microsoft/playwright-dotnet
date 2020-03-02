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
    ///<playwright-describe>Browser.disconnect</playwright-describe>
    public class DisconnectTests : PlaywrightSharpBrowserContextBaseTest
    {
        /// <inheritdoc/>
        public DisconnectTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.disconnect</playwright-describe>
        ///<playwright-it>should reject navigation when browser closes</playwright-it>
        [Fact]
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
            var exception = await Assert.ThrowsAsync<NavigationException>(() => navigationTask);
            Assert.Contains("Navigation failed because browser has disconnected!", exception.Message);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.disconnect</playwright-describe>
        ///<playwright-it>should reject waitForSelector when browser closes</playwright-it>
        [Fact]
        public async Task ShouldRejectWaitForSelectorWhenBrowserCloses()
        {
            Server.SetRoute("/empty.html", context => Task.Delay(10000));

            using var browserApp = await Playwright.LaunchBrowserAppAsync(TestConstants.GetDefaultBrowserOptions());
            var remote = await Playwright.ConnectAsync(browserApp.ConnectOptions);
            var page = await remote.DefaultContext.NewPageAsync();
            var watchdog = page.WaitForSelectorAsync("div", new WaitForSelectorOptions { Timeout = 60000 });

            await page.WaitForSelectorAsync("body");

            await remote.DisconnectAsync();
            var exception = await Assert.ThrowsAsync<TargetClosedException>(() => watchdog);
            Assert.Equal("Connection disposed", exception.CloseReason);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Browser.disconnect</playwright-describe>
        ///<playwright-it>should throw if used after disconnect</playwright-it>
        [Fact]
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
