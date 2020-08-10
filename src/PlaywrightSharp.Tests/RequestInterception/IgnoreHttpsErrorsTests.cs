using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class IgnoreHttpsErrorsTests : PlaywrightSharpBaseTest
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
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                IgnoreHTTPSErrors = true
            });

            var page = await context.NewPageAsync();

            await page.SetRequestInterceptionAsync(true);
            page.Request += async (sender, e) => await e.Request.ContinueAsync();
            var response = await page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }
    }
}
