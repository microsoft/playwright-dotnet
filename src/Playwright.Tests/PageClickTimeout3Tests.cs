using System;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageClickTimeout3Tests : PageTestEx
    {
        [PlaywrightTest("page-click-timeout-3.spec.ts", "should fail when element jumps during hit testing")]
        [Test, Ignore(" Skip USES_HOOKS")]
        public void ShouldFailWhenElementJumpsDuringHitTesting()
        {
        }

        [PlaywrightTest("page-click-timeout-3.spec.ts", "should timeout waiting for hit target")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForHitTarget()
        {
            await Page.GotoAsync(Server.Prefix + "/input/button.html");
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

            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(()
                => button.ClickAsync(new() { Timeout = 5000 }));

            StringAssert.Contains("Timeout 5000ms exceeded.", exception.Message);
        }
    }
}
