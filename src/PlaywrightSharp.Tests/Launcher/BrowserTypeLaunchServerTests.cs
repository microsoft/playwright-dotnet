using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>browserType.launchServer</playwright-describe>
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class BrowserTypeLaunchServerTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public BrowserTypeLaunchServerTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launchServer</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            await using var browser = await BrowserType.ConnectAsync(new ConnectOptions { WSEndpoint = browserServer.WSEndpoint });
            var browserContext = await browser.NewContextAsync();
            Assert.Empty(browserContext.Pages);
            Assert.NotEmpty(browserServer.WSEndpoint);
            var page = await browserContext.NewPageAsync();
            Assert.Equal(121, await page.EvaluateAsync<int>("11 * 11"));
            await page.CloseAsync();
            await browser.CloseAsync();
            await browserServer.CloseAsync();
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launchServer</playwright-describe>
        ///<playwright-it>should fire "disconnected" when closing the server</playwright-it>
        [Retry]
        public async Task ShouldFireDisconnectedWhenClosingTheServer()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            await using var browser = await BrowserType.ConnectAsync(new ConnectOptions { WSEndpoint = browserServer.WSEndpoint });
            var disconnectedTcs = new TaskCompletionSource<bool>();
            var closedTcs = new TaskCompletionSource<bool>();
            browser.Disconnected += (server, e) => disconnectedTcs.TrySetResult(true);
            browserServer.Closed += (server, e) => closedTcs.TrySetResult(true);
            _ = browserServer.KillAsync();

            await TaskUtils.WhenAll(disconnectedTcs.Task, closedTcs.Task).WithTimeout();
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launchServer</playwright-describe>
        ///<playwright-it>should fire "close" event during kill</playwright-it>
        [Retry]
        public async Task ShouldFireClosedEventDuringKill()
        {
            var order = new List<string>();
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            var closedTcs = new TaskCompletionSource<bool>();
            browserServer.Closed += (server, e) =>
            {
                order.Add("closed");
                closedTcs.TrySetResult(true);
            };

            await TaskUtils.WhenAll(
                browserServer.KillAsync().ContinueWith(t => order.Add("killed")),
                closedTcs.Task);

            Assert.Equal(new[] { "closed", "killed" }, order.ToArray());
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launchServer</playwright-describe>
        ///<playwright-it>should return child_process instance</playwright-it>
        [Retry]
        public async Task ShouldReturnChildProcessInstance()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            Assert.True(browserServer.ProcessId > 0);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launchServer</playwright-describe>
        ///<playwright-it>should fire close event</playwright-it>
        [Retry]
        public async Task ShouldFireCloseEvent()
        {
            var tcs = new TaskCompletionSource<bool>();
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            browserServer.Closed += (sender, e) => tcs.TrySetResult(true);
            await TaskUtils.WhenAll(tcs.Task, browserServer.CloseAsync());
        }
    }
}
