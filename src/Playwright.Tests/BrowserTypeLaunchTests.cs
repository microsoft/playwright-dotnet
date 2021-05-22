using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Transport;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
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
            await using var browser = await BrowserType.LaunchDefaultAsync();
            var page = await (await browser.NewContextAsync()).NewPageAsync();
            var neverResolves = page.EvaluateHandleAsync("() => new Promise(r => {})");
            await browser.CloseAsync();
            var exception = await Assert.ThrowsAsync<PlaywrightException>(() => neverResolves);
            Assert.Contains("Protocol error", exception.Message);

        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should throw if port option is passed")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowIfPortOptionIsPassed()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should throw if userDataDir option is passed")]
        [Fact(Skip = "This isn't supported in our language port.")]
        public void ShouldThrowIfUserDataDirOptionIsPassed()
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
            await Assert.ThrowsAnyAsync<PlaywrightException>(() => BrowserType.LaunchDefaultAsync(args: new[] { TestConstants.EmptyPage }));
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should reject if launched browser fails immediately")]
        [Fact(Skip = "Skipped in playwright")]
        public void ShouldRejectIfLaunchedBrowserFailsImmediately()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should reject if executable path is invalid")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectIfExecutablePathIsInvalid()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightException>(() => BrowserType.LaunchAsync(new BrowserTypeLaunchOptions { ExecutablePath = "random-invalid-path" }));

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
            await using var browser = await BrowserType.LaunchDefaultAsync();
            var context = await browser.NewContextAsync();
            var closeTask = new TaskCompletionSource<bool>();

            context.Close += (_, _) => closeTask.TrySetResult(true);

            await TaskUtils.WhenAll(browser.CloseAsync(), closeTask.Task);
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should be callable twice")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeCallableTwice()
        {
            await using var browser = await BrowserType.LaunchDefaultAsync();
            await TaskUtils.WhenAll(browser.CloseAsync(), browser.CloseAsync());
            await browser.CloseAsync();
        }

        /// <summary>
        /// PuppeteerSharp test. It's not in upstream
        /// </summary>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEnvironmentVariables()
        {
            var env = new Dictionary<string, string>
            {
                ["Foo"] = "Var"
            };

            await using var browser = await BrowserType.LaunchAsync(new BrowserTypeLaunchOptions { Env = env });
        }

        /// <summary>
        /// PuppeteerSharp test. It's not in upstream
        /// </summary>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkWithIgnoreDefaultArgs()
        {
            string[] args = new[]
            {
                "--remote-debugging-pipe",
                "--headless",
                "--hide-scrollbars",
                "--mute-audio",
                "--blink-settings=primaryHoverType=2,availableHoverTypes=2,primaryPointerType=4,availablePointerTypes=4"
            };

            await using var browser = await BrowserType.LaunchAsync(new BrowserTypeLaunchOptions { IgnoreAllDefaultArgs = true, Args = args });
        }
    }
}
