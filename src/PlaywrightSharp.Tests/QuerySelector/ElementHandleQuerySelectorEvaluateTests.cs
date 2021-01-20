using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.$eval</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleEvalOnSelectorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleEvalOnSelectorTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("queryselector.spec.js", "ElementHandle.$eval", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"retweets\">10</div></div></body></html>");
            var tweet = await Page.QuerySelectorAsync(".tweet");
            string content = await tweet.EvalOnSelectorAsync<string>(".like", "node => node.innerText");
            Assert.Equal("100", content);
        }

        [PlaywrightTest("queryselector.spec.js", "ElementHandle.$eval", "should retrieve content from subtree")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRetrieveContentFromSubtree()
        {
            string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a-child-div</div></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            string content = await elementHandle.EvalOnSelectorAsync<string>(".a", "node => node.innerText");
            Assert.Equal("a-child-div", content);
        }

        [PlaywrightTest("queryselector.spec.js", "ElementHandle.$eval", "should throw in case of missing selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowInCaseOfMissingSelector()
        {
            string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => elementHandle.EvalOnSelectorAsync(".a", "node => node.innerText"));
            Assert.Contains("failed to find element matching selector \".a\"", exception.Message);
        }
    }
}
