using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageDragTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageDragTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-drag.spec.ts", "should work")]
        [Fact(Skip = "Skipped in Playwright")]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/drag-n-drop.html");
            await Page.HoverAsync("#source");
            await Page.Mouse.DownAsync();
            await Page.HoverAsync("#target");
            await Page.Mouse.UpAsync();

            Assert.True(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"));
        }
    }
}
