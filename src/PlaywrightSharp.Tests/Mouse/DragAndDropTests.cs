using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Mouse
{
    ///<playwright-file>mouse.spec.js</playwright-file>
    ///<playwright-describe>Drag and Drop</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class DragAndDropTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public DragAndDropTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("mouse.spec.js", "Drag and Drop", "should work")]
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
