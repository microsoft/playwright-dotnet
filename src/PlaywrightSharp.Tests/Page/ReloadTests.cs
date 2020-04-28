using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>Page.reload</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ReloadTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ReloadTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.reload</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync("() => window._foo = 10");
            await Page.ReloadAsync();
            Assert.Null(await Page.EvaluateAsync("() => window._foo"));
        }
    }
}
