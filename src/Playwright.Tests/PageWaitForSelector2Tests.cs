using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageWaitForSelector2Tests : PageTestEx
    {
        private const string AddElement = "tag => document.body.appendChild(document.createElement(tag))";

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should survive cross-process navigation")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSurviveCrossProcessNavigation()
        {
            bool boxFound = false;
            var waitForSelector = Page.WaitForSelectorAsync(".box").ContinueWith(_ => boxFound = true);
            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.False(boxFound);
            await Page.ReloadAsync();
            Assert.False(boxFound);
            await Page.GotoAsync(TestConstants.CrossProcessHttpPrefix + "/grid.html");
            await waitForSelector;
            Assert.True(boxFound);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should wait for visible")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForVisible()
        {
            bool divFound = false;
            var waitForSelector = Page.WaitForSelectorAsync("div", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible })
                .ContinueWith(_ => divFound = true);
            await Page.SetContentAsync("<div style='display: none; visibility: hidden;'>1</div>");
            Assert.False(divFound);
            await Page.EvaluateAsync("document.querySelector('div').style.removeProperty('display')");
            Assert.False(divFound);
            await Page.EvaluateAsync("document.querySelector('div').style.removeProperty('visibility')");
            Assert.True(await waitForSelector);
            Assert.True(divFound);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should not consider visible when zero-sized")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotConsiderVisibleWhenZeroSized()
        {
            await Page.SetContentAsync("<div style='width: 0; height: 0;'>1</div>");
            var exception = await AssertThrowsAsync<TimeoutException>(() => Page.WaitForSelectorAsync("div", new PageWaitForSelectorOptions { Timeout = 1000 }));
            StringAssert.Contains("Timeout 1000ms", exception.Message);
            await Page.EvaluateAsync("() => document.querySelector('div').style.width = '10px'");
            exception = await AssertThrowsAsync<TimeoutException>(() => Page.WaitForSelectorAsync("div", new PageWaitForSelectorOptions { Timeout = 1000 }));
            StringAssert.Contains("Timeout 1000ms", exception.Message);
            await Page.EvaluateAsync("() => document.querySelector('div').style.height = '10px'");
            Assert.NotNull(await Page.WaitForSelectorAsync("div", new PageWaitForSelectorOptions { Timeout = 1000 }));
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should wait for visible recursively")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForVisibleRecursively()
        {
            bool divVisible = false;
            var waitForSelector = Page.WaitForSelectorAsync("div#inner", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible }).ContinueWith(_ => divVisible = true);
            await Page.SetContentAsync("<div style='display: none; visibility: hidden;'><div id='inner'>hi</div></div>");
            Assert.False(divVisible);
            await Page.EvaluateAsync("document.querySelector('div').style.removeProperty('display')");
            Assert.False(divVisible);
            await Page.EvaluateAsync("document.querySelector('div').style.removeProperty('visibility')");
            Assert.True(await waitForSelector);
            Assert.True(divVisible);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "hidden should wait for removal")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task HiddenShouldWaitForRemoval()
        {
            await Page.SetContentAsync("<div>content</div>");
            bool divRemoved = false;
            var waitForSelector = Page.WaitForSelectorAsync("div", new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden })
                .ContinueWith(_ => divRemoved = true);
            await Page.WaitForSelectorAsync("div"); // do a round trip
            Assert.False(divRemoved);
            await Page.EvaluateAsync("document.querySelector('div').remove()");
            Assert.True(await waitForSelector);
            Assert.True(divRemoved);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should return null if waiting to hide non-existing element")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullIfWaitingToHideNonExistingElement()
        {
            var handle = await Page.WaitForSelectorAsync("non-existing", new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden });
            Assert.Null(handle);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should respect timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var exception = await AssertThrowsAsync<TimeoutException>(()
                => Page.WaitForSelectorAsync("div", new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached, Timeout = 3000 }));

            StringAssert.Contains("Timeout 3000ms exceeded", exception.Message);
            StringAssert.Contains("waiting for selector \"div\"", exception.Message);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should have an error message specifically for awaiting an element to be hidden")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveAnErrorMessageSpecificallyForAwaitingAnElementToBeHidden()
        {
            await Page.SetContentAsync("<div>content</div>");
            var exception = await AssertThrowsAsync<TimeoutException>(()
                => Page.WaitForSelectorAsync("div", new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden, Timeout = 1000 }));

            StringAssert.Contains("Timeout 1000ms exceeded", exception.Message);
            StringAssert.Contains("waiting for selector \"div\" to be hidden", exception.Message);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should respond to node attribute mutation")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespondToNodeAttributeMutation()
        {
            bool divFound = false;
            var waitForSelector = Page.WaitForSelectorAsync(".zombo", new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached }).ContinueWith(_ => divFound = true);
            await Page.SetContentAsync("<div class='notZombo'></div>");
            Assert.False(divFound);
            await Page.EvaluateAsync("document.querySelector('div').className = 'zombo'");
            Assert.True(await waitForSelector);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should return the element handle")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnTheElementHandle()
        {
            var waitForSelector = Page.WaitForSelectorAsync(".zombo");
            await Page.SetContentAsync("<div class='zombo'>anything</div>");
            Assert.AreEqual("anything", await Page.EvaluateAsync<string>("x => x.textContent", await waitForSelector));
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should have correct stack trace for timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveCorrectStackTraceForTimeout()
        {
            Exception exception = null;
            try
            {
                await Page.WaitForSelectorAsync(".zombo", new PageWaitForSelectorOptions { Timeout = 10 });
            }
            catch (Exception e)
            {
                exception = e;
            }
            StringAssert.Contains("WaitForSelector2Tests", exception.ToString());
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should throw for unknown state option")]
        [Test, Ignore("We don't need this test")]
        public void ShouldThrowForUnknownStateOption() { }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should throw for visibility option")]
        [Test, Ignore("We don't need this test")]
        public void ShouldThrowForVisibilityOption() { }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should throw for true state option")]
        [Test, Ignore("We don't need this test")]
        public void ShouldThrowForTrueStateOption() { }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should throw for false state option")]
        [Test, Ignore("We don't need this test")]
        public void ShouldThrowForFalseStateOption() { }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should support >> selector syntax")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportSelectorSyntax()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var frame = Page.MainFrame;
            var watchdog = frame.WaitForSelectorAsync("css=div >> css=span", new FrameWaitForSelectorOptions { State = WaitForSelectorState.Attached });
            await frame.EvaluateAsync(AddElement, "br");
            await frame.EvaluateAsync(AddElement, "div");
            await frame.EvaluateAsync("() => document.querySelector('div').appendChild(document.createElement('span'))");
            var eHandle = await watchdog;
            var tagProperty = await eHandle.GetPropertyAsync("tagName");
            string tagName = await tagProperty.JsonValueAsync<string>();
            Assert.AreEqual("SPAN", tagName);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should wait for detached if already detached")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForDetachedIfAlreadyDetached()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            Assert.Null(await Page.WaitForSelectorAsync("css=div", new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached }));
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should wait for detached")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForDetached()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\"><div>43543</div></section>");
            var waitForTask = Page.WaitForSelectorAsync("css=div", new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached });
            Assert.False(waitForTask.IsCompleted);
            await Page.WaitForSelectorAsync("css=section");
            Assert.False(waitForTask.IsCompleted);
            await Page.EvalOnSelectorAsync("div", "div => div.remove()");
            await waitForTask;
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should support some fancy xpath")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportSomeFancyXpath()
        {
            await Page.SetContentAsync("<p>red herring</p><p>hello  world  </p>");
            var waitForXPath = Page.WaitForSelectorAsync("//p[normalize-space(.)=\"hello world\"]");
            Assert.AreEqual("hello  world  ", await Page.EvaluateAsync<string>("x => x.textContent", await waitForXPath));
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should respect timeout xpath")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeoutXpath()
        {
            var exception = await AssertThrowsAsync<TimeoutException>(()
                    => Page.WaitForSelectorAsync("//div", new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached, Timeout = 3000 }));

            StringAssert.Contains("Timeout 3000ms exceeded", exception.Message);
            StringAssert.Contains("waiting for selector \"//div\"", exception.Message);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should run in specified frame xpath")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRunInSpecifiedFrameXPath()
        {
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame2", TestConstants.EmptyPage);
            var frame1 = Page.Frames.First(f => f.Name == "frame1");
            var frame2 = Page.Frames.First(f => f.Name == "frame2");
            var waitForXPathPromise = frame2.WaitForSelectorAsync("//div", new FrameWaitForSelectorOptions { State = WaitForSelectorState.Attached });
            await frame1.EvaluateAsync(AddElement, "div");
            await frame2.EvaluateAsync(AddElement, "div");
            var eHandle = await waitForXPathPromise;
            Assert.AreEqual(frame2, await eHandle.OwnerFrameAsync());
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should throw when frame is detached xpath")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenFrameIsDetachedXPath()
        {
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var frame = Page.FirstChildFrame();
            var waitPromise = frame.WaitForSelectorAsync("//*[@class=\"box\"]");
            await FrameUtils.DetachFrameAsync(Page, "frame1");
            var exception = await AssertThrowsAsync<PlaywrightException>(() => waitPromise);
            StringAssert.Contains("waitForFunction failed: frame got detached.", exception.Message);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should return the element handle xpath")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnTheElementHandleXPath()
        {
            var waitForXPath = Page.WaitForSelectorAsync("//*[@class=\"zombo\"]");
            await Page.SetContentAsync("<div class='zombo'>anything</div>");
            Assert.AreEqual("anything", await Page.EvaluateAsync<string>("x => x.textContent", await waitForXPath));
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should allow you to select an element with single slash xpath")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAllowYouToSelectAnElementWithSingleSlashXPath()
        {
            await Page.SetContentAsync("<div>some text</div>");
            var waitForXPath = Page.WaitForSelectorAsync("//html/body/div");
            Assert.AreEqual("some text", await Page.EvaluateAsync<string>("x => x.textContent", await waitForXPath));
        }
    }
}
