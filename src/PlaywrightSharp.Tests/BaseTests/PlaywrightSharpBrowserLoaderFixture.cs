using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// This [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class setup a single browser instance for tests.
    /// </summary>
    public class PlaywrightSharpBrowserLoaderFixture : PlaywrightSharpDriverLoaderFixture
    {
        internal static IBrowser Browser { get; private set; }

        /// <inheritdoc />
        public PlaywrightSharpBrowserLoaderFixture()
        {
            SetupBrowserAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            Browser = null;

            base.Dispose();
        }

        private async Task SetupBrowserAsync()
        {
            Browser = await Playwright[TestConstants.Product].LaunchAsync(TestConstants.GetDefaultBrowserOptions());
        }
    }
}
