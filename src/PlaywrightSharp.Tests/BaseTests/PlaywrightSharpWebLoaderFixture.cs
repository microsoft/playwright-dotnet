using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// This class setup web tests.
    /// </summary>
    public class PlaywrightSharpWebLoaderFixture : PlaywrightSharpLoaderFixture, IDisposable
    {
        /// <inheritdoc/>
        public PlaywrightSharpWebLoaderFixture() : base()
        {
            SetupWebAsync().GetAwaiter().GetResult();
        }

        internal static IBrowserApp ControlledBrowserApp { get; private set; }
        internal static IBrowserApp HostBrowserApp { get; private set; }
        internal static IBrowser HostBrowser { get; private set; }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public override void Dispose()
        {
            base.Dispose();

            HostBrowserApp.CloseAsync().GetAwaiter().GetResult();
            HostBrowserApp = null;
            HostBrowser = null;

            ControlledBrowserApp.CloseAsync().GetAwaiter().GetResult();
            ControlledBrowserApp = null;
        }

        private async Task SetupWebAsync()
        {
            ControlledBrowserApp = await TestConstants.GetNewBrowserType().LaunchBrowserAppAsync();
            HostBrowserApp = await TestConstants.GetNewBrowserType().LaunchBrowserAppAsync();
            HostBrowser = await TestConstants.GetNewBrowserType().ConnectAsync(HostBrowserApp.ConnectOptions);
        }
    }
}
