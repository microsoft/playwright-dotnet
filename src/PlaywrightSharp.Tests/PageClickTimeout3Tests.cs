using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageClickTimeout3Tests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageClickTimeout3Tests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-click-timeout-3.spec.ts", "should fail when element jumps during hit testing")]
        [Fact(Skip = " Skip USES_HOOKS")]
        public void ShouldFailWhenElementJumpsDuringHitTesting()
        {
        }

        [PlaywrightTest("page-click-timeout-3.spec.ts", "should timeout waiting for hit target")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForHitTarget()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");

            await Page.EvalOnSelectorAsync("button", @"button => {
                button.style.borderWidth = '0';
                button.style.width = '200px';
                button.style.height = '20px';
                document.body.style.margin = '0';
                document.body.style.position = 'relative';
                const flyOver = document.createElement('div');
                flyOver.className = 'flyover';
                flyOver.style.position = 'absolute';
                flyOver.style.width = '400px';
                flyOver.style.height = '20px';
                flyOver.style.left = '-200px';
                flyOver.style.top = '0';
                flyOver.style.background = 'red';
                document.body.appendChild(flyOver);
            }");

            var exception = await Assert.ThrowsAsync<TimeoutException>(()
                => button.ClickAsync(timeout: 5000));

            Assert.Contains("Timeout 5000ms exceeded.", exception.Message);
        }
    }
}
