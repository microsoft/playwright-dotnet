using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// This [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class setup a single browser instance for tests.
    /// </summary>
    public class PlaywrightSharpBrowserLoaderFixture : PlaywrightSharpDriverLoaderFixture
    {
        internal static IBrowserServer BrowserServer { get; private set; }
        internal static IBrowser Browser { get; private set; }

        /// <inheritdoc />
        public PlaywrightSharpBrowserLoaderFixture()
        {
            SetupBrowserAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            BrowserServer.CloseAsync().GetAwaiter().GetResult();
            BrowserServer = null;
            Browser = null;

            base.Dispose();
        }

        private async Task SetupBrowserAsync()
        {
            BrowserServer = await Playwright[TestConstants.Product].LaunchServerAsync(TestConstants.GetDefaultBrowserOptions());
            Browser = await Playwright[TestConstants.Product].ConnectAsync(new ConnectOptions { WSEndpoint = BrowserServer.WSEndpoint });
        }
    }
}
