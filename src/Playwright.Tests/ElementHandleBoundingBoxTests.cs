using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleBoundingBoxTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleBoundingBoxTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var elementHandle = await Page.QuerySelectorAsync(".box:nth-of-type(13)");
            var box = await elementHandle.BoundingBoxAsync();
            Assert.Equal(new ElementHandleBoundingBoxResult(x: 100, y: 50, width: 50, height: 50), box);
        }

        [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should handle nested frames")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHandleNestedFrames()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/nested-frames.html");
            var nestedFrame = Page.Frames.First(frame => frame.Name == "dos");
            var elementHandle = await nestedFrame.QuerySelectorAsync("div");
            var box = await elementHandle.BoundingBoxAsync();
            Assert.Equal(new ElementHandleBoundingBoxResult(x: 24, y: 224, width: 268, height: 18), box);
        }

        [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should return null for invisible elements")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullForInvisibleElements()
        {
            await Page.SetContentAsync("<div style=\"display:none\">hi</div>");
            var element = await Page.QuerySelectorAsync("div");
            Assert.Null(await element.BoundingBoxAsync());
        }

        [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should force a layout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldForceALayout()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.SetContentAsync("<div style=\"width: 100px; height: 100px\">hello</div>");
            var elementHandle = await Page.QuerySelectorAsync("div");
            await Page.EvaluateAsync("element => element.style.height = '200px'", elementHandle);
            var box = await elementHandle.BoundingBoxAsync();
            Assert.Equal(new ElementHandleBoundingBoxResult(x: 8, y: 8, width: 100, height: 200), box);
        }

        [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should work with SVG nodes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithSVGNodes()
        {
            await Page.SetContentAsync(@"
                  <svg xmlns=""http://www.w3.org/2000/svg"" width=""500"" height=""500"">
                    <rect id=""theRect"" x=""30"" y=""50"" width=""200"" height=""300""></rect>
                  </svg>");
            var element = await Page.QuerySelectorAsync("#therect");
            var pwBoundingBox = await element.BoundingBoxAsync();
            var webBoundingBox = await Page.EvaluateAsync<ElementHandleBoundingBoxResult>(@"e => {
                    const rect = e.getBoundingClientRect();
                    return { x: rect.x, y: rect.y, width: rect.width, height: rect.height};
                }", element);
            Assert.Equal(webBoundingBox, pwBoundingBox);
        }

        [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should work with page scale")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithPageScale()
        {
            var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Height = 400,
                    Width = 400,
                },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await page.QuerySelectorAsync("button");

            await button.EvaluateAsync(@"button => {
                document.body.style.margin = '0';
                  button.style.borderWidth = '0';
                  button.style.width = '200px';
                  button.style.height = '20px';
                  button.style.marginLeft = '17px';
                  button.style.marginTop = '23px';
            }");

            var box = await button.BoundingBoxAsync();
            Assert.Equal(17 * 100, Math.Round(box.X * 100));
            Assert.Equal(23 * 100, Math.Round(box.Y * 100));
            Assert.Equal(200 * 100, Math.Round(box.Width * 100));
            Assert.Equal(20 * 100, Math.Round(box.Height * 100));
        }

        [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should work when inline box child is outside of viewport")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenInlineBoxChildIsOutsideOfViewport()
        {
            await Page.SetContentAsync(@"
                <style>
                i {
                    position: absolute;
                    top: -1000px;
                }
                body {
                    margin: 0;
                    font-size: 12px;
                }
                </style>
                <span><i>woof</i><b>doggo</b></span>");

            var handle = await Page.QuerySelectorAsync("span");
            var box = await handle.BoundingBoxAsync();
            var webBoundingBox = await Page.EvaluateAsync<ElementHandleBoundingBoxResult>(@"e => {
                    const rect = e.getBoundingClientRect();
                    return { x: rect.x, y: rect.y, width: rect.width, height: rect.height};
                }", handle);

            Assert.Equal(Math.Round(webBoundingBox.X * 100), Math.Round(box.X * 100));
            Assert.Equal(Math.Round(webBoundingBox.Y * 100), Math.Round(box.Y * 100));
            Assert.Equal(Math.Round(webBoundingBox.Width * 100), Math.Round(box.Width * 100));
            Assert.Equal(Math.Round(webBoundingBox.Height * 100), Math.Round(box.Height * 100));
        }
    }
}
