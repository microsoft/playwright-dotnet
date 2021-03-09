using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Xunit;
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

        [PlaywrightTest("browsertype-launch.spec.ts", "should reject all promises when browser is closed")]
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

        [PlaywrightTest("browsertype-launch.spec.ts", "should throw if userDataDir option is passed")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowIfUserDataDirOptionIsPassed()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.UserDataDir = "random-invalid-path";

            var exception = await Assert.ThrowsAsync<ArgumentException>(() => BrowserType.LaunchAsync(options));

            Assert.Contains("LaunchPersistentContextAsync", exception.Message);
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should throw if port option is passed")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowIfPortOptionIsPassed()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should throw if port option is passed for persistent context")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowIfPortOptionIsPassedForPersistenContext()
        {
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should throw if page argument is passed")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldThrowIfPageArgumentIsPassed()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = new[] { TestConstants.EmptyPage };

            await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => BrowserType.LaunchAsync(options));
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should reject if launched browser fails immediately")]
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
            Assert.Contains("Try re-installing the browsers running `playwright.cmd install` in windows or `./playwright.sh install` in MacOS or Linux.", exception.Message);
            Assert.DoesNotContain("npm install playwright", exception.Message);
            Environment.SetEnvironmentVariable(EnvironmentVariables.BrowsersPathEnvironmentVariable, null);
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should reject if executable path is invalid")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectIfExecutablePathIsInvalid()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.ExecutablePath = "random-invalid-path";

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => BrowserType.LaunchAsync(options));

            Assert.Contains("Failed to launch", exception.Message);
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should handle timeout")]
        [Fact(Skip = "We ignore hook tests")]
        public void ShouldHandleTimeout()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should report launch log")]
        [Fact(Skip = "We ignore hook tests")]
        public void ShouldReportLaunchLog()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should accept objects as options")]
        [Fact(Skip = "We don't need to test this")]
        public void ShouldAcceptObjectsAsOptions()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should fire close event for all contexts")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireCloseEventForAllContexts()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var context = await browser.NewContextAsync();
            var closeTask = new TaskCompletionSource<bool>();

            context.Close += (_, _) => closeTask.TrySetResult(true);

            await TaskUtils.WhenAll(browser.CloseAsync(), closeTask.Task);
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should be callable twice")]
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

        /// <summary>
        /// PuppeteerSharp test. It's not in upstream
        /// </summary>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldValidateFirefoxUserPrefs()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.FirefoxUserPrefs = new Dictionary<string, object>
            {
                ["Foo"] = "Var"
            };

            await Assert.ThrowsAsync<ArgumentException>(() => BrowserType.LaunchPersistentContextAsync("foo", options));
        }

        /// <summary>
        /// PuppeteerSharp test. It's not in upstream
        /// </summary>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkWithIgnoreDefaultArgs()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.IgnoreAllDefaultArgs = true;
            options.Args = new[]
            {
                "--remote-debugging-pipe",
                "--headless",
                "--hide-scrollbars",
                "--mute-audio",
                "--blink-settings=primaryHoverType=2,availableHoverTypes=2,primaryPointerType=4,availablePointerTypes=4"
            };

            await using var browser = await BrowserType.LaunchAsync(options);
        }
    }
}
