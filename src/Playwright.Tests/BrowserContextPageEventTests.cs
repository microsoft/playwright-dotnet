using System.Collections.Generic;
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
    public class BrowserContextPageEventTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextPageEventTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should have url")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveUrl()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForEventAsync(ContextEvent.Page),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

            Assert.Equal(TestConstants.EmptyPage, otherPage.Url);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should have url after domcontentloaded")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveUrlAfterDomcontentloaded()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForEventAsync(ContextEvent.Page),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

            await otherPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.Equal(TestConstants.EmptyPage, otherPage.Url);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should have about:blank url with domcontentloaded")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveAboutBlankUrlWithDomcontentloaded()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var otherPage = await context.WaitForEventAsync(ContextEvent.Page, async () =>
            {
                await page.EvaluateAsync("url => window.open(url)", "about:blank");
            });
            await otherPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.Equal("about:blank", otherPage.Url);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should have about:blank for empty url with domcontentloaded")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveAboutBlankUrlForEmptyUrlWithDomcontentloaded()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var otherPage = await context.WaitForEventAsync(ContextEvent.Page, async () =>
            {
                await page.EvaluateAsync("() => window.open()");
            });
            await otherPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.Equal("about:blank", otherPage.Url);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should report when a new page is created and closed")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportWhenANewPageIsCreatedAndClosed()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var otherPage = await context.WaitForEventAsync(ContextEvent.Page, async () =>
            {
                await page.EvaluateAsync("url => window.open(url)", TestConstants.CrossProcessUrl + "/empty.html");
            });

            Assert.Contains(TestConstants.CrossProcessUrl, otherPage.Url);
            Assert.Equal("Hello world", await otherPage.EvaluateAsync<string>("() => ['Hello', 'world'].join(' ')"));
            Assert.NotNull(await otherPage.QuerySelectorAsync("body"));


            var allPages = context.Pages;
            Assert.Contains(page, allPages);
            Assert.Contains(otherPage, allPages);

            var closeEventReceived = new TaskCompletionSource<bool>();
            otherPage.Close += (_, _) => closeEventReceived.TrySetResult(true);

            await otherPage.CloseAsync();
            await closeEventReceived.Task.WithTimeout(TestConstants.DefaultTaskTimeout);

            allPages = context.Pages;
            Assert.Contains(page, allPages);
            Assert.DoesNotContain(otherPage, allPages);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should report initialized pages")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportInitializedPages()
        {
            await using var context = await Browser.NewContextAsync();
            var pageTask = context.WaitForEventAsync(ContextEvent.Page);
            _ = context.NewPageAsync();
            var newPage = await pageTask;
            Assert.Equal("about:blank", newPage.Url);

            var popupTask = context.WaitForEventAsync(ContextEvent.Page);
            var evaluateTask = newPage.EvaluateAsync("() => window.open('about:blank')");
            var popup = await popupTask;
            Assert.Equal("about:blank", popup.Url);
            await evaluateTask;
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should not crash while redirecting of original request was missed")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotCrashWhileRedirectingOfOriginalRequestWasMissed()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            Server.SetRoute("/one-style.css", context =>
            {
                context.Response.Redirect("/one-style.css");
                return Task.CompletedTask;
            });

            // Open a new page. Use window.open to connect to the page later.
            var pageCreatedTask = context.WaitForEventAsync(ContextEvent.Page);
            await TaskUtils.WhenAll(
                pageCreatedTask,
                page.EvaluateAsync("url => window.open(url)", TestConstants.ServerUrl + "/one-style.html"),
                Server.WaitForRequest("/one-style.css"));

            var newPage = pageCreatedTask.Result;

            await newPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.Equal(TestConstants.ServerUrl + "/one-style.html", newPage.Url);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should have an opener")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveAnOpener()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);

            var (popupEvent, _) = await TaskUtils.WhenAll(
              context.WaitForEventAsync(ContextEvent.Page),
              page.GotoAsync(TestConstants.ServerUrl + "/popup/window-open.html"));

            var popup = popupEvent;
            Assert.Equal(TestConstants.ServerUrl + "/popup/popup.html", popup.Url);
            Assert.Same(page, await popup.OpenerAsync());
            Assert.Null(await page.OpenerAsync());
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should fire page lifecycle events")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFirePageLoadStates()
        {
            await using var context = await Browser.NewContextAsync();
            var events = new List<string>();

            context.Page += (_, e) =>
            {
                events.Add("CREATED: " + e.Url);
                e.Close += (sender, _) => events.Add("DESTROYED: " + ((IPage)sender).Url);
            };

            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);
            await page.CloseAsync();
            Assert.Equal(
                new List<string>()
                {
                    "CREATED: about:blank",
                    $"DESTROYED: {TestConstants.EmptyPage}"
                },
                events);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should work with Shift-clicking")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldWorkWithShiftClicking()
        {
            // WebKit: Shift+Click does not open a new window.
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);
            await page.SetContentAsync("<a href=\"/one-style.html\">yo</a>");

            var popupEventTask = context.WaitForEventAsync(ContextEvent.Page);
            await TaskUtils.WhenAll(
              popupEventTask,
              page.ClickAsync("a", modifiers: new[] { KeyboardModifier.Shift }));

            Assert.Null(await popupEventTask.Result.OpenerAsync());
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should report when a new page is created and closed")]
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipFirefox: true)]
        public async Task ShouldWorkWithCtrlClicking()
        {
            // Firefox: reports an opener in this case.
            // WebKit: Ctrl+Click does not open a new tab.
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);
            await page.SetContentAsync("<a href=\"/one-style.html\">yo</a>");

            var popupEventTask = context.WaitForEventAsync(ContextEvent.Page);
            await TaskUtils.WhenAll(
              popupEventTask,
              page.ClickAsync("a", modifiers: new[] { TestConstants.IsMacOSX ? KeyboardModifier.Meta : KeyboardModifier.Control }));

            Assert.Null(await popupEventTask.Result.OpenerAsync());
        }
    }
}
