using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Testing.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class SelectorMiscTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public SelectorMiscTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("selectors-misc.spec.ts", "should work for open shadow roots")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForOpenShadowRoots()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/deep-shadow.html");
            Assert.Equal("Hello from root2", await Page.EvalOnSelectorAsync<string>("id=target", "e => e.textContent"));
            Assert.Equal("Hello from root1", await Page.EvalOnSelectorAsync<string>("data-testid=foo", "e => e.textContent"));
            Assert.Equal(3, await Page.EvalOnSelectorAllAsync<int>("data-testid=foo", "els => els.length"));
            Assert.Null(await Page.QuerySelectorAsync("id:light=target"));
            Assert.Null(await Page.QuerySelectorAsync("data-testid:light=foo"));
            Assert.Empty(await Page.QuerySelectorAllAsync("data-testid:light=foo"));
        }
    }
}
