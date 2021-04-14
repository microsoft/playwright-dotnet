using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleOwnerFrameTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleOwnerFrameTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var frame = Page.Frames.ElementAt(1);
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.body");
            Assert.Equal(frame, await elementHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for cross-process iframes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForCrossProcessIframes()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.CrossProcessUrl + "/empty.html");
            var frame = Page.Frames.ElementAt(1);
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.body");
            Assert.Equal(frame, await elementHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for document")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForDocument()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var frame = Page.Frames.ElementAt(1);
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document");
            Assert.Equal(frame, await elementHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for iframe elements")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForIframeElements()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var frame = Page.MainFrame;
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.querySelector('#frame1')");
            Assert.Equal(frame, await elementHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for cross-frame evaluations")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForCrossFrameEvaluations()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var frame = Page.MainFrame;
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.querySelector('#frame1').contentWindow.document.body");
            Assert.Equal(frame.ChildFrames.First(), await elementHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for detached elements")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForDetachedElements()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var divHandle = (IElementHandle)await Page.EvaluateHandleAsync(@"() => {
                    var div = document.createElement('div');
                    document.body.appendChild(div);
                    return div;
                }");
            Assert.Equal(Page.MainFrame, await divHandle.OwnerFrameAsync());
            await Page.EvaluateAsync(@"() => {
                    var div = document.querySelector('div');
                    document.body.removeChild(div);
                }");
            Assert.Equal(Page.MainFrame, await divHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for adopted elements")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkForAdoptedElements()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);
            await TaskUtils.WhenAll(
              popupTask,
              Page.EvaluateAsync("url => window.__popup = window.open(url)", TestConstants.EmptyPage));
            var popup = await popupTask;
            var divHandle = (IElementHandle)await Page.EvaluateHandleAsync(@"() => {
                    var div = document.createElement('div');
                    document.body.appendChild(div);
                    return div;
                }");
            Assert.Equal(Page.MainFrame, await divHandle.OwnerFrameAsync());
            await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.EvaluateAsync(@"() => {
                    var div = document.querySelector('div');
                    window.__popup.document.body.appendChild(div);
                }");
            Assert.Same(popup.MainFrame, await divHandle.OwnerFrameAsync());
        }
    }
}
