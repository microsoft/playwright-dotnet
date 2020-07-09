using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.$$</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class ElementHandleQuerySelectorAllTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleQuerySelectorAllTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$$</playwright-describe>
        ///<playwright-it>should query existing elements</playwright-it>
        [Retry]
        public async Task ShouldQueryExistingElements()
        {
            await Page.SetContentAsync("<html><body><div>A</div><br/><div>B</div></body></html>");
            var html = await Page.QuerySelectorAsync("html");
            var elements = await html.QuerySelectorAllAsync("div");
            Assert.Equal(2, elements.Length);
            var tasks = elements.Select(element => Page.EvaluateAsync<string>("e => e.textContent", element));
            Assert.Equal(new[] { "A", "B" }, await Task.WhenAll(tasks));
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$$</playwright-describe>
        ///<playwright-it>should return empty array for non-existing elements</playwright-it>
        [Retry]
        public async Task ShouldReturnEmptyArrayForNonExistingElements()
        {
            await Page.SetContentAsync("<html><body><span>A</span><br/><span>B</span></body></html>");
            var html = await Page.QuerySelectorAsync("html");
            var elements = await html.QuerySelectorAllAsync("div");
            Assert.Empty(elements);
        }
    }
}
