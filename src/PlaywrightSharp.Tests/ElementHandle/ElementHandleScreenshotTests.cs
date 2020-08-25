using System;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using SixLabors.ImageSharp;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>screenshot.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleScreenshotTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleScreenshotTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Page.EvaluateAsync("window.scrollBy(50, 100)");
            var elementHandle = await Page.QuerySelectorAsync(".box:nth-of-type(3)");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-bounding-box.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should take into account padding and border</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldTakeIntoAccountPaddingAndBorder()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.SetContentAsync(@"
                <div style=""height: 14px"">oooo</div>
                <style>div {
                    border: 2px solid blue;
                    background: green;
                    width: 50px;
                    height: 50px;
                }
                </style>
                <div id=""d""></div>");
            var elementHandle = await Page.QuerySelectorAsync("div#d");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-padding-border.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should capture full element when larger than viewport in parallel</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldCaptureFullElementWhenLargerThanViewportInParallel()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.SetContentAsync(@"
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
            var elementHandles = await Page.QuerySelectorAllAsync("div.to-screenshot");
            var screenshotTasks = elementHandles.Select(e => e.ScreenshotAsync()).ToArray();
            await TaskUtils.WhenAll(screenshotTasks);

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-larger-than-viewport.png", screenshotTasks.ElementAt(2).Result));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should capture full element when larger than viewport</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldCaptureFullElementWhenLargerThanViewport()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.SetContentAsync(@"
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

            var elementHandle = await Page.QuerySelectorAsync("div.to-screenshot");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-larger-than-viewport.png", screenshot));
            await TestUtils.VerifyViewportAsync(Page, 500, 500);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should scroll element into view</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldScrollElementIntoView()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.SetContentAsync(@"
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
            var elementHandle = await Page.QuerySelectorAsync("div.to-screenshot");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-scrolled-into-view.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should scroll 15000px into view</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldScroll15000pxIntoView()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.SetContentAsync(@"
                <div style=""height: 14px"">oooo</div>
                <style>div.above {
                  border: 2px solid blue;
                  background: red;
                  height: 15000px;
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
            var elementHandle = await Page.QuerySelectorAsync("div.to-screenshot");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-scrolled-into-view.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should work with a rotated element</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithARotatedElement()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.SetContentAsync(@"
                <div style='position: absolute;
                top: 100px;
                left: 100px;
                width: 100px;
                height: 100px;
                background: green;
                transform: rotateZ(200deg); '>&nbsp;</div>
            ");
            var elementHandle = await Page.QuerySelectorAsync("div");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-rotate.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should fail to screenshot a detached element</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFailToScreenshotADetachedElement()
        {
            await Page.SetContentAsync("<h1>remove this</h1>");
            var elementHandle = await Page.QuerySelectorAsync("h1");
            await Page.EvaluateAsync("element => element.remove()", elementHandle);

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => elementHandle.ScreenshotAsync());
            Assert.Contains("Element is not attached to the DOM", exception.Message);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should timeout waiting for visible</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldTimeoutWaitingForVisible()
        {
            await Page.SetContentAsync(@"<div style='width: 50px; height: 0'></div>");
            var elementHandle = await Page.QuerySelectorAsync("div");
            var exception = await Assert.ThrowsAsync<TimeoutException>(() => elementHandle.ScreenshotAsync(timeout: 3000));
            Assert.Contains("Timeout 3000ms exceeded during elementHandle.screenshot", exception.Message);
            Assert.Contains("element is not visible", exception.Message);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should wait for visible</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWaitForVisible()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Page.EvaluateAsync("() => window.scrollBy(50, 100)");
            var elementHandle = await Page.QuerySelectorAsync(".box:nth-of-type(3)");
            await elementHandle.EvaluateAsync("e => e.style.visibility = 'hidden'");
            var task = elementHandle.ScreenshotAsync();

            for (int i = 0; i < 10; i++)
            {
                await Page.EvaluateAsync("() => new Promise(f => requestAnimationFrame(f))");
            }
            Assert.False(task.IsCompleted);
            await elementHandle.EvaluateAsync("e => e.style.visibility = 'visible'");

            byte[] screenshot = await task;
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-bounding-box.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should work for an element with fractional dimensions</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkForAnElementWithFractionalDimensions()
        {
            await Page.SetContentAsync("<div style=\"width:48.51px;height:19.8px;border:1px solid black;\"></div>");
            var elementHandle = await Page.QuerySelectorAsync("div");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-fractional.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should work with a mobile viewport</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithAMobileViewport()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await page.EvaluateAsync("() => window.scrollBy(50, 100)");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            byte[] screenshot = await elementHandle.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-mobile.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should work with device scale factor</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithDeviceScaleFactor()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                DeviceScaleFactor = 2,
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await page.EvaluateAsync("() => window.scrollBy(50, 100)");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            byte[] screenshot = await elementHandle.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-mobile-dsf.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should work for an element with an offset</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkForAnElementWithAnOffset()
        {
            await Page.SetContentAsync("<div style=\"position:absolute; top: 10.3px; left: 20.4px;width:50.3px;height:20.2px;border:1px solid black;\"></div>");
            var elementHandle = await Page.QuerySelectorAsync("div");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-fractional-offset.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should take screenshots when default viewport is null</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldTakeScreenshotsWhenDefaultViewportIsNull()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = null
            });
            var page = await context.NewPageAsync();
            await page.SetContentAsync("<div style='height: 10000px; background: red'></div>");
            var windowSize = await page.EvaluateAsync<ViewportSize>("() => ({ width: window.innerWidth * window.devicePixelRatio, height: window.innerHeight * window.devicePixelRatio })");
            var sizeBefore = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");

            byte[] screenshot = await page.ScreenshotAsync();
            Assert.NotNull(screenshot);
            Assert.NotEmpty(screenshot);
            var decoded = Image.Load(screenshot);
            Assert.Equal(windowSize.Width, decoded.Width);
            Assert.Equal(windowSize.Height, decoded.Height);

            var sizeAfter = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            Assert.Equal(sizeBefore, sizeAfter);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should take fullPage screenshots when default viewport is null</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldTakeFullPageScreenshotsWhenDefaultViewportIsNull()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = null
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var sizeBefore = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");

            byte[] screenshot = await page.ScreenshotAsync(true);
            Assert.NotNull(screenshot);
            Assert.NotEmpty(screenshot);

            var sizeAfter = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            Assert.Equal(sizeBefore, sizeAfter);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should restore default viewport after fullPage screenshot</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRestoreDefaultViewportAfterFullPageScreenshot()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize { Width = 456, Height = 789 },
            });
            var page = await context.NewPageAsync();
            await TestUtils.VerifyViewportAsync(page, 456, 789);
            byte[] screenshot = await page.ScreenshotAsync(true);
            Assert.NotNull(screenshot);
            Assert.NotEmpty(screenshot);

            await TestUtils.VerifyViewportAsync(page, 456, 789);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should restore viewport after page screenshot and exception</playwright-it>
        [Fact(Skip = "Skip USES_HOOKS")]
        public void ShouldRestoreViewportAfterPageScreenshotAndException()
        {
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should restore viewport after page screenshot and timeout</playwright-it>
        [Fact(Skip = "Skip USES_HOOKS")]
        public void ShouldRestoreViewportAfterPageScreenshotAndTimeout()
        {
        }


        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should take element screenshot when default viewport is null and restore back</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldTakeElementScreenshotWhenDefaultViewportIsNullAndRestoreBack()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = null,
            });
            var page = await context.NewPageAsync();

            await Page.SetContentAsync(@"
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

            var sizeBefore = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            var elementHandle = await page.QuerySelectorAsync("div.to-screenshot");
            byte[] screenshot = await page.ScreenshotAsync();
            Assert.NotNull(screenshot);
            Assert.NotEmpty(screenshot);

            var sizeAfter = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            Assert.Equal(sizeBefore, sizeAfter);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should restore viewport after element screenshot and exception</playwright-it>
        [Fact(Skip = "Skip USES_HOOKS")]
        public void ShouldRestoreViewportAfterElementScreenshotAndException()
        {
        }
    }
}
