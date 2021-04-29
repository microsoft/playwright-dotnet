using System.Threading.Tasks;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageFillTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageFillTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-fill.spec.ts", "should fill textarea")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillTextarea()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FillAsync("textarea", "some value");
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should fill input")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillInput()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FillAsync("input", "some value");
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should throw on unsupported inputs")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowOnUnsupportedInputs()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            foreach (string type in new[] { "button", "checkbox", "file", "image", "radio", "range", "reset", "submit" })
            {
                await Page.EvalOnSelectorAsync("input", "(input, type) => input.setAttribute('type', type)", type);
                var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.FillAsync("input", string.Empty));
                Assert.Contains($"input of type \"{type}\" cannot be filled", exception.Message);
            }
        }

        [PlaywrightTest("page-fill.spec.ts", "should fill different input types")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillDifferentInputTypes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            foreach (string type in new[] { "password", "search", "tel", "text", "url" })
            {
                await Page.EvalOnSelectorAsync("input", "(input, type) => input.setAttribute('type', type)", type);
                await Page.FillAsync("input", "text " + type);
                Assert.Equal("text " + type, await Page.EvaluateAsync<string>("() => result"));
            }
        }

        [PlaywrightTest("page-fill.spec.ts", "should fill date input after clicking")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillDateInputAfterClicking()
        {
            await Page.SetContentAsync("<input type=date>");
            await Page.ClickAsync("input");
            await Page.FillAsync("input", "2020-03-02");
            Assert.Equal("2020-03-02", await Page.EvalOnSelectorAsync<string>("input", "input => input.value"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should throw on incorrect date")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldThrowOnIncorrectDate()
        {
            await Page.SetContentAsync("<input type=date>");
            await Page.ClickAsync("input");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.FillAsync("input", "2020-13-02"));
            Assert.Contains("Malformed value", exception.Message);
        }

        [PlaywrightTest("page-fill.spec.ts", "should fill time input after clicking")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillTimeInputAfterClicking()
        {
            await Page.SetContentAsync("<input type=time>");
            await Page.ClickAsync("input");
            await Page.FillAsync("input", "13:15");
            Assert.Equal("13:15", await Page.EvalOnSelectorAsync<string>("input", "input => input.value"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should throw on incorrect time")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldThrowOnIncorrectTime()
        {
            await Page.SetContentAsync("<input type=time>");
            await Page.ClickAsync("input");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.FillAsync("input", "25:05"));
            Assert.Contains("Malformed value", exception.Message);
        }

        [PlaywrightTest("page-fill.spec.ts", "should fill datetime-local input")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillDatetimeLocalInput()
        {
            await Page.SetContentAsync("<input type=datetime-local>");
            await Page.ClickAsync("input");
            await Page.FillAsync("input", "2020-03-02T05:15");
            Assert.Equal("2020-03-02T05:15", await Page.EvalOnSelectorAsync<string>("input", "input => input.value"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should throw on incorrect datetime-local")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldThrowOnIncorrectDateTimeLocal()
        {
            await Page.SetContentAsync("<input type=datetime-local>");
            await Page.ClickAsync("input");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.FillAsync("input", "abc"));
            Assert.Contains("Malformed value", exception.Message);
        }

        [PlaywrightTest("page-fill.spec.ts", "should fill contenteditable")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillContenteditable()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FillAsync("div[contenteditable]", "some value");
            Assert.Equal("some value", await Page.EvalOnSelectorAsync<string>("div[contenteditable]", "div => div.textContent"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should fill elements with existing value and selection")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillElementsWithExistingValueAndSelection()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");

            await Page.EvalOnSelectorAsync("input", "input => input.value = 'value one'");
            await Page.FillAsync("input", "another value");
            Assert.Equal("another value", await Page.EvaluateAsync<string>("() => result"));

            await Page.EvalOnSelectorAsync("input", @"input => {
                input.selectionStart = 1;
                input.selectionEnd = 2;
            }");
            await Page.FillAsync("input", "maybe this one");
            Assert.Equal("maybe this one", await Page.EvaluateAsync<string>("() => result"));

            await Page.EvalOnSelectorAsync("div[contenteditable]", @"div => {
                div.innerHTML = 'some text <span>some more text<span> and even more text';
                var range = document.createRange();
                range.selectNodeContents(div.querySelector('span'));
                var selection = window.getSelection();
                selection.removeAllRanges();
                selection.addRange(range);
            }");
            await Page.FillAsync("div[contenteditable]", "replace with this");
            Assert.Equal("replace with this", await Page.EvalOnSelectorAsync<string>("div[contenteditable]", "div => div.textContent"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should throw when element is not an &lt;input&gt;, &lt;textarea&gt; or [contenteditable]")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenElementIsNotAnInputOrTextareaOrContenteditable()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.FillAsync("body", string.Empty));
            Assert.Contains("Element is not an <input>", exception.Message);
        }

        [PlaywrightTest("page-fill.spec.ts", "should throw if passed a non-string value")]
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldThrowIfPassedANonStringValue()
        {
        }

        [PlaywrightTest("page-fill.spec.ts", "should retry on disabled element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRetryOnDisabledElement()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.EvalOnSelectorAsync("input", "i => i.disabled = true");

            var task = Page.FillAsync("input", "some value");
            await GiveItAChanceToFillAsync(Page);
            Assert.False(task.IsCompleted);
            Assert.Empty(await Page.EvaluateAsync<string>("() => result"));

            await Page.EvalOnSelectorAsync("input", "i => i.disabled = false");
            await task;
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should retry on readonly element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRetryOnReadonlyElement()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.EvalOnSelectorAsync("textarea", "i => i.readOnly = true");
            var task = Page.FillAsync("textarea", "some value");
            await GiveItAChanceToFillAsync(Page);
            Assert.False(task.IsCompleted);
            Assert.Empty(await Page.EvaluateAsync<string>("() => result"));

            await Page.EvalOnSelectorAsync("textarea", "i => i.readOnly = false");
            await task;
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should retry on invisible element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRetryOnInvisibleElement()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.EvalOnSelectorAsync("input", "i => i.style.display = 'none'");

            var task = Page.FillAsync("input", "some value");
            await GiveItAChanceToFillAsync(Page);
            Assert.False(task.IsCompleted);
            Assert.Empty(await Page.EvaluateAsync<string>("() => result"));

            await Page.EvalOnSelectorAsync("input", "i => i.style.display = 'inline'");
            await task;
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should be able to fill the body")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToFillTheBody()
        {
            await Page.SetContentAsync("<body contentEditable=\"true\"></body>");
            await Page.FillAsync("body", "some value");
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => document.body.textContent"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should fill fixed position input")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillFixedPositionInput()
        {
            await Page.SetContentAsync("<input style='position: fixed;' />");
            await Page.FillAsync("input", "some value");
            Assert.Equal("some value", await Page.EvalOnSelectorAsync<string>("input", "i => i.value"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should be able to fill when focus is in the wrong frame")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToFillWhenFocusIsInTheWrongFrame()
        {
            await Page.SetContentAsync("<div contentEditable=\"true\"></div><iframe></iframe>");
            await Page.FocusAsync("iframe");
            await Page.FillAsync("div", "some value");
            Assert.Equal("some value", await Page.EvalOnSelectorAsync<string>("div", "d => d.textContent"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should be able to fill the input[type=number]")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToFillTheInputTypeNumber()
        {
            await Page.SetContentAsync("<input id=\"input\" type=\"number\"></input>");
            await Page.FillAsync("input", "42");
            Assert.Equal("42", await Page.EvaluateAsync<string>("() => input.value"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should be able to fill exponent into the input[type=number]")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToFillTheInputExponentIntoTypeNumber()
        {
            await Page.SetContentAsync("<input id=\"input\" type=\"number\"></input>");
            await Page.FillAsync("input", "-10e5");
            Assert.Equal("-10e5", await Page.EvaluateAsync<string>("() => input.value"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should be able to fill the input[type=number] with empty string")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToFillTheInputTypeNumberWithEmptyString()
        {
            await Page.SetContentAsync("<input id=\"input\" type=\"number\"></input>");
            await Page.FillAsync("input", "");
            Assert.Empty(await Page.EvaluateAsync<string>("() => input.value"));
        }

        [PlaywrightTest("page-fill.spec.ts", "should not be able to fill text into the input[type=number]")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotBeAbleToFillTextIntoTheInputTypeNumber()
        {
            await Page.SetContentAsync("<input id=\"input\" type=\"number\"></input>");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.FillAsync("input", "abc"));
            Assert.Contains("Cannot type text into input[type=number]", exception.Message);
        }

        [PlaywrightTest("page-fill.spec.ts", "should be able to clear")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToClear()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FillAsync("input", "some value");
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
            await Page.FillAsync("input", "");
            Assert.Empty(await Page.EvaluateAsync<string>("() => result"));
        }

        private async Task GiveItAChanceToFillAsync(IPage page)
        {
            for (int i = 0; i < 5; i++)
            {
                await page.EvaluateAsync("() => new Promise(f => requestAnimationFrame(() => requestAnimationFrame(f)))");
            }
        }
    }
}
