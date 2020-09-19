using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Popup
{
    ///<playwright-file>popup.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.Popup</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventsPopupTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventsPopupTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            var popup = popupTask.Result.Page;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should work with window features</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithWindowFeatures()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync<string>("() => window.open('about:blank', 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=780,height=200,top=0,left=0')")
            );
            var popup = popupTask.Result.Page;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should emit for immediately closed popups</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldEmitForImmediatelyClosedPopups()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync<string>(@"() => {
                    const win = window.open('about:blank');
                    win.close();
                }")
            );
            Assert.NotNull(popupTask.Result.Page);
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should emit for immediately closed popups</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldEmitForImmediatelyClosedPopupsWithLocation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync<string>(@"() => {
                    const win = window.open(window.location.href);
                    win.close();
                }")
            );
            Assert.NotNull(popupTask.Result.Page);
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should be able to capture alert</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeAbleToCaptureAlert()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
            var evaluateTask = Page.EvaluateAsync<string>(@"() => {
                const win = window.open('');
                win.alert('hello');
            }");

            var popup = await popupTask;
            var dialog = await popup.Page.WaitForEvent<DialogEventArgs>(PageEvent.Dialog);

            Assert.Equal("hello", dialog.Dialog.Message);
            await dialog.Dialog.DismissAsync();
            await evaluateTask;
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should work with empty url</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithEmptyUrl()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open('')")
            );
            var popup = popupTask.Result.Page;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should work with noopener and no url</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithNoopenerAndNoUrl()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open(undefined, null, 'noopener')")
            );
            var popup = popupTask.Result.Page;
            Assert.Equal("about:blank", popup.Url.Split('#')[0]);
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should work with noopener and about:blank</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithNoopenerAndAboutBlank()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open('about:blank', null, 'noopener')")
            );
            var popup = popupTask.Result.Page;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should work with noopener and url</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithNoopenerAndUrl()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("url => window.open(url, null, 'noopener')", TestConstants.EmptyPage)
            );
            var popup = popupTask.Result.Page;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should work with clicking target=_blank</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithClickingTargetBlank()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a target=_blank rel=\"opener\" href=\"/one-style.html\">yo</a>");
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup).ContinueWith(async task =>
            {
                var popup = task.Result.Page;
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

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should work with fake-clicking target=_blank and rel=noopener</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithFakeClickingTargetBlankAndRelNoopener()
        {
            // TODO: FFOX sends events for "one-style.html" request to both pages.
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup).ContinueWith(async task =>
            {
                var popup = task.Result.Page;
                await popup.WaitForLoadStateAsync();
                return popup;
            });
            await TaskUtils.WhenAll(
                popupTask,
                Page.EvalOnSelectorAsync("a", "a => a.click()")
            );
            var popup = await popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            // TODO: At this point popup might still have about:blank as the current document.
            // FFOX is slow enough to trigger this. We should do something about popups api.
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should work with clicking target=_blank and rel=noopener</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithClickingTargetBlankAndRelNoopener()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup).ContinueWith(async task =>
            {
                var popup = task.Result.Page;
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

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should not treat navigations as new popups</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldNotTreatNavigationsAsNewPopups()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup).ContinueWith(async task =>
            {
                var popup = task.Result.Page;
                await popup.WaitForLoadStateAsync();
                return popup;
            });
            await TaskUtils.WhenAll(
                popupTask,
                Page.ClickAsync("a")
            );
            var popup = await popupTask.Result;
            bool badSecondPopup = false;
            Page.Popup += (sender, e) => badSecondPopup = true;
            await popup.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            Assert.False(badSecondPopup);
        }
    }
}
