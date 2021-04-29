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
    public class PageClickTimeout2Tests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageClickTimeout2Tests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-click-timeout-2.spec.ts", "should timeout waiting for display:none to be gone")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForDisplayNoneToBeGone()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");
            var exception = await Assert.ThrowsAsync<TimeoutException>(()
                => Page.ClickAsync("button", timeout: 5000));

            Assert.Contains("Timeout 5000ms exceeded", exception.Message);
            Assert.Contains("waiting for element to be visible, enabled and stable", exception.Message);
            Assert.Contains("element is not visible - waiting", exception.Message);
        }

        [PlaywrightTest("page-click-timeout-2.spec.ts", "should timeout waiting for visbility:hidden to be gone")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForVisbilityHiddenToBeGone()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'hidden'");
            var clickTask = Page.ClickAsync("button", timeout: 5000);
            var exception = await Assert.ThrowsAsync<TimeoutException>(()
                => Page.ClickAsync("button", timeout: 5000));

            Assert.Contains("Timeout 5000ms exceeded", exception.Message);
            Assert.Contains("waiting for element to be visible, enabled and stable", exception.Message);
            Assert.Contains("element is not visible - waiting", exception.Message);
        }

    }
}
