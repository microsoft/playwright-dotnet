using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.url</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageUrlTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageUrlTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.url</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            Assert.Equal("about:blank", Page.Url);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, Page.Url);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.url</playwright-describe>
        ///<playwright-it>should include hashes</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeHashes()
        {
            await Page.GoToAsync(TestConstants.EmptyPage + "#hash");
            Assert.Equal(TestConstants.EmptyPage + "#hash", Page.Url);
            await Page.EvaluateAsync("() => window.location.hash = 'dynamic'");
            Assert.Equal(TestConstants.EmptyPage + "#dynamic", Page.Url);
        }
    }
}
