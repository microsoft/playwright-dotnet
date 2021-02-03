using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.contentFrame</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleContentFrameTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleContentFrameTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.contentFrame", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var elementHandle = await Page.QuerySelectorAsync("#frame1");
            var frame = await elementHandle.GetContentFrameAsync();
            Assert.Equal(Page.Frames[1], frame);
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.contentFrame", "should work for cross-process iframes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForCrossProcessIframes()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.CrossProcessUrl + "/empty.html");
            var elementHandle = await Page.QuerySelectorAsync("#frame1");
            var frame = await elementHandle.GetContentFrameAsync();
            Assert.Equal(Page.Frames[1], frame);
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.contentFrame", "should work for cross-frame evaluations")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForCrossFrameEvaluations()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var frame = Page.Frames[1];
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => window.top.document.querySelector('#frame1')");
            Assert.Equal(frame, await elementHandle.GetContentFrameAsync());
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.contentFrame", "should return null for non-iframes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullForNonIframes()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var frame = Page.Frames[1];
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.body");
            Assert.Null(await elementHandle.GetContentFrameAsync());
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.contentFrame", "should return null for document.documentElement")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullForDocumentDocumentElement()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var frame = Page.Frames[1];
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.documentElement");
            Assert.Null(await elementHandle.GetContentFrameAsync());
        }
    }
}
