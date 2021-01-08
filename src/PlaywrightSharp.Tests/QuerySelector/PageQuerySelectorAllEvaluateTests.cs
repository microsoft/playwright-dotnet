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
    public class PageEvalOnSelectorAllTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEvalOnSelectorAllTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should work with css selector</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCssSelector()
        {
            await Page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
            int divsCount = await Page.EvalOnSelectorAllAsync<int>("css=div", "divs => divs.length");
            Assert.Equal(3, divsCount);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should work with text selector</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithTextSelector()
        {
            await Page.SetContentAsync("<div>hello</div><div>beautiful</div><div>beautiful</div><div>world!</div>");
            int divsCount = await Page.EvalOnSelectorAllAsync<int>("text=beautiful", "divs => divs.length");
            Assert.Equal(2, divsCount);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should work with xpath selector</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithXpathSelector()
        {
            await Page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
            int divsCount = await Page.EvalOnSelectorAllAsync<int>("xpath=/html/body/div", "divs => divs.length");
            Assert.Equal(3, divsCount);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should auto-detect css selector</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectCssSelector()
        {
            await Page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
            int divsCount = await Page.EvalOnSelectorAllAsync<int>("div", "divs => divs.length");
            Assert.Equal(3, divsCount);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should support >> syntax</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportDoubleGreaterThanSyntax()
        {
            await Page.SetContentAsync("<div><span>hello</span></div><div>beautiful</div><div><span>wo</span><span>rld!</span></div><span>Not this one</span>");
            int spansCount = await Page.EvalOnSelectorAllAsync<int>("css=div >> css=span", "spans => spans.length");
            Assert.Equal(3, spansCount);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should should support * capture</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportStarCapture()
        {
            await Page.SetContentAsync("<section><div><span>a</span></div></section><section><div><span>b</span></div></section>");
            Assert.Equal(1, await Page.EvalOnSelectorAllAsync<int>("*css=div >> \"b\"", "divs => divs.length"));
            Assert.Equal(1, await Page.EvalOnSelectorAllAsync<int>("section >> *css=div >> \"b\"", "divs => divs.length"));
            Assert.Equal(4, await Page.EvalOnSelectorAllAsync<int>("section >> *", "divs => divs.length"));

            await Page.SetContentAsync("<section><div><span>a</span><span>a</span></div></section>");
            Assert.Equal(1, await Page.EvalOnSelectorAllAsync<int>("*css=div >> \"a\"", "divs => divs.length"));
            Assert.Equal(1, await Page.EvalOnSelectorAllAsync<int>("section >> *css=div >> \"a\"", "divs => divs.length"));

            await Page.SetContentAsync("<div><span>a</span></div><div><span>a</span></div><section><div><span>a</span></div></section>");
            Assert.Equal(3, await Page.EvalOnSelectorAllAsync<int>("*css=div >> \"a\"", "divs => divs.length"));
            Assert.Equal(1, await Page.EvalOnSelectorAllAsync<int>("section >> *css=div >> \"a\"", "divs => divs.length"));
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should support * capture when multiple paths match</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportStarCaptureWhenMultiplePathsMatch()
        {
            await Page.SetContentAsync("<div><div><span></span></div></div><div></div>");
            Assert.Equal(2, await Page.EvalOnSelectorAllAsync<int>("*css=div >> span", "els => els.length"));

            await Page.SetContentAsync("<div><div><span></span></div><span></span><span></span></div><div></div>");
            Assert.Equal(2, await Page.EvalOnSelectorAllAsync<int>("*css=div >> span", "els => els.length"));
        }
    }
}
