using System;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.Crash</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventsPageCrashTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventsPageCrashTests(ITestOutputHelper output) : base(output)
        {
        }

        // We skip all browser because crash uses internals.
        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Crash</playwright-describe>
        ///<playwright-it>should emit crash event when page crashes</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldEmitCrashEventWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var crashTask = Page.WaitForEventAsync(PageEvent.Crash);
            await CrashAsync(Page);
            await crashTask.WithTimeout();
        }

        // We skip all browser because crash uses internals.
        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Crash</playwright-describe>
        ///<playwright-it>should throw on any action after page crashes</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldThrowOnAnyActionAfterPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var crashTask = Page.WaitForEventAsync(PageEvent.Crash);
            await CrashAsync(Page);
            await crashTask.WithTimeout();
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.EvaluateAsync("() => {}"));
            Assert.Contains("crash", exception.Message);
        }

        // We skip all browser because crash uses internals.
        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Crash</playwright-describe>
        ///<playwright-it>should cancel waitForEvent when page crashes</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldCancelWaitForEventWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var responseTask = Page.WaitForEventAsync(PageEvent.Response);
            await CrashAsync(Page);
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => responseTask.WithTimeout());
            Assert.Contains("Page crashed", exception.Message);
        }

        // We skip all browser because crash uses internals.
        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Crash</playwright-describe>
        ///<playwright-it>should cancel navigation when page crashes</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldCancelNavigationWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            Server.SetRoute("/one-style.css", context => Task.Delay(2000));
            var task = Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            await Page.WaitForNavigationAsync(LifecycleEvent.DOMContentLoaded);

            await CrashAsync(Page);
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => task);
            Assert.Contains("Navigation failed because page crashed", exception.Message);
        }

        // We skip all browser because crash uses internals.
        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Crash</playwright-describe>
        ///<playwright-it>should be able to close context when page crashes</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldBeAbleToCloseContextWhenPageCrashes()
        {
            await Page.SetContentAsync("<div>This page should crash</div>");
            var crashTask = Page.WaitForEventAsync(PageEvent.Crash);
            await CrashAsync(Page);
            await crashTask.WithTimeout();
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
