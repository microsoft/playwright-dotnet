using System;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.selectOption</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageSelectOptionTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageSelectOptionTests(ITestOutputHelper output) : base(output)
        {
            DefaultOptions = TestConstants.GetHeadfulOptions();
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should select single option</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOption()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", "blue");
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should select single option by value</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOptionByValue()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", new SelectOption { Value = "blue" });
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should select single option by label</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOptionByLabel()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", new SelectOption { Label = "Indigo" });
            Assert.Equal(new[] { "indigo" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "indigo" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should select single option by handle</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOptionByHandle()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", await Page.QuerySelectorAsync("[id=whiteOption]"));
            Assert.Equal(new[] { "white" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "white" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should select single option by index</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOptionByIndex()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", new SelectOption { Index = 2 });
            Assert.Equal(new[] { "brown" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "brown" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should select single option by multiple attributes</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOptionByMultipleAttributes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", new SelectOption { Value = "green", Label = "Green" });
            Assert.Equal(new[] { "green" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "green" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should not select single option when some attributes do not match</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotSelectSingleOptionWhenSomeAttributesDoNotMatch()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", new SelectOption { Value = "green", Label = "Brown" });
            Assert.Empty(await Page.EvaluateAsync<string>("() => document.querySelector('select').value"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should select only first option</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectOnlyFirstOption()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", "blue", "green", "red");
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should not throw when select causes navigation</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotThrowWhenSelectCausesNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvalOnSelectorAsync("select", "select => select.addEventListener('input', () => window.location = '/empty.html')");
            await TaskUtils.WhenAll(
                Page.SelectOptionAsync("select", "blue"),
                Page.WaitForNavigationAsync()
            );
            Assert.Contains("empty.html", Page.Url);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should select multiple options</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectMultipleOptions()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync("() => makeMultiple()");
            await Page.SelectOptionAsync("select", "blue", "green", "red");
            Assert.Equal(new[] { "blue", "green", "red" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue", "green", "red" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should select multiple options with attributes</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectMultipleOptionsWithAttributes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync("() => makeMultiple()");
            await Page.SelectOptionAsync("select", new SelectOption { Value = "blue" }, new SelectOption { Label = "Green" }, new SelectOption { Index = 4 });
            Assert.Equal(new[] { "blue", "gray", "green" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue", "gray", "green" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should respect event bubbling</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectEventBubbling()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", "blue");
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onBubblingInput"));
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onBubblingChange"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should throw when element is not a &lt;select&gt;</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenElementIsNotASelect()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.SelectOptionAsync("body", string.Empty));
            Assert.Contains("Element is not a <select> element.", exception.Message);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should return [] on no matched values</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEmptyArrayOnNoMatchedValues()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            string[] result = await Page.SelectOptionAsync("select", "42", "abc");
            Assert.Empty(result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should return an array of matched values</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnAnArrayOfMatchedValues()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync<string>("() => makeMultiple()");
            string[] result = await Page.SelectOptionAsync("select", "blue", "black", "magenta");
            Assert.Equal(new[] { "blue", "black", "magenta" }.OrderBy(v => v), result.OrderBy(v => v));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should return an array of one element when multiple is not set</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnAnArrayOfOneElementWhenMultipleIsNotSet()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            string[] result = await Page.SelectOptionAsync("select", "42", "blue", "black", "magenta");
            Assert.Single(result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should return [] on no values</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEmptyArrayOnNoValues()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            string[] result = await Page.SelectOptionAsync("select", Array.Empty<string>());
            Assert.Empty(result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should not allow null items</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotAllowNullItems()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync("() => makeMultiple()");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(
                () => Page.SelectOptionAsync("select", new[] { "blue", null, "black", "magenta" }));
            Assert.Contains("got null", exception.Message);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should unselect with null</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUnselectWithNull()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync("() => makeMultiple()");
            string[] result = await Page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
            Assert.True(result.All(r => new[] { "blue", "black", "magenta" }.Contains(r)));
            await Page.SelectOptionAsync("select");
            Assert.True(await Page.EvalOnSelectorAsync<bool?>("select", "select => Array.from(select.options).every(option => !option.selected)"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should deselect all options when passed no values for a multiple select</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDeselectAllOptionsWhenPassedNoValuesForAMultipleSelect()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync("() => makeMultiple()");
            await Page.SelectOptionAsync("select", "blue", "black", "magenta");
            await Page.SelectOptionAsync("select");
            Assert.True(await Page.EvalOnSelectorAsync<bool>("select", "select => Array.from(select.options).every(option => !option.selected)"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should deselect all options when passed no values for a select without multiple</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDeselectAllOptionsWhenPassedNoValuesForASelectWithoutMultiple()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", "blue", "black", "magenta");
            await Page.SelectOptionAsync("select", Array.Empty<string>());
            Assert.True(await Page.EvalOnSelectorAsync<bool>("select", "select => Array.from(select.options).every(option => !option.selected)"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should throw if passed wrong types</playwright-it>
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldThrowIfPassedWrongTypes()
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.selectOption</playwright-describe>
        ///<playwright-it>should work when re-defining top-level Event class</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenReDefiningTopLevelEventClass()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync("() => window.Event = null");
            await Page.SelectOptionAsync("select", "blue");
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }
    }
}
