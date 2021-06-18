using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright.Tests.TestServer;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageEventNetworkTests : PageTestEx
    {
        [PlaywrightTest("page-event-network.spec.ts", "Page.Events.Request")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PageEventsRequest()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.GotoAsync(Server.EmptyPage);
            Assert.That(requests, Has.Count.EqualTo(1));
            Assert.AreEqual(Server.EmptyPage, requests[0].Url);
            Assert.AreEqual("document", requests[0].ResourceType);
            Assert.AreEqual(HttpMethod.Get.Method, requests[0].Method);
            Assert.NotNull(await requests[0].ResponseAsync());
            Assert.AreEqual(Page.MainFrame, requests[0].Frame);
            Assert.AreEqual(Server.EmptyPage, requests[0].Frame.Url);
        }

        [PlaywrightTest("page-event-network.spec.ts", "Page.Events.Response")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PageEventsResponse()
        {
            var responses = new List<IResponse>();
            Page.Response += (_, e) => responses.Add(e);
            await Page.GotoAsync(Server.EmptyPage);
            Assert.That(responses, Has.Count.EqualTo(1));
            Assert.AreEqual(Server.EmptyPage, responses[0].Url);
            Assert.AreEqual((int)HttpStatusCode.OK, responses[0].Status);
            Assert.True(responses[0].Ok);
            Assert.NotNull(responses[0].Request);
        }

        [PlaywrightTest("page-event-network.spec.ts", "Page.Events.RequestFailed")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PageEventsRequestFailed()
        {
            int port = Server.Port + 100;
            var disposableServer = new SimpleServer(port, TestUtils.FindParentDirectory("Playwright.Tests.TestServer"), false);
            await disposableServer.StartAsync();

            disposableServer.SetRoute("/one-style.css", async _ =>
            {
                await disposableServer.StopAsync();
            });
            var failedRequests = new List<IRequest>();

            Page.RequestFailed += (_, e) => failedRequests.Add(e);

            await Page.GotoAsync($"http://localhost:{port}/one-style.html");

            Assert.That(failedRequests, Has.Count.EqualTo(1));
            StringAssert.Contains("one-style.css", failedRequests[0].Url);
            Assert.Null(await failedRequests[0].ResponseAsync());
            Assert.AreEqual("stylesheet", failedRequests[0].ResourceType);

            //We just need to test that we had a failure.
            Assert.NotNull(failedRequests[0].Failure);
            Assert.NotNull(failedRequests[0].Frame);
        }

        [PlaywrightTest("page-event-network.spec.ts", "Page.Events.RequestFinished")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PageEventsRequestFinished()
        {
            var (_, response) = await TaskUtils.WhenAll(
                Page.WaitForRequestFinishedAsync(),
                Page.GotoAsync(Server.EmptyPage));

            var request = response.Request;
            Assert.AreEqual(Server.EmptyPage, request.Url);
            Assert.NotNull(await request.ResponseAsync());
            Assert.AreEqual(HttpMethod.Get.Method, request.Method);
            Assert.AreEqual(Page.MainFrame, request.Frame);
            Assert.AreEqual(Server.EmptyPage, request.Frame.Url);
        }

        [PlaywrightTest("page-event-network.spec.ts", "should fire events in proper order")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireEventsInProperOrder()
        {
            var events = new List<string>();
            Page.Request += (_, _) => events.Add("request");
            Page.Response += (_, _) => events.Add("response");
            var response = await Page.GotoAsync(Server.EmptyPage);
            await response.FinishedAsync();
            events.Add("requestfinished");
            Assert.AreEqual(new[] { "request", "response", "requestfinished" }, events);
        }

        [PlaywrightTest("page-event-network.spec.ts", "should support redirects")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportRedirects()
        {
            var events = new List<string>();
            Page.Request += (_, e) => events.Add($"{e.Method} {e.Url}");
            Page.Response += (_, e) => events.Add($"{(int)e.Status} {e.Url}");
            Page.RequestFinished += (_, e) => events.Add($"DONE {e.Url}");
            Page.RequestFailed += (_, e) => events.Add($"FAIL {e.Url}");
            Server.SetRedirect("/foo.html", "/empty.html");
            string FOO_URL = Server.Prefix + "/foo.html";
            var response = await Page.GotoAsync(FOO_URL);
            await response.FinishedAsync();
            Assert.AreEqual(new[] {
                $"GET {FOO_URL}",
                $"302 {FOO_URL}",
                $"DONE {FOO_URL}",
                $"GET {Server.EmptyPage}",
                $"200 {Server.EmptyPage}",
                $"DONE {Server.EmptyPage}"
            }, events);

            // Check redirect chain
            var redirectedFrom = response.Request.RedirectedFrom;

            StringAssert.Contains("/foo.html", redirectedFrom.Url);
            Assert.NotNull(redirectedFrom.RedirectedTo);
            Assert.AreEqual(response.Request, redirectedFrom.RedirectedTo);
        }
    }
}
