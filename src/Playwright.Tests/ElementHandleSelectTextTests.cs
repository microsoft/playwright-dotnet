using System;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleSelectTextTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleSelectTextTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("elementhandle-select-text.spec.ts", "should select textarea")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectTextarea()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var textarea = await Page.QuerySelectorAsync("textarea");
            await textarea.EvaluateAsync("textarea => textarea.value = 'some value'");
            await textarea.SelectTextAsync();

            if (TestConstants.IsFirefox)
            {
                Assert.Equal(0, await textarea.EvaluateAsync<int>("el => el.selectionStart"));
                Assert.Equal(10, await textarea.EvaluateAsync<int>("el => el.selectionEnd"));
            }
            else
            {
                Assert.Equal("some value", await Page.EvaluateAsync<string>("() => window.getSelection().toString()"));
            }
        }

        [PlaywrightTest("elementhandle-select-text.spec.ts", "should select input")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectInput()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var input = await Page.QuerySelectorAsync("input");
            await input.EvaluateAsync("input => input.value = 'some value'");
            await input.SelectTextAsync();

            if (TestConstants.IsFirefox)
            {
                Assert.Equal(0, await input.EvaluateAsync<int>("el => el.selectionStart"));
                Assert.Equal(10, await input.EvaluateAsync<int>("el => el.selectionEnd"));
            }
            else
            {
                Assert.Equal("some value", await Page.EvaluateAsync<string>("() => window.getSelection().toString()"));
            }
        }

        [PlaywrightTest("elementhandle-select-text.spec.ts", "should select plain div")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectPlainDiv()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var div = await Page.QuerySelectorAsync("div.plain");
            await div.EvaluateAsync("input => input.value = 'some value'");
            await div.SelectTextAsync();

            Assert.Equal("Plain div", await Page.EvaluateAsync<string>("() => window.getSelection().toString()"));
        }

        [PlaywrightTest("elementhandle-select-text.spec.ts", "should timeout waiting for invisible element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForInvisibleElement()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var textarea = await Page.QuerySelectorAsync("textarea");
            await textarea.EvaluateAsync("e => e.style.display = 'none'");

            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => textarea.SelectTextAsync(new ElementHandleSelectTextOptions { Timeout = 3000 }));
            Assert.Contains("element is not visible", exception.Message);
        }

        [PlaywrightTest("elementhandle-select-text.spec.ts", "should wait for visible")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForVisible()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var textarea = await Page.QuerySelectorAsync("textarea");
            await textarea.EvaluateAsync("textarea => textarea.value = 'some value'");
            await textarea.EvaluateAsync("e => e.style.display = 'none'");

            var task = textarea.SelectTextAsync(new ElementHandleSelectTextOptions { Timeout = 3000 });
            await Page.EvaluateAsync("() => new Promise(f => setTimeout(f, 1000))");
            Assert.False(task.IsCompleted);
            await textarea.EvaluateAsync("e => e.style.display = 'block'");
            await task;
        }
    }
}
