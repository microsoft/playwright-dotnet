using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$</playwright-describe>
    public class PageQuerySelectorTests : PlaywrightSharpPageBaseTest
    {
        internal PageQuerySelectorTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should query existing element with css selector</playwright-it>
        [Fact]
        public async Task ShouldQueryExistingElementWithCssSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("css=section");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should query existing element with zs selector</playwright-it>
        [Fact]
        public async Task ShouldQueryExistingElementWithZsSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("zs=\"test\"");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should query existing element with xpath selector</playwright-it>
        [Fact]
        public async Task ShouldQueryExistingElementWithXpathSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("xpath=/html/body/section");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should return null for non-existing element</playwright-it>
        [Fact]
        public async Task ShouldReturnNullForNonExistingElement()
        {
            var element = await Page.QuerySelectorAsync("non-existing-element");
            Assert.Null(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should auto-detect xpath selector</playwright-it>
        [Fact]
        public async Task ShouldAutoDetectXpathSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("//html/body/section");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should auto-detect text selector</playwright-it>
        [Fact]
        public async Task ShouldAutoDetectTextSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("\"test\"");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should auto-detect css selector</playwright-it>
        [Fact]
        public async Task ShouldAutoDetectCssSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("section");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should support >> syntax</playwright-it>
        [Fact]
        public async Task ShouldSupportDoubleGreaterThanSyntax()
        {
            await Page.SetContentAsync("<section><div>test</div></section>");
            var element = await Page.QuerySelectorAsync("css=section >> css=div");
            Assert.NotNull(element);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should respect waitFor visibility</playwright-it>
        [Fact]
        public async Task ShouldRespectWaitForVisibility()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            Assert.NotNull(await Page.WaitForSelectorAsync("css=section", new WaitForSelectorOptions { WaitFor = WaitForOption.Visible }));
            Assert.NotNull(await Page.WaitForSelectorAsync("css=section", new WaitForSelectorOptions { WaitFor = WaitForOption.Any }));
            Assert.NotNull(await Page.WaitForSelectorAsync("css=section"));

            await Page.SetContentAsync("<section id=\"testAttribute\" style=\"display: none\">43543</section>");
            Assert.NotNull(await Page.WaitForSelectorAsync("css=section", new WaitForSelectorOptions { WaitFor = WaitForOption.Hidden }));
            Assert.NotNull(await Page.WaitForSelectorAsync("css=section", new WaitForSelectorOptions { WaitFor = WaitForOption.Any }));
            Assert.NotNull(await Page.WaitForSelectorAsync("css=section"));
        }
    }
}
