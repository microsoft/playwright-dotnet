using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageWaitForSelector1Tests : PlaywrightSharpPageBaseTest
    {
        private const string AddElement = "tag => document.body.appendChild(document.createElement(tag))";

        /// <inheritdoc/>
        public PageWaitForSelector1Tests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should throw on waitFor")]
        [Fact(Skip = "We don't need to test this")]
        public void ShouldThrowOnWaitFor()
        {
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should tolerate waitFor=visible")]
        [Fact(Skip = "We don't need to test this")]
        public void ShouldTolerateWaitForVisible()
        {
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should immediately resolve promise if node exists")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldImmediatelyResolveTaskIfNodeExists()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var frame = Page.MainFrame;
            await frame.WaitForSelectorAsync("*");
            await frame.EvaluateAsync(AddElement, "div");
            await frame.WaitForSelectorAsync("div", WaitForSelectorState.Attached);
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "elementHandle.waitForSelector should immediately resolve if node exists")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ElementHandleWaitForSelectorShouldImmediatelyResolveIfNodeExists()
        {
            await Page.SetContentAsync("<span>extra</span><div><span>target</span></div>");
            var div = await Page.QuerySelectorAsync("div");
            var span = await div.WaitForSelectorAsync("span", WaitForSelectorState.Attached);
            Assert.Equal("target", await span.EvaluateAsync<string>("e => e.textContent"));
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "elementHandle.waitForSelector should wait")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ElementHandleWaitForSelectorShouldWait()
        {
            await Page.SetContentAsync("<div></div>");
            var div = await Page.QuerySelectorAsync("div");
            var task = div.WaitForSelectorAsync("span", WaitForSelectorState.Attached);
            await div.EvaluateAsync("div => div.innerHTML = '<span>target</span>'");
            var span = await task;
            Assert.Equal("target", await span.EvaluateAsync<string>("e => e.textContent"));
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "elementHandle.waitForSelector should timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ElementHandleWaitForSelectorShouldTimeout()
        {
            await Page.SetContentAsync("<div></div>");
            var div = await Page.QuerySelectorAsync("div");
            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => div.WaitForSelectorAsync("span", WaitForSelectorState.Attached, 100));
            Assert.Contains("Timeout 100ms exceeded.", exception.Message);
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "elementHandle.waitForSelector should throw on navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ElementHandleWaitForSelectorShouldThrowOnNavigation()
        {
            await Page.SetContentAsync("<div></div>");
            var div = await Page.QuerySelectorAsync("div");
            var task = div.WaitForSelectorAsync("span");

            for (int i = 0; i < 10; i++)
            {
                await Page.EvaluateAsync("() => 1");
            }

            await Page.GotoAsync(TestConstants.EmptyPage);
            var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => task);
            Assert.Contains("Execution context was destroyed, most likely because of a navigation", exception.Message);
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should work with removed MutationObserver")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRemovedMutationObserver()
        {
            await Page.EvaluateAsync("delete window.MutationObserver");
            var waitForSelector = Page.WaitForSelectorAsync(".zombo");

            await TaskUtils.WhenAll(
                waitForSelector,
                Page.SetContentAsync("<div class='zombo'>anything</div>"));

            Assert.Equal("anything", await Page.EvaluateAsync<string>("x => x.textContent", await waitForSelector));
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should resolve promise when node is added")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldResolveTaskWhenNodeIsAdded()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var frame = Page.MainFrame;
            var watchdog = frame.WaitForSelectorAsync("div", WaitForSelectorState.Attached);
            await frame.EvaluateAsync(AddElement, "br");
            await frame.EvaluateAsync(AddElement, "div");
            var eHandle = await watchdog;
            var property = await eHandle.GetPropertyAsync("tagName");
            string tagName = await property.JsonValueAsync<string>();
            Assert.Equal("DIV", tagName);
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should report logs while waiting for visible")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportLogsWhileWaitingForVisible()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var frame = Page.MainFrame;
            var watchdog = frame.WaitForSelectorAsync("div", timeout: 5000);

            await frame.EvaluateAsync(@"() => {
              const div = document.createElement('div');
              div.className = 'foo bar';
              div.id = 'mydiv';
              div.setAttribute('style', 'display: none');
              div.setAttribute('foo', '123456789012345678901234567890123456789012345678901234567890');
              div.textContent = 'abcdefghijklmnopqrstuvwyxzabcdefghijklmnopqrstuvwyxzabcdefghijklmnopqrstuvwyxz';
              document.body.appendChild(div);
            }");

            await GiveItTimeToLogAsync(frame);

            await frame.EvaluateAsync("() => document.querySelector('div').remove()");
            await GiveItTimeToLogAsync(frame);

            await frame.EvaluateAsync(@"() => {
              const div = document.createElement('div');
              div.className = 'another';
              div.style.display = 'none';
              document.body.appendChild(div);
            }");
            await GiveItTimeToLogAsync(frame);

            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => watchdog);

            Assert.Contains("Timeout 5000ms", exception.Message);
            Assert.Contains("waiting for selector \"div\" to be visible", exception.Message);
            Assert.Contains("selector resolved to hidden <div id=\"mydiv\" class=\"foo bar\" foo=\"1234567890123456…>abcdefghijklmnopqrstuvwyxzabcdefghijklmnopqrstuvw…</div>", exception.Message);
            Assert.Contains("selector did not resolve to any element", exception.Message);
            Assert.Contains("selector resolved to hidden <div class=\"another\"></div>", exception.Message);
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should report logs while waiting for hidden")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportLogsWhileWaitingForHidden()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var frame = Page.MainFrame;

            await frame.EvaluateAsync(@"() => {
              const div = document.createElement('div');
              div.className = 'foo bar';
              div.id = 'mydiv';
              div.textContent = 'hello';
              document.body.appendChild(div);
            }");

            var watchdog = frame.WaitForSelectorAsync("div", WaitForSelectorState.Hidden, 5000);
            await GiveItTimeToLogAsync(frame);

            await frame.EvaluateAsync(@"() => {
              document.querySelector('div').remove();
              const div = document.createElement('div');
              div.className = 'another';
              div.textContent = 'hello';
              document.body.appendChild(div);
            }");
            await GiveItTimeToLogAsync(frame);

            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => watchdog);

            Assert.Contains("Timeout 5000ms", exception.Message);
            Assert.Contains("waiting for selector \"div\" to be hidden", exception.Message);
            Assert.Contains("selector resolved to visible <div id=\"mydiv\" class=\"foo bar\">hello</div>", exception.Message);
            Assert.Contains("selector resolved to visible <div class=\"another\">hello</div>", exception.Message);
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should resolve promise when node is added in shadow dom")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldResolvePromiseWhenNodeIsAddedInShadowDom()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var watchdog = Page.WaitForSelectorAsync("span");

            await Page.EvaluateAsync(@"() => {
              const div = document.createElement('div');
              div.attachShadow({mode: 'open'});
              document.body.appendChild(div);
            }");

            await Page.EvaluateAsync(@"() => new Promise(f => setTimeout(f, 100))");

            await Page.EvaluateAsync(@"() => {
              const span = document.createElement('span');
              span.textContent = 'Hello from shadow';
              document.querySelector('div').shadowRoot.appendChild(span);
            }");

            var handle = await watchdog;

            Assert.Equal("Hello from shadow", await handle.EvaluateAsync<string>("e => e.textContent"));
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should work when node is added through innerHTML")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenNodeIsAddedThroughInnerHTML()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var watchdog = Page.WaitForSelectorAsync("h3 div", WaitForSelectorState.Attached);
            await Page.EvaluateAsync(AddElement, "span");
            await Page.EvaluateAsync("document.querySelector('span').innerHTML = '<h3><div></div></h3>'");
            await watchdog;
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "Page.$ waitFor is shortcut for main frame")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PageWaitForSelectorAsyncIsShortcutForMainFrame()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var otherFrame = Page.FirstChildFrame();
            var watchdog = Page.WaitForSelectorAsync("div", WaitForSelectorState.Attached);
            await otherFrame.EvaluateAsync(AddElement, "div");
            await Page.EvaluateAsync(AddElement, "div");
            var eHandle = await watchdog;
            Assert.Equal(Page.MainFrame, await eHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should run in specified frame")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRunInSpecifiedFrame()
        {
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame2", TestConstants.EmptyPage);
            var frame1 = Page.FirstChildFrame();
            var frame2 = Page.Frames.ElementAt(2);
            var waitForSelectorPromise = frame2.WaitForSelectorAsync("div", WaitForSelectorState.Attached);
            await frame1.EvaluateAsync(AddElement, "div");
            await frame2.EvaluateAsync(AddElement, "div");
            var eHandle = await waitForSelectorPromise;
            Assert.Equal(frame2, await eHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should throw when frame is detached")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenFrameIsDetached()
        {
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var frame = Page.FirstChildFrame();
            var waitTask = frame.WaitForSelectorAsync(".box").ContinueWith(task => task.Exception?.InnerException);
            await FrameUtils.DetachFrameAsync(Page, "frame1");
            var waitException = await waitTask;
            Assert.NotNull(waitException);
            Assert.Contains("waitForFunction failed: frame got detached.", waitException.Message);
        }

        private async Task GiveItTimeToLogAsync(IFrame frame)
        {
            await frame.EvaluateAsync("() => new Promise(f => requestAnimationFrame(() => requestAnimationFrame(f)))");
            await frame.EvaluateAsync("() => new Promise(f => requestAnimationFrame(() => requestAnimationFrame(f)))");
        }
    }
}
