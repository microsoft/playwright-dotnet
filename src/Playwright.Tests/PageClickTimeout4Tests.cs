using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageClickTimeout4Tests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageClickTimeout4Tests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-click-timeout-4.spec.ts", "should timeout waiting for stable position")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForStablePosition()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", @"button => {
                button.style.transition = 'margin 5s linear 0s';
                button.style.marginLeft = '200px';
            }");

            var exception = await Assert.ThrowsAsync<TimeoutException>(()
                => Page.ClickAsync("button", timeout: 3000));

            Assert.Contains("Timeout 3000ms exceeded", exception.Message);
            Assert.Contains("waiting for element to be visible, enabled and stable", exception.Message);
            Assert.Contains("element is not stable - waiting", exception.Message);
        }
    }
}
