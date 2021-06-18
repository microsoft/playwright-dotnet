using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class ElementHandleContentFrameTests : PageTestEx
    {
        [PlaywrightTest("elementhandle-content-frame.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            var elementHandle = await Page.QuerySelectorAsync("#frame1");
            var frame = await elementHandle.ContentFrameAsync();
            Assert.AreEqual(Page.Frames.ElementAt(1), frame);
        }

        [PlaywrightTest("elementhandle-content-frame.spec.ts", "should work for cross-process iframes")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForCrossProcessIframes()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.CrossProcessPrefix + "/empty.html");
            var elementHandle = await Page.QuerySelectorAsync("#frame1");
            var frame = await elementHandle.ContentFrameAsync();
            Assert.AreEqual(Page.Frames.ElementAt(1), frame);
        }

        [PlaywrightTest("elementhandle-content-frame.spec.ts", "should work for cross-frame evaluations")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForCrossFrameEvaluations()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            var frame = Page.Frames.ElementAt(1);
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => window.top.document.querySelector('#frame1')");
            Assert.AreEqual(frame, await elementHandle.ContentFrameAsync());
        }

        [PlaywrightTest("elementhandle-content-frame.spec.ts", "should return null for non-iframes")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullForNonIframes()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            var frame = Page.Frames.ElementAt(1);
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.body");
            Assert.Null(await elementHandle.ContentFrameAsync());
        }

        [PlaywrightTest("elementhandle-content-frame.spec.ts", "should return null for document.documentElement")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullForDocumentDocumentElement()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            var frame = Page.Frames.ElementAt(1);
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.documentElement");
            Assert.Null(await elementHandle.ContentFrameAsync());
        }
    }
}
