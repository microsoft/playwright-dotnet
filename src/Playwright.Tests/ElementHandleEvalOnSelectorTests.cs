using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class ElementHandleEvalOnSelectorTests : PageTestEx
    {
        [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should work for all")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForAll()
        {
            await Page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"like\">10</div></div></body></html>");
            var tweet = await Page.QuerySelectorAsync(".tweet");
            string[] content = await tweet.EvalOnSelectorAllAsync<string[]>(".like", "nodes => nodes.map(n => n.innerText)");
            Assert.AreEqual(new[] { "100", "10" }, content);
        }

        [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should retrieve content from subtree for all")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRetrieveContentFromSubtreeForAll()
        {
            string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a1-child-div</div><div class=\"a\">a2-child-div</div></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            string[] content = await elementHandle.EvalOnSelectorAllAsync<string[]>(".a", "nodes => nodes.map(n => n.innerText)");
            Assert.AreEqual(new[] { "a1-child-div", "a2-child-div" }, content);
        }

        [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should not throw in case of missing selector for all")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotThrowInCaseOfMissingSelectorForAll()
        {
            string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            int nodesLength = await elementHandle.EvalOnSelectorAllAsync<int>(".a", "nodes => nodes.length");
            Assert.AreEqual(0, nodesLength);
        }

        [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"retweets\">10</div></div></body></html>");
            var tweet = await Page.QuerySelectorAsync(".tweet");
            string content = await tweet.EvalOnSelectorAsync<string>(".like", "node => node.innerText");
            Assert.AreEqual("100", content);
        }

        [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should retrieve content from subtree")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRetrieveContentFromSubtree()
        {
            string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a-child-div</div></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            string content = await elementHandle.EvalOnSelectorAsync<string>(".a", "node => node.innerText");
            Assert.AreEqual("a-child-div", content);
        }

        [PlaywrightTest("elementhandle-eval-on-selector.spec.ts", "should throw in case of missing selector")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowInCaseOfMissingSelector()
        {
            string htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            var exception = Assert.ThrowsAsync<PlaywrightException>(async () => await elementHandle.EvalOnSelectorAsync(".a", "node => node.innerText"));
            StringAssert.Contains("failed to find element matching selector \".a\"", exception.Message);
        }
    }
}
