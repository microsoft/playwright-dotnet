/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>browsertype-launch.spec.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class BrowserTypeLaunchTests : PlaywrightTestEx
    {
        [PlaywrightTest("browsertype-launch.spec.ts", "should reject all promises when browser is closed")]
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
        [Ignore("We don't need this test")]
        public void ShouldThrowIfPortOptionIsPassed()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should throw if userDataDir option is passed")]
        [Ignore("This isn't supported in our language port.")]
        public void ShouldThrowIfUserDataDirOptionIsPassed()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should throw if port option is passed for persistent context")]
        [Ignore("We don't need this test")]
        public void ShouldThrowIfPortOptionIsPassedForPersistentContext()
        {
        }

        [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should throw if page argument is passed")]
        [Skip(SkipAttribute.Targets.Firefox)]
        public Task ShouldThrowIfPageArgumentIsPassed()
        {
            var args = new[] { Server.EmptyPage };
            return PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => BrowserType.LaunchAsync(new() { Args = args }));
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should reject if launched browser fails immediately")]
        [Ignore("Skipped in playwright")]
        public void ShouldRejectIfLaunchedBrowserFailsImmediately()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should reject if executable path is invalid")]
        public async Task ShouldRejectIfExecutablePathIsInvalid()
        {
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => BrowserType.LaunchAsync(new() { ExecutablePath = "random-invalid-path" }));

            StringAssert.Contains("Failed to launch", exception.Message);
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should handle timeout")]
        [Ignore("We ignore hook tests")]
        public void ShouldHandleTimeout()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should report launch log")]
        [Ignore("We ignore hook tests")]
        public void ShouldReportLaunchLog()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should accept objects as options")]
        [Ignore("We don't need to test this")]
        public void ShouldAcceptObjectsAsOptions()
        {
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should fire close event for all contexts")]
        public async Task ShouldFireCloseEventForAllContexts()
        {
            await using var browser = await BrowserType.LaunchAsync();
            var context = await browser.NewContextAsync();
            var closeTask = new TaskCompletionSource<bool>();

            context.Close += (_, _) => closeTask.TrySetResult(true);

            await TaskUtils.WhenAll(browser.CloseAsync(), closeTask.Task);
        }

        [PlaywrightTest("browsertype-launch.spec.ts", "should be callable twice")]
        public async Task ShouldBeCallableTwice()
        {
            await using var browser = await BrowserType.LaunchAsync();
            await TaskUtils.WhenAll(browser.CloseAsync(), browser.CloseAsync());
            await browser.CloseAsync();
        }

        /// <summary>
        /// PuppeteerSharp test. It's not in upstream
        /// </summary>
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
        [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
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
