using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserTypeConnectTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public BrowserTypeConnectTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should be able to reconnect to a browser")]
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldBeAbleToReconnectToABrowser()
        {
            /*
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            await using var browser = await BrowserType.ConnectAsync(browserServer.WSEndpoint);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            await browser.CloseAsync();

            await using var remote = await BrowserType.ConnectAsync(browserServer.WSEndpoint);

            context = await remote.NewContextAsync();
            page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await remote.CloseAsync();
            await browserServer.CloseAsync();
            */
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should be able to connect two browsers at the same time")]
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldBeAbleToConnectTwoBrowsersAtTheSameTime()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "disconnected event should be emitted when browser is closed or server is closed")]
        [Fact(Skip = "SKIP WIRE")]
        public void DisconnectedEventShouldBeEmittedWhenBrowserIsClosedOrServerIsClosed()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should handle exceptions during connect")]
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldHandleExceptionsDuringConnect()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should set the browser connected state")]
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldSetTheBrowserConnectedState()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should throw when used after isConnected returns false")]
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldThrowWhenUsedAfterIsConnectedReturnsFalse()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should reject navigation when browser closes")]
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldRejectNavigationWhenBrowserCloses()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should reject waitForSelector when browser closes")]
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldRejectWaitForSelectorWhenBrowserCloses()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should emit close events on pages and contexts")]
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldEmitCloseEventsOnPagesAndContexts()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should terminate network waiters")]
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldTerminateNetworkWaiters()
        {
        }

        [PlaywrightTest("browsertype-connect.spec.ts", "should respect selectors")]
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldRespectSelectors()
        {
        }
    }
}
