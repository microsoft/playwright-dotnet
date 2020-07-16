using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// This [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class setup a single browser instance for tests.
    /// </summary>
    public class PlaywrightSharpDriverLoaderFixture : IDisposable
    {
        internal static IPlaywright Playwright { get; private set; }

        /// <inheritdoc />
        public PlaywrightSharpDriverLoaderFixture()
        {
            SetupBrowserAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            Playwright.Dispose();
            Playwright = null;
        }

        private async Task SetupBrowserAsync()
        {
            Playwright = await PlaywrightSharp.Playwright.CreateAsync();
        }
    }
}
