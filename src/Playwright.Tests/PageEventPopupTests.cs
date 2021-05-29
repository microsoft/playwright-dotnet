using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageEventPopupTests : PageTestEx
    {
        [PlaywrightTest("page-event-popup.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var popupTask = Page.WaitForPopupAsync();
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            var popup = popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with window features")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithWindowFeatures()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForPopupAsync();
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync<string>("() => window.open('about:blank', 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=780,height=200,top=0,left=0')")
            );
            var popup = popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should emit for immediately closed popups")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmitForImmediatelyClosedPopups()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForPopupAsync();
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmitForImmediatelyClosedPopupsWithLocation()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var popup = await Page.RunAndWaitForPopupAsync(async () =>
            {
                await Page.EvaluateAsync<string>(@"() => {
                    const win = window.open(window.location.href);
                    win.close();
                }");
            });
            Assert.NotNull(popup);
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should be able to capture alert")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public void ShouldBeAbleToCaptureAlert()
        {
            // Too fancy.
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with empty url")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEmptyUrl()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForPopupAsync();
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open('')")
            );
            var popup = popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with noopener and no url")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoopenerAndNoUrl()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForPopupAsync();
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open(undefined, null, 'noopener')")
            );
            var popup = popupTask.Result;
            Assert.AreEqual("about:blank", popup.Url.Split('#')[0]);
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with noopener and about:blank")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoopenerAndAboutBlank()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForPopupAsync();
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open('about:blank', null, 'noopener')")
            );
            var popup = popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with noopener and url")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoopenerAndUrl()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForPopupAsync();
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("url => window.open(url, null, 'noopener')", TestConstants.EmptyPage)
            );
            var popup = popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        [PlaywrightTest("page-event-popup.spec.ts", "should work with clicking target=_blank")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldWorkWithClickingTargetBlank()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a target=_blank rel=\"opener\" href=\"/one-style.html\">yo</a>");
            var popupTask = Page.WaitForPopupAsync().ContinueWith(async task =>
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
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldWorkWithFakeClickingTargetBlankAndRelNoopener()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
            var popupTask = Page.WaitForPopupAsync().ContinueWith(async task =>
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
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldWorkWithClickingTargetBlankAndRelNoopener()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
            var popupTask = Page.WaitForPopupAsync().ContinueWith(async task =>
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
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldNotTreatNavigationsAsNewPopups()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
            var popupTask = Page.WaitForPopupAsync().ContinueWith(async task =>
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
            await popup.GotoAsync(TestConstants.CrossProcessUrl + "/empty.html");
            Assert.False(badSecondPopup);
        }
    }
}
