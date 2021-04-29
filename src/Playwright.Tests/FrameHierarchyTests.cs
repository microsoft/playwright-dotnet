using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Testing.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class FrameHierarchyTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public FrameHierarchyTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("frame-hierarchy.spec.ts", "should handle nested frames")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHandleNestedFrames()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/nested-frames.html");
            Assert.Equal(TestConstants.NestedFramesDumpResult, FrameUtils.DumpFrames(Page.MainFrame));
        }

        [PlaywrightTest("frame-hierarchy.spec.ts", "should send events when frames are manipulated dynamically")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSendEventsWhenFramesAreManipulatedDynamically()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            // validate frameattached events
            var attachedFrames = new List<IFrame>();
            Page.FrameAttached += (_, e) => attachedFrames.Add(e);
            await FrameUtils.AttachFrameAsync(Page, "frame1", "./assets/frame.html");
            Assert.Single(attachedFrames);
            Assert.Contains("/assets/frame.html", attachedFrames[0].Url);

            // validate framenavigated events
            var navigatedFrames = new List<IFrame>();
            Page.FrameNavigated += (_, e) => navigatedFrames.Add(e);
            await Page.EvaluateAsync(@"() => {
                const frame = document.getElementById('frame1');
                frame.src = './empty.html';
                return new Promise(x => frame.onload = x);
            }");
            Assert.Single(navigatedFrames);
            Assert.Equal(TestConstants.EmptyPage, navigatedFrames[0].Url);

            // validate framedetached events
            var detachedFrames = new List<IFrame>();
            Page.FrameDetached += (_, e) => detachedFrames.Add(e);
            await FrameUtils.DetachFrameAsync(Page, "frame1");
            Assert.Single(detachedFrames);
            Assert.True(detachedFrames[0].IsDetached);
        }

        [PlaywrightTest("frame-hierarchy.spec.ts", @"should send ""framenavigated"" when navigating on anchor URLs")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSendFrameNavigatedWhenNavigatingOnAnchorURLs()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.FrameNavigated),
                Page.GoToAsync(TestConstants.EmptyPage + "#foo"));

            Assert.Equal(TestConstants.EmptyPage + "#foo", Page.Url);
        }

        [PlaywrightTest("frame-hierarchy.spec.ts", "should persist mainFrame on cross-process navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldPersistMainFrameOnCrossProcessNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var mainFrame = Page.MainFrame;
            await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/empty.html");
            Assert.Same(mainFrame, Page.MainFrame);
        }

        [PlaywrightTest("frame-hierarchy.spec.ts", "should not send attach/detach events for main frame")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotSendAttachDetachEventsForMainFrame()
        {
            bool hasEvents = false;
            Page.FrameAttached += (_, _) => hasEvents = true;
            Page.FrameDetached += (_, _) => hasEvents = true;
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.False(hasEvents);
        }

        [PlaywrightTest("frame-hierarchy.spec.ts", "should detach child frames on navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDetachChildFramesOnNavigation()
        {
            var attachedFrames = new List<IFrame>();
            var detachedFrames = new List<IFrame>();
            var navigatedFrames = new List<IFrame>();
            Page.FrameAttached += (_, e) => attachedFrames.Add(e);
            Page.FrameDetached += (_, e) => detachedFrames.Add(e);
            Page.FrameNavigated += (_, e) => navigatedFrames.Add(e);
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/nested-frames.html");
            Assert.Equal(4, attachedFrames.Count);
            Assert.Empty(detachedFrames);
            Assert.Equal(5, navigatedFrames.Count);

            attachedFrames.Clear();
            detachedFrames.Clear();
            navigatedFrames.Clear();
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Empty(attachedFrames);
            Assert.Equal(4, detachedFrames.Count);
            Assert.Single(navigatedFrames);
        }

        [PlaywrightTest("frame-hierarchy.spec.ts", "should support framesets")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportFramesets()
        {
            var attachedFrames = new List<IFrame>();
            var detachedFrames = new List<IFrame>();
            var navigatedFrames = new List<IFrame>();
            Page.FrameAttached += (_, e) => attachedFrames.Add(e);
            Page.FrameDetached += (_, e) => detachedFrames.Add(e);
            Page.FrameNavigated += (_, e) => navigatedFrames.Add(e);
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/frameset.html");
            Assert.Equal(4, attachedFrames.Count);
            Assert.Empty(detachedFrames);
            Assert.Equal(5, navigatedFrames.Count);

            attachedFrames.Clear();
            detachedFrames.Clear();
            navigatedFrames.Clear();
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Empty(attachedFrames);
            Assert.Equal(4, detachedFrames.Count);
            Assert.Single(navigatedFrames);
        }

        [PlaywrightTest("frame-hierarchy.spec.ts", "should report frame from-inside shadow DOM")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportFrameFromInsideShadowDOM()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/shadow.html");
            await Page.EvaluateAsync(@"async url => {
                const frame = document.createElement('iframe');
                frame.src = url;
                document.body.shadowRoot.appendChild(frame);
                await new Promise(x => frame.onload = x);
            }", TestConstants.EmptyPage);
            Assert.Equal(2, Page.Frames.Count);
            Assert.Equal(TestConstants.EmptyPage, Page.Frames.ElementAt(1).Url);
        }

        [PlaywrightTest("frame-hierarchy.spec.ts", "should report frame.name()")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportFrameName()
        {
            await FrameUtils.AttachFrameAsync(Page, "theFrameId", TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"url => {
                const frame = document.createElement('iframe');
                frame.name = 'theFrameName';
                frame.src = url;
                document.body.appendChild(frame);
                return new Promise(x => frame.onload = x);
            }", TestConstants.EmptyPage);
            Assert.Empty(Page.Frames.First().Name);
            Assert.Equal("theFrameId", Page.Frames.ElementAt(1).Name);
            Assert.Equal("theFrameName", Page.Frames.ElementAt(2).Name);
        }

        [PlaywrightTest("frame-hierarchy.spec.ts", "should report frame.parent()")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportFrameParent()
        {
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame2", TestConstants.EmptyPage);
            Assert.Null(Page.Frames.First().ParentFrame);
            Assert.Same(Page.MainFrame, Page.Frames.ElementAt(1).ParentFrame);
            Assert.Same(Page.MainFrame, Page.Frames.ElementAt(2).ParentFrame);
        }

        [PlaywrightTest("frame-hierarchy.spec.ts", "should report different frame instance when frame re-attaches")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportDifferentFrameInstanceWhenFrameReAttaches()
        {
            var frame1 = await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"() => {
                window.frame = document.querySelector('#frame1');
                window.frame.remove();
            }");
            Assert.True(frame1.IsDetached);

            var (frame2, _) = await TaskUtils.WhenAll(
              Page.WaitForEventAsync(PageEvent.FrameNavigated),
              Page.EvaluateAsync("() => document.body.appendChild(window.frame)")
            );

            Assert.False(frame2.IsDetached);
            Assert.NotSame(frame1, frame2);
        }
    }
}
