using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.fill</playwright-describe>
    public class PageFillTests : PlaywrightSharpPageBaseTest
    {
        internal PageFillTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should fill textarea</playwright-it>
        [Fact]
        public async Task ShouldFillTextarea()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FillAsync("textarea", "some value");
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should fill input</playwright-it>
        [Fact]
        public async Task ShouldFillInput()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FillAsync("input", "some value");
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should throw on non-text inputs</playwright-it>
        [Fact]
        public async Task ShouldThrowOnNonTextInputs()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            foreach (string type in new[] { "color", "number", "date" })
            {
                await Page.QuerySelectorEvaluateAsync("input", "(input, type) => input.setAttribute('type', type)", type);
                var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.FillAsync("input", string.Empty));
                Assert.Contains("Cannot fill input of type", exception.Message);
            }
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should fill different input types</playwright-it>
        [Fact]
        public async Task ShouldFillDifferentInputTypes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            foreach (string type in new[] { "password", "search", "tel", "text", "url" })
            {
                await Page.QuerySelectorEvaluateAsync("input", "(input, type) => input.setAttribute('type', type)", type);
                await Page.FillAsync("input", "text " + type);
                Assert.Equal("text " + type, await Page.EvaluateAsync<string>("() => result"));
            }
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should fill contenteditable</playwright-it>
        [Fact]
        public async Task ShouldFillContenteditable()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FillAsync("div[contenteditable]", "some value");
            Assert.Equal("some value", await Page.QuerySelectorEvaluateAsync<string>("div[contenteditable]", "div => div.textContent"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should fill elements with existing value and selection</playwright-it>
        [Fact]
        public async Task ShouldFillElementsWithExistingValueAndSelection()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");

            await Page.QuerySelectorEvaluateAsync("input", "input => input.value = 'value one'");
            await Page.FillAsync("input", "another value");
            Assert.Equal("another value", await Page.EvaluateAsync<string>("() => result"));

            await Page.QuerySelectorEvaluateAsync("input", @"input => {
                input.selectionStart = 1;
                input.selectionEnd = 2;
            }");
            await Page.FillAsync("input", "maybe this one");
            Assert.Equal("maybe this one", await Page.EvaluateAsync<string>("() => result"));

            await Page.QuerySelectorEvaluateAsync("div[contenteditable]", @"div => {
                div.innerHTML = 'some text <span>some more text<span> and even more text';
                var range = document.createRange();
                range.selectNodeContents(div.querySelector('span'));
                var selection = window.getSelection();
                selection.removeAllRanges();
                selection.addRange(range);
            }");
            await Page.FillAsync("div[contenteditable]", "replace with this");
            Assert.Equal("replace with this", await Page.QuerySelectorEvaluateAsync<string>("div[contenteditable]", "div => div.textContent"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should throw when element is not an <input>, <textarea> or [contenteditable]</playwright-it>
        [Fact]
        public async Task ShouldThrowWhenElementIsNotAnInputOrTextareaOrContenteditable()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.FillAsync("body", string.Empty));
            Assert.Contains("Element is not an <input>", exception.Message);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should throw if passed a non-string value</playwright-it>
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldThrowIfPassedANonStringValue()
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should wait for visible visibilty</playwright-it>
        [Fact]
        public async Task ShouldWaitForVisibleVisibilty()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.FillAsync("input", "some value");
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));

            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.QuerySelectorEvaluateAsync("input", "i => i.style.display = 'none'");
            await Task.WhenAll(
                Page.FillAsync("input", "some value"),
                Page.QuerySelectorEvaluateAsync("input", "i => i.style.display = 'block'")
            );
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should throw on disabled and readonly elements</playwright-it>
        [Fact]
        public async Task ShouldThrowOnDisabledAndReadonlyElements()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.QuerySelectorEvaluateAsync("input", "i => i.disabled = true");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.FillAsync("input", "some value"));
            Assert.Equal("Cannot fill a disabled input.", exception.Message);

            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.QuerySelectorEvaluateAsync("textarea", "i => i.readOnly = true");
            exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.FillAsync("textarea", "some value"));
            Assert.Equal("Cannot fill a readonly textarea.", exception.Message);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should throw on hidden and invisible elements</playwright-it>
        [Fact]
        public async Task ShouldThrowOnHiddenAndInvisibleElements()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.QuerySelectorEvaluateAsync("input", "i => i.style.display = 'none'");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.FillAsync("input", "some value", new { waitFor = "nowait" }));
            Assert.Equal("Element is not visible", exception.Message);

            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.QuerySelectorEvaluateAsync("input", "i => i.style.visibility = 'hidden'");
            exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.FillAsync("input", "some value", new { waitFor = "nowait" });
            Assert.Equal("Element is hidden", exception.Message);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should be able to fill the body</playwright-it>
        [Fact]
        public async Task ShouldBeAbleToFillTheBody()
        {
            await Page.SetContentAsync("<body contentEditable=\"true\"></body>");
            await Page.FillAsync("body", "some value");
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => document.body.textContent"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should be able to fill when focus is in the wrong frame</playwright-it>
        [Fact]
        public async Task ShouldBeAbleToFillWhenFocusIsInTheWrongFrame()
        {
            await Page.SetContentAsync("<div contentEditable=\"true\"></div><iframe></iframe>");
            await Page.FocusAsync("iframe");
            await Page.FillAsync("div", "some value");
            Assert.Equal("some value", await Page.QuerySelectorEvaluateAsync<string>("div", "d => d.textContent"));
        }

    }

}
