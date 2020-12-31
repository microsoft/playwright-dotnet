using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using PlaywrightSharp.Transport;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>browsertype-launch.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserTypeLaunchTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public BrowserTypeLaunchTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsertype-launch.spec.ts</playwright-file>
        ///<playwright-it>should reject all promises when browser is closed</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectAllPromisesWhenBrowserIsClosed()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page = await (await browser.NewContextAsync()).NewPageAsync();
            var neverResolves = page.EvaluateHandleAsync("() => new Promise(r => {})");
            await browser.CloseAsync();
            var exception = await Assert.ThrowsAsync<TargetClosedException>(() => neverResolves);
            Assert.Contains("Protocol error", exception.Message);

        }

        ///<playwright-file>browsertype-launch.spec.ts</playwright-file>
        ///<playwright-it>should throw if userDataDir option is passed</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowIfUserDataDirOptionIsPassed()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.UserDataDir = "random-invalid-path";

            var exception = await Assert.ThrowsAsync<ArgumentException>(() => BrowserType.LaunchAsync(options));

            Assert.Contains("LaunchPersistentContextAsync", exception.Message);
        }

        ///<playwright-file>browsertype-launch.spec.ts</playwright-file>
        ///<playwright-it>should throw if port option is passed</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowIfPortOptionIsPassed()
        {
        }

        ///<playwright-file>browsertype-launch.spec.ts</playwright-file>
        ///<playwright-it>should throw if port option is passed for persistent context</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowIfPortOptionIsPassedForPersistenContext()
        {
        }

        /// <playwright-file>defaultbrowsercontext-2.spec.js</playwright-file>
        /// <playwright-it>should throw if page argument is passed</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldThrowIfPageArgumentIsPassed()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = new[] { TestConstants.EmptyPage };

            await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => BrowserType.LaunchAsync(options));
        }

        ///<playwright-file>browsertype-launch.spec.ts</playwright-file>
        ///<playwright-it>should reject if launched browser fails immediately</playwright-it>
        [Fact(Skip = "Skipped in playwright")]
        public void ShouldRejectIfLaunchedBrowserFailsImmediately()
        {
        }

        /// <summary>
        /// Should curante the message coming from Playwright
        /// </summary>
        /// <returns></returns>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCurateTheLaunchError()
        {
            // Set an invalid location
            using var playwright = await PlaywrightSharp.Playwright.CreateAsync(browsersPath: Path.Combine(typeof(PlaywrightSharp.Playwright).Assembly.Location));
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => playwright[TestConstants.Product].LaunchAsync());

            Assert.Contains("Failed to launch", exception.Message);
            Assert.Contains("Try re-installing the browsers running `playwright-cli.exe install` in windows or `playwright-cli install` in MacOS or Linux.", exception.Message);
            Assert.DoesNotContain("npm install playwright", exception.Message);
            Assert.Contains("pass `debug: \"pw:api\"` to LaunchAsync", exception.Message);
            Environment.SetEnvironmentVariable(EnvironmentVariables.BrowsersPathEnvironmentVariable, null);
        }

        ///<playwright-file>browsertype-launch.spec.ts</playwright-file>
        ///<playwright-it>should reject if executable path is invalid</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectIfExecutablePathIsInvalid()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.ExecutablePath = "random-invalid-path";

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => BrowserType.LaunchAsync(options));

            Assert.Contains("Failed to launch", exception.Message);
            Assert.Contains("pass `debug: \"pw:api\"` to LaunchAsync", exception.Message);
        }

        ///<playwright-file>browsertype-launch.spec.ts</playwright-file>
        ///<playwright-it>should handle timeout</playwright-it>
        [Fact(Skip = "We ignore hook tests")]
        public void ShouldHandleTimeout()
        {
        }

        ///<playwright-file>browsertype-launch.spec.ts</playwright-file>
        ///<playwright-it>should report launch log</playwright-it>
        [Fact(Skip = "We ignore hook tests")]
        public void ShouldReportLaunchLog()
        {
        }

        ///<playwright-file>browsertype-launch.spec.ts</playwright-file>
        ///<playwright-it>should accept objects as options</playwright-it>
        [Fact(Skip = "We don't need to test this")]
        public void ShouldAcceptObjectsAsOptions()
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-it>should fire close event for all contexts</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireCloseEventForAllContexts()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var context = await browser.NewContextAsync();
            var closeTask = new TaskCompletionSource<bool>();

            context.Close += (sender, e) => closeTask.TrySetResult(true);

            await TaskUtils.WhenAll(browser.CloseAsync(), closeTask.Task);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-it>should be callable twice</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeCallableTwice()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            await TaskUtils.WhenAll(browser.CloseAsync(), browser.CloseAsync());
            await browser.CloseAsync();
        }

        /// <summary>
        /// PuppeteerSharp test. It's not in upstream
        /// </summary>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEnvironmentVariables()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Env = new Dictionary<string, string>
            {
                ["Foo"] = "Var"
            };

            await using var browser = await BrowserType.LaunchAsync(options);
        }
    }
}
