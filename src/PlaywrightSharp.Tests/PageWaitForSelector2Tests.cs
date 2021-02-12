using System;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageWaitForSelector2Tests : PlaywrightSharpPageBaseTest
    {
        private const string AddElement = "tag => document.body.appendChild(document.createElement(tag))";

        /// <inheritdoc/>
        public PageWaitForSelector2Tests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should survive cross-process navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should wait for visible")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should not consider visible when zero-sized")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should wait for visible recursively")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "hidden should wait for hidden")]
        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "hidden should wait for display: none")]
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

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "hidden should wait for removal")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should return null if waiting to hide non-existing element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullIfWaitingToHideNonExistingElement()
        {
            var handle = await Page.WaitForSelectorAsync("non-existing", WaitForState.Hidden);
            Assert.Null(handle);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should respect timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(async ()
                => await Page.WaitForSelectorAsync("div", WaitForState.Attached, 3000));

            Assert.Contains("Timeout 3000ms exceeded", exception.Message);
            Assert.Contains("waiting for selector \"div\"", exception.Message);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should have an error message specifically for awaiting an element to be hidden")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveAnErrorMessageSpecificallyForAwaitingAnElementToBeHidden()
        {
            await Page.SetContentAsync("<div>content</div>");
            var exception = await Assert.ThrowsAsync<TimeoutException>(async ()
                => await Page.WaitForSelectorAsync("div", WaitForState.Hidden, 1000));

            Assert.Contains("Timeout 1000ms exceeded", exception.Message);
            Assert.Contains("waiting for selector \"div\" to be hidden", exception.Message);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should respond to node attribute mutation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespondToNodeAttributeMutation()
        {
            bool divFound = false;
            var waitForSelector = Page.WaitForSelectorAsync(".zombo", WaitForState.Attached).ContinueWith(_ => divFound = true);
            await Page.SetContentAsync("<div class='notZombo'></div>");
            Assert.False(divFound);
            await Page.EvaluateAsync("document.querySelector('div').className = 'zombo'");
            Assert.True(await waitForSelector);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should return the element handle")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnTheElementHandle()
        {
            var waitForSelector = Page.WaitForSelectorAsync(".zombo");
            await Page.SetContentAsync("<div class='zombo'>anything</div>");
            Assert.Equal("anything", await Page.EvaluateAsync<string>("x => x.textContent", await waitForSelector));
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should have correct stack trace for timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveCorrectStackTraceForTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(async ()
                => await Page.WaitForSelectorAsync(".zombo", timeout: 10));
            Assert.Contains("WaitForSelector2Tests", exception.StackTrace);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should throw for unknown state option")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForUnknownStateOption() { }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should throw for visibility option")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForVisibilityOption() { }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should throw for true state option")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForTrueStateOption() { }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should throw for false state option")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForFalseStateOption() { }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should support >> selector syntax")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should wait for detached if already detached")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForDetachedIfAlreadyDetached()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            Assert.Null(await Page.WaitForSelectorAsync("css=div", WaitForState.Detached));
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should wait for detached")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForDetached()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\"><div>43543</div></section>");
            var waitForTask = Page.WaitForSelectorAsync("css=div", WaitForState.Detached);
            Assert.False(waitForTask.IsCompleted);
            await Page.WaitForSelectorAsync("css=section");
            Assert.False(waitForTask.IsCompleted);
            await Page.EvalOnSelectorAsync("div", "div => div.remove()");
            await waitForTask;
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should support some fancy xpath")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportSomeFancyXpath()
        {
            await Page.SetContentAsync("<p>red herring</p><p>hello  world  </p>");
            var waitForXPath = Page.WaitForSelectorAsync("//p[normalize-space(.)=\"hello world\"]");
            Assert.Equal("hello  world  ", await Page.EvaluateAsync<string>("x => x.textContent", await waitForXPath));
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should respect timeout xpath")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeoutXpath()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(()
                    => Page.WaitForSelectorAsync("//div", WaitForState.Attached, timeout: 3000));

            Assert.Contains("Timeout 3000ms exceeded", exception.Message);
            Assert.Contains("waiting for selector \"//div\"", exception.Message);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should run in specified frame xpath")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRunInSpecifiedFrameXPath()
        {
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame2", TestConstants.EmptyPage);
            var frame1 = Page.Frames.First(f => f.Name == "frame1");
            var frame2 = Page.Frames.First(f => f.Name == "frame2");
            var waitForXPathPromise = frame2.WaitForSelectorAsync("//div", WaitForState.Attached);
            await frame1.EvaluateAsync(AddElement, "div");
            await frame2.EvaluateAsync(AddElement, "div");
            var eHandle = await waitForXPathPromise;
            Assert.Equal(frame2, await eHandle.GetOwnerFrameAsync());
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should throw when frame is detached xpath")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenFrameIsDetachedXPath()
        {
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var frame = Page.FirstChildFrame();
            var waitPromise = frame.WaitForSelectorAsync("//*[@class=\"box\"]");
            await FrameUtils.DetachFrameAsync(Page, "frame1");
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => waitPromise);
            Assert.Contains("waitForFunction failed: frame got detached.", exception.Message);
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should return the element handle xpath")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnTheElementHandleXPath()
        {
            var waitForXPath = Page.WaitForSelectorAsync("//*[@class=\"zombo\"]");
            await Page.SetContentAsync("<div class='zombo'>anything</div>");
            Assert.Equal("anything", await Page.EvaluateAsync<string>("x => x.textContent", await waitForXPath));
        }

        [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should allow you to select an element with single slash xpath")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAllowYouToSelectAnElementWithSingleSlashXPath()
        {
            await Page.SetContentAsync("<div>some text</div>");
            var waitForXPath = Page.WaitForSelectorAsync("//html/body/div");
            Assert.Equal("some text", await Page.EvaluateAsync<string>("x => x.textContent", await waitForXPath));
        }
    }
}
