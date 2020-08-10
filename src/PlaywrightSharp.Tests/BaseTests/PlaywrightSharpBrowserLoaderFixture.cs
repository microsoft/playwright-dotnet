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

        public async Task InitializeAsync()
        {
            Playwright = await PlaywrightSharp.Playwright.CreateAsync(TestConstants.LoggerFactory);
            Browser = await Playwright[TestConstants.Product].LaunchAsync(TestConstants.GetDefaultBrowserOptions());
        }

        public async Task DisposeAsync()
        {
            await Browser.CloseAsync();
            Playwright.Dispose();
        }
    }
}
