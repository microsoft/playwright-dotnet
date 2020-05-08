using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Request.isNavigationRequest</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class RequestIsNavigationRequestTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public RequestIsNavigationRequestTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.isNavigationRequest</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var requests = new Dictionary<string, IRequest>();
            Page.Request += (sender, e) => requests[e.Request.Url.Split('/').Last()] = e.Request;
            Server.SetRedirect("/rrredirect", "/frames/one-frame.html");
            await Page.GoToAsync(TestConstants.ServerUrl + "/rrredirect");
            Assert.True(requests["rrredirect"].IsNavigationRequest);
            Assert.True(requests["one-frame.html"].IsNavigationRequest);
            Assert.True(requests["frame.html"].IsNavigationRequest);
            Assert.False(requests["script.js"].IsNavigationRequest);
            Assert.False(requests["style.css"].IsNavigationRequest);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.isNavigationRequest</playwright-describe>
        ///<playwright-it>should work when navigating to image</playwright-it>
        [Fact]
        public async Task ShouldWorkWhenNavigatingToImage()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.ServerUrl + "/pptr.png");
            Assert.True(requests[0].IsNavigationRequest);
        }
    }
}
