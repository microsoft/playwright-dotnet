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
    public class PageSelectOptionTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageSelectOptionTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-select-option.spec.ts", "should select single option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOption()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", "blue");
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should select single option by value")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOptionByValue()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", new SelectOptionValue { Value = "blue" });
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should select single option by label")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOptionByLabel()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", new SelectOptionValue { Label = "Indigo" });
            Assert.Equal(new[] { "indigo" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "indigo" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should select single option by handle")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOptionByHandle()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", await Page.QuerySelectorAsync("[id=whiteOption]"));
            Assert.Equal(new[] { "white" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "white" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should select single option by index")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOptionByIndex()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", new SelectOptionValue { Index = 2 });
            Assert.Equal(new[] { "brown" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "brown" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should select single option by multiple attributes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOptionByMultipleAttributes()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", new SelectOptionValue { Value = "green", Label = "Green" });
            Assert.Equal(new[] { "green" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "green" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should not select single option when some attributes do not match")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotSelectSingleOptionWhenSomeAttributesDoNotMatch()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvalOnSelectorAsync("select", "s => s.value = undefined");
            await Assert.ThrowsAsync<TimeoutException>(() => Page.SelectOptionAsync("select", new SelectOptionValue { Value = "green", Label = "Brown" }));
            Assert.Empty(await Page.EvaluateAsync<string>("() => document.querySelector('select').value"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should select only first option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectOnlyFirstOption()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", new[] { "blue", "green", "red" });
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should not throw when select causes navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotThrowWhenSelectCausesNavigation()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvalOnSelectorAsync("select", "select => select.addEventListener('input', () => window.location = '/empty.html')");
            await TaskUtils.WhenAll(
                Page.SelectOptionAsync("select", "blue"),
                Page.WaitForNavigationAsync()
            );
            Assert.Contains("empty.html", Page.Url);
        }

        [PlaywrightTest("page-select-option.spec.ts", "should select multiple options")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectMultipleOptions()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync("() => makeMultiple()");
            await Page.SelectOptionAsync("select", new[] { "blue", "green", "red" });
            Assert.Equal(new[] { "blue", "green", "red" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue", "green", "red" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should select multiple options with attributes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectMultipleOptionsWithAttributes()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync("() => makeMultiple()");
            await Page.SelectOptionAsync(
                "select",
                new[] {
                    new SelectOptionValue { Value = "blue" },
                    new SelectOptionValue { Label = "Green" },
                    new SelectOptionValue { Index = 4 }
                });
            Assert.Equal(new[] { "blue", "gray", "green" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue", "gray", "green" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should respect event bubbling")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectEventBubbling()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", "blue");
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onBubblingInput"));
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onBubblingChange"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should throw when element is not a &lt;select&gt;")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenElementIsNotASelect()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            var exception = await Assert.ThrowsAsync<PlaywrightException>(() => Page.SelectOptionAsync("body", string.Empty));
            Assert.Contains("Element is not a <select> element.", exception.Message);
        }

        [PlaywrightTest("page-select-option.spec.ts", "should return [] on no matched values")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEmptyArrayOnNoMatchedValues()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            var result = await Page.SelectOptionAsync("select", Array.Empty<string>());
            Assert.Empty(result);
        }

        [PlaywrightTest("page-select-option.spec.ts", "should return an array of matched values")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnAnArrayOfMatchedValues()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync<string>("() => makeMultiple()");
            var result = await Page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
            Assert.Equal(new[] { "blue", "black", "magenta" }.OrderBy(v => v), result.OrderBy(v => v));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should return an array of one element when multiple is not set")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnAnArrayOfOneElementWhenMultipleIsNotSet()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            var result = await Page.SelectOptionAsync("select", new[] { "42", "blue", "black", "magenta" });
            Assert.Single(result);
        }

        [PlaywrightTest("page-select-option.spec.ts", "should return [] on no values")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEmptyArrayOnNoValues()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            var result = await Page.SelectOptionAsync("select", Array.Empty<string>());
            Assert.Empty(result);
        }

        [PlaywrightTest("page-select-option.spec.ts", "should not allow null items")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotAllowNullItems()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync("() => makeMultiple()");
            var exception = await Assert.ThrowsAsync<PlaywrightException>(
                () => Page.SelectOptionAsync("select", new[] { "blue", null, "black", "magenta" }));
            Assert.Contains("got null", exception.Message);
        }

        [PlaywrightTest("page-select-option.spec.ts", "should unselect with null")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUnselectWithNull()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync("() => makeMultiple()");
            var result = await Page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
            Assert.True(result.All(r => new[] { "blue", "black", "magenta" }.Contains(r)));
            await Page.SelectOptionAsync("select");
            Assert.True(await Page.EvalOnSelectorAsync<bool?>("select", "select => Array.from(select.options).every(option => !option.selected)"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should deselect all options when passed no values for a multiple select")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDeselectAllOptionsWhenPassedNoValuesForAMultipleSelect()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync("() => makeMultiple()");
            await Page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
            await Page.SelectOptionAsync("select");
            Assert.True(await Page.EvalOnSelectorAsync<bool>("select", "select => Array.from(select.options).every(option => !option.selected)"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should deselect all options when passed no values for a select without multiple")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDeselectAllOptionsWhenPassedNoValuesForASelectWithoutMultiple()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
            await Page.SelectOptionAsync("select", Array.Empty<string>());
            Assert.True(await Page.EvalOnSelectorAsync<bool>("select", "select => Array.from(select.options).every(option => !option.selected)"));
        }

        [PlaywrightTest("page-select-option.spec.ts", "should throw if passed wrong types")]
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldThrowIfPassedWrongTypes()
        {
        }

        [PlaywrightTest("page-select-option.spec.ts", "should work when re-defining top-level Event class")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenReDefiningTopLevelEventClass()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            await Page.EvaluateAsync("() => window.Event = null");
            await Page.SelectOptionAsync("select", "blue");
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }
    }
}
