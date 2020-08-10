using System;
using System.Threading.Tasks;
using Xunit;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// This class setup a single browser instance for tests.
    /// </summary>
    public class PlaywrightSharpBrowserLoaderFixture : IAsyncLifetime
    {
        internal static IPlaywright Playwright { get; private set; }

        internal static IBrowser Browser { get; private set; }

        /// <inheritdoc/>
        public Task InitializeAsync() => LaunchBrowserAsync();

        /// <inheritdoc/>
        public Task DisposeAsync() => ShutDownAsync();


        private static async Task LaunchBrowserAsync()
        {
            Playwright = await PlaywrightSharp.Playwright.CreateAsync(TestConstants.LoggerFactory);
            Browser = await Playwright[TestConstants.Product].LaunchAsync(TestConstants.GetDefaultBrowserOptions());
        }

        internal static async Task ShutDownAsync()
        {
            await Browser.CloseAsync();
            Playwright.Dispose();
        }

        internal static async Task RestartAsync()
        {
            await Browser.CloseAsync();
            await LaunchBrowserAsync();
        }
    }
}
