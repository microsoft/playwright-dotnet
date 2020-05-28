using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.boundingBox</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleBoundingBoxTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleBoundingBoxTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.boundingBox</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            await Page.SetViewportAsync(new Viewport { Width = 500, Height = 500 });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var elementHandle = await Page.QuerySelectorAsync(".box:nth-of-type(13)");
            var box = await elementHandle.GetBoundingBoxAsync();
            Assert.Equal(new Rect(x: 100, y: 50, width: 50, height: 50), box);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.boundingBox</playwright-describe>
        ///<playwright-it>should handle nested frames</playwright-it>
        [Retry]
        public async Task ShouldHandleNestedFrames()
        {
            await Page.SetViewportAsync(new Viewport { Width = 500, Height = 500 });
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/nested-frames.html");
            var nestedFrame = Page.Frames.First(frame => frame.Name == "dos");
            var elementHandle = await nestedFrame.QuerySelectorAsync("div");
            var box = await elementHandle.GetBoundingBoxAsync();
            Assert.Equal(new Rect(x: 24, y: 224, width: 268, height: 18), box);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.boundingBox</playwright-describe>
        ///<playwright-it>should return null for invisible elements</playwright-it>
        [Retry]
        public async Task ShouldReturnNullForInvisibleElements()
        {
            await Page.SetContentAsync("<div style=\"display:none\">hi</div>");
            var element = await Page.QuerySelectorAsync("div");
            Assert.Null(await element.GetBoundingBoxAsync());
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.boundingBox</playwright-describe>
        ///<playwright-it>should force a layout</playwright-it>
        [Retry]
        public async Task ShouldForceALayout()
        {
            await Page.SetViewportAsync(new Viewport { Width = 500, Height = 500 });
            await Page.SetContentAsync("<div style=\"width: 100px; height: 100px\">hello</div>");
            var elementHandle = await Page.QuerySelectorAsync("div");
            await Page.EvaluateAsync("element => element.style.height = '200px'", elementHandle);
            var box = await elementHandle.GetBoundingBoxAsync();
            Assert.Equal(new Rect(x: 8, y: 8, width: 100, height: 200), box);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.boundingBox</playwright-describe>
        ///<playwright-it>should work with SVG nodes</playwright-it>
        [Retry]
        public async Task ShouldWorkWithSVGNodes()
        {
            await Page.SetContentAsync(@"
                  <svg xmlns=""http://www.w3.org/2000/svg"" width=""500"" height=""500"">
                    <rect id=""theRect"" x=""30"" y=""50"" width=""200"" height=""300""></rect>
                  </svg>");
            var element = await Page.QuerySelectorAsync("#therect");
            var pwBoundingBox = await element.GetBoundingBoxAsync();
            var webBoundingBox = await Page.EvaluateAsync<Rect>(@"e => {
                    const rect = e.getBoundingClientRect();
                    return { x: rect.x, y: rect.y, width: rect.width, height: rect.height};
                }", element);
            Assert.Equal(webBoundingBox, pwBoundingBox);
        }
    }
}
