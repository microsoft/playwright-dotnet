using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>screenshot.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
    public class ElementHandleScreenshotTests : PlaywrightSharpPageBaseTest
    {
        internal ElementHandleScreenshotTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.SetViewportAsync(new Viewport
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
        [Fact]
        public async Task ShouldTakeIntoAccountPaddingAndBorder()
        {
            await Page.SetViewportAsync(new Viewport
            {
                Width = 500,
                Height = 500
            });
            await Page.SetContentAsync(@"
                something above
                <style> div {
                    border: 2px solid blue;
                    background: green;
                    width: 50px;
                    height: 50px;
                }
                </style>
                <div></div>
            ");
            var elementHandle = await Page.QuerySelectorAsync("div");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-padding-border.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should capture full element when larger than viewport in parallel</playwright-it>
        [Fact]
        public async Task ShouldCaptureFullElementWhenLargerThanViewportInParallel()
        {
            await Page.SetViewportAsync(new Viewport
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
            var screenshotTasks = elementHandles.Select(e => e.ScreenshotAsync());
            await Task.WhenAll(screenshotTasks);

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-padding-border.png", screenshotTasks.ElementAt(2).Result));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should capture full element when larger than viewport</playwright-it>
        [Fact]
        public async Task ShouldCaptureFullElementWhenLargerThanViewport()
        {
            await Page.SetViewportAsync(new Viewport
            {
                Width = 500,
                Height = 500
            });
            await Page.SetContentAsync(@"
                something above
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
                <div class='to-screenshot'></div>"
            );
            var elementHandle = await Page.QuerySelectorAsync("div.to-screenshot");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-larger-than-Viewport.png", screenshot));
            Assert.Equal(new[] { 500, 500 }, await Page.EvaluateAsync<int[]>("[window.innerWidth, window.innerHeight]"));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should scroll element into view</playwright-it>
        [Fact]
        public async Task ShouldScrollElementIntoView()
        {
            await Page.SetViewportAsync(new Viewport
            {
                Width = 500,
                Height = 500
            });
            await Page.SetContentAsync(@"
                something above
                <style> div.above {
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
                <div class='above'></div>
                <div class='to-screenshot'></div>
            ");
            var elementHandle = await Page.QuerySelectorAsync("div.to-screenshot");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-scrolled-into-view.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.screenshot</playwright-describe>
        ///<playwright-it>should work with a rotated element</playwright-it>
        [Fact]
        public async Task ShouldWorkWithARotatedElement()
        {
            await Page.SetViewportAsync(new Viewport
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
        [Fact]
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
        [Fact]
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
        [Fact]
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
        [Fact]
        public async Task ShouldWorkForAnElementWithAnOffset()
        {
            await Page.SetContentAsync("<div style=\"position:absolute; top: 10.3px; left: 20.4px;width:50.3px;height:20.2px;border:1px solid black;\"></div>");
            var elementHandle = await Page.QuerySelectorAsync("div");
            byte[] screenshot = await elementHandle.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-fractional-offset.png", screenshot));
        }
    }
}
