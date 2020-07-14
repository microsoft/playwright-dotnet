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
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class BrowserTypeLaunchTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public BrowserTypeLaunchTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launch</playwright-describe>
        ///<playwright-it>should reject all promises when browser is closed</playwright-it>
        [Retry]
        public async Task ShouldRejectAllPromisesWhenBrowserIsClosed()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page = await browser.NewPageAsync();
            var neverResolves = page.EvaluateHandleAsync("() => new Promise(r => {})");
            await browser.CloseAsync();
            var exception = await Assert.ThrowsAsync<TargetClosedException>(() => neverResolves);
            Assert.Contains("Protocol error", exception.Message);

        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launch</playwright-describe>
        ///<playwright-it>should throw if userDataDir option is passed</playwright-it>
        [Retry]
        public async Task ShouldThrowIfUserDataDirOptionIsPassed()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.UserDataDir = "random-invalid-path";

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => BrowserType.LaunchAsync(options));

            Assert.Contains("launchPersistentContext", exception.Message);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launch</playwright-describe>
        ///<playwright-it>should throw if page argument is passed</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldThrowIfPageArgumentIsPassed()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = new[] { "http://example.com" };

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => BrowserType.LaunchAsync(options));

            Assert.Contains("can not specify page", exception.Message);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launch</playwright-describe>
        ///<playwright-it>should reject if executable path is invalid</playwright-it>
        [Retry]
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

        /*
        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launch</playwright-describe>
        ///<playwright-it>should have default URL when launching browser</playwright-it>
        [Retry]
        public async Task ShouldHaveDefaultUrlWhenLaunchingBrowser()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var pages = (await browser.DefaultContext.GetPagesAsync()).Select(page => page.Url);
            Assert.Equal(new[] { TestConstants.AboutBlank }, pages);

        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>browserType.launch</playwright-describe>
        ///<playwright-it>should have custom URL when launching browser</playwright-it>
        [Retry]
        public async Task ShouldHaveCustomUrlWhenLaunchingBrowser()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = options.Args.Prepend(TestConstants.EmptyPage).ToArray();
            await using var browser = await BrowserType.LaunchAsync(options);

            var pages = await browser.DefaultContext.GetPagesAsync();
            Assert.Single(pages);
            if (pages[0].Url != TestConstants.EmptyPage)
            {
                await pages[0].WaitForNavigationAsync();
            }
            Assert.Equal(TestConstants.EmptyPage, pages[0].Url);
        }

        */
    }
}
