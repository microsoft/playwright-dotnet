using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>ignorehttpserrors.spec.js</playwright-file>
    ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
    public class IgnoreHttpsErrorsTests : PlaywrightSharpBrowserContextBaseTest
    {
        /// <inheritdoc/>
        public IgnoreHttpsErrorsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>ignorehttpserrors.spec.js</playwright-file>
        ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var page = await NewPageAsync(new BrowserContextOptions { IgnoreHTTPSErrors = true });
            var requestTask = HttpsServer.WaitForRequest(
                "/empty.html",
                request => request.HttpContext.Features.Get<ITlsHandshakeFeature>().Protocol);
            var responseTask = page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html");

            await Task.WhenAll(
                requestTask,
                responseTask);

            var response = responseTask.Result;
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>ignorehttpserrors.spec.js</playwright-file>
        ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
        ///<playwright-it>should work with mixed content</playwright-it>
        [Fact]
        public async Task ShouldWorkWithMixedContent()
        {
            HttpsServer.SetRoute("/mixedcontent.html", async (context) =>
            {
                await context.Response.WriteAsync($"<iframe src='{TestConstants.EmptyPage}'></iframe>");
            });
            var page = await NewPageAsync(new BrowserContextOptions { IgnoreHTTPSErrors = true });
            await page.GoToAsync(TestConstants.HttpsPrefix + "/mixedcontent.html", new GoToOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Load }
            });
            Assert.Equal(2, page.Frames.Length);
            Assert.Equal(3, await page.MainFrame.EvaluateAsync<int>("1 + 2"));
            Assert.Equal(5, await page.FirstChildFrame().EvaluateAsync<int>("2 + 3"));
        }
    }
}
