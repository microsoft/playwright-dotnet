using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.$</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleQuerySelectorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleQuerySelectorTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$</playwright-describe>
        ///<playwright-it>should query existing element</playwright-it>
        [Retry]
        public async Task ShouldQueryExistingElement()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/playground.html");
            await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
            var html = await Page.QuerySelectorAsync("html");
            var second = await html.QuerySelectorAsync(".second");
            var inner = await second.QuerySelectorAsync(".inner");
            string content = await Page.EvaluateAsync<string>("e => e.textContent", inner);
            Assert.Equal("A", content);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$</playwright-describe>
        ///<playwright-it>should query existing element with zs selector</playwright-it>
        [Retry]
        public async Task ShouldQueryExistingElementWithZsSelector()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/playground.html");
            await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
            var html = await Page.QuerySelectorAsync("zs=html");
            var second = await html.QuerySelectorAsync("zs=.second");
            var inner = await second.QuerySelectorAsync("zs=.inner");
            string content = await Page.EvaluateAsync<string>("e => e.textContent", inner);
            Assert.Equal("A", content);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$</playwright-describe>
        ///<playwright-it>should return null for non-existing element</playwright-it>
        [Retry]
        public async Task ShouldReturnNullForNonExistingElement()
        {
            await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">B</div></div></body></html>");
            var html = await Page.QuerySelectorAsync("html");
            var second = await html.QuerySelectorAsync(".third");
            Assert.Null(second);
        }
    }
}
