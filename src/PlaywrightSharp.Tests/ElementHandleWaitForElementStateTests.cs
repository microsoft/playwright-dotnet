using System;
using System.Threading.Tasks;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>elementhandle-wait-for-element-state.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleWaitForElementStateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleWaitForElementStateTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for visible")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForVisible()
        {
            await Page.SetContentAsync("<div style='display:none'>content</div>");
            var div = await Page.QuerySelectorAsync("div");
            var task = div.WaitForElementStateAsync(ElementState.Visible);
            await GiveItAChanceToResolve(Page);
            Assert.False(task.IsCompleted);
            await div.EvaluateAsync("div => div.style.display = 'block'");
            await task;
        }

        [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for already visible")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForAlreadyVisible()
        {
            await Page.SetContentAsync("<div>content</div>");
            var div = await Page.QuerySelectorAsync("div");
            await div.WaitForElementStateAsync(ElementState.Visible);
        }

        [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should timeout waiting for visible")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForVisible()
        {
            await Page.SetContentAsync("<div style='display:none'>content</div>");
            var div = await Page.QuerySelectorAsync("div");
            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => div.WaitForElementStateAsync(ElementState.Visible, 1000));
            Assert.Contains("Timeout 1000ms exceeded", exception.Message);
        }

        [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should throw waiting for visible when detached")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWaitingForVisibleWhenDetached()
        {
            await Page.SetContentAsync("<div style='display:none'>content</div>");
            var div = await Page.QuerySelectorAsync("div");
            var task = div.WaitForElementStateAsync(ElementState.Visible);
            await div.EvaluateAsync("div => div.remove()");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => task);
            Assert.Contains("Element is not attached to the DOM", exception.Message);
        }

        [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for hidden")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForHidden()
        {
            await Page.SetContentAsync("<div>content</div>");
            var div = await Page.QuerySelectorAsync("div");
            var task = div.WaitForElementStateAsync(ElementState.Hidden);
            await GiveItAChanceToResolve(Page);
            Assert.False(task.IsCompleted);
            await div.EvaluateAsync("div => div.style.display = 'none'");
            await task;
        }

        [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for already hidden")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForAlreadyHidden()
        {
            await Page.SetContentAsync("<div></div>");
            var div = await Page.QuerySelectorAsync("div");
            await div.WaitForElementStateAsync(ElementState.Hidden);
        }

        [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should throw waiting for hidden when detached")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWaitingForHiddenWhenDetached()
        {
            await Page.SetContentAsync("<div>content</div>");
            var div = await Page.QuerySelectorAsync("div");
            var task = div.WaitForElementStateAsync(ElementState.Hidden);
            await GiveItAChanceToResolve(Page);
            Assert.False(task.IsCompleted);
            await div.EvaluateAsync("div => div.remove()");
            await task;
        }

        [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for enabled button")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForEnabledButton()
        {
            await Page.SetContentAsync("<button disabled><span>Target</span></button>");
            var span = await Page.QuerySelectorAsync("text=Target");
            var task = span.WaitForElementStateAsync(ElementState.Enabled);
            await GiveItAChanceToResolve(Page);
            Assert.False(task.IsCompleted);
            await span.EvaluateAsync("span => span.parentElement.disabled = false");
            await task;
        }

        [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should throw waiting for enabled when detached")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWaitingForEnabledWhenDetached()
        {
            await Page.SetContentAsync("<button disabled>Target</button>");
            var button = await Page.QuerySelectorAsync("button");
            var task = button.WaitForElementStateAsync(ElementState.Enabled);
            await button.EvaluateAsync("button => button.remove()");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => task);
            Assert.Contains("Element is not attached to the DOM", exception.Message);
        }

        [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for disabled button")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForDisabledButton()
        {
            await Page.SetContentAsync("<button><span>Target</span></button>");
            var span = await Page.QuerySelectorAsync("text=Target");
            var task = span.WaitForElementStateAsync(ElementState.Disabled);
            await GiveItAChanceToResolve(Page);
            Assert.False(task.IsCompleted);
            await span.EvaluateAsync("span => span.parentElement.disabled = true");
            await task;
        }

        [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for stable position")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWaitForStablePosition()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvalOnSelectorAsync("button", @"button => {
                button.style.transition = 'margin 10000ms linear 0s';
                button.style.marginLeft = '20000px';
            }");

            var task = button.WaitForElementStateAsync(ElementState.Stable);
            await GiveItAChanceToResolve(Page);
            Assert.False(task.IsCompleted);
            await button.EvaluateAsync("button => button.style.transition = ''");
            await task;
        }

        private async Task GiveItAChanceToResolve(IPage page)
        {
            for (int i = 0; i < 5; i++)
            {
                await page.EvaluateAsync("() => new Promise(f => requestAnimationFrame(() => requestAnimationFrame(f)))");
            }
        }
    }
}
