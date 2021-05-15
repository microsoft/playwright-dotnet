using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ClickNavigationTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ClickNavigationTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-navigation.spec.ts", "should work with _blank target")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBblankTarget()
        {
            Server.SetRoute("/empty.html", ctx =>
            ctx.Response.WriteAsync($"<a href=\"{TestConstants.EmptyPage}\" target=\"_blank\">Click me</a>"));
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.ClickAsync("\"Click me\"");
        }

        [PlaywrightTest("page-navigation.spec.ts", "should work with cross-process _blank target")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCrossProcessBlankTarget()
        {
            Server.SetRoute("/empty.html", ctx =>
            ctx.Response.WriteAsync($"<a href=\"{TestConstants.CrossProcessUrl}/empty.html\" target=\"_blank\">Click me</a>"));
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.ClickAsync("\"Click me\"");
        }
    }
}
