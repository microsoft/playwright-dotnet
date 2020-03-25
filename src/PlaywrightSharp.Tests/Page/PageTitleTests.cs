using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.title</playwright-describe>
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class PageTitleTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageTitleTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.title</playwright-describe>
        ///<playwright-it>should return the page title</playwright-it>
        [Fact]
        public async Task ShouldReturnThePageTitle()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/title.html");
            Assert.Equal("Woof-Woof", await Page.GetTitleAsync());
        }
    }
}
