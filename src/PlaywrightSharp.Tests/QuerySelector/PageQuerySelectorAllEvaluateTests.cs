using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$$eval</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class PageQuerySelectorAllEvaluateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageQuerySelectorAllEvaluateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should work with css selector</playwright-it>
        [Retry]
        public async Task ShouldWorkWithCssSelector()
        {
            await Page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
            int divsCount = await Page.QuerySelectorAllEvaluateAsync<int>("css=div", "divs => divs.length");
            Assert.Equal(3, divsCount);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should work with zs selector</playwright-it>
        [Retry]
        public async Task ShouldWorkWithZsSelector()
        {
            await Page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
            int divsCount = await Page.QuerySelectorAllEvaluateAsync<int>("zs=div", "divs => divs.length");
            Assert.Equal(3, divsCount);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should work with xpath selector</playwright-it>
        [Retry]
        public async Task ShouldWorkWithXpathSelector()
        {
            await Page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
            int divsCount = await Page.QuerySelectorAllEvaluateAsync<int>("xpath=/html/body/div", "divs => divs.length");
            Assert.Equal(3, divsCount);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should auto-detect css selector</playwright-it>
        [Retry]
        public async Task ShouldAutoDetectCssSelector()
        {
            await Page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
            int divsCount = await Page.QuerySelectorAllEvaluateAsync<int>("div", "divs => divs.length");
            Assert.Equal(3, divsCount);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should support >> syntax</playwright-it>
        [Retry]
        public async Task ShouldSupportDoubleGreaterThanSyntax()
        {
            await Page.SetContentAsync("<div><span>hello</span></div><div>beautiful</div><div><span>wo</span><span>rld!</span></div><span>Not this one</span>");
            int spansCount = await Page.QuerySelectorAllEvaluateAsync<int>("css=div >> css=span", "spans => spans.length");
            Assert.Equal(3, spansCount);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should enter shadow roots with >> syntax</playwright-it>
        [Retry]
        public async Task ShouldEnterShadowRootsWithDoubleGreaterThanSyntax()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/deep-shadow.html");
            int spansCount = await Page.QuerySelectorAllEvaluateAsync<int>("css=div >> css=div >> css=span", "spans => spans.length");
            Assert.Equal(2, spansCount);
        }
    }
}
