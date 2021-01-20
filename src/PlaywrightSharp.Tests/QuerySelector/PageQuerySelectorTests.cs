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

        [PlaywrightTest("queryselector.spec.js", "Page.$", "should query existing element with css selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElementWithCssSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("css=section");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.js", "Page.$", "should query existing element with text selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElementWithTextSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("text=\"test\"");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.js", "Page.$", "should query existing element with xpath selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElementWithXpathSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("xpath=/html/body/section");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.js", "Page.$", "should return null for non-existing element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullForNonExistingElement()
        {
            var element = await Page.QuerySelectorAsync("non-existing-element");
            Assert.Null(element);
        }

        [PlaywrightTest("queryselector.spec.js", "Page.$", "should auto-detect xpath selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectXpathSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("//html/body/section");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.js", "Page.$", "should auto-detect xpath selector with starting parenthesis")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectXpathSelectorWithStartingParenthesis()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("(//section)[1]");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.js", "Page.$", "should auto-detect text selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectTextSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("\"test\"");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.js", "Page.$", "should auto-detect css selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectCssSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("section");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.js", "Page.$", "should support >> syntax")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportDoubleGreaterThanSyntax()
        {
            await Page.SetContentAsync("<section><div>test</div></section>");
            var element = await Page.QuerySelectorAsync("css=section >> css=div");
            Assert.NotNull(element);
        }
    }
}
