using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame
{
    ///<playwright-file>frame.spec.js</playwright-file>
    ///<playwright-describe>Frame.evaluate</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class FrameEvaluateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public FrameEvaluateTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("frame.spec.js", "Frame.evaluate", "should throw for detached frames")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForDetachedFrames()
        {
            var frame1 = await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await FrameUtils.DetachFrameAsync(Page, "frame1");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => frame1.EvaluateAsync("() => 7 * 8"));
            Assert.Contains("Execution Context is not available in detached frame", exception.Message);
        }

        [PlaywrightTest("frame.spec.js", "Frame.evaluate", "should be isolated between frames")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeIsolatedBetweenFrames()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            Assert.Equal(2, Page.Frames.Length);
            var frames = Page.Frames;
            Assert.NotSame(frames[0], frames[1]);

            await TaskUtils.WhenAll(
                frames[0].EvaluateAsync("() => window.a = 1"),
                frames[1].EvaluateAsync("() => window.a = 2")
            );

            var (result1, result2) = await TaskUtils.WhenAll(
                frames[0].EvaluateAsync<int>("() => window.a"),
                frames[1].EvaluateAsync<int>("() => window.a")
            );
            Assert.Equal(1, result1);
            Assert.Equal(2, result2);
        }

        [PlaywrightTest("frame.spec.js", "Frame.evaluate", "should work in iframes that failed initial navigation")]
        [SkipBrowserAndPlatformFact(skipChromium: true, skipFirefox: true)]
        public async Task ShouldWorkIniframesThatFailedInitialNavigation()
        {
            await Page.SetContentAsync(
                @"<meta http-equiv=""Content-Security-Policy"" content=""script-src 'none';"">
                 <iframe src='javascript:""""'></iframe>",
                LifecycleEvent.DOMContentLoaded);

            await Page.EvaluateAsync(@"() => {
                const iframe = document.querySelector('iframe');
                const div = iframe.contentDocument.createElement('div');
                iframe.contentDocument.body.appendChild(div);
            }");

            Assert.Equal("about:blank", Page.Frames[1].Url);
            Assert.Equal("about:blank", await Page.Frames[1].EvaluateAsync<string>("() => window.location.href"));
            Assert.NotNull(await Page.Frames[1].QuerySelectorAsync("DIV"));
        }

        [PlaywrightTest("frame.spec.js", "Frame.evaluate", "should work in iframes that failed initial navigation")]
        [SkipBrowserAndPlatformFact(skipChromium: true, skipFirefox: true)]
        public async Task ShouldWorkInIframesThatInterruptedInitialJavascriptUrlNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await Page.EvaluateAsync(@"() => {
                const iframe = document.createElement('iframe');
                iframe.src = 'javascript:""""';
                document.body.appendChild(iframe);
                iframe.contentDocument.open();
                iframe.contentDocument.write('<div>hello</div>');
                iframe.contentDocument.close();
            }");

            Assert.Equal(TestConstants.EmptyPage, await Page.Frames[1].EvaluateAsync<string>("() => window.location.href"));
            Assert.NotNull(await Page.Frames[1].QuerySelectorAsync("DIV"));
        }
    }
}
