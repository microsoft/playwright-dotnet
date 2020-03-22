using System;
using System.IO;
using System.Threading.Tasks;
using Xunit.Abstractions;
using PlaywrightSharp.Chromium;
using Xunit;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// Based on <see cref="PlaywrightSharpBaseTest"/>, this base class also creates a new Browser
    /// </summary>
    public class PlaywrightSharpBrowserBaseTest : PlaywrightSharpBaseTest, IAsyncLifetime
    {
        internal IBrowser Browser { get; set; }

        internal LaunchOptions DefaultOptions { get; set; }

        internal PlaywrightSharpBrowserBaseTest(ITestOutputHelper output) : base(output)
        {
            BaseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "workspace");
            var dirInfo = new DirectoryInfo(BaseDirectory);

            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
        }

        internal Task<IBrowserContext> NewContextAsync(BrowserContextOptions options = null) => Browser.NewContextAsync(options);

        internal async Task<IPage> NewPageAsync(BrowserContextOptions options = null)
        {
            var context = await NewContextAsync(options);
            return await context.NewPageAsync();
        }

        /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
        public virtual async Task InitializeAsync()
            => Browser = await Playwright.LaunchAsync(DefaultOptions ?? TestConstants.GetDefaultBrowserOptions());

        /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
        public virtual Task DisposeAsync() => Browser.CloseAsync();
    }
}
