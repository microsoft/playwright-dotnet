using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageDragTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageDragTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-drag.spec.ts", "Drag and drop", "should work")]
        [Fact(Skip = "Skipped in Playwright")]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/drag-n-drop.html");
            await Page.HoverAsync("#source");
            await Page.Mouse.DownAsync();
            await Page.HoverAsync("#target");
            await Page.Mouse.UpAsync();

            Assert.True(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"));
        }
    }
}
