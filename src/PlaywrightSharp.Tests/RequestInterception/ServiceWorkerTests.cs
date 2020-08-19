using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>service worker</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ServiceWorkerTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ServiceWorkerTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>service worker</playwright-describe>
        ///<playwright-it>should intercept after a service worker</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldInterceptAfterAServiceWorker()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/serviceworkers/fetchdummy/sw.html");
            await Page.EvaluateAsync("() => window.activationPromise");

            string swResponse = await Page.EvaluateAsync<string>("() => fetchDummy('foo')");
            Assert.Equal("responseFromServiceWorker:foo", swResponse);

            await Page.RouteAsync("**/foo", (route, _) =>
            {
                int slash = route.Request.Url.LastIndexOf("/");
                string name = route.Request.Url.Substring(slash + 1);

                route.FulfillAsync(new RouteFilfillResponse
                {
                    Status = HttpStatusCode.OK,
                    ContentType = "text/css",
                    Body = "responseFromInterception:" + name,
                });
            });

            string swResponse2 = await Page.EvaluateAsync<string>("() => fetchDummy('foo')");
            Assert.Equal("responseFromServiceWorker:foo", swResponse2);

            string nonInterceptedResponse = await Page.EvaluateAsync<string>("() => fetchDummy('passthrough')");
            Assert.Equal("FAILURE: Not Found", nonInterceptedResponse);
        }
    }
}
