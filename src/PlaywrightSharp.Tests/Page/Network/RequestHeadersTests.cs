using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Request.headers</playwright-describe>
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
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
                response.Request.Headers["user-agent"]);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.headers</playwright-describe>
        ///<playwright-it>should get the same headers as the server</playwright-it>
        [Fact(Skip = "We don't need to test this")]
        public void ShouldGetTheSameHeadersAsTheServer()
        {
        }
    }
}
