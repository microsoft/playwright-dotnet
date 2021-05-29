using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageClickTimeout4Tests : PageTestEx
    {
        [PlaywrightTest("page-click-timeout-4.spec.ts", "should timeout waiting for stable position")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForStablePosition()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", @"button => {
                button.style.transition = 'margin 5s linear 0s';
                button.style.marginLeft = '200px';
            }");

            var exception = await AssertThrowsAsync<TimeoutException>(()
                => Page.ClickAsync("button", new PageClickOptions { Timeout = 3000 }));

            StringAssert.Contains("Timeout 3000ms exceeded", exception.Message);
            StringAssert.Contains("waiting for element to be visible, enabled and stable", exception.Message);
            StringAssert.Contains("element is not stable - waiting", exception.Message);
        }
    }
}
