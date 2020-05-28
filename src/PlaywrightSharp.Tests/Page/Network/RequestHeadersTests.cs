using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Request.headers</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class RequestHeadersTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public RequestHeadersTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.headers</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Contains(
                TestConstants.Product switch
                {
                    TestConstants.ChromiumProduct => "Chrome",
                    TestConstants.FirefoxProduct => "Firefox",
                    TestConstants.WebkitProduct => "WebKit",
                    _ => "None"
                },
                response.Request.Headers["User-Agent"]);
        }
    }
}
