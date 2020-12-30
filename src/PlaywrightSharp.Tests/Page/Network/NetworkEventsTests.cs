using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.TestServer;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Network Events</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class NetworkEventsTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public NetworkEventsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Network Events</playwright-describe>
        ///<playwright-it>Page.Events.Request</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PageEventsRequest()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Single(requests);
            Assert.Equal(TestConstants.EmptyPage, requests[0].Url);
            Assert.Equal(ResourceType.Document, requests[0].ResourceType);
            Assert.Equal(HttpMethod.Get, requests[0].Method);
            Assert.NotNull(await requests[0].GetResponseAsync());
            Assert.Equal(Page.MainFrame, requests[0].Frame);
            Assert.Equal(TestConstants.EmptyPage, requests[0].Frame.Url);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Network Events</playwright-describe>
        ///<playwright-it>Page.Events.Response</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PageEventsResponse()
        {
            var responses = new List<IResponse>();
            Page.Response += (sender, e) => responses.Add(e.Response);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Single(responses);
            Assert.Equal(TestConstants.EmptyPage, responses[0].Url);
            Assert.Equal(HttpStatusCode.OK, responses[0].Status);
            Assert.True(responses[0].Ok);
            Assert.NotNull(responses[0].Request);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Network Events</playwright-describe>
        ///<playwright-it>Page.Events.RequestFailed</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PageEventsRequestFailed()
        {
            int port = TestConstants.Port + 100;
            var disposableServer = new SimpleServer(port, TestUtils.FindParentDirectory("PlaywrightSharp.TestServer"), false);
            await disposableServer.StartAsync();

            disposableServer.SetRoute("/one-style.css", async context =>
            {
                await disposableServer.StopAsync();
            });
            var failedRequests = new List<IRequest>();

            Page.RequestFailed += (sender, e) => failedRequests.Add(e.Request);

            await Page.GoToAsync($"http://localhost:{port}/one-style.html");

            Assert.Single(failedRequests);
            Assert.Contains("one-style.css", failedRequests[0].Url);
            Assert.Null(await failedRequests[0].GetResponseAsync());
            Assert.Equal(ResourceType.StyleSheet, failedRequests[0].ResourceType);

            string error = string.Empty;

            //We just need to test that we had a failure.
            Assert.NotNull(failedRequests[0].Failure);
            Assert.NotNull(failedRequests[0].Frame);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Network Events</playwright-describe>
        ///<playwright-it>Page.Events.RequestFinished</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PageEventsRequestFinished()
        {
            var (_, response) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.RequestFinished),
                Page.GoToAsync(TestConstants.EmptyPage));

            var request = response.Request;
            Assert.Equal(TestConstants.EmptyPage, request.Url);
            Assert.NotNull(await request.GetResponseAsync());
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(Page.MainFrame, request.Frame);
            Assert.Equal(TestConstants.EmptyPage, request.Frame.Url);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Network Events</playwright-describe>
        ///<playwright-it>should fire events in proper order</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireEventsInProperOrder()
        {
            var events = new List<string>();
            Page.Request += (sender, e) => events.Add("request");
            Page.Response += (sender, e) => events.Add("response");
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            await response.FinishedAsync();
            events.Add("requestfinished");
            Assert.Equal(new[] { "request", "response", "requestfinished" }, events);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Network Events</playwright-describe>
        ///<playwright-it>should support redirects</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportRedirects()
        {
            var events = new List<string>();
            Page.Request += (sender, e) => events.Add($"{e.Request.Method} {e.Request.Url}");
            Page.Response += (sender, e) => events.Add($"{(int)e.Response.Status} {e.Response.Url}");
            Page.RequestFinished += (sender, e) => events.Add($"DONE {e.Request.Url}");
            Page.RequestFailed += (sender, e) => events.Add($"FAIL {e.Request.Url}");
            Server.SetRedirect("/foo.html", "/empty.html");
            const string FOO_URL = TestConstants.ServerUrl + "/foo.html";
            var response = await Page.GoToAsync(FOO_URL);
            await response.FinishedAsync();
            Assert.Equal(new[] {
                $"GET {FOO_URL}",
                $"302 {FOO_URL}",
                $"DONE {FOO_URL}",
                $"GET {TestConstants.EmptyPage}",
                $"200 {TestConstants.EmptyPage}",
                $"DONE {TestConstants.EmptyPage}"
            }, events);

            // Check redirect chain
            var redirectedFrom = response.Request.RedirectedFrom;

            Assert.Contains("/foo.html", redirectedFrom.Url);
            Assert.NotNull(redirectedFrom.RedirectedTo);
            Assert.Equal(response.Request, redirectedFrom.RedirectedTo);
        }
    }
}
