using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Multiclient
{
    ///<playwright-file>multiclient.spec.js</playwright-file>
    ///<playwright-describe>Browser.Events.Disconnected</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserEventsDisconnected : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserEventsDisconnected(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>multiclient.spec.js</playwright-file>
        ///<playwright-describe>Browser.Events.Disconnected</playwright-describe>
        ///<playwright-it>should be emitted when: browser gets closed, disconnected or underlying websocket gets closed</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeEmittedWhenBrowserGetsClosedDisconnectedOrUnderlyingWebsocketGetsClosed()
        {
            await using var browserServer = await BrowserType.LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            await using var originalBrowser = await BrowserType.ConnectAsync(browserServer.WSEndpoint);
            string wsEndpoint = browserServer.WSEndpoint;

            await using var remoteBrowser1 = await BrowserType.ConnectAsync(wsEndpoint);
            await using var remoteBrowser2 = await BrowserType.ConnectAsync(wsEndpoint);

            int disconnectedOriginal = 0;
            int disconnectedRemote1 = 0;
            int disconnectedRemote2 = 0;

            var originalBrowserDisconnectedTcs = new TaskCompletionSource<bool>();
            var remote1DisconnectedTcs = new TaskCompletionSource<bool>();
            var remote2DisconnectedTcs = new TaskCompletionSource<bool>();

            originalBrowser.Disconnected += (sender, e) =>
            {
                ++disconnectedOriginal;
                originalBrowserDisconnectedTcs.TrySetResult(true);
            };

            remoteBrowser1.Disconnected += (sender, e) =>
            {
                ++disconnectedRemote1;
                remote1DisconnectedTcs.TrySetResult(true);
            };

            remoteBrowser2.Disconnected += (sender, e) =>
            {
                ++disconnectedRemote2;
                remote2DisconnectedTcs.TrySetResult(true);
            };

            await TaskUtils.WhenAll(
                remote2DisconnectedTcs.Task,
                remoteBrowser2.CloseAsync());

            Assert.Equal(0, disconnectedOriginal);
            Assert.Equal(0, disconnectedRemote1);
            Assert.Equal(1, disconnectedRemote2);

            await TaskUtils.WhenAll(
                originalBrowserDisconnectedTcs.Task,
                remote1DisconnectedTcs.Task,
                browserServer.CloseAsync());

            Assert.Equal(1, disconnectedOriginal);
            Assert.Equal(1, disconnectedRemote1);
            Assert.Equal(1, disconnectedRemote2);
        }
    }
}
