using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.Request</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventsRequestTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventsRequestTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Request</playwright-describe>
        ///<playwright-it>should fire for navigation requests</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFireForNavigationRequests()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Single(requests);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Request</playwright-describe>
        ///<playwright-it>should fire for iframes</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFireForIframes()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            Assert.Equal(2, requests.Count);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Request</playwright-describe>
        ///<playwright-it>should fire for fetches</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFireForFetches()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync("fetch('/empty.html')");
            Assert.Equal(2, requests.Count);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Request</playwright-describe>
        ///<playwright-it>should report requests and responses handled by service worker</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReportRequestsAndResponsesHandledByServiceWorker()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/serviceworkers/fetchdummy/sw.html");
            await Page.EvaluateAsync("() => window.activationPromise");

            var (request, swResponse) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Request),
                Page.EvaluateAsync<string>("() => fetchDummy('foo')"));

            Assert.Equal("responseFromServiceWorker:foo", swResponse);
            Assert.Equal(TestConstants.ServerUrl + "/serviceworkers/fetchdummy/foo", request.Request.Url);
            var response = await request.Request.GetResponseAsync();
            Assert.Equal(TestConstants.ServerUrl + "/serviceworkers/fetchdummy/foo", response.Url);
            Assert.Equal("responseFromServiceWorker:foo", await response.GetTextAsync());
        }
    }
}
