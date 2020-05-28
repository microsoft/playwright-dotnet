using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>browsercontext.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class BrowserContextTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should create new incognito context</playwright-it>
        [Retry]
        public async Task ShouldHaveDefaultContext()
        {
            await using var browser = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            Assert.Single(browser.BrowserContexts);
            var defaultContext = browser.BrowserContexts.First();
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(defaultContext.CloseAsync);
            Assert.Same(defaultContext, browser.DefaultContext);
            Assert.Contains("cannot be closed", exception.Message);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should have default context</playwright-it>
        [Retry]
        public async Task ShouldCreateNewIncognitoContext()
        {
            await using var browser = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            Assert.Single(browser.BrowserContexts);
            var context = await browser.NewContextAsync();
            Assert.Equal(2, browser.BrowserContexts.Count());
            Assert.Contains(context, browser.BrowserContexts);
            await context.CloseAsync();
            Assert.Single(browser.BrowserContexts);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>window.open should use parent tab context</playwright-it>
        [Retry]
        public async Task WindowOpenShouldUseParentTabContext()
        {
            await using var browser = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var popupTargetCompletion = new TaskCompletionSource<IPage>();
            page.Popup += (sender, e) => popupTargetCompletion.SetResult(e.Page);

            var (popupTarget, _) = await TaskUtils.WhenAll(
                popupTargetCompletion.Task,
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage)
            );

            Assert.Same(context, popupTarget.BrowserContext);
            await context.CloseAsync();
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should isolate localStorage and cookies</playwright-it>
        [Retry]
        public async Task ShouldIsolateLocalStorageAndCookies()
        {
            // Create two incognito contexts.
            await using var browser = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var context1 = await browser.NewContextAsync();
            var context2 = await browser.NewContextAsync();
            Assert.Empty(await context1.GetPagesAsync());
            Assert.Empty(await context2.GetPagesAsync());

            // Create a page in first incognito context.
            var page1 = await context1.NewPageAsync();
            await page1.GoToAsync(TestConstants.EmptyPage);
            await page1.EvaluateAsync(@"() => {
                localStorage.setItem('name', 'page1');
                document.cookie = 'name=page1';
            }");

            Assert.Single(await context1.GetPagesAsync());
            Assert.Empty(await context2.GetPagesAsync());

            // Create a page in second incognito context.
            var page2 = await context2.NewPageAsync();
            await page2.GoToAsync(TestConstants.EmptyPage);
            await page2.EvaluateAsync(@"() => {
                localStorage.setItem('name', 'page2');
                document.cookie = 'name=page2';
            }");

            Assert.Single(await context1.GetPagesAsync());
            Assert.Equal(page1, (await context1.GetPagesAsync())[0]);
            Assert.Single(await context2.GetPagesAsync());
            Assert.Equal(page2, (await context2.GetPagesAsync())[0]);

            // Make sure pages don't share localstorage or cookies.
            Assert.Equal("page1", await page1.EvaluateAsync<string>("() => localStorage.getItem('name')"));
            Assert.Equal("name=page1", await page1.EvaluateAsync<string>("() => document.cookie"));
            Assert.Equal("page2", await page2.EvaluateAsync<string>("() => localStorage.getItem('name')"));
            Assert.Equal("name=page2", await page2.EvaluateAsync<string>("() => document.cookie"));

            // Cleanup contexts.
            await Task.WhenAll(context1.CloseAsync(), context2.CloseAsync());
            Assert.Single(browser.BrowserContexts);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should propagate default viewport to the page</playwright-it>
        [Retry]
        public async Task ShouldPropagateDefaultViewportToThePage()
        {
            await using var browser = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page = await NewPageAsync(browser, new BrowserContextOptions
            {
                Viewport = new Viewport
                {
                    Width = 456,
                    Height = 789
                }
            });

            Assert.Equal(456, page.Viewport.Width);
            Assert.Equal(789, page.Viewport.Height);
            Assert.Equal(456, await page.EvaluateAsync<int>("window.innerWidth"));
            Assert.Equal(789, await page.EvaluateAsync<int>("window.innerHeight"));
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should take fullPage screenshots when default viewport is null</playwright-it>
        [Retry]
        public async Task ShouldTakeFullPageScreenshotsWhenDefaultViewportIsNull()
        {
            await using var browser = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page = await NewPageAsync(browser, new BrowserContextOptions
            {
                Viewport = null
            });

            await page.GoToAsync(TestConstants.EmptyPage + "/grid.html");

            var sizeBefore = await page.EvaluateAsync<Viewport>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            byte[] screenshot = await page.ScreenshotAsync(new ScreenshotOptions
            {
                FullPage = true
            });

            Assert.NotEmpty(screenshot);
            var sizeAfter = await page.EvaluateAsync<Viewport>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            Assert.Equal(sizeBefore, sizeAfter);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should restore default viewport after fullPage screenshot</playwright-it>
        [Retry]
        public async Task ShouldRestoreDefaultViewportAfterFullPageScreenshot()
        {
            await using var browser = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page = await NewPageAsync(browser, new BrowserContextOptions
            {
                Viewport = new Viewport
                {
                    Width = 456,
                    Height = 789
                }
            });

            Assert.Equal(456, page.Viewport.Width);
            Assert.Equal(789, page.Viewport.Height);
            Assert.Equal(456, await page.EvaluateAsync<int>("window.innerWidth"));
            Assert.Equal(789, await page.EvaluateAsync<int>("window.innerHeight"));

            byte[] screenshot = await page.ScreenshotAsync(new ScreenshotOptions
            {
                FullPage = true
            });

            Assert.NotEmpty(screenshot);
            Assert.Equal(456, page.Viewport.Width);
            Assert.Equal(789, page.Viewport.Height);
            Assert.Equal(456, await page.EvaluateAsync<int>("window.innerWidth"));
            Assert.Equal(789, await page.EvaluateAsync<int>("window.innerHeight"));
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should make a copy of default viewport</playwright-it>
        [Retry]
        public async Task ShouldMakeACopyOfDefaultViewport()
        {
            var viewport = new Viewport
            {
                Width = 456,
                Height = 789
            };

            await using var browser = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = viewport
            });

            viewport.Width = 567;

            var page = await context.NewPageAsync();

            Assert.Equal(456, page.Viewport.Width);
            Assert.Equal(789, page.Viewport.Height);
            Assert.Equal(456, await page.EvaluateAsync<int>("window.innerWidth"));
            Assert.Equal(789, await page.EvaluateAsync<int>("window.innerHeight"));
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should take element screenshot when default viewport is null and restore back</playwright-it>
        [Retry]
        public async Task ShouldTakeElementScreenshotWhenDefaultViewportIsNullAndRestoreBack()
        {
            await using var browser = await Playwright.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page = await NewPageAsync(browser, new BrowserContextOptions { Viewport = null });
            await page.SetContentAsync(@"
                <div style=""height: 14px"">oooo</div>
                <style>
                div.to-screenshot {
                    border: 1px solid blue;
                    width: 600px;
                    height: 600px;
                    margin-left: 50px;
                }
                ::-webkit-scrollbar{
                    display: none;
                }
                </style>
                <div class=""to-screenshot""></div>
                <div class=""to-screenshot""></div>
                <div class=""to-screenshot""></div>
            ");
            var sizeBefore = await page.EvaluateAsync<Viewport>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            var elementHandle = await page.QuerySelectorAsync("div.to-screenshot");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.NotEmpty(screenshot);
            var sizeAfter = await page.EvaluateAsync<Viewport>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            Assert.Equal(sizeBefore, sizeAfter);
        }
    }
}
