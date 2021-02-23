using System;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventCrashTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventCrashTests(ITestOutputHelper output) : base(output)
        {
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should emit crash event when page crashes")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldEmitCrashEventWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var crashTask = Page.WaitForEventAsync(PageEvent.Crash);
            await CrashAsync(Page);
            await crashTask.WithTimeout(TestConstants.DefaultTaskTimeout);
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should throw on any action after page crashes")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldThrowOnAnyActionAfterPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var crashTask = Page.WaitForEventAsync(PageEvent.Crash);
            await CrashAsync(Page);
            await crashTask.WithTimeout(TestConstants.DefaultTaskTimeout);
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => {}"));
            Assert.Contains("crash", exception.Message);
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should cancel waitForEvent when page crashes")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldCancelWaitForEventWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var responseTask = Page.WaitForEventAsync(PageEvent.Response);
            await CrashAsync(Page);
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => responseTask.WithTimeout(TestConstants.DefaultTaskTimeout));
            Assert.Contains("Page crashed", exception.Message);
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should cancel navigation when page crashes")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldCancelNavigationWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            Server.SetRoute("/one-style.css", _ => Task.Delay(2000));
            var task = Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            await Page.WaitForNavigationAsync(LifecycleEvent.DOMContentLoaded);

            await CrashAsync(Page);
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => task);
            Assert.Contains("Navigation failed because page crashed", exception.Message);
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should be able to close context when page crashes")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldBeAbleToCloseContextWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var crashTask = Page.WaitForEventAsync(PageEvent.Crash);
            await CrashAsync(Page);
            await crashTask.WithTimeout(TestConstants.DefaultTaskTimeout);
            await Page.Context.CloseAsync();
        }

        private async Task CrashAsync(IPage page)
        {
            try
            {
                await page.GoToAsync("chrome://crash");
            }
            catch
            {
            }
        }
    }
}
