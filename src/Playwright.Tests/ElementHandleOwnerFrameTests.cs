using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class ElementHandleOwnerFrameTests : PageTestEx
    {
        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            var frame = Page.Frames.ElementAt(1);
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.body");
            Assert.AreEqual(frame, await elementHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for cross-process iframes")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForCrossProcessIframes()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.CrossProcessPrefix + "/empty.html");
            var frame = Page.Frames.ElementAt(1);
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.body");
            Assert.AreEqual(frame, await elementHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for document")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForDocument()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            var frame = Page.Frames.ElementAt(1);
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document");
            Assert.AreEqual(frame, await elementHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for iframe elements")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForIframeElements()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            var frame = Page.MainFrame;
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.querySelector('#frame1')");
            Assert.AreEqual(frame, await elementHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for cross-frame evaluations")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForCrossFrameEvaluations()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            var frame = Page.MainFrame;
            var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.querySelector('#frame1').contentWindow.document.body");
            Assert.AreEqual(frame.ChildFrames.First(), await elementHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for detached elements")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForDetachedElements()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var divHandle = (IElementHandle)await Page.EvaluateHandleAsync(@"() => {
                    var div = document.createElement('div');
                    document.body.appendChild(div);
                    return div;
                }");
            Assert.AreEqual(Page.MainFrame, await divHandle.OwnerFrameAsync());
            await Page.EvaluateAsync(@"() => {
                    var div = document.querySelector('div');
                    document.body.removeChild(div);
                }");
            Assert.AreEqual(Page.MainFrame, await divHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for adopted elements")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldWorkForAdoptedElements()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var popupTask = Page.WaitForPopupAsync();
            await TaskUtils.WhenAll(
              popupTask,
              Page.EvaluateAsync("url => window.__popup = window.open(url)", Server.EmptyPage));
            var popup = await popupTask;
            var divHandle = (IElementHandle)await Page.EvaluateHandleAsync(@"() => {
                    var div = document.createElement('div');
                    document.body.appendChild(div);
                    return div;
                }");
            Assert.AreEqual(Page.MainFrame, await divHandle.OwnerFrameAsync());
            await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.EvaluateAsync(@"() => {
                    var div = document.querySelector('div');
                    window.__popup.document.body.appendChild(div);
                }");
            Assert.AreEqual(popup.MainFrame, await divHandle.OwnerFrameAsync());
        }
    }
}
