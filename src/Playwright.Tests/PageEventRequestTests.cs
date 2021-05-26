using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventRequestTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventRequestTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-event-request.spec.ts", "should fire for navigation requests")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireForNavigationRequests()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.Single(requests);
        }

        [PlaywrightTest("page-event-request.spec.ts", "should fire for iframes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireForIframes()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.GotoAsync(TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            Assert.Equal(2, requests.Count);
        }

        [PlaywrightTest("page-event-request.spec.ts", "should fire for fetches")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireForFetches()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync("fetch('/empty.html')");
            Assert.Equal(2, requests.Count);
        }

        [PlaywrightTest("page-event-request.spec.ts", "should report requests and responses handled by service worker")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportRequestsAndResponsesHandledByServiceWorker()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/serviceworkers/fetchdummy/sw.html");
            await Page.EvaluateAsync("() => window.activationPromise");

            var (request, swResponse) = await TaskUtils.WhenAll(
                Page.WaitForRequestAsync("**/*"),
                Page.EvaluateAsync<string>("() => fetchDummy('foo')"));

            Assert.Equal("responseFromServiceWorker:foo", swResponse);
            Assert.Equal(TestConstants.ServerUrl + "/serviceworkers/fetchdummy/foo", request.Url);
            var response = await request.ResponseAsync();
            Assert.Equal(TestConstants.ServerUrl + "/serviceworkers/fetchdummy/foo", response.Url);
            Assert.Equal("responseFromServiceWorker:foo", await response.TextAsync());
        }
    }
}
