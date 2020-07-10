using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// This [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class setup a single browser instance for tests.
    /// </summary>
    public class PlaywrightSharpBrowserLoaderFixture : IDisposable
    {
        //internal static IBrowserApp BrowserApp { get; private set; }
        internal static IBrowser Browser { get; private set; }

        /// <inheritdoc />
        public PlaywrightSharpBrowserLoaderFixture()
        {
            SetupBrowserAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            /*
            BrowserApp.CloseAsync().GetAwaiter().GetResult();
            BrowserApp = null;
            */
            Browser = null;
        }

        private /*async*/ Task SetupBrowserAsync()
        {
            return Task.CompletedTask;
            /*
            BrowserApp = await TestConstants.GetNewBrowserType().LaunchBrowserAppAsync(TestConstants.GetDefaultBrowserOptions());
            Browser = await TestConstants.GetNewBrowserType().ConnectAsync(BrowserApp.ConnectOptions);
            */
        }
    }
}
