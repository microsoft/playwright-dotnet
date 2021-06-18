using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageEventRequestTests : PageTestEx
    {
        [PlaywrightTest("page-event-request.spec.ts", "should fire for navigation requests")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireForNavigationRequests()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.GotoAsync(Server.EmptyPage);
            Assert.That(requests, Has.Count.EqualTo(1));
        }

        [PlaywrightTest("page-event-request.spec.ts", "should fire for iframes")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireForIframes()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.GotoAsync(Server.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            Assert.AreEqual(2, requests.Count);
        }

        [PlaywrightTest("page-event-request.spec.ts", "should fire for fetches")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireForFetches()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.GotoAsync(Server.EmptyPage);
            await Page.EvaluateAsync("fetch('/empty.html')");
            Assert.AreEqual(2, requests.Count);
        }

        [PlaywrightTest("page-event-request.spec.ts", "should report requests and responses handled by service worker")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportRequestsAndResponsesHandledByServiceWorker()
        {
            await Page.GotoAsync(Server.Prefix + "/serviceworkers/fetchdummy/sw.html");
            await Page.EvaluateAsync("() => window.activationPromise");

            var (request, swResponse) = await TaskUtils.WhenAll(
                Page.WaitForRequestAsync("**/*"),
                Page.EvaluateAsync<string>("() => fetchDummy('foo')"));

            Assert.AreEqual("responseFromServiceWorker:foo", swResponse);
            Assert.AreEqual(Server.Prefix + "/serviceworkers/fetchdummy/foo", request.Url);
            var response = await request.ResponseAsync();
            Assert.AreEqual(Server.Prefix + "/serviceworkers/fetchdummy/foo", response.Url);
            Assert.AreEqual("responseFromServiceWorker:foo", await response.TextAsync());
        }
    }
}
