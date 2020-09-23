using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
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

        ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
        ///<playwright-it>should be able to reconnect to a browser</playwright-it>
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

        ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
        ///<playwright-it>should be able to connect two browsers at the same time</playwright-it>
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldBeAbleToConnectTwoBrowsersAtTheSameTime()
        {
        }

        ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
        ///<playwright-it>disconnected event should be emitted when browser is closed or server is closed</playwright-it>
        [Fact(Skip = "SKIP WIRE")]
        public void DisconnectedEventShouldBeEmittedWhenBrowserIsClosedOrServerIsClosed()
        {
        }

        ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
        ///<playwright-it>should handle exceptions during connect</playwright-it>
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldHandleExceptionsDuringConnect()
        {
        }

        ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
        ///<playwright-it>should set the browser connected state</playwright-it>
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldSetTheBrowserConnectedState()
        {
        }

        ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
        ///<playwright-it>should throw when used after isConnected returns false</playwright-it>
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldThrowWhenUsedAfterIsConnectedReturnsFalse()
        {
        }

        ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
        ///<playwright-it>should reject navigation when browser closes</playwright-it>
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldRejectNavigationWhenBrowserCloses()
        {
        }

        ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
        ///<playwright-it>should reject waitForSelector when browser closes</playwright-it>
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldRejectWaitForSelectorWhenBrowserCloses()
        {
        }

        ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
        ///<playwright-it>should emit close events on pages and contexts</playwright-it>
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldEmitCloseEventsOnPagesAndContexts()
        {
        }

        ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
        ///<playwright-it>should terminate network waiters</playwright-it>
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldTerminateNetworkWaiters()
        {
        }

        ///<playwright-file>browsertype-connect.spec.ts</playwright-file>
        ///<playwright-it>should respect selectors</playwright-it>
        [Fact(Skip = "SKIP WIRE")]
        public void ShouldRespectSelectors()
        {
        }

    }
}
