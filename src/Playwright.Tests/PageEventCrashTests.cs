using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageEventCrashTests : PageTestEx
    {
        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should emit crash event when page crashes")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldEmitCrashEventWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var crashEvent = new TaskCompletionSource<bool>();
            Page.Crash += (_, _) => crashEvent.TrySetResult(true);

            await CrashAsync(Page);
            await crashEvent.Task;
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should throw on any action after page crashes")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldThrowOnAnyActionAfterPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var crashEvent = new TaskCompletionSource<bool>();
            Page.Crash += (_, _) => crashEvent.TrySetResult(true);

            await CrashAsync(Page);
            await crashEvent.Task;
            var exception = Assert.ThrowsAsync<PlaywrightException>(async () => await Page.EvaluateAsync("() => {}"));
            StringAssert.Contains("crash", exception.Message);
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should cancel waitForEvent when page crashes")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldCancelWaitForEventWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var responseTask = Page.WaitForResponseAsync("**/*");
            await CrashAsync(Page);
            var exception = Assert.ThrowsAsync<PlaywrightException>(async () => await responseTask);
            StringAssert.Contains("Page crashed", exception.Message);
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should cancel navigation when page crashes")]
        [Ignore("Not relevant downstream")]
        public void ShouldCancelNavigationWhenPageCrashes()
        {
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should be able to close context when page crashes")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldBeAbleToCloseContextWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");

            var crashEvent = new TaskCompletionSource<bool>();
            Page.Crash += (_, dialog) => crashEvent.TrySetResult(true);

            await CrashAsync(Page);
            await crashEvent.Task;
            await Page.Context.CloseAsync();
        }

        private async Task CrashAsync(IPage page)
        {
            try
            {
                await page.GotoAsync("chrome://crash");
            }
            catch
            {
            }
        }
    }
}
