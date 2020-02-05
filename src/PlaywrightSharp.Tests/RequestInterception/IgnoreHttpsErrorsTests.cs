using PlaywrightSharp.Tests.BaseTests;
using Xunit.Abstractions;
using Xunit;
using System.Threading.Tasks;
using System.Net;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
    public class IgnoreHttpsErrorsTests : PlaywrightSharpPageBaseTest
    {
        internal IgnoreHttpsErrorsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
        ///<playwright-it>should work with request interception</playwright-it>
        [Fact]
        public async Task ShouldWorkWithRequestInterception()
        {
            var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                IgnoreHTTPSErrors = true
            });
            var page = await context.NewPageAsync();

            await page.SetRequestInterceptionAsync(true);
            page.Request += async (sender, e) => await e.Request.ContinueAsync();
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }
    }
}
