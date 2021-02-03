using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Emulation
{
    ///<playwright-file>emulation.spec.js</playwright-file>
    ///<playwright-describe>focus</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class FocusTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public FocusTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("emulation.spec.js", "focus", "should think that it is focused by default")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThinkThatItIsFocusedByDefault()
        {
            Assert.True(await Page.EvaluateAsync<bool>("document.hasFocus()"));
        }

        [PlaywrightTest("emulation.spec.js", "focus", "should think that all pages are focused")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThinkThatAllPagesAreFocused()
        {
            var page2 = await Page.Context.NewPageAsync();
            Assert.True(await Page.EvaluateAsync<bool>("document.hasFocus()"));
            Assert.True(await page2.EvaluateAsync<bool>("document.hasFocus()"));
        }

        [PlaywrightTest("emulation.spec.js", "focus", "should focus popups by default")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFocusPopupsByDefault()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var popupTask = Page.WaitForEventAsync(PageEvent.Popup);

            await TaskUtils.WhenAll(
                popupTask,
                Page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

            var popup = popupTask.Result.Page;

            Assert.True(await Page.EvaluateAsync<bool>("document.hasFocus()"));
            Assert.True(await popup.EvaluateAsync<bool>("document.hasFocus()"));
        }

        [PlaywrightTest("emulation.spec.js", "focus", "should provide target for keyboard events")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldProvideTargetForKeyboardEvents()
        {
            var page2 = await Page.Context.NewPageAsync();

            await TaskUtils.WhenAll(
                Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html"),
                page2.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html"));

            await TaskUtils.WhenAll(
                Page.FocusAsync("input"),
                page2.FocusAsync("input"));

            string text = "first";
            string text2 = "second";

            await TaskUtils.WhenAll(
                Page.Keyboard.TypeAsync(text),
                page2.Keyboard.TypeAsync(text2));

            var results = await TaskUtils.WhenAll(
                Page.EvaluateAsync<string>("result"),
                page2.EvaluateAsync<string>("result"));

            Assert.Equal(text, results.Item1);
            Assert.Equal(text2, results.Item2);
        }

        [PlaywrightTest("emulation.spec.js", "focus", "should not affect mouse event target page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotAffectMouseEventTargetPage()
        {
            var page2 = await Page.Context.NewPageAsync();
            string clickCounter = @"function clickCounter() {
              document.onclick = () => window.clickCount  = (window.clickCount || 0) + 1;
            }";

            await TaskUtils.WhenAll(
                Page.EvaluateAsync(clickCounter),
                page2.EvaluateAsync(clickCounter),
                Page.FocusAsync("body"),
                page2.FocusAsync("body"));

            await TaskUtils.WhenAll(
                Page.Mouse.ClickAsync(1, 1),
                page2.Mouse.ClickAsync(1, 1));

            var counters = await TaskUtils.WhenAll(
                Page.EvaluateAsync<int>("window.clickCount"),
                page2.EvaluateAsync<int>("window.clickCount"));

            Assert.Equal(1, counters.Item1);
            Assert.Equal(1, counters.Item2);
        }

        [PlaywrightTest("emulation.spec.js", "focus", "should change document.activeElement")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldChangeDocumentActiveElement()
        {
            var page2 = await Page.Context.NewPageAsync();

            await TaskUtils.WhenAll(
                Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html"),
                page2.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html"));

            await TaskUtils.WhenAll(
                Page.FocusAsync("input"),
                page2.FocusAsync("textArea"));

            var results = await TaskUtils.WhenAll(
                Page.EvaluateAsync<string>("document.activeElement.tagName"),
                page2.EvaluateAsync<string>("document.activeElement.tagName"));

            Assert.Equal("INPUT", results.Item1);
            Assert.Equal("TEXTAREA", results.Item2);
        }

        [PlaywrightTest("emulation.spec.js", "focus", "should not affect screenshots")]
        [Fact(Skip = "We need screenshot features first")]
        public void ShouldNotAffectScreenshots()
        {
        }

        [PlaywrightTest("emulation.spec.js", "focus", "should change focused iframe")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldChangeFocusedIframe()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            var (frame1, frame2) = await TaskUtils.WhenAll(
                FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.ServerUrl + "/input/textarea.html"),
                FrameUtils.AttachFrameAsync(Page, "frame2", TestConstants.ServerUrl + "/input/textarea.html"));

            string logger = @"function logger() {
              self._events = [];
              const element = document.querySelector('input');
              element.onfocus = element.onblur = (e) => self._events.push(e.type);
            }";

            await TaskUtils.WhenAll(
                frame1.EvaluateAsync(logger),
                frame2.EvaluateAsync(logger));

            var focused = await TaskUtils.WhenAll(
                frame1.EvaluateAsync<bool>("document.hasFocus()"),
                frame2.EvaluateAsync<bool>("document.hasFocus()"));

            Assert.False(focused.Item1);
            Assert.False(focused.Item2);

            await frame1.FocusAsync("input");
            var events = await TaskUtils.WhenAll(
                frame1.EvaluateAsync<string[]>("self._events"),
                frame2.EvaluateAsync<string[]>("self._events"));

            Assert.Equal(new[] { "focus" }, events.Item1);
            Assert.Empty(events.Item2);

            focused = await TaskUtils.WhenAll(
                frame1.EvaluateAsync<bool>("document.hasFocus()"),
                frame2.EvaluateAsync<bool>("document.hasFocus()"));

            Assert.True(focused.Item1);
            Assert.False(focused.Item2);

            await frame2.FocusAsync("input");
            events = await TaskUtils.WhenAll(
                frame1.EvaluateAsync<string[]>("self._events"),
                frame2.EvaluateAsync<string[]>("self._events"));

            Assert.Equal(new[] { "focus", "blur" }, events.Item1);
            Assert.Equal(new[] { "focus" }, events.Item2);

            focused = await TaskUtils.WhenAll(
                frame1.EvaluateAsync<bool>("document.hasFocus()"),
                frame2.EvaluateAsync<bool>("document.hasFocus()"));

            Assert.False(focused.Item1);
            Assert.True(focused.Item2);
        }
    }
}
