using System;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class ElementHandleSelectTextTests : PageTestEx
    {
        [PlaywrightTest("elementhandle-select-text.spec.ts", "should select textarea")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectTextarea()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var textarea = await Page.QuerySelectorAsync("textarea");
            await textarea.EvaluateAsync("textarea => textarea.value = 'some value'");
            await textarea.SelectTextAsync();

            if (TestConstants.IsFirefox)
            {
                Assert.AreEqual(0, await textarea.EvaluateAsync<int>("el => el.selectionStart"));
                Assert.AreEqual(10, await textarea.EvaluateAsync<int>("el => el.selectionEnd"));
            }
            else
            {
                Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => window.getSelection().toString()"));
            }
        }

        [PlaywrightTest("elementhandle-select-text.spec.ts", "should select input")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectInput()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var input = await Page.QuerySelectorAsync("input");
            await input.EvaluateAsync("input => input.value = 'some value'");
            await input.SelectTextAsync();

            if (TestConstants.IsFirefox)
            {
                Assert.AreEqual(0, await input.EvaluateAsync<int>("el => el.selectionStart"));
                Assert.AreEqual(10, await input.EvaluateAsync<int>("el => el.selectionEnd"));
            }
            else
            {
                Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => window.getSelection().toString()"));
            }
        }

        [PlaywrightTest("elementhandle-select-text.spec.ts", "should select plain div")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectPlainDiv()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var div = await Page.QuerySelectorAsync("div.plain");
            await div.EvaluateAsync("input => input.value = 'some value'");
            await div.SelectTextAsync();

            Assert.AreEqual("Plain div", await Page.EvaluateAsync<string>("() => window.getSelection().toString()"));
        }

        [PlaywrightTest("elementhandle-select-text.spec.ts", "should timeout waiting for invisible element")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForInvisibleElement()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var textarea = await Page.QuerySelectorAsync("textarea");
            await textarea.EvaluateAsync("e => e.style.display = 'none'");

            var exception = await AssertThrowsAsync<TimeoutException>(() => textarea.SelectTextAsync(new ElementHandleSelectTextOptions { Timeout = 3000 }));
            StringAssert.Contains("element is not visible", exception.Message);
        }

        [PlaywrightTest("elementhandle-select-text.spec.ts", "should wait for visible")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
