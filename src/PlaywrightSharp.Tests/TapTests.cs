using PlaywrightSharp.Tests.BaseTests;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    /// <playwright-file>tap.specs.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public sealed class TapTests : PlaywrightSharpPageBaseTest
    {

        /// <inheritdoc/>
        public TapTests(ITestOutputHelper output) :
                base(output)
        {
        }

        /// <inheritdoc/>
        public override async Task InitializeAsync()
        {
            Context = await Browser.NewContextAsync();
            Page = await Browser.NewPageAsync(hasTouch: true);
        }

        /// <playwright-file>tap.specs.ts</playwright-file>
        /// <playwright-it>should send all of the correct events</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSendAllOfTheCorrectEvents()
        {
            await Page.SetContentAsync(
                @"<div id=""a"" style=""background: lightblue; width: 50px; height: 50px"">a</div>" +
                @"<div id=""b"" style=""background: pink; width: 50px; height: 50px"">b</div>");

            await Page.TapAsync("#a");
            var handle = await TrackEvents("#b");
            await Page.TapAsync("#b");

            string[] result = await handle.GetJsonValueAsync<string[]>();

            Assert.Equal(result, new string[]
            {
                "pointerover",  "pointerenter",
                "pointerdown",  "touchstart",
                "pointerup",    "pointerout",
                "pointerleave", "touchend",
                "mouseover",    "mouseenter",
                "mousemove",    "mousedown",
                "mouseup",      "click",
            });
        }

        private async Task<IJSHandle> TrackEvents(string selector)
        {
            var target = await Page.QuerySelectorAsync(selector);
            string jsFunc = @"(target) => {
                const events = [];
                for(const event of [
                    'mousedown', 'mouseenter', 'mouseleave', 'mousemove', 'mouseout', 'mouseover', 'mouseup', 'click',
                    'pointercancel', 'pointerdown', 'pointerenter', 'pointerleave', 'pointermove', 'pointerout', 'pointerover', 'pointerup',
                    'touchstart', 'touchend', 'touchmove', 'touchcancel',])
                        target.addEventListener(event, () => events.push(event), false);
                    return events;
                }";

            return await target.EvaluateHandleAsync(jsFunc);
        }

        /// <playwright-file>tap.specs.ts</playwright-file>
        /// <playwright-it>should not send mouse events touchstart is canceled</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout, Skip = "This test is not yet implemented.")]
        public async Task ShouldNotSendMouseEventsTouchstartIsCanceled()
        {
        }

        /// <playwright-file>tap.specs.ts</playwright-file>
        /// <playwright-it>should not send mouse events when touchend is canceled</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout, Skip = "This test is not yet implemented.")]
        public async Task ShouldNotSendMouseEventsWhenTouchendIsCanceled()
        {
        }

        /// <playwright-file>tap.specs.ts</playwright-file>
        /// <playwright-it>should wait for a navigation caused by a tap</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout, Skip = "This test is not yet implemented.")]
        public async Task ShouldWaitForANavigationCausedByATap()
        {
        }

        /// <playwright-file>tap.specs.ts</playwright-file>
        /// <playwright-it>should work with modifiers</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout, Skip = "This test is not yet implemented.")]
        public async Task ShouldWorkWithModifiers()
        {
        }

        /// <playwright-file>tap.specs.ts</playwright-file>
        /// <playwright-it>should send well formed touch points</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout, Skip = "This test is not yet implemented.")]
        public async Task ShouldSendWellFormedTouchPoints()
        {
        }

        /// <playwright-file>tap.specs.ts</playwright-file>
        /// <playwright-it>should wait until an element is visible to tap it</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout, Skip = "This test is not yet implemented.")]
        public async Task ShouldWaitUntilAnElementIsVisibleToTapIt()
        {
        }
    }
}
