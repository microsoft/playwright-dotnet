using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.Popup</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventsPopupTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventsPopupTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
            await Task.WhenAll(
                popupTask,
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            var popup = popupTask.Result.Page;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Popup</playwright-describe>
        ///<playwright-it>should work with noopener</playwright-it>
        [Retry]
        public async Task ShouldWorkWithNoopener()
        {
            var popupTask = Page.WaitForEvent<PopupEventArgs>(PageEvent.Popup);
            await Task.WhenAll(
                popupTask,
                Page.EvaluateAsync<string>("() => window.open('about:blank', null, 'noopener')")
            );
            var popup = popupTask.Result.Page;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
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
            await Task.WhenAll(
                popupTask,
                Page.ClickAsync("a")
            );

            var popup = await popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
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
            await Task.WhenAll(
                popupTask,
                Page.QuerySelectorEvaluateAsync("a", "a => a.click()")
            );
            var popup = await popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            // TODO: At this point popup might still have about:blank as the current document.
            // FFOX is slow enough to trigger this. We should do something about popups api.
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
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
            await Task.WhenAll(
                popupTask,
                Page.ClickAsync("a")
            );
            var popup = await popupTask.Result;
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
            Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
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
            await Task.WhenAll(
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
