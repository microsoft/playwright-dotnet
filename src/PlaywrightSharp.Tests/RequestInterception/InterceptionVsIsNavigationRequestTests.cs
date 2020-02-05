using PlaywrightSharp.Tests.BaseTests;
using Xunit.Abstractions;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>Interception vs isNavigationRequest</playwright-describe>
    public class InterceptionVsIsNavigationRequestTests : PlaywrightSharpPageBaseTest
    {
        internal InterceptionVsIsNavigationRequestTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Interception vs isNavigationRequest</playwright-describe>
        ///<playwright-it>should work with request interception</playwright-it>
        [Fact]
        public async Task ShouldWorkWithRequestInterception()
        {
            var requests = new Dictionary<string, IRequest>();
            Page.Request += async (sender, e) =>
            {
                requests.Add(e.Request.Url.Split('/').Last(), e.Request);
                await e.Request.ContinueAsync();
            };
            await Page.SetRequestInterceptionAsync(true);
            Server.SetRedirect("/rrredirect", "/frames/one-frame.html");
            await Page.GoToAsync(TestConstants.ServerUrl + "/rrredirect");
            Assert.True(requests["rrredirect"].IsNavigationRequest);
            Assert.True(requests["one-frame.html"].IsNavigationRequest);
            Assert.True(requests["frame.html"].IsNavigationRequest);
            Assert.False(requests["script.js"].IsNavigationRequest);
            Assert.False(requests["style.css"].IsNavigationRequest);
        }
    }
}
