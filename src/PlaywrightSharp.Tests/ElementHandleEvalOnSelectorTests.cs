using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleEvalOnSelectorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleEvalOnSelectorTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should work for all")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForAll()
        {
            await Page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"like\">10</div></div></body></html>");
            var tweet = await Page.QuerySelectorAsync(".tweet");
            string[] content = await tweet.EvalOnSelectorAllAsync<string[]>(".like", "nodes => nodes.map(n => n.innerText)");
            Assert.Equal(new[] { "100", "10" }, content);
        }

        [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should retrieve content from subtree for all")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRetrieveContentFromSubtreeForAll()
        {
            string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a1-child-div</div><div class=\"a\">a2-child-div</div></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            string[] content = await elementHandle.EvalOnSelectorAllAsync<string[]>(".a", "nodes => nodes.map(n => n.innerText)");
            Assert.Equal(new[] { "a1-child-div", "a2-child-div" }, content);
        }

        [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should not throw in case of missing selector for all")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotThrowInCaseOfMissingSelectorForAll()
        {
            string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            int nodesLength = await elementHandle.EvalOnSelectorAllAsync<int>(".a", "nodes => nodes.length");
            Assert.Equal(0, nodesLength);
        }

        [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"retweets\">10</div></div></body></html>");
            var tweet = await Page.QuerySelectorAsync(".tweet");
            string content = await tweet.EvalOnSelectorAsync<string>(".like", "node => node.innerText");
            Assert.Equal("100", content);
        }

        [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should retrieve content from subtree")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRetrieveContentFromSubtree()
        {
            string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a-child-div</div></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            string content = await elementHandle.EvalOnSelectorAsync<string>(".a", "node => node.innerText");
            Assert.Equal("a-child-div", content);
        }

        [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should throw in case of missing selector")]
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
