using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.launch</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class LaunchTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public LaunchTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch</playwright-describe>
        ///<playwright-it>should reject all promises when browser is closed</playwright-it>
        [Retry]
        public async Task ShouldRejectAllPromisesWhenBrowserIsClosed()
        {
            using var browser = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page = await browser.DefaultContext.NewPageAsync();
            var neverResolves = page.EvaluateHandleAsync("() => new Promise(r => {})");
            await browser.CloseAsync();
            var exception = await Assert.ThrowsAsync<TargetClosedException>(() => neverResolves);
            Assert.Contains("Protocol error", exception.Message);

        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch</playwright-describe>
        ///<playwright-it>should reject if executable path is invalid</playwright-it>
        [Retry]
        public async Task ShouldRejectIfExecutablePathIsInvalid()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.ExecutablePath = "random-invalid-path";

            var exception = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            {
                return Playwright.LaunchAsync(options);
            });

            Assert.Contains("Failed to launch", exception.Message);
            Assert.Equal(options.ExecutablePath, exception.FileName);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch</playwright-describe>
        ///<playwright-it>should have default URL when launching browser</playwright-it>
        [Retry]
        public async Task ShouldHaveDefaultUrlWhenLaunchingBrowser()
        {
            using var browser = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var pages = (await browser.DefaultContext.GetPagesAsync()).Select(page => page.Url);
            Assert.Equal(new[] { TestConstants.AboutBlank }, pages);

        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch</playwright-describe>
        ///<playwright-it>should have custom URL when launching browser</playwright-it>
        [Retry]
        public async Task ShouldHaveCustomUrlWhenLaunchingBrowser()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = options.Args.Prepend(TestConstants.EmptyPage).ToArray();
            using var browser = await Playwright.LaunchAsync(options);

            var pages = await browser.DefaultContext.GetPagesAsync();
            Assert.Single(pages);
            if (pages[0].Url != TestConstants.EmptyPage)
            {
                await pages[0].WaitForNavigationAsync();
            }
            Assert.Equal(TestConstants.EmptyPage, pages[0].Url);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.launch</playwright-describe>
        ///<playwright-it>should return child_process instance</playwright-it>
        [Retry]
        public async Task ShouldReturnChildProcessInstance()
        {
            using var browserApp = await Playwright.LaunchBrowserAppAsync(TestConstants.GetDefaultBrowserOptions());
            Assert.True(browserApp.Process.Id > 0);
        }
    }
}
