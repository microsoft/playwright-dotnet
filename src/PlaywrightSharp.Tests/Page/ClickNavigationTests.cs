using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>Click navigation</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ClickNavigationTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ClickNavigationTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("navigation.spec.js", "Click navigation", "should work with _blank target")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBblankTarget()
        {
            Server.SetRoute("/empty.html", ctx =>
            ctx.Response.WriteAsync($"<a href=\"{TestConstants.EmptyPage}\" target=\"_blank\">Click me</a>"));
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.ClickAsync("\"Click me\"");
        }

        [PlaywrightTest("navigation.spec.js", "Click navigation", "should work with cross-process _blank target")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCrossProcessBlankTarget()
        {
            Server.SetRoute("/empty.html", ctx =>
            ctx.Response.WriteAsync($"<a href=\"{TestConstants.CrossProcessUrl}/empty.html\" target=\"_blank\">Click me</a>"));
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.ClickAsync("\"Click me\"");
        }
    }
}
