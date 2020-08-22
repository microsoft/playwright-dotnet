using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>browsercontext.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should create new context</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldCreateNewContext()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            Assert.Empty(browser.BrowserContexts);
            await using var context = await browser.NewContextAsync();
            Assert.Single(browser.BrowserContexts);
            Assert.Contains(context, browser.BrowserContexts);
            Assert.Contains(context, browser.Contexts);
            await context.CloseAsync();
            Assert.Empty(browser.BrowserContexts);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>window.open should use parent tab context</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task WindowOpenShouldUseParentTabContext()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var popupTargetCompletion = new TaskCompletionSource<IPage>();
            page.Popup += (sender, e) => popupTargetCompletion.SetResult(e.Page);

            var (popupTarget, _) = await TaskUtils.WhenAll(
                popupTargetCompletion.Task,
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage)
            );

            Assert.Same(context, popupTarget.Context);
            await context.CloseAsync();
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should isolate localStorage and cookies</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIsolateLocalStorageAndCookies()
        {
            // Create two incognito contexts.
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var context1 = await browser.NewContextAsync();
            var context2 = await browser.NewContextAsync();
            Assert.Empty(context1.Pages);
            Assert.Empty(context2.Pages);

            // Create a page in first incognito context.
            var page1 = await context1.NewPageAsync();
            await page1.GoToAsync(TestConstants.EmptyPage);
            await page1.EvaluateAsync(@"() => {
                localStorage.setItem('name', 'page1');
                document.cookie = 'name=page1';
            }");

            Assert.Single(context1.Pages);
            Assert.Empty(context2.Pages);

            // Create a page in second incognito context.
            var page2 = await context2.NewPageAsync();
            await page2.GoToAsync(TestConstants.EmptyPage);
            await page2.EvaluateAsync(@"() => {
                localStorage.setItem('name', 'page2');
                document.cookie = 'name=page2';
            }");

            Assert.Single(context1.Pages);
            Assert.Equal(page1, context1.Pages[0]);
            Assert.Single(context2.Pages);
            Assert.Equal(page2, context2.Pages[0]);

            // Make sure pages don't share localstorage or cookies.
            Assert.Equal("page1", await page1.EvaluateAsync<string>("() => localStorage.getItem('name')"));
            Assert.Equal("name=page1", await page1.EvaluateAsync<string>("() => document.cookie"));
            Assert.Equal("page2", await page2.EvaluateAsync<string>("() => localStorage.getItem('name')"));
            Assert.Equal("name=page2", await page2.EvaluateAsync<string>("() => document.cookie"));

            // Cleanup contexts.
            await TaskUtils.WhenAll(context1.CloseAsync(), context2.CloseAsync());
            Assert.Empty(browser.BrowserContexts);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should propagate default viewport to the page</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldPropagateDefaultViewportToThePage()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 456,
                    Height = 789
                }
            });

            var page = await context.NewPageAsync();
            await TestUtils.VerifyViewportAsync(page, 456, 789);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should make a copy of default viewport</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldMakeACopyOfDefaultViewport()
        {
            var viewport = new ViewportSize
            {
                Width = 456,
                Height = 789
            };

            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { Viewport = viewport });

            viewport.Width = 567;

            var page = await context.NewPageAsync();

            await TestUtils.VerifyViewportAsync(page, 456, 789);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should respect deviceScaleFactor</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRespectDeviceScaleFactor()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                DeviceScaleFactor = 3
            });

            var page = await context.NewPageAsync();
            Assert.Equal(3, await page.EvaluateAsync<int>("window.devicePixelRatio"));
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should not allow deviceScaleFactor with null viewport</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotAllowDeviceScaleFactorWithNullViewport()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = null,
                DeviceScaleFactor = 3,
            }));
            Assert.Equal("\"deviceScaleFactor\" option is not supported with null \"viewport\"", exception.Message);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should not allow isMobile with null viewport</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotAllowIsMobileWithNullViewport()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = null,
                IsMobile = true,
            }));
            Assert.Equal("\"isMobile\" option is not supported with null \"viewport\"", exception.Message);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>close() should work for empty context</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task CloseShouldWorkForEmptyContext()
        {
            var context = await Browser.NewContextAsync();
            await context.CloseAsync();
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>close() should abort waitForEvent</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task CloseShouldAbortWaitForEvent()
        {
            var context = await Browser.NewContextAsync();
            var waitTask = context.WaitForEvent<PageEventArgs>(ContextEvent.Page);
            await context.CloseAsync();
            var exception = await Assert.ThrowsAsync<TargetClosedException>(() => waitTask);
            Assert.Equal("Context closed", exception.Message);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>close() should be callable twice</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task CloseShouldBeCallableTwice()
        {
            var context = await Browser.NewContextAsync();
            await TaskUtils.WhenAll(context.CloseAsync(), context.CloseAsync());
            await context.CloseAsync();
        }
    }
}
