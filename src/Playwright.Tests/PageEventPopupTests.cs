using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventPopupTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventPopupTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            var popup = popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with window features")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithWindowFeatures()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync<string>("() => window.open('about:blank', 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=780,height=200,top=0,left=0')")
            );
            var popup = popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should emit for immediately closed popups")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmitForImmediatelyClosedPopups()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync<string>(@"() => {
                    const win = window.open('about:blank');
                    win.close();
                }")
            );
            Assert.NotNull(popupTask.Result);
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should emit for immediately closed popups")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmitForImmediatelyClosedPopupsWithLocation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync<string>(@"() => {
                    const win = window.open(window.location.href);
                    win.close();
                }")
            );
            Assert.NotNull(popupTask.Result);
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should be able to capture alert")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToCaptureAlert()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);
            var evaluateTask = Page.EvaluateAsync<string>(@"() => {
                const win = window.open('');
                win.alert('hello');
            }");

            var popup = await popupTask;
            var dialog = await popup.WaitForEventAsync(PageEvent.Dialog);

            Assert.Equal("hello", dialog.Message);
            await dialog.DismissAsync();
            await evaluateTask;
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with empty url")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEmptyUrl()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open('')")
            );
            var popup = popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with noopener and no url")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoopenerAndNoUrl()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open(undefined, null, 'noopener')")
            );
            var popup = popupTask.Result;
            Assert.Equal("about:blank", popup.Url.Split('#')[0]);
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with noopener and about:blank")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoopenerAndAboutBlank()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open('about:blank', null, 'noopener')")
            );
            var popup = popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with noopener and url")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoopenerAndUrl()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("url => window.open(url, null, 'noopener')", TestConstants.EmptyPage)
            );
            var popup = popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with clicking target=_blank")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithClickingTargetBlank()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a target=_blank rel=\"opener\" href=\"/one-style.html\">yo</a>");
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup).ContinueWith(async task =>
            {
                var popup = task.Result;
                await popup.WaitForLoadStateAsync();
                return popup;
            });
            await TaskUtils.WhenAll(
                popupTask,
                Page.ClickAsync("a")
            );

            var popup = await popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with fake-clicking target=_blank and rel=noopener")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithFakeClickingTargetBlankAndRelNoopener()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup).ContinueWith(async task =>
            {
                var popup = task.Result;
                await popup.WaitForLoadStateAsync();
                return popup;
            });
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvalOnSelectorAsync("a", "a => a.click()")
            );
            var popup = await popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with clicking target=_blank and rel=noopener")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithClickingTargetBlankAndRelNoopener()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup).ContinueWith(async task =>
            {
                var popup = task.Result;
                await popup.WaitForLoadStateAsync();
                return popup;
            });
            await TaskUtils.WhenAll(
                popupTask,
                Page.ClickAsync("a")
            );
            var popup = await popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should not treat navigations as new popups")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldNotTreatNavigationsAsNewPopups()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup).ContinueWith(async task =>
            {
                var popup = task.Result;
                await popup.WaitForLoadStateAsync();
                return popup;
            });
            await TaskUtils.WhenAll(
                popupTask,
                Page.ClickAsync("a")
            );
            var popup = await popupTask.Result;
            bool badSecondPopup = false;
            Page.Popup += (_, _) => badSecondPopup = true;
            await popup.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            Assert.False(badSecondPopup);
        }
    }
}
