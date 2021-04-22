using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
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
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
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
