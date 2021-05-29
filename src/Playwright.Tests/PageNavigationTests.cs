using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageNavigationTests : PageTestEx
    {
        [PlaywrightTest("page-navigation.spec.ts", "should work with _blank target")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBblankTarget()
        {
            HttpServer.Server.SetRoute("/empty.html", ctx =>
            ctx.Response.WriteAsync($"<a href=\"{TestConstants.EmptyPage}\" target=\"_blank\">Click me</a>"));
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.ClickAsync("\"Click me\"");
        }

        [PlaywrightTest("page-navigation.spec.ts", "should work with cross-process _blank target")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCrossProcessBlankTarget()
        {
            HttpServer.Server.SetRoute("/empty.html", ctx =>
            ctx.Response.WriteAsync($"<a href=\"{TestConstants.CrossProcessUrl}/empty.html\" target=\"_blank\">Click me</a>"));
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.ClickAsync("\"Click me\"");
        }
    }
}
