using System;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
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
            var crashEvent = new TaskCompletionSource<bool>();
            Page.Crash += (_, _) => crashEvent.TrySetResult(true);

            await CrashAsync(Page);
            await crashEvent.Task;
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should throw on any action after page crashes")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldThrowOnAnyActionAfterPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var crashEvent = new TaskCompletionSource<bool>();
            Page.Crash += (_, _) => crashEvent.TrySetResult(true);

            await CrashAsync(Page);
            await crashEvent.Task;
            var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => Page.EvaluateAsync("() => {}"));
            Assert.Contains("crash", exception.Message);
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should cancel waitForEvent when page crashes")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldCancelWaitForEventWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var responseTask = Page.WaitForResponseAsync("**/*");
            await CrashAsync(Page);
            var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => responseTask);
            Assert.Contains("Page crashed", exception.Message);
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should cancel navigation when page crashes")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldCancelNavigationWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            Server.SetRoute("/one-style.css", _ => Task.Delay(2000));
            var task = Page.GotoAsync(TestConstants.ServerUrl + "/one-style.html");
            await Page.WaitForNavigationAsync(new PageWaitForNavigationOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            await CrashAsync(Page);
            var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => task);
            Assert.Contains("Navigation failed because page crashed", exception.Message);
        }

        // We skip all browser because crash uses internals.
        [PlaywrightTest("page-event-crash.spec.ts", "should be able to close context when page crashes")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
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
