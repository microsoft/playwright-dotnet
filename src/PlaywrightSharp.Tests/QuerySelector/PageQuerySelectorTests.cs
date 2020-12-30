using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageQuerySelectorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageQuerySelectorTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should query existing element with css selector</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElementWithCssSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("css=section");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should query existing element with text selector</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElementWithTextSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("text=\"test\"");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should query existing element with xpath selector</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElementWithXpathSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("xpath=/html/body/section");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should return null for non-existing element</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullForNonExistingElement()
        {
            var element = await Page.QuerySelectorAsync("non-existing-element");
            Assert.Null(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should auto-detect xpath selector</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectXpathSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("//html/body/section");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should auto-detect xpath selector with starting parenthesis</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectXpathSelectorWithStartingParenthesis()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("(//section)[1]");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should auto-detect text selector</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectTextSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("\"test\"");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should auto-detect css selector</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectCssSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("section");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should support >> syntax</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportDoubleGreaterThanSyntax()
        {
            await Page.SetContentAsync("<section><div>test</div></section>");
            var element = await Page.QuerySelectorAsync("css=section >> css=div");
            Assert.NotNull(element);
        }
    }
}
