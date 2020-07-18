using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>screenshot.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class ElementHandleScreenshotTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleScreenshotTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await page.EvaluateAsync("window.scrollBy(50, 100)");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-bounding-box.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should take into account padding and border</playwright-it>
        [Retry]
        public async Task ShouldTakeIntoAccountPaddingAndBorder()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.SetContentAsync(@"
                <div style=""height: 14px"">oooo</div>
                <style>div {
                    border: 2px solid blue;
                    background: green;
                    width: 50px;
                    height: 50px;
                }
                </style>
                <div id=""d""></div>");
            var elementHandle = await page.QuerySelectorAsync("div#d");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-padding-border.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should capture full element when larger than viewport in parallel</playwright-it>
        [Retry]
        public async Task ShouldCaptureFullElementWhenLargerThanViewportInParallel()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
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
            var elementHandles = await page.QuerySelectorAllAsync("div.to-screenshot");
            var screenshotTasks = elementHandles.Select(e => e.ScreenshotAsync()).ToArray();
            await Task.WhenAll(screenshotTasks);

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-larger-than-viewport.png", screenshotTasks.ElementAt(2).Result));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should capture full element when larger than viewport</playwright-it>
        [Retry]
        public async Task ShouldCaptureFullElementWhenLargerThanViewport()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
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
                <div class=""to-screenshot""></div>");

            var elementHandle = await page.QuerySelectorAsync("div.to-screenshot");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-larger-than-viewport.png", screenshot));
            Assert.Equal(new[] { 500, 500 }, await page.EvaluateAsync<int[]>("[window.innerWidth, window.innerHeight]"));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should scroll element into view</playwright-it>
        [Retry]
        public async Task ShouldScrollElementIntoView()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.SetContentAsync(@"
                <div style=""height: 14px"">oooo</div>
                <style>div.above {
                  border: 2px solid blue;
                  background: red;
                  height: 1500px;
                }
                div.to-screenshot {
                  border: 2px solid blue;
                  background: green;
                  width: 50px;
                  height: 50px;
                }
                </style>
                <div class=""above""></div>
                <div class=""to-screenshot""></div>");
            var elementHandle = await page.QuerySelectorAsync("div.to-screenshot");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-scrolled-into-view.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should work with a rotated element</playwright-it>
        [Retry]
        public async Task ShouldWorkWithARotatedElement()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.SetContentAsync(@"
                <div style='position: absolute;
                top: 100px;
                left: 100px;
                width: 100px;
                height: 100px;
                background: green;
                transform: rotateZ(200deg); '>&nbsp;</div>
            ");
            var elementHandle = await page.QuerySelectorAsync("div");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-rotate.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should fail to screenshot a detached element</playwright-it>
        [Retry]
        public async Task ShouldFailToScreenshotADetachedElement()
        {
            await Page.SetContentAsync("<h1>remove this</h1>");
            var elementHandle = await Page.QuerySelectorAsync("h1");
            await Page.EvaluateAsync("element => element.remove()", elementHandle);

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => elementHandle.ScreenshotAsync());
            Assert.Equal("Node is either not visible or not an HTMLElement", exception.Message);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should not hang with zero width/height element</playwright-it>
        [Retry]
        public async Task ShouldNotHangWithZeroWidthHeightElement()
        {
            await Page.SetContentAsync(@"<div style='width: 50px; height: 0'></div>");
            var elementHandle = await Page.QuerySelectorAsync("div");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => elementHandle.ScreenshotAsync());
            Assert.Equal("Node has 0 height.", exception.Message);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should work for an element with fractional dimensions</playwright-it>
        [Retry]
        public async Task ShouldWorkForAnElementWithFractionalDimensions()
        {
            await Page.SetContentAsync("<div style=\"width:48.51px;height:19.8px;border:1px solid black;\"></div>");
            var elementHandle = await Page.QuerySelectorAsync("div");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-fractional.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should work for an element with an offset</playwright-it>
        [Retry]
        public async Task ShouldWorkForAnElementWithAnOffset()
        {
            await Page.SetContentAsync("<div style=\"position:absolute; top: 10.3px; left: 20.4px;width:50.3px;height:20.2px;border:1px solid black;\"></div>");
            var elementHandle = await Page.QuerySelectorAsync("div");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-fractional-offset.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should take fullPage screenshots when default viewport is null</playwright-it>
        [Retry]
        public async Task ShouldTakeFullPageScreenshotsWhenDefaultViewportIsNull()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page = await NewPageAsync(browser, new BrowserContextOptions
            {
                Viewport = null
            });

            await page.GoToAsync(TestConstants.EmptyPage + "/grid.html");

            var sizeBefore = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            byte[] screenshot = await page.ScreenshotAsync(new ScreenshotOptions
            {
                FullPage = true
            });

            Assert.NotEmpty(screenshot);
            var sizeAfter = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            Assert.Equal(sizeBefore, sizeAfter);
        }


        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should restore default viewport after fullPage screenshot</playwright-it>
        [Retry]
        public async Task ShouldRestoreDefaultViewportAfterFullPageScreenshot()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var page = await NewPageAsync(browser, new BrowserContextOptions
            {
                Viewport = new ViewportSize
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
        ///<playwright-it>should take element screenshot when default viewport is null and restore back</playwright-it>
        [Retry]
        public async Task ShouldTakeElementScreenshotWhenDefaultViewportIsNullAndRestoreBack()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
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
            var sizeBefore = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            var elementHandle = await page.QuerySelectorAsync("div.to-screenshot");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.NotEmpty(screenshot);
            var sizeAfter = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            Assert.Equal(sizeBefore, sizeAfter);
        }
    }
}
