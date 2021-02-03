using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.selectText</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleSelectTextTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleSelectTextTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.selectText", "should select textarea")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectTextarea()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
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

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.selectText", "should select input")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectInput()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
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

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.selectText", "should select plain div")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectPlainDiv()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var div = await Page.QuerySelectorAsync("div.plain");
            await div.EvaluateAsync("input => input.value = 'some value'");
            await div.SelectTextAsync();

            Assert.Equal("Plain div", await Page.EvaluateAsync<string>("() => window.getSelection().toString()"));
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.selectText", "should timeout waiting for invisible element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForInvisibleElement()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var textarea = await Page.QuerySelectorAsync("textarea");
            await textarea.EvaluateAsync("e => e.style.display = 'none'");

            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => textarea.SelectTextAsync(3000));
            Assert.Contains("element is not visible", exception.Message);
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.selectText", "should wait for visible")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForVisible()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var textarea = await Page.QuerySelectorAsync("textarea");
            await textarea.EvaluateAsync("textarea => textarea.value = 'some value'");
            await textarea.EvaluateAsync("e => e.style.display = 'none'");

            var task = textarea.SelectTextAsync(3000);
            await Page.EvaluateAsync("() => new Promise(f => setTimeout(f, 1000))");
            Assert.False(task.IsCompleted);
            await textarea.EvaluateAsync("e => e.style.display = 'block'");
            await task;
        }
    }
}
