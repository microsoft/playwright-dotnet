using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>browsertype-launch.spec.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class BrowserTypeLaunchTests : PlaywrightTestEx
    {
        [PlaywrightTest("browsertype-launch.spec.ts", "should reject all promises when browser is closed")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectAllPromisesWhenBrowserIsClosed()
        {
            await using var browser = await BrowserType.LaunchAsync();
            var page = await (await browser.NewContextAsync()).NewPageAsync();
            var neverResolves = page.EvaluateHandleAsync("() => new Promise(r => {})");
            await browser.CloseAsync();
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => neverResolves);
            StringAssert.Contains("Protocol error", exception.Message);

        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should throw if port option is passed")]
        [Test, Ignore("We don't need this test")]
        public void ShouldThrowIfPortOptionIsPassed()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should throw if userDataDir option is passed")]
        [Test, Ignore("This isn't supported in our language port.")]
        public void ShouldThrowIfUserDataDirOptionIsPassed()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should throw if port option is passed for persistent context")]
        [Test, Ignore("We don't need this test")]
        public void ShouldThrowIfPortOptionIsPassedForPersistentContext()
        {
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should throw if page argument is passed")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public Task ShouldThrowIfPageArgumentIsPassed()
        {
            var args = new[] { Server.EmptyPage };
            return PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => BrowserType.LaunchAsync(new() { Args = args }));
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should reject if launched browser fails immediately")]
        [Test, Ignore("Skipped in playwright")]
        public void ShouldRejectIfLaunchedBrowserFailsImmediately()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should reject if executable path is invalid")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectIfExecutablePathIsInvalid()
        {
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => BrowserType.LaunchAsync(new() { ExecutablePath = "random-invalid-path" }));

            StringAssert.Contains("Failed to launch", exception.Message);
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should handle timeout")]
        [Test, Ignore("We ignore hook tests")]
        public void ShouldHandleTimeout()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should report launch log")]
        [Test, Ignore("We ignore hook tests")]
        public void ShouldReportLaunchLog()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should accept objects as options")]
        [Test, Ignore("We don't need to test this")]
        public void ShouldAcceptObjectsAsOptions()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should fire close event for all contexts")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireCloseEventForAllContexts()
        {
            await using var browser = await BrowserType.LaunchAsync();
            var context = await browser.NewContextAsync();
            var closeTask = new TaskCompletionSource<bool>();

            context.Close += (_, _) => closeTask.TrySetResult(true);

            await TaskUtils.WhenAll(browser.CloseAsync(), closeTask.Task);
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should be callable twice")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeCallableTwice()
        {
            await using var browser = await BrowserType.LaunchAsync();
            await TaskUtils.WhenAll(browser.CloseAsync(), browser.CloseAsync());
            await browser.CloseAsync();
        }

        /// <summary>
        /// PuppeteerSharp test. It's not in upstream
        /// </summary>
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEnvironmentVariables()
        {
            var env = new Dictionary<string, string>
            {
                ["Foo"] = "Var"
            };

            await using var browser = await BrowserType.LaunchAsync(new() { Env = env });
        }

        /// <summary>
        /// PuppeteerSharp test. It's not in upstream
        /// </summary>
        [Test, SkipBrowserAndPlatform(skipFirefox: true, skipWebkit: true)]
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

            await using var browser = await BrowserType.LaunchAsync(new() { IgnoreAllDefaultArgs = true, Args = args });
        }
    }
}
