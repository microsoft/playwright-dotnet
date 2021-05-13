using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class FrameFrameElementTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public FrameFrameElementTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("frame-frame-element.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var frame1 = await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var frame2 = await FrameUtils.AttachFrameAsync(Page, "frame2", TestConstants.EmptyPage);
            var frame3 = await FrameUtils.AttachFrameAsync(Page, "frame3", TestConstants.EmptyPage);

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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithContentFrame()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var frame = await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var handle = await frame.FrameElementAsync();
            var contentFrame = await handle.ContentFrameAsync();

            Assert.Same(contentFrame, frame);
        }

        [PlaywrightTest("frame-frame-element.spec.ts", "should throw when detached")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenDetached()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var frame1 = await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await Page.EvalOnSelectorAsync("#frame1", "e => e.remove()");

            var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => frame1.FrameElementAsync());

            Assert.Equal("Frame has been detached.", exception.Message);
        }
    }
}
