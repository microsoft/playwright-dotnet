using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.hover</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class ElementHandleHoverTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleHoverTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.hover</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/scrollable.html");
            var button = await Page.QuerySelectorAsync("#button-6");
            await button.HoverAsync();
            Assert.Equal("button-6", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.hover</playwright-describe>
        ///<playwright-it>should work when Node is removed</playwright-it>
        [Retry]
        public async Task ShouldWorkWhenNodeIsRemoved()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/scrollable.html");
            await Page.EvaluateAsync("() => delete window['Node']");
            var button = await Page.QuerySelectorAsync("#button-6");
            await button.HoverAsync();
            Assert.Equal("button-6", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
        }
    }
}
