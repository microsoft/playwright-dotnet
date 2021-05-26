using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextBasicTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextBasicTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should create new context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCreateNewContext()
        {
            await using var browser = await BrowserType.LaunchDefaultAsync();
            Assert.Empty(browser.Contexts);
            await using var context = await browser.NewContextAsync();
            Assert.Single(browser.Contexts);
            Assert.Contains(context, browser.Contexts);
            Assert.Same(browser, context.Browser);
            await context.CloseAsync();
            Assert.Empty(browser.Contexts);
            Assert.Same(browser, context.Browser);
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "window.open should use parent tab context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task WindowOpenShouldUseParentTabContext()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);
            var popupTargetCompletion = new TaskCompletionSource<IPage>();
            page.Popup += (_, e) => popupTargetCompletion.SetResult(e);

            var (popupTarget, _) = await TaskUtils.WhenAll(
                popupTargetCompletion.Task,
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage)
            );

            Assert.Same(context, popupTarget.Context);
            await context.CloseAsync();
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should isolate localStorage and cookies")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolateLocalStorageAndCookies()
        {
            // Create two incognito contexts.
            await using var browser = await BrowserType.LaunchDefaultAsync();
            var context1 = await browser.NewContextAsync();
            var context2 = await browser.NewContextAsync();
            Assert.Empty(context1.Pages);
            Assert.Empty(context2.Pages);

            // Create a page in first incognito context.
            var page1 = await context1.NewPageAsync();
            await page1.GotoAsync(TestConstants.EmptyPage);
            await page1.EvaluateAsync(@"() => {
                localStorage.setItem('name', 'page1');
                document.cookie = 'name=page1';
            }");

            Assert.Single(context1.Pages);
            Assert.Empty(context2.Pages);

            // Create a page in second incognito context.
            var page2 = await context2.NewPageAsync();
            await page2.GotoAsync(TestConstants.EmptyPage);
            await page2.EvaluateAsync(@"() => {
                localStorage.setItem('name', 'page2');
                document.cookie = 'name=page2';
            }");

            Assert.Single(context1.Pages);
            Assert.Equal(page1, context1.Pages.FirstOrDefault());
            Assert.Single(context2.Pages);
            Assert.Equal(page2, context2.Pages.FirstOrDefault());

            // Make sure pages don't share localstorage or cookies.
            Assert.Equal("page1", await page1.EvaluateAsync<string>("() => localStorage.getItem('name')"));
            Assert.Equal("name=page1", await page1.EvaluateAsync<string>("() => document.cookie"));
            Assert.Equal("page2", await page2.EvaluateAsync<string>("() => localStorage.getItem('name')"));
            Assert.Equal("name=page2", await page2.EvaluateAsync<string>("() => document.cookie"));

            // Cleanup contexts.
            await TaskUtils.WhenAll(context1.CloseAsync(), context2.CloseAsync());
            Assert.Empty(browser.Contexts);
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should propagate default viewport to the page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldPropagateDefaultViewportToThePage()
        {
            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = 456,
                    Height = 789
                }
            });

            var page = await context.NewPageAsync();
            await TestUtils.VerifyViewportAsync(page, 456, 789);
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should make a copy of default viewport")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldMakeACopyOfDefaultViewport()
        {
            var viewport = new ViewportSize
            {
                Width = 456,
                Height = 789
            };

            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions { ViewportSize = viewport });

            viewport.Width = 567;

            var page = await context.NewPageAsync();

            await TestUtils.VerifyViewportAsync(page, 456, 789);
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should respect deviceScaleFactor")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectDeviceScaleFactor()
        {
            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                DeviceScaleFactor = 3
            });

            var page = await context.NewPageAsync();
            Assert.Equal(3, await page.EvaluateAsync<int>("window.devicePixelRatio"));
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should not allow deviceScaleFactor with null viewport")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotAllowDeviceScaleFactorWithViewportDisabled()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightException>(() => Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = ViewportSize.NoViewport,
                DeviceScaleFactor = 3,
            }));
            Assert.Equal("\"deviceScaleFactor\" option is not supported with null \"viewport\"", exception.Message);
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should not allow isMobile with null viewport")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotAllowIsMobileWithViewportDisabled()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightException>(() => Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = ViewportSize.NoViewport,
                IsMobile = true,
            }));
            Assert.Equal("\"isMobile\" option is not supported with null \"viewport\"", exception.Message);
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "close() should work for empty context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task CloseShouldWorkForEmptyContext()
        {
            var context = await Browser.NewContextAsync();
            await context.CloseAsync();
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "close() should abort waitForEvent")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task CloseShouldAbortWaitForEvent()
        {
            var context = await Browser.NewContextAsync();
            var waitTask = context.WaitForEventAsync(BrowserContextEvent.Page);
            await context.CloseAsync();
            var exception = await Assert.ThrowsAsync<PlaywrightException>(() => waitTask);
            Assert.Equal("Context closed", exception.Message);
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should not report frameless pages on error")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotReportFramelessPagesOnError()
        {
            var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            Server.SetRoute("/empty.html", context =>
            {
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync($"<a href=\"{TestConstants.EmptyPage}\" target=\"_blank\">Click me</a>");
            });

            IPage popup = null;
            context.Page += (_, e) => popup = e;
            await page.GotoAsync(TestConstants.EmptyPage);
            await page.ClickAsync("'Click me'");
            await context.CloseAsync();

            if (popup != null)
            {
                Assert.True(popup.IsClosed);
                Assert.NotNull(popup.MainFrame);
            }
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "close() should be callable twice")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task CloseShouldBeCallableTwice()
        {
            var context = await Browser.NewContextAsync();
            await TaskUtils.WhenAll(context.CloseAsync(), context.CloseAsync());
            await context.CloseAsync();
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should return all of the pages")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnAllOfThePages()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var second = await context.NewPageAsync();

            Assert.Equal(2, context.Pages.Count);
            Assert.Contains(page, context.Pages);
            Assert.Contains(second, context.Pages);
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "BrowserContext.pages()", "should close all belonging pages once closing context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCloseAllBelongingPagesOnceClosingContext()
        {
            await using var context = await Browser.NewContextAsync();
            await context.NewPageAsync();

            Assert.Single(context.Pages);

            await context.CloseAsync();

            Assert.Empty(context.Pages);
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should disable javascript")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDisableJavascript()
        {
            await using (var context = await Browser.NewContextAsync(new BrowserNewContextOptions { JavaScriptEnabled = false }))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync("data:text/html, <script>var something = 'forbidden'</script>");

                var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await page.EvaluateAsync("something"));

                Assert.Contains(
                    TestConstants.IsWebKit ? "Can\'t find variable: something" : "something is not defined",
                    exception.Message);
            }

            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync("data:text/html, <script>var something = 'forbidden'</script>");
                Assert.Equal("forbidden", await page.EvaluateAsync<string>("something"));
            }
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should be able to navigate after disabling javascript")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToNavigateAfterDisablingJavascript()
        {
            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions { JavaScriptEnabled = false });
            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should work with offline option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithOfflineOption()
        {
            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions { Offline = true });
            var page = await context.NewPageAsync();
            await Assert.ThrowsAsync<PlaywrightException>(() => page.GotoAsync(TestConstants.EmptyPage));
            await context.SetOfflineAsync(false);
            var response = await page.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal((int)HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("browsercontext-basic.spec.ts", "should emulate navigator.onLine")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldEmulateNavigatorOnLine()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            Assert.True(await page.EvaluateAsync<bool>("() => window.navigator.onLine"));
            await context.SetOfflineAsync(true);
            Assert.False(await page.EvaluateAsync<bool>("() => window.navigator.onLine"));
            await context.SetOfflineAsync(false);
            Assert.True(await page.EvaluateAsync<bool>("() => window.navigator.onLine"));
        }
    }
}
