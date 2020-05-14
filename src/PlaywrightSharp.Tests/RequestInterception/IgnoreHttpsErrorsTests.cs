using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class IgnoreHttpsErrorsTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public IgnoreHttpsErrorsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
        ///<playwright-it>should work with request interception</playwright-it>
        [Fact]
        public async Task ShouldWorkWithRequestInterception()
        {
            var page = await NewPageAsync(new BrowserContextOptions
            {
                IgnoreHTTPSErrors = true
            });

            await page.SetRequestInterceptionAsync(true);
            page.Request += async (sender, e) => await e.Request.ContinueAsync();
            var response = await page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }
    }
}
