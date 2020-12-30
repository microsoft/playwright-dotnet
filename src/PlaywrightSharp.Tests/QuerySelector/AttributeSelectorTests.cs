using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>attribute selector</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class AttributeSelectorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public AttributeSelectorTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>css selector</playwright-describe>
        ///<playwright-it>should work for open shadow roots</playwright-it>
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
