using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Request.frame</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class RequestFrameTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public RequestFrameTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.frame</playwright-describe>
        ///<playwright-it>should work for main frame navigation request</playwright-it>
        [Fact]
        public async Task ShouldWorkForMainFrameNavigationRequests()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Single(requests);
            Assert.Equal(Page.MainFrame, requests[0].Frame);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.frame</playwright-describe>
        ///<playwright-it>should work for subframe navigation request</playwright-it>
        [Fact]
        public async Task ShouldWorkForSubframeNavigationRequest()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);

            await Page.GoToAsync(TestConstants.EmptyPage);

            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            Assert.Equal(2, requests.Count);
            Assert.Equal(Page.FirstChildFrame(), requests[1].Frame);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Request.frame</playwright-describe>
        ///<playwright-it>should work for fetch requests</playwright-it>
        [Fact]
        public async Task ShouldWorkForFetchRequests()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.EvaluateAsync("fetch('/digits/1.png')");
            Assert.Single(requests.Where(r => !r.Url.Contains("favicon")));
            Assert.Equal(Page.MainFrame, requests[0].Frame);
        }
    }
}
