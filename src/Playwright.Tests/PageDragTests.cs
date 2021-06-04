using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageDragTests : PageTestEx
    {
        [PlaywrightTest("page-drag.spec.ts", "should work")]
        [Test, Ignore("Skipped in Playwright")]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
            await Page.HoverAsync("#source");
            await Page.Mouse.DownAsync();
            await Page.HoverAsync("#target");
            await Page.Mouse.UpAsync();

            Assert.True(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"));
        }
    }
}
