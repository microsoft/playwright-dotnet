using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$$</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class PageQuerySelectorAllTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageQuerySelectorAllTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$</playwright-describe>
        ///<playwright-it>should query existing elements</playwright-it>
        [Retry]
        public async Task ShouldQueryExistingElements()
        {
            await Page.SetContentAsync("<div>A</div><br/><div>B</div>");
            var elements = await Page.QuerySelectorAllAsync("div");
            Assert.Equal(2, elements.Length);
            var tasks = elements.Select(element => Page.EvaluateAsync<string>("e => e.textContent", element));
            Assert.Equal(new[] { "A", "B" }, await Task.WhenAll(tasks));
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$</playwright-describe>
        ///<playwright-it>should return empty array if nothing is found</playwright-it>
        [Retry]
        public async Task ShouldReturnEmptyArrayIfNothingIsFound()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var elements = await Page.QuerySelectorAllAsync("div");
            Assert.Empty(elements);
        }
    }
}
