using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Tests.Helpers;
using SixLabors.ImageSharp;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>elementhandle-screenshot.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleScreenshotTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleScreenshotTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Page.EvaluateAsync("window.scrollBy(50, 100)");
            var elementHandle = await Page.QuerySelectorAsync(".box:nth-of-type(3)");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-bounding-box.png", screenshot));
        }

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should take into account padding and border")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTakeIntoAccountPaddingAndBorder()
        {
            await Page.SetViewportSizeAsync(500, 500);
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

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should capture full element when larger than viewport in parallel")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCaptureFullElementWhenLargerThanViewportInParallel()
        {
            await Page.SetViewportSizeAsync(500, 500);
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

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should capture full element when larger than viewport")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCaptureFullElementWhenLargerThanViewport()
        {
            await Page.SetViewportSizeAsync(500, 500);
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

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should scroll element into view")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldScrollElementIntoView()
        {
            await Page.SetViewportSizeAsync(500, 500);
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

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should scroll 15000px into view")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should work with a rotated element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithARotatedElement()
        {
            await Page.SetViewportSizeAsync(500, 500);
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

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should fail to screenshot a detached element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailToScreenshotADetachedElement()
        {
            await Page.SetContentAsync("<h1>remove this</h1>");
            var elementHandle = await Page.QuerySelectorAsync("h1");
            await Page.EvaluateAsync("element => element.remove()", elementHandle);

            var exception = await Assert.ThrowsAsync<PlaywrightException>(() => elementHandle.ScreenshotAsync());
            Assert.Contains("Element is not attached to the DOM", exception.Message);
        }

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should timeout waiting for visible")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForVisible()
        {
            await Page.SetContentAsync(@"<div style='width: 50px; height: 0'></div>");
            var elementHandle = await Page.QuerySelectorAsync("div");
            var exception = await Assert.ThrowsAsync<TimeoutException>(() => elementHandle.ScreenshotAsync(timeout: 3000));
            Assert.Contains("Timeout 3000ms exceeded", exception.Message);
            Assert.Contains("element is not visible", exception.Message);
        }

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should wait for visible")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should work for an element with fractional dimensions")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForAnElementWithFractionalDimensions()
        {
            await Page.SetContentAsync("<div style=\"width:48.51px;height:19.8px;border:1px solid black;\"></div>");
            var elementHandle = await Page.QuerySelectorAsync("div");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-fractional.png", screenshot));
        }

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should work with a mobile viewport")]
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

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should work with device scale factor")]
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

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should work for an element with an offset")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForAnElementWithAnOffset()
        {
            await Page.SetContentAsync("<div style=\"position:absolute; top: 10.3px; left: 20.4px;width:50.3px;height:20.2px;border:1px solid black;\"></div>");
            var elementHandle = await Page.QuerySelectorAsync("div");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-fractional-offset.png", screenshot));
        }

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should take screenshots when default viewport is null")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTakeScreenshotsWhenDefaultViewportIsNull()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = ViewportSize.NoViewport
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

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should take fullPage screenshots when default viewport is null")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTakeFullPageScreenshotsWhenDefaultViewportIsNull()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = ViewportSize.NoViewport
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var sizeBefore = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");

            byte[] screenshot = await page.ScreenshotAsync(fullPage: true);
            Assert.NotNull(screenshot);
            Assert.NotEmpty(screenshot);

            var sizeAfter = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            Assert.Equal(sizeBefore, sizeAfter);
        }

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should restore default viewport after fullPage screenshot")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRestoreDefaultViewportAfterFullPageScreenshot()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize { Width = 456, Height = 789 },
            });
            var page = await context.NewPageAsync();
            await TestUtils.VerifyViewportAsync(page, 456, 789);
            byte[] screenshot = await page.ScreenshotAsync(fullPage: true);
            Assert.NotNull(screenshot);
            Assert.NotEmpty(screenshot);

            await TestUtils.VerifyViewportAsync(page, 456, 789);
        }

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should restore viewport after page screenshot and exception")]
        [Fact(Skip = "Skip USES_HOOKS")]
        public void ShouldRestoreViewportAfterPageScreenshotAndException()
        {
        }

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should restore viewport after page screenshot and timeout")]
        [Fact(Skip = "Skip USES_HOOKS")]
        public void ShouldRestoreViewportAfterPageScreenshotAndTimeout()
        {
        }


        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should take element screenshot when default viewport is null and restore back")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTakeElementScreenshotWhenDefaultViewportIsNullAndRestoreBack()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = ViewportSize.NoViewport,
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

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should restore viewport after element screenshot and exception")]
        [Fact(Skip = "Skip USES_HOOKS")]
        public void ShouldRestoreViewportAfterElementScreenshotAndException()
        {
        }

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "should take screenshot of disabled button")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTakeScreenshotOfDisabledButton()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.SetContentAsync("<button disabled>Click me</button>");
            var button = await Page.QuerySelectorAsync("button");
            byte[] screenshot = await button.ScreenshotAsync();
            Assert.NotNull(screenshot);
            Assert.NotEmpty(screenshot);
        }

        [PlaywrightTest("elementhandle-screenshot.spec.ts", "path option should create subdirectories")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PathOptionShouldCreateSubdirectories()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Page.EvaluateAsync("() => window.scrollBy(50, 100)");
            var elementHandle = await Page.QuerySelectorAsync(".box:nth-of-type(3)");
            using var tmpDir = new TempDirectory();
            string outputPath = Path.Combine(tmpDir.Path, "these", "are", "directories", "screenshot.png");
            await elementHandle.ScreenshotAsync(outputPath);
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-bounding-box.png", outputPath));
        }
    }
}
