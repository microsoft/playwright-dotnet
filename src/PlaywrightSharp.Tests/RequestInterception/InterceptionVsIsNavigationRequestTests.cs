using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>Interception vs isNavigationRequest</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class InterceptionVsIsNavigationRequestTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public InterceptionVsIsNavigationRequestTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("interception.spec.js", "Interception vs isNavigationRequest", "should work with request interception")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRequestInterception()
        {
            var requests = new Dictionary<string, IRequest>();
            await Page.RouteAsync("**/*", (route, request) =>
            {
                requests.Add(request.Url.Split('/').Last(), request);
                route.ContinueAsync();
            });

            Server.SetRedirect("/rrredirect", "/frames/one-frame.html");
            await Page.GoToAsync(TestConstants.ServerUrl + "/rrredirect");
            Assert.True(requests["rrredirect"].IsNavigationRequest);
            Assert.True(requests["frame.html"].IsNavigationRequest);
            Assert.False(requests["script.js"].IsNavigationRequest);
            Assert.False(requests["style.css"].IsNavigationRequest);
        }
    }
}
