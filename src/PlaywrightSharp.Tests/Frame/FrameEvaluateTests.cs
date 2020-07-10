using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame
{
    ///<playwright-file>frame.spec.js</playwright-file>
    ///<playwright-describe>Frame.evaluate</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class FrameEvaluateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public FrameEvaluateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame.evaluate</playwright-describe>
        ///<playwright-it>should throw for detached frames</playwright-it>
        [Retry]
        public async Task ShouldThrowForDetachedFrames()
        {
            var frame1 = await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await FrameUtils.DetachFrameAsync(Page, "frame1");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => frame1.EvaluateAsync("() => 7 * 8"));
            Assert.Contains("Execution Context is not available in detached frame", exception.Message);
        }

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame.evaluate</playwright-describe>
        ///<playwright-it>should be isolated between frames</playwright-it>
        [Retry]
        public async Task ShouldBeIsolatedBetweenFrames()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            Assert.Equal(2, Page.Frames.Length);
            var frames = Page.Frames;
            Assert.NotSame(frames[0], frames[1]);

            await Task.WhenAll(
                frames[0].EvaluateAsync("() => window.a = 1"),
                frames[1].EvaluateAsync("() => window.a = 2")
            );
            int[] results = await Task.WhenAll(
                frames[0].EvaluateAsync<int>("() => window.a"),
                frames[1].EvaluateAsync<int>("() => window.a")
            );
            Assert.Equal(1, results[0]);
            Assert.Equal(2, results[1]);
        }
    }
}
