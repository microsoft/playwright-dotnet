using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.title</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageTitleTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageTitleTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page.spec.js", "Page.title", "should return the page title")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnThePageTitle()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/title.html");
            Assert.Equal("Woof-Woof", await Page.GetTitleAsync());
        }
    }
}
