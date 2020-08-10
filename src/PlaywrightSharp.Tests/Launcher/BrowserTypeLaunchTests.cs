using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>browserType.launch</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserTypeLaunchTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public BrowserTypeLaunchTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launch</playwright-describe>
        ///<playwright-it>should reject all promises when browser is closed</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRejectAllPromisesWhenBrowserIsClosed()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page = await (await browser.NewContextAsync()).NewPageAsync();
            var neverResolves = page.EvaluateHandleAsync("() => new Promise(r => {})");
            await browser.CloseAsync();
            var exception = await Assert.ThrowsAsync<TargetClosedException>(() => neverResolves);
            Assert.Contains("Protocol error", exception.Message);

        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launch</playwright-describe>
        ///<playwright-it>should throw if userDataDir option is passed</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldThrowIfUserDataDirOptionIsPassed()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.UserDataDir = "random-invalid-path";

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => BrowserType.LaunchAsync(options));

            Assert.Contains("launchPersistentContext", exception.Message);
        }


        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launch</playwright-describe>
        ///<playwright-it>should reject if executable path is invalid</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRejectIfExecutablePathIsInvalid()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.ExecutablePath = "random-invalid-path";

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => BrowserType.LaunchAsync(options));

            Assert.Contains("Failed to launch", exception.Message);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launch</playwright-describe>
        ///<playwright-it>should handle timeout</playwright-it>
        [Fact(Skip = "We ignore hook tests")]
        public void ShouldHandleTimeout()
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launch</playwright-describe>
        ///<playwright-it>should report launch log</playwright-it>
        [Fact(Skip = "We ignore hook tests")]
        public void ShouldReportLaunchLog()
        {
        }
    }
}
