using System;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageClickTimeout2Tests : PageTestEx
    {
        [PlaywrightTest("page-click-timeout-2.spec.ts", "should timeout waiting for display:none to be gone")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForDisplayNoneToBeGone()
        {
            await Page.GotoAsync(Server.Prefix + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(()
                => Page.ClickAsync("button", new() { Timeout = 5000 }));

            StringAssert.Contains("Timeout 5000ms exceeded", exception.Message);
            StringAssert.Contains("waiting for element to be visible, enabled and stable", exception.Message);
            StringAssert.Contains("element is not visible - waiting", exception.Message);
        }

        [PlaywrightTest("page-click-timeout-2.spec.ts", "should timeout waiting for visbility:hidden to be gone")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForVisibilityHiddenToBeGone()
        {
            await Page.GotoAsync(Server.Prefix + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'hidden'");
            var clickTask = Page.ClickAsync("button", new() { Timeout = 5000 });
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(()
                => Page.ClickAsync("button", new() { Timeout = 5000 }));

            StringAssert.Contains("Timeout 5000ms exceeded", exception.Message);
            StringAssert.Contains("waiting for element to be visible, enabled and stable", exception.Message);
            StringAssert.Contains("element is not visible - waiting", exception.Message);
        }

    }
}
