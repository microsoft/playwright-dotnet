using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$$ xpath</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageQuerySelectorAllXpathTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageQuerySelectorAllXpathTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("queryselector.spec.js", "Page.$$ xpath", "should query existing element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElement()
        {
            await Page.SetContentAsync("<section>test</section>");
            var elements = await Page.QuerySelectorAllAsync("xpath=/html/body/section");
            Assert.NotNull(elements.FirstOrDefault());
            Assert.Single(elements);
        }

        [PlaywrightTest("queryselector.spec.js", "Page.$$ xpath", "should return empty array for non-existing element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEmptyArrayForNonExistingElement()
        {
            var elements = await Page.QuerySelectorAllAsync("//html/body/non-existing-element");
            Assert.Empty(elements);
        }

        [PlaywrightTest("queryselector.spec.js", "Page.$$ xpath", "should return multiple elements")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnMultipleElements()
        {
            await Page.SetContentAsync("<div></div><div></div>");
            var elements = await Page.QuerySelectorAllAsync("xpath=/html/body/div");
            Assert.Equal(2, elements.Count());
        }
    }
}
