using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextPageEventTests : BrowserTestEx
    {
        [PlaywrightTest("browsercontext-page-event.spec.ts", "should have url")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveUrl()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForPageAsync(),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

            Assert.AreEqual(TestConstants.EmptyPage, otherPage.Url);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should have url after domcontentloaded")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveUrlAfterDomcontentloaded()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForPageAsync(),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

            await otherPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.AreEqual(TestConstants.EmptyPage, otherPage.Url);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should have about:blank url with domcontentloaded")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveAboutBlankUrlWithDomcontentloaded()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var otherPage = await context.RunAndWaitForPageAsync(async () =>
            {
                await page.EvaluateAsync("url => window.open(url)", "about:blank");
            });
            await otherPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.AreEqual("about:blank", otherPage.Url);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should have about:blank for empty url with domcontentloaded")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveAboutBlankUrlForEmptyUrlWithDomcontentloaded()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var otherPage = await context.RunAndWaitForPageAsync(async () =>
            {
                await page.EvaluateAsync("() => window.open()");
            });
            await otherPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.AreEqual("about:blank", otherPage.Url);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should report when a new page is created and closed")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportWhenANewPageIsCreatedAndClosed()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var otherPage = await context.RunAndWaitForPageAsync(async () =>
            {
                await page.EvaluateAsync("url => window.open(url)", TestConstants.CrossProcessUrl + "/empty.html");
            });

            StringAssert.Contains(TestConstants.CrossProcessUrl, otherPage.Url);
            Assert.AreEqual("Hello world", await otherPage.EvaluateAsync<string>("() => ['Hello', 'world'].join(' ')"));
            Assert.NotNull(await otherPage.QuerySelectorAsync("body"));


            var allPages = context.Pages;
            CollectionAssert.Contains(allPages, page);
            CollectionAssert.Contains(allPages, otherPage);

            var closeEventReceived = new TaskCompletionSource<bool>();
            otherPage.Close += (_, _) => closeEventReceived.TrySetResult(true);

            await otherPage.CloseAsync();
            await closeEventReceived.Task;

            allPages = context.Pages;
            CollectionAssert.Contains(allPages, page);
            CollectionAssert.DoesNotContain(allPages, otherPage);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should report initialized pages")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportInitializedPages()
        {
            await using var context = await Browser.NewContextAsync();
            var pageTask = context.WaitForPageAsync();
            _ = context.NewPageAsync();
            var newPage = await pageTask;
            Assert.AreEqual("about:blank", newPage.Url);

            var popupTask = context.WaitForPageAsync();
            var evaluateTask = newPage.EvaluateAsync("() => window.open('about:blank')");
            var popup = await popupTask;
            Assert.AreEqual("about:blank", popup.Url);
            await evaluateTask;
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should not crash while redirecting of original request was missed")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotCrashWhileRedirectingOfOriginalRequestWasMissed()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            HttpServer.Server.SetRoute("/one-style.css", context =>
            {
                context.Response.Redirect("/one-style.css");
                return Task.CompletedTask;
            });

            // Open a new page. Use window.open to connect to the page later.
            var pageCreatedTask = context.WaitForPageAsync();
            await TaskUtils.WhenAll(
                pageCreatedTask,
                page.EvaluateAsync("url => window.open(url)", TestConstants.ServerUrl + "/one-style.html"),
                HttpServer.Server.WaitForRequest("/one-style.css"));

            var newPage = pageCreatedTask.Result;

            await newPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Assert.AreEqual(TestConstants.ServerUrl + "/one-style.html", newPage.Url);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should have an opener")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveAnOpener()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);

            var (popupEvent, _) = await TaskUtils.WhenAll(
              context.WaitForPageAsync(),
              page.GotoAsync(TestConstants.ServerUrl + "/popup/window-open.html"));

            var popup = popupEvent;
            Assert.AreEqual(TestConstants.ServerUrl + "/popup/popup.html", popup.Url);
            Assert.AreEqual(page, await popup.OpenerAsync());
            Assert.Null(await page.OpenerAsync());
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should fire page lifecycle events")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
            Assert.AreEqual(
                new List<string>()
                {
                    "CREATED: about:blank",
                    $"DESTROYED: {TestConstants.EmptyPage}"
                },
                events);
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should work with Shift-clicking")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldWorkWithShiftClicking()
        {
            // WebKit: Shift+Click does not open a new window.
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);
            await page.SetContentAsync("<a href=\"/one-style.html\">yo</a>");

            var popupEventTask = context.WaitForPageAsync();
            await TaskUtils.WhenAll(
              popupEventTask,
              page.ClickAsync("a", new PageClickOptions { Modifiers = new[] { KeyboardModifier.Shift } }));

            Assert.Null(await popupEventTask.Result.OpenerAsync());
        }

        [PlaywrightTest("browsercontext-page-event.spec.ts", "should report when a new page is created and closed")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true, skipFirefox: true)]
        public async Task ShouldWorkWithCtrlClicking()
        {
            // Firefox: reports an opener in this case.
            // WebKit: Ctrl+Click does not open a new tab.
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);
            await page.SetContentAsync("<a href=\"/one-style.html\">yo</a>");

            var popupEventTask = context.WaitForPageAsync();
            await TaskUtils.WhenAll(
              popupEventTask,
              page.ClickAsync("a", new PageClickOptions { Modifiers = new[] { TestConstants.IsMacOSX ? KeyboardModifier.Meta : KeyboardModifier.Control } }));

            Assert.Null(await popupEventTask.Result.OpenerAsync());
        }
    }
}
