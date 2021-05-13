using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleMiscTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleMiscTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("elementhandle-misc.spec.ts", "should hover")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHover()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/scrollable.html");
            var button = await Page.QuerySelectorAsync("#button-6");
            await button.HoverAsync();
            Assert.Equal("button-6", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
        }

        [PlaywrightTest("elementhandle-misc.spec.ts", "should hover when Node is removed")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHoverWhenNodeIsRemoved()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/scrollable.html");
            await Page.EvaluateAsync("() => delete window['Node']");
            var button = await Page.QuerySelectorAsync("#button-6");
            await button.HoverAsync();
            Assert.Equal("button-6", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
        }

        [PlaywrightTest("elementhandle-misc.spec.ts", "should fill input")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillInput()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var handle = await Page.QuerySelectorAsync("input");
            await handle.FillAsync("some value");
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("elementhandle-misc.spec.ts", "should fill input when Node is removed")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillInputWhenNodeIsRemoved()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.EvaluateAsync("() => delete window['Node']");
            var handle = await Page.QuerySelectorAsync("input");
            await handle.FillAsync("some value");
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("elementhandle-misc.spec.ts", "should check the box")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckTheBox()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
            var input = await Page.QuerySelectorAsync("input");
            await input.CheckAsync();
            Assert.True(await Page.EvaluateAsync<bool>("() => checkbox.checked"));
        }

        [PlaywrightTest("elementhandle-misc.spec.ts", "should uncheck the box")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUncheckTheBox()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
            var input = await Page.QuerySelectorAsync("input");
            await input.UncheckAsync();
            Assert.False(await Page.EvaluateAsync<bool>("() => checkbox.checked"));
        }

        [PlaywrightTest("elementhandle-misc.spec.ts", "should focus a button")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFocusAButton()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");

            Assert.False(await button.EvaluateAsync<bool?>("button => document.activeElement === button"));
            await button.FocusAsync();
            Assert.True(await button.EvaluateAsync<bool?>("button => document.activeElement === button"));
        }

        [PlaywrightTest("elementhandle-misc.spec.ts", "should select single option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectSingleOption()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/select.html");
            var select = await Page.QuerySelectorAsync("select");
            await select.SelectOptionAsync("blue");

            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }
    }
}
