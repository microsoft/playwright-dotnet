using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageClickTimeout1Tests : PageTestEx
    {
        [PlaywrightTest("page-click-timeout-1.spec.ts", "should avoid side effects after timeout")]
        [Test, Ignore("Ignore USES_HOOKS")]
        public void ShouldAvoidSideEffectsAfterTimeout()
        {
        }

        [PlaywrightTest("page-click-timeout-1.spec.ts", "should timeout waiting for button to be enabled")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForButtonToBeEnabled()
        {
            await Page.SetContentAsync("<button onclick=\"javascript: window.__CLICKED = true;\" disabled><span>Click target</span></button>");
            var clickTask = Page.ClickAsync("text=Click target", new() { Timeout = 3000 });
            Assert.Null(await Page.EvaluateAsync<bool?>("window.__CLICKED"));

            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => clickTask);

            StringAssert.Contains("Timeout 3000ms exceeded", exception.Message);
            StringAssert.Contains("element is not enabled - waiting", exception.Message);
        }
    }
}
