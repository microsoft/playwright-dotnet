using System;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageClickTimeout1Tests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageClickTimeout1Tests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-click-timeout-1.spec.ts", "should avoid side effects after timeout")]
        [Fact(Skip = "Ignore USES_HOOKS")]
        public void ShouldAvoidSideEffectsAfterTimeout()
        {
        }

        [PlaywrightTest("page-click-timeout-1.spec.ts", "should timeout waiting for button to be enabled")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForButtonToBeEnabled()
        {
            await Page.SetContentAsync("<button onclick=\"javascript: window.__CLICKED = true;\" disabled><span>Click target</span></button>");
            var clickTask = Page.ClickAsync("text=Click target", timeout: 3000);
            Assert.Null(await Page.EvaluateAsync<bool?>("window.__CLICKED"));

            var exception = await Assert.ThrowsAsync<TimeoutException>(() => clickTask);

            Assert.Contains("Timeout 3000ms exceeded", exception.Message);
            Assert.Contains("element is not enabled - waiting", exception.Message);
        }
    }
}
