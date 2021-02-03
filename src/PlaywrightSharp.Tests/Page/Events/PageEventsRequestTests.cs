using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
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

        [PlaywrightTest("network.spec.js", "Page.Events.Request", "should fire for navigation requests")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireForNavigationRequests()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Single(requests);
        }

        [PlaywrightTest("network.spec.js", "Page.Events.Request", "should fire for iframes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireForIframes()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            Assert.Equal(2, requests.Count);
        }

        [PlaywrightTest("network.spec.js", "Page.Events.Request", "should fire for fetches")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireForFetches()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync("fetch('/empty.html')");
            Assert.Equal(2, requests.Count);
        }

        [PlaywrightTest("network.spec.js", "Page.Events.Request", "should report requests and responses handled by service worker")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
