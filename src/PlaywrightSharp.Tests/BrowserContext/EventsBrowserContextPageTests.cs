using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>browsercontext.spec.js</playwright-file>
    ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class EventsBrowserContextPageTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public EventsBrowserContextPageTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should have url</playwright-it>
        [Retry]
        public async Task ShouldHaveUrl()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

            Assert.Equal(TestConstants.EmptyPage, otherPage.Page.Url);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should have url after domcontentloaded</playwright-it>
        [Retry]
        public async Task ShouldHaveUrlAfterDomcontentloaded()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

            await otherPage.Page.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);
            Assert.Equal(TestConstants.EmptyPage, otherPage.Page.Url);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should have about:blank url with domcontentloaded</playwright-it>
        [Retry]
        public async Task ShouldHaveAboutBlankUrlWithDomcontentloaded()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated),
                page.EvaluateAsync("url => window.open(url)", "about:blank"));

            await otherPage.Page.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);
            Assert.Equal("about:blank", otherPage.Page.Url);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should have about:blank for empty url with domcontentloaded</playwright-it>
        [Retry]
        public async Task ShouldHaveAboutBlankUrlForEmptyUrlWithDomcontentloaded()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated),
                page.EvaluateAsync("() => window.open()"));

            await otherPage.Page.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);
            Assert.Equal("about:blank", otherPage.Page.Url);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should report when a new page is created and closed</playwright-it>
        [Retry]
        public async Task ShouldReportWhenANewPageIsCreatedAndClosed()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPageEvent, _) = await TaskUtils.WhenAll(
                context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated),
                page.EvaluateAsync("url => window.open(url)", TestConstants.CrossProcessUrl + "/empty.html"));
            var otherPage = otherPageEvent.Page;

            Assert.Contains(TestConstants.CrossProcessUrl, otherPage.Url);
            Assert.Equal("Hello world", await otherPage.EvaluateAsync<string>("() => ['Hello', 'world'].join(' ')"));
            Assert.NotNull(await otherPage.QuerySelectorAsync("body'"));

            var allPages = context.Pages;
            Assert.Contains(page, allPages);
            Assert.Contains(otherPage, allPages);

            var closeEventReceived = new TaskCompletionSource<bool>();
            otherPage.Closed += (sender, e) => closeEventReceived.TrySetResult(true);

            await otherPage.CloseAsync();
            await closeEventReceived.Task.WithTimeout();

            allPages = context.Pages;
            Assert.Contains(page, allPages);
            Assert.DoesNotContain(otherPage, allPages);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should report initialized pages</playwright-it>
        [Retry]
        public async Task ShouldReportInitializedPages()
        {
            await using var context = await Browser.NewContextAsync();
            var pageTask = context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated);
            _ = context.NewPageAsync();
            var newPage = await pageTask;
            Assert.Equal("about:blank", newPage.Page.Url);

            var popupTask = context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated);
            var evaluateTask = newPage.Page.EvaluateAsync("() => window.open('about:blank')");
            var popup = await popupTask;
            Assert.Equal("about:blank", popup.Page.Url);
            await evaluateTask;
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should not crash while redirecting of original request was missed</playwright-it>
        [Retry]
        public async Task ShouldNotCrashWhileRedirectingOfOriginalRequestWasMissed()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            Server.SetRedirect("/one-style.css", "/injectedstyle.css");

            // Open a new page. Use window.open to connect to the page later.
            var pageCreatedTask = context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated);
            await Task.WhenAll(
                pageCreatedTask,
                page.EvaluateAsync("url => window.open(url), server.PREFIX + '/one-style.html'"),
                Server.WaitForRequest("/one-style.css'"));

            var newPage = pageCreatedTask.Result.Page;

            await newPage.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);
            Assert.Equal(TestConstants.ServerUrl + "/one-style.html", newPage.Url);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should have an opener</playwright-it>
        [Retry]
        public async Task ShouldHaveAnOpener()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            var (popupEvent, _) = await TaskUtils.WhenAll(
              context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated),
              page.GoToAsync(TestConstants.ServerUrl + "/popup/window-open.html"));

            var popup = popupEvent.Page;
            Assert.Equal(TestConstants.ServerUrl + "/popup/popup.html", popup.Url);
            Assert.Same(page, await popup.GetOpenerAsync());
            Assert.Null(await page.GetOpenerAsync());
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should fire page lifecycle events</playwright-it>
        [Retry]
        public async Task ShouldFirePageLifecycleEvents()
        {
            await using var context = await Browser.NewContextAsync();
            var events = new List<string>();

            context.PageCreated += (sender, e) =>
            {
                events.Add("CREATED: " + e.Page.Url);
                e.Page.Closed += (sender, closeArgs) => events.Add("DESTROYED: " + e.Page.Url);
            };

            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await page.CloseAsync();
            Assert.Equal(
                new List<string>()
                {
                    "CREATED: about:blank",
                    $"DESTROYED: {TestConstants.EmptyPage}"
                },
                events);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should work with Shift-clicking</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldWorkWithShiftClicking()
        {
            // WebKit: Shift+Click does not open a new window.
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await page.SetContentAsync("<a href=\"/one-style.html\">yo</a>");

            var popupEventTask = context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated);
            await Task.WhenAll(
              popupEventTask,
              page.ClickAsync("a", new ClickOptions { Modifiers = new[] { Modifier.Shift } }));

            Assert.Null(await popupEventTask.Result.Page.GetOpenerAsync());
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should report when a new page is created and closed</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipFirefox: true)]
        public async Task ShouldWorkWithCtrlClicking()
        {
            // Firefox: reports an opener in this case.
            // WebKit: Ctrl+Click does not open a new tab.
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            await page.SetContentAsync("<a href=\"/one-style.html\">yo</a>");

            var popupEventTask = context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated);
            await Task.WhenAll(
              popupEventTask,
              page.ClickAsync("a", new ClickOptions { Modifiers = new[] { TestConstants.IsMacOSX ? Modifier.Meta : Modifier.Control } }));

            Assert.Null(await popupEventTask.Result.Page.GetOpenerAsync());
        }
    }
}
