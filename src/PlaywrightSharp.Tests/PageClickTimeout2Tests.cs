using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageClickTimeout2Tests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageClickTimeout2Tests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-click-timeout2.spec.ts", "should timeout waiting for display:none to be gone")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForDisplayNoneToBeGone()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");
            var exception = await Assert.ThrowsAsync<TimeoutException>(()
                => Page.ClickAsync("button", timeout: 5000));

            Assert.Contains("Timeout 5000ms exceeded", exception.Message);
            Assert.Contains("waiting for element to be visible, enabled and not moving", exception.Message);
            Assert.Contains("element is not visible - waiting", exception.Message);
        }

        [PlaywrightTest("page-click-timeout2.spec.ts", "should timeout waiting for visbility:hidden to be gone")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForVisbilityHiddenToBeGone()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'hidden'");
            var clickTask = Page.ClickAsync("button", timeout: 5000);
            var exception = await Assert.ThrowsAsync<TimeoutException>(()
                => Page.ClickAsync("button", timeout: 5000));

            Assert.Contains("Timeout 5000ms exceeded", exception.Message);
            Assert.Contains("waiting for element to be visible, enabled and not moving", exception.Message);
            Assert.Contains("element is not visible - waiting", exception.Message);
        }

    }
}
