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
            try
            {
                Playwright = await PlaywrightSharp.Playwright.CreateAsync(TestConstants.LoggerFactory, debug: "pw*");
                Browser = await Playwright[TestConstants.Product].LaunchDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new Exception("Launch failed", ex);
            }
        }

        internal static async Task ShutDownAsync()
        {
            try
            {
                await Browser.CloseAsync();
                Playwright.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new Exception("Shutdown failed", ex);
            }
        }

        internal static async Task RestartAsync()
        {
            await Browser.CloseAsync();
            await LaunchBrowserAsync();
        }
    }
}
