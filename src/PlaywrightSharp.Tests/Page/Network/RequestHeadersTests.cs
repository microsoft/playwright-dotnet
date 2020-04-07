using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Request.headers</playwright-describe>
    public class RequestHeadersTests : PlaywrightSharpPageBaseTest
    {
        internal RequestHeadersTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.headers</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Contains(
                TestConstants.Product switch
                {
                    TestConstants.ChromiumProduct => "Chromium",
                    TestConstants.FirefoxProduct => "Firefox",
                    TestConstants.WebkitProduct => "WebKit",
                    _ => "None"
                },
                response.Request.Headers["User-Agent"]);
        }
    }
}
