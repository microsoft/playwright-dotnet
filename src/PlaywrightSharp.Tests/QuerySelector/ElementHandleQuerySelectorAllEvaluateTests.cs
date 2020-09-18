using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.$$eval</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleEvalOnSelectorAllTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleEvalOnSelectorAllTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$$eval</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"like\">10</div></div></body></html>");
            var tweet = await Page.QuerySelectorAsync(".tweet");
            string[] content = await tweet.EvalOnSelectorAllAsync<string[]>(".like", "nodes => nodes.map(n => n.innerText)");
            Assert.Equal(new[] { "100", "10" }, content);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$$eval</playwright-describe>
        ///<playwright-it>should retrieve content from subtree</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRetrieveContentFromSubtree()
        {
            string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a1-child-div</div><div class=\"a\">a2-child-div</div></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            string[] content = await elementHandle.EvalOnSelectorAllAsync<string[]>(".a", "nodes => nodes.map(n => n.innerText)");
            Assert.Equal(new[] { "a1-child-div", "a2-child-div" }, content);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$$eval</playwright-describe>
        ///<playwright-it>should not throw in case of missing selector</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotThrowInCaseOfMissingSelector()
        {
            string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            int nodesLength = await elementHandle.EvalOnSelectorAllAsync<int>(".a", "nodes => nodes.length");
            Assert.Equal(0, nodesLength);
        }
    }
}
