using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class SelectorMiscTests : PageTestEx
    {
        [PlaywrightTest("selectors-misc.spec.ts", "should work for open shadow roots")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForOpenShadowRoots()
        {
            await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
            Assert.AreEqual("Hello from root2", await Page.EvalOnSelectorAsync<string>("id=target", "e => e.textContent"));
            Assert.AreEqual("Hello from root1", await Page.EvalOnSelectorAsync<string>("data-testid=foo", "e => e.textContent"));
            Assert.AreEqual(3, await Page.EvalOnSelectorAllAsync<int>("data-testid=foo", "els => els.length"));
            Assert.Null(await Page.QuerySelectorAsync("id:light=target"));
            Assert.Null(await Page.QuerySelectorAsync("data-testid:light=foo"));
            Assert.IsEmpty(await Page.QuerySelectorAllAsync("data-testid:light=foo"));
        }
    }
}
