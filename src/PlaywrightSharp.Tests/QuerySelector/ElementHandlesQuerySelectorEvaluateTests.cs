using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.$eval</playwright-describe>
    public class ElementHandlesQuerySelectorEvaluateTests : PlaywrightSharpPageBaseTest
    {
        internal ElementHandlesQuerySelectorEvaluateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$eval</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<html><body><div class=\"tweet\"><div class=\"like\">100</div><div class=\"retweets\">10</div></div></body></html>");
            var tweet = await Page.QuerySelectorAsync(".tweet");
            var content = await tweet.QuerySelectorEvaluateAsync<string>(".like", "node => node.innerText");
            Assert.Equal("100", content);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$eval</playwright-describe>
        ///<playwright-it>should retrieve content from subtree</playwright-it>
        [Fact]
        public async Task ShouldRetrieveContentFromSubtree()
        {
            var htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"><div class=\"a\">a-child-div</div></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            var content = await elementHandle.QuerySelectorEvaluateAsync<string>(".a", "node => node.innerText");
            expect(content).toBe("a-child-div");
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$eval</playwright-describe>
        ///<playwright-it>should throw in case of missing selector</playwright-it>
        [Fact]
        public async Task ShouldThrowInCaseOfMissingSelector()
        {
            var htmlContent = "<div class=\"a\">not-a-child-div</div><div id=\"myId\"></div>";
            await Page.SetContentAsync(htmlContent);
            var elementHandle = await Page.QuerySelectorAsync("#myId");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => elementHandle.QuerySelectorEvaluateAsync(".a", "node => node.innerText"));
            Assert.Equal("Error: failed to find element matching selector \".a\"", exception.Message);
        }
    }

        }
