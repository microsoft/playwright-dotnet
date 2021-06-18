using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageNavigationTests : PageTestEx
    {
        [PlaywrightTest("page-navigation.spec.ts", "should work with _blank target")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBlankTarget()
        {
            Server.SetRoute("/empty.html", ctx =>
            ctx.Response.WriteAsync($"<a href=\"{Server.EmptyPage}\" target=\"_blank\">Click me</a>"));
            await Page.GotoAsync(Server.EmptyPage);
            await Page.ClickAsync("\"Click me\"");
        }

        [PlaywrightTest("page-navigation.spec.ts", "should work with cross-process _blank target")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCrossProcessBlankTarget()
        {
            Server.SetRoute("/empty.html", ctx =>
            ctx.Response.WriteAsync($"<a href=\"{Server.CrossProcessPrefix}/empty.html\" target=\"_blank\">Click me</a>"));
            await Page.GotoAsync(Server.EmptyPage);
            await Page.ClickAsync("\"Click me\"");
        }
    }
}
