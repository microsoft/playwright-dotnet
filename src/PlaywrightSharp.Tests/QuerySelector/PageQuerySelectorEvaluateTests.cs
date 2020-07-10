using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$eval</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class PageQuerySelectorEvaluateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageQuerySelectorEvaluateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with css selector</playwright-it>
        [Retry]
        public async Task ShouldWorkWithCssSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("css=section", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with id selector</playwright-it>
        [Retry]
        public async Task ShouldWorkWithIdSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("id=testAttribute", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with data-test selector</playwright-it>
        [Retry]
        public async Task ShouldWorkWithDataTestSelector()
        {
            await Page.SetContentAsync("<section data-test=foo id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("data-test=foo", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with data-testid selector</playwright-it>
        [Retry]
        public async Task ShouldWorkWithDataTestidSelector()
        {
            await Page.SetContentAsync("<section data-testid=foo id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("data-testid=foo", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with data-test-id selector</playwright-it>
        [Retry]
        public async Task ShouldWorkWithDataTestIdSelector()
        {
            await Page.SetContentAsync("<section data-test-id=foo id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("data-test-id=foo", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with zs selector</playwright-it>
        [Retry]
        public async Task ShouldWorkWithZsSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("zs=\"43543\"", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with xpath selector</playwright-it>
        [Retry]
        public async Task ShouldWorkWithXpathSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("xpath=/html/body/section", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with text selector</playwright-it>
        [Retry]
        public async Task ShouldWorkWithTextSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("text=43543", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should auto-detect css selector</playwright-it>
        [Retry]
        public async Task ShouldAutoDetectCssSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("section", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should auto-detect css selector with attributes</playwright-it>
        [Retry]
        public async Task ShouldAutoDetectCssSelectorWithAttributes()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("section[id=\"testAttribute\"]", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should accept arguments</playwright-it>
        [Retry]
        public async Task ShouldAcceptArguments()
        {
            await Page.SetContentAsync("<section>hello</section>");
            string text = await Page.QuerySelectorEvaluateAsync<string>("section", "(e, suffix) => e.textContent + suffix", " world!");
            Assert.Equal("hello world!", text);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should accept ElementHandles as arguments</playwright-it>
        [Retry]
        public async Task ShouldAcceptElementHandlesAsArguments()
        {
            await Page.SetContentAsync("<section>hello</section><div> world</div>");
            var divHandle = await Page.QuerySelectorAsync("div");
            string text = await Page.QuerySelectorEvaluateAsync<string>("section", "(e, div) => e.textContent + div.textContent", divHandle);
            Assert.Equal("hello world", text);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should throw error if no element is found</playwright-it>
        [Retry]
        public async Task ShouldThrowErrorIfNoElementIsFound()
        {
            var exception = await Assert.ThrowsAsync<SelectorException>(()
                => Page.QuerySelectorEvaluateAsync("section", "e => e.id"));
            Assert.Equal("Failed to find element matching selector", exception.Message);
            Assert.Equal("section", exception.Selector);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should support >> syntax</playwright-it>
        [Retry]
        public async Task ShouldSupportDoubleGreaterThanSyntax()
        {
            await Page.SetContentAsync("<section><div>hello</div></section>");
            string text = await Page.QuerySelectorEvaluateAsync<string>("css=section >> css=div", "(e, suffix) => e.textContent + suffix", " world!");
            Assert.Equal("hello world!", text);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should support >> syntax with different engines</playwright-it>
        [Retry]
        public async Task ShouldSupportDoubleGreaterThanSyntaxWithDifferentEngines()
        {
            await Page.SetContentAsync("<section><div><span>hello</span></div></section>");
            string text = await Page.QuerySelectorEvaluateAsync<string>("xpath=/html/body/section >> css=div >> zs=\"hello\"", "(e, suffix) => e.textContent + suffix", " world!");
            Assert.Equal("hello world!", text);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should support spaces with >> syntax</playwright-it>
        [Retry]
        public async Task ShouldSupportSpacesWithDoubleGreaterThanSyntax()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/deep-shadow.html");
            string text = await Page.QuerySelectorEvaluateAsync<string>(" css = div >>css=div>>css   = span  ", "e => e.textContent");
            Assert.Equal("Hello from root2", text);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should enter shadow roots with >> syntax</playwright-it>
        [Retry]
        public async Task ShouldEnterShadowRootsWithDoubleGreaterThanSyntax()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/deep-shadow.html");
            string text1 = await Page.QuerySelectorEvaluateAsync<string>("css=div >> css=span", "e => e.textContent");
            Assert.Equal("Hello from root1", text1);
            string text2 = await Page.QuerySelectorEvaluateAsync<string>("css=div >> css=*:nth-child(2) >> css=span", "e => e.textContent");
            Assert.Equal("Hello from root2", text2);
            var nonExisting = await Page.QuerySelectorAsync("css=div div >> css=span");
            Assert.Null(nonExisting);
            string text3 = await Page.QuerySelectorEvaluateAsync<string>("css=section div >> css=span", "e => e.textContent");
            Assert.Equal("Hello from root1", text3);
            string text4 = await Page.QuerySelectorEvaluateAsync<string>("xpath=/html/body/section/div >> css=div >> css=span", "e => e.textContent");
            Assert.Equal("Hello from root2", text4);
            string text5 = await Page.QuerySelectorEvaluateAsync<string>("zs=section div >> css=div >> css=span", "e => e.textContent");
            Assert.Equal("Hello from root2", text5);
        }
    }
}
