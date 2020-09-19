using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame
{
    ///<playwright-file>frame.spec.js</playwright-file>
    ///<playwright-describe>Frame Management</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class FrameManagementTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public FrameManagementTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame Management</playwright-describe>
        ///<playwright-it>should handle nested frames</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldHandleNestedFrames()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/nested-frames.html");
            Assert.Equal(TestConstants.NestedFramesDumpResult, FrameUtils.DumpFrames(Page.MainFrame));
        }

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame Management</playwright-describe>
        ///<playwright-it>should send events when frames are manipulated dynamically</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSendEventsWhenFramesAreManipulatedDynamically()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            // validate frameattached events
            var attachedFrames = new List<IFrame>();
            Page.FrameAttached += (sender, e) => attachedFrames.Add(e.Frame);
            await FrameUtils.AttachFrameAsync(Page, "frame1", "./assets/frame.html");
            Assert.Single(attachedFrames);
            Assert.Contains("/assets/frame.html", attachedFrames[0].Url);

            // validate framenavigated events
            var navigatedFrames = new List<IFrame>();
            Page.FrameNavigated += (sender, e) => navigatedFrames.Add(e.Frame);
            await Page.EvaluateAsync(@"() => {
                const frame = document.getElementById('frame1');
                frame.src = './empty.html';
                return new Promise(x => frame.onload = x);
            }");
            Assert.Single(navigatedFrames);
            Assert.Equal(TestConstants.EmptyPage, navigatedFrames[0].Url);

            // validate framedetached events
            var detachedFrames = new List<IFrame>();
            Page.FrameDetached += (sender, e) => detachedFrames.Add(e.Frame);
            await FrameUtils.DetachFrameAsync(Page, "frame1");
            Assert.Single(detachedFrames);
            Assert.True(detachedFrames[0].IsDetached);
        }

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame Management</playwright-describe>
        ///<playwright-it>should send "framenavigated" when navigating on anchor URLs</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSendFrameNavigatedWhenNavigatingOnAnchorURLs()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await TaskUtils.WhenAll(
                Page.GoToAsync(TestConstants.EmptyPage + "#foo"),
                Page.WaitForEvent<FrameEventArgs>(PageEvent.FrameNavigated)
            );
            Assert.Equal(TestConstants.EmptyPage + "#foo", Page.Url);
        }

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame Management</playwright-describe>
        ///<playwright-it>should persist mainFrame on cross-process navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldPersistMainFrameOnCrossProcessNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var mainFrame = Page.MainFrame;
            await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/empty.html");
            Assert.Same(mainFrame, Page.MainFrame);
        }

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame Management</playwright-describe>
        ///<playwright-it>should not send attach/detach events for main frame</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotSendAttachDetachEventsForMainFrame()
        {
            bool hasEvents = false;
            Page.FrameAttached += (sender, e) => hasEvents = true;
            Page.FrameDetached += (sender, e) => hasEvents = true;
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.False(hasEvents);
        }

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame Management</playwright-describe>
        ///<playwright-it>should detach child frames on navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDetachChildFramesOnNavigation()
        {
            var attachedFrames = new List<IFrame>();
            var detachedFrames = new List<IFrame>();
            var navigatedFrames = new List<IFrame>();
            Page.FrameAttached += (sender, e) => attachedFrames.Add(e.Frame);
            Page.FrameDetached += (sender, e) => detachedFrames.Add(e.Frame);
            Page.FrameNavigated += (sender, e) => navigatedFrames.Add(e.Frame);
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

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame Management</playwright-describe>
        ///<playwright-it>should support framesets</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportFramesets()
        {
            var attachedFrames = new List<IFrame>();
            var detachedFrames = new List<IFrame>();
            var navigatedFrames = new List<IFrame>();
            Page.FrameAttached += (sender, e) => attachedFrames.Add(e.Frame);
            Page.FrameDetached += (sender, e) => detachedFrames.Add(e.Frame);
            Page.FrameNavigated += (sender, e) => navigatedFrames.Add(e.Frame);
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

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame Management</playwright-describe>
        ///<playwright-it>should report frame from-inside shadow DOM</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportFrameFromInsideShadowDOM()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/shadow.html");
            await Page.EvaluateAsync(@"async url => {
                const frame = document.createElement('iframe');
                frame.src = url;
                document.body.shadowRoot.appendChild(frame);
                await new Promise(x => frame.onload = x);
            }", TestConstants.EmptyPage);
            Assert.Equal(2, Page.Frames.Length);
            Assert.Equal(TestConstants.EmptyPage, Page.Frames[1].Url);
        }

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame Management</playwright-describe>
        ///<playwright-it>should report frame.name()</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
            Assert.Empty(Page.Frames[0].Name);
            Assert.Equal("theFrameId", Page.Frames[1].Name);
            Assert.Equal("theFrameName", Page.Frames[2].Name);
        }

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame Management</playwright-describe>
        ///<playwright-it>should report frame.parent()</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportFrameParent()
        {
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame2", TestConstants.EmptyPage);
            Assert.Null(Page.Frames[0].ParentFrame);
            Assert.Same(Page.MainFrame, Page.Frames[1].ParentFrame);
            Assert.Same(Page.MainFrame, Page.Frames[2].ParentFrame);
        }

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame Management</playwright-describe>
        ///<playwright-it>should report different frame instance when frame re-attaches</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportDifferentFrameInstanceWhenFrameReAttaches()
        {
            var frame1 = await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"() => {
                window.frame = document.querySelector('#frame1');
                window.frame.remove();
            }");
            Assert.True(frame1.IsDetached);

            var (frame2, _) = await TaskUtils.WhenAll(
              Page.WaitForEvent<FrameEventArgs>(PageEvent.FrameNavigated),
              Page.EvaluateAsync("() => document.body.appendChild(window.frame)")
            );

            Assert.False(frame2.Frame.IsDetached);
            Assert.NotSame(frame1, frame2);
        }
    }
}
