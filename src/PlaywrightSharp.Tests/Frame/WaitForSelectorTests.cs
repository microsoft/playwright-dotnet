using System;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame
{
    ///<playwright-file>waittask.spec.js</playwright-file>
    ///<playwright-describe>Frame.waitForSelector</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class WaitForSelectorTests : PlaywrightSharpPageBaseTest
    {
        private const string AddElement = "tag => document.body.appendChild(document.createElement(tag))";

        /// <inheritdoc/>
        public WaitForSelectorTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should throw on waitFor</playwright-it>
        [Fact(Skip = "We don't need to test this")]
        public void ShouldThrowOnWaitFor()
        {
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should tolerate waitFor=visible</playwright-it>
        [Fact(Skip = "We don't need to test this")]
        public void ShouldTolerateWaitForVisible()
        {
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should immediately resolve promise if node exists</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldImmediatelyResolveTaskIfNodeExists()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var frame = Page.MainFrame;
            await frame.WaitForSelectorAsync("*");
            await frame.EvaluateAsync(AddElement, "div");
            await frame.WaitForSelectorAsync("div", WaitForState.Attached);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should work with removed MutationObserver</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithRemovedMutationObserver()
        {
            await Page.EvaluateAsync("delete window.MutationObserver");
            var waitForSelector = Page.WaitForSelectorAsync(".zombo");

            await TaskUtils.WhenAll(
                waitForSelector,
                Page.SetContentAsync("<div class='zombo'>anything</div>"));

            Assert.Equal("anything", await Page.EvaluateAsync<string>("x => x.textContent", await waitForSelector));
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should resolve promise when node is added</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldResolveTaskWhenNodeIsAdded()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var frame = Page.MainFrame;
            var watchdog = frame.WaitForSelectorAsync("div", WaitForState.Attached);
            await frame.EvaluateAsync(AddElement, "br");
            await frame.EvaluateAsync(AddElement, "div");
            var eHandle = await watchdog;
            var property = await eHandle.GetPropertyAsync("tagName");
            string tagName = await property.GetJsonValueAsync<string>();
            Assert.Equal("DIV", tagName);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should report logs while waiting for visible</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportLogsWhileWaitingForVisible()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
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

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should report logs while waiting for hidden</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportLogsWhileWaitingForHidden()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var frame = Page.MainFrame;

            await frame.EvaluateAsync(@"() => {
              const div = document.createElement('div');
              div.className = 'foo bar';
              div.id = 'mydiv';
              div.textContent = 'hello';
              document.body.appendChild(div);
            }");

            var watchdog = frame.WaitForSelectorAsync("div", WaitForState.Hidden, 5000);
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

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should resolve promise when node is added in shadow dom</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldResolvePromiseWhenNodeIsAddedInShadowDom()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
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

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should work when node is added through innerHTML</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWhenNodeIsAddedThroughInnerHTML()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var watchdog = Page.WaitForSelectorAsync("h3 div", WaitForState.Attached);
            await Page.EvaluateAsync(AddElement, "span");
            await Page.EvaluateAsync("document.querySelector('span').innerHTML = '<h3><div></div></h3>'");
            await watchdog;
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>Page.$ waitFor is shortcut for main frame</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task PageWaitForSelectorAsyncIsShortcutForMainFrame()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var otherFrame = Page.FirstChildFrame();
            var watchdog = Page.WaitForSelectorAsync("div", WaitForState.Attached);
            await otherFrame.EvaluateAsync(AddElement, "div");
            await Page.EvaluateAsync(AddElement, "div");
            var eHandle = await watchdog;
            Assert.Equal(Page.MainFrame, await eHandle.GetOwnerFrameAsync());
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should run in specified frame</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRunInSpecifiedFrame()
        {
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame2", TestConstants.EmptyPage);
            var frame1 = Page.FirstChildFrame();
            var frame2 = Page.Frames.ElementAt(2);
            var waitForSelectorPromise = frame2.WaitForSelectorAsync("div", WaitForState.Attached);
            await frame1.EvaluateAsync(AddElement, "div");
            await frame2.EvaluateAsync(AddElement, "div");
            var eHandle = await waitForSelectorPromise;
            Assert.Equal(frame2, await eHandle.GetOwnerFrameAsync());
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should throw when frame is detached</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should survive cross-process navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSurviveCrossProcessNavigation()
        {
            bool boxFound = false;
            var waitForSelector = Page.WaitForSelectorAsync(".box").ContinueWith(_ => boxFound = true);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.False(boxFound);
            await Page.ReloadAsync();
            Assert.False(boxFound);
            await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/grid.html");
            await waitForSelector;
            Assert.True(boxFound);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should wait for visible</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWaitForVisible()
        {
            bool divFound = false;
            var waitForSelector = Page.WaitForSelectorAsync("div", WaitForState.Visible)
                .ContinueWith(_ => divFound = true);
            await Page.SetContentAsync("<div style='display: none; visibility: hidden;'>1</div>");
            Assert.False(divFound);
            await Page.EvaluateAsync("document.querySelector('div').style.removeProperty('display')");
            Assert.False(divFound);
            await Page.EvaluateAsync("document.querySelector('div').style.removeProperty('visibility')");
            Assert.True(await waitForSelector);
            Assert.True(divFound);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should not consider visible when zero-sized</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotConsiderVisibleWhenZeroSized()
        {
            await Page.SetContentAsync("<div style='width: 0; height: 0;'>1</div>");
            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => Page.WaitForSelectorAsync("div", timeout: 1000));
            Assert.Contains("Timeout 1000ms", exception.Message);
            await Page.EvaluateAsync("() => document.querySelector('div').style.width = '10px'");
            exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => Page.WaitForSelectorAsync("div", timeout: 1000));
            Assert.Contains("Timeout 1000ms", exception.Message);
            await Page.EvaluateAsync("() => document.querySelector('div').style.height = '10px'");
            Assert.NotNull(await Page.WaitForSelectorAsync("div", timeout: 1000));
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should wait for visible recursively</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWaitForVisibleRecursively()
        {
            bool divVisible = false;
            var waitForSelector = Page.WaitForSelectorAsync("div#inner", WaitForState.Visible).ContinueWith(_ => divVisible = true);
            await Page.SetContentAsync("<div style='display: none; visibility: hidden;'><div id='inner'>hi</div></div>");
            Assert.False(divVisible);
            await Page.EvaluateAsync("document.querySelector('div').style.removeProperty('display')");
            Assert.False(divVisible);
            await Page.EvaluateAsync("document.querySelector('div').style.removeProperty('visibility')");
            Assert.True(await waitForSelector);
            Assert.True(divVisible);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-its>
        /// <playwright-it>hidden should wait for hidden</playwright-it>
        /// <playwright-it>hidden should wait for display: none</playwright-it>
        ///</playwright-its>
        [Theory]
        [InlineData("visibility", "hidden")]
        [InlineData("display", "none")]
        public async Task HiddenShouldWaitForVisibility(string propertyName, string propertyValue)
        {
            bool divHidden = false;
            await Page.SetContentAsync("<div style='display: block;'>content</div>");
            var waitForSelector = Page.WaitForSelectorAsync("div", WaitForState.Hidden)
                .ContinueWith(_ => divHidden = true);
            await Page.WaitForSelectorAsync("div"); // do a round trip
            Assert.False(divHidden);
            await Page.EvaluateAsync($"document.querySelector('div').style.setProperty('{propertyName}', '{propertyValue}')");
            Assert.True(await waitForSelector);
            Assert.True(divHidden);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>hidden should wait for removal</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task HiddenShouldWaitForRemoval()
        {
            await Page.SetContentAsync("<div>content</div>");
            bool divRemoved = false;
            var waitForSelector = Page.WaitForSelectorAsync("div", WaitForState.Hidden)
                .ContinueWith(_ => divRemoved = true);
            await Page.WaitForSelectorAsync("div"); // do a round trip
            Assert.False(divRemoved);
            await Page.EvaluateAsync("document.querySelector('div').remove()");
            Assert.True(await waitForSelector);
            Assert.True(divRemoved);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should return null if waiting to hide non-existing element</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReturnNullIfWaitingToHideNonExistingElement()
        {
            var handle = await Page.WaitForSelectorAsync("non-existing", WaitForState.Hidden);
            Assert.Null(handle);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should respect timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(async ()
                => await Page.WaitForSelectorAsync("div", WaitForState.Attached, 3000));

            Assert.Contains("Timeout 3000ms exceeded", exception.Message);
            Assert.Contains("waiting for selector \"div\"", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should have an error message specifically for awaiting an element to be hidden</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldHaveAnErrorMessageSpecificallyForAwaitingAnElementToBeHidden()
        {
            await Page.SetContentAsync("<div>content</div>");
            var exception = await Assert.ThrowsAsync<TimeoutException>(async ()
                => await Page.WaitForSelectorAsync("div", WaitForState.Hidden, 1000));

            Assert.Contains("Timeout 1000ms exceeded", exception.Message);
            Assert.Contains("waiting for selector \"div\" to be hidden", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should respond to node attribute mutation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRespondToNodeAttributeMutation()
        {
            bool divFound = false;
            var waitForSelector = Page.WaitForSelectorAsync(".zombo", WaitForState.Attached).ContinueWith(_ => divFound = true);
            await Page.SetContentAsync("<div class='notZombo'></div>");
            Assert.False(divFound);
            await Page.EvaluateAsync("document.querySelector('div').className = 'zombo'");
            Assert.True(await waitForSelector);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should return the element handle</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReturnTheElementHandle()
        {
            var waitForSelector = Page.WaitForSelectorAsync(".zombo");
            await Page.SetContentAsync("<div class='zombo'>anything</div>");
            Assert.Equal("anything", await Page.EvaluateAsync<string>("x => x.textContent", await waitForSelector));
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should have correct stack trace for timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldHaveCorrectStackTraceForTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(async ()
                => await Page.WaitForSelectorAsync(".zombo", timeout: 10));
            Assert.Contains("WaitForSelectorTests", exception.StackTrace);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should throw for unknown state option</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForUnknownStateOption() { }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should throw for visibility option</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForVisibilityOption() { }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should throw for true state option</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForTrueStateOption() { }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should throw for false state option</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForFalseStateOption() { }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should support >> selector syntax</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSupportSelectorSyntax()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var frame = Page.MainFrame;
            var watchdog = frame.WaitForSelectorAsync("css=div >> css=span", WaitForState.Attached);
            await frame.EvaluateAsync(AddElement, "br");
            await frame.EvaluateAsync(AddElement, "div");
            await frame.EvaluateAsync("() => document.querySelector('div').appendChild(document.createElement('span'))");
            var eHandle = await watchdog;
            var tagProperty = await eHandle.GetPropertyAsync("tagName");
            string tagName = await tagProperty.GetJsonValueAsync<string>();
            Assert.Equal("SPAN", tagName);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should wait for detached if already detached</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWaitForDetachedIfAlreadyDetached()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            Assert.Null(await Page.WaitForSelectorAsync("css=div", WaitForState.Detached));
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should wait for detached</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWaitForDetached()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\"><div>43543</div></section>");
            var waitForTask = Page.WaitForSelectorAsync("css=div", WaitForState.Detached);
            Assert.False(waitForTask.IsCompleted);
            await Page.WaitForSelectorAsync("css=section");
            Assert.False(waitForTask.IsCompleted);
            await Page.QuerySelectorEvaluateAsync("div", "div => div.remove()");
            await waitForTask;
        }

        private async Task GiveItTimeToLogAsync(IFrame frame)
        {
            await frame.EvaluateAsync("() => new Promise(f => requestAnimationFrame(() => requestAnimationFrame(f)))");
            await frame.EvaluateAsync("() => new Promise(f => requestAnimationFrame(() => requestAnimationFrame(f)))");
        }
    }
}
