using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame
{
    ///<playwright-file>waittask.spec.js</playwright-file>
    ///<playwright-describe>Frame.waitForSelector</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
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
        ///<playwright-it>should immediately resolve promise if node exists</playwright-it>
        [Fact]
        public async Task ShouldImmediatelyResolveTaskIfNodeExists()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var frame = Page.MainFrame;
            await frame.WaitForSelectorAsync("*");
            await frame.EvaluateAsync(AddElement, "div");
            await frame.WaitForSelectorAsync("div");
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should work with removed MutationObserver</playwright-it>
        [Fact]
        public async Task ShouldWorkWithRemovedMutationObserver()
        {
            await Page.EvaluateAsync("delete window.MutationObserver");
            var waitForSelector = Page.WaitForSelectorAsync(".zombo");

            await Task.WhenAll(
                waitForSelector,
                Page.SetContentAsync("<div class='zombo'>anything</div>"));

            Assert.Equal("anything", await Page.EvaluateAsync<string>("x => x.textContent", await waitForSelector));
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should resolve promise when node is added</playwright-it>
        [Fact]
        public async Task ShouldResolveTaskWhenNodeIsAdded()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var frame = Page.MainFrame;
            var watchdog = frame.WaitForSelectorAsync("div");
            await frame.EvaluateAsync(AddElement, "br");
            await frame.EvaluateAsync(AddElement, "div");
            var eHandle = await watchdog;
            var property = await eHandle.GetPropertyAsync("tagName");
            string tagName = await property.GetJsonValueAsync<string>();
            Assert.Equal("DIV", tagName);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should work when node is added through innerHTML</playwright-it>
        [Fact]
        public async Task ShouldWorkWhenNodeIsAddedThroughInnerHTML()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var watchdog = Page.WaitForSelectorAsync("h3 div");
            await Page.EvaluateAsync(AddElement, "span");
            await Page.EvaluateAsync("document.querySelector('span').innerHTML = '<h3><div></div></h3>'");
            await watchdog;
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>Page.$ waitFor is shortcut for main frame</playwright-it>
        [Fact]
        public async Task PageWaitForSelectorAsyncIsShortcutForMainFrame()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var otherFrame = Page.FirstChildFrame();
            var watchdog = Page.WaitForSelectorAsync("div");
            await otherFrame.EvaluateAsync(AddElement, "div");
            await Page.EvaluateAsync(AddElement, "div");
            var eHandle = await watchdog;
            Assert.Equal(Page.MainFrame, await eHandle.GetOwnerFrameAsync());
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should run in specified frame</playwright-it>
        [Fact]
        public async Task ShouldRunInSpecifiedFrame()
        {
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame2", TestConstants.EmptyPage);
            var frame1 = Page.FirstChildFrame();
            var frame2 = Page.Frames.ElementAt(2);
            var waitForSelectorPromise = frame2.WaitForSelectorAsync("div");
            await frame1.EvaluateAsync(AddElement, "div");
            await frame2.EvaluateAsync(AddElement, "div");
            var eHandle = await waitForSelectorPromise;
            Assert.Equal(frame2, await eHandle.GetOwnerFrameAsync());
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should throw when frame is detached</playwright-it>
        [Fact]
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
        [Fact]
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
        [Fact]
        public async Task ShouldWaitForVisible()
        {
            bool divFound = false;
            var waitForSelector = Page.WaitForSelectorAsync("div", new WaitForSelectorOptions { WaitFor = WaitForOption.Visible })
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
        ///<playwright-it>should wait for visible recursively</playwright-it>
        [Fact]
        public async Task ShouldWaitForVisibleRecursively()
        {
            bool divVisible = false;
            var waitForSelector = Page.WaitForSelectorAsync("div#inner", new WaitForSelectorOptions { WaitFor = WaitForOption.Visible })
                .ContinueWith(_ => divVisible = true);
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
        /// <playwright-it>hidden should wait for visibility: hidden</playwright-it>
        /// <playwright-it>hidden should wait for display: none</playwright-it>
        ///</playwright-its>
        [Theory]
        [InlineData("visibility", "hidden")]
        [InlineData("display", "none")]
        public async Task HiddenShouldWaitForVisibility(string propertyName, string propertyValue)
        {
            bool divHidden = false;
            await Page.SetContentAsync("<div style='display: block;'></div>");
            var waitForSelector = Page.WaitForSelectorAsync("div", new WaitForSelectorOptions { WaitFor = WaitForOption.Hidden })
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
        [Fact]
        public async Task HiddenShouldWaitForRemoval()
        {
            await Page.SetContentAsync("<div></div>");
            bool divRemoved = false;
            var waitForSelector = Page.WaitForSelectorAsync("div", new WaitForSelectorOptions { WaitFor = WaitForOption.Hidden })
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
        [Fact]
        public async Task ShouldReturnNullIfWaitingToHideNonExistingElement()
        {
            var handle = await Page.WaitForSelectorAsync("non-existing", new WaitForSelectorOptions { WaitFor = WaitForOption.Hidden });
            Assert.Null(handle);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should respect timeout</playwright-it>
        [Fact]
        public async Task ShouldRespectTimeout()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(async ()
                => await Page.WaitForSelectorAsync("div", new WaitForSelectorOptions { Timeout = 10 }));

            Assert.Contains("waiting for selector \"[visible] div\" failed: timeout", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should have an error message specifically for awaiting an element to be hidden</playwright-it>
        [Fact]
        public async Task ShouldHaveAnErrorMessageSpecificallyForAwaitingAnElementToBeHidden()
        {
            await Page.SetContentAsync("<div></div>");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(async ()
                => await Page.WaitForSelectorAsync("div", new WaitForSelectorOptions { WaitFor = WaitForOption.Hidden, Timeout = 10 }));

            Assert.Contains("waiting for selector \"[hidden] div\" failed: timeout", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should respond to node attribute mutation</playwright-it>
        [Fact]
        public async Task ShouldRespondToNodeAttributeMutation()
        {
            bool divFound = false;
            var waitForSelector = Page.WaitForSelectorAsync(".zombo").ContinueWith(_ => divFound = true);
            await Page.SetContentAsync("<div class='notZombo'></div>");
            Assert.False(divFound);
            await Page.EvaluateAsync("document.querySelector('div').className = 'zombo'");
            Assert.True(await waitForSelector);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should return the element handle</playwright-it>
        [Fact]
        public async Task ShouldReturnTheElementHandle()
        {
            var waitForSelector = Page.WaitForSelectorAsync(".zombo");
            await Page.SetContentAsync("<div class='zombo'>anything</div>");
            Assert.Equal("anything", await Page.EvaluateAsync<string>("x => x.textContent", await waitForSelector));
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should have correct stack trace for timeout</playwright-it>
        [Fact]
        public async Task ShouldHaveCorrectStackTraceForTimeout()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(async ()
                => await Page.WaitForSelectorAsync(".zombo", new WaitForSelectorOptions { Timeout = 10 }));
            Assert.Contains("WaitForSelectorTests", exception.StackTrace);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should throw for unknown waitFor option</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForUnknownWaitForOption() { }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should throw for numeric waitFor option</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForNumericWaitForOption() { }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should throw for true waitFor option</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForTrueWaitForOption() { }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should throw for false waitFor option</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForFalseWaitForOption() { }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForSelector</playwright-describe>
        ///<playwright-it>should support >> selector syntax</playwright-it>
        [Fact]
        public async Task ShouldSupportSelectorSyntax()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var frame = Page.MainFrame;
            var watchdog = frame.WaitForSelectorAsync("css=div >> css=span");
            await frame.EvaluateAsync(AddElement, "br");
            await frame.EvaluateAsync(AddElement, "div");
            await frame.EvaluateAsync("() => document.querySelector('div').appendChild(document.createElement('span'))");
            var eHandle = await watchdog;
            var tagProperty = await eHandle.GetPropertyAsync("tagName");
            string tagName = await tagProperty.GetJsonValueAsync<string>();
            Assert.Equal("SPAN", tagName);
        }
    }
}
