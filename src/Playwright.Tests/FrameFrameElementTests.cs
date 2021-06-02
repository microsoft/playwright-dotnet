using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class FrameFrameElementTests : PageTestEx
    {
        [PlaywrightTest("frame-frame-element.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var frame1 = await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            var frame2 = await FrameUtils.AttachFrameAsync(Page, "frame2", Server.EmptyPage);
            var frame3 = await FrameUtils.AttachFrameAsync(Page, "frame3", Server.EmptyPage);

            var frame1handle1 = await Page.QuerySelectorAsync("#frame1");
            var frame1handle2 = await frame1.FrameElementAsync();
            var frame3handle1 = await Page.QuerySelectorAsync("#frame3");
            var frame3handle2 = await frame3.FrameElementAsync();

            Assert.True(await frame1handle1.EvaluateAsync<bool>("(a, b) => a === b", frame1handle2));
            Assert.True(await frame3handle1.EvaluateAsync<bool>("(a, b) => a === b", frame3handle2));
            Assert.False(await frame1handle1.EvaluateAsync<bool>("(a, b) => a === b", frame3handle2));

            var windowHandle = await Page.MainFrame.EvaluateHandleAsync("() => window");
            Assert.NotNull(windowHandle);
        }

        [PlaywrightTest("frame-frame-element.spec.ts", "should work with contentFrame")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithContentFrame()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var frame = await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            var handle = await frame.FrameElementAsync();
            var contentFrame = await handle.ContentFrameAsync();

            Assert.AreEqual(contentFrame, frame);
        }

        [PlaywrightTest("frame-frame-element.spec.ts", "should throw when detached")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenDetached()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var frame1 = await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            await Page.EvalOnSelectorAsync("#frame1", "e => e.remove()");

            var exception = await AssertThrowsAsync<PlaywrightException>(() => frame1.FrameElementAsync());

            Assert.AreEqual("Frame has been detached.", exception.Message);
        }
    }
}
