using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using PlaywrightSharp.TestServer;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Network Events</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class NetworkEventsTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public NetworkEventsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Network Events</playwright-describe>
        ///<playwright-it>Page.Events.Request</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task PageEventsRequest()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Single(requests);
            Assert.Equal(TestConstants.EmptyPage, requests[0].Url);
            Assert.Equal(ResourceType.Document, requests[0].ResourceType);
            Assert.Equal(HttpMethod.Get, requests[0].Method);
            Assert.NotNull(requests[0].Response);
            Assert.Equal(Page.MainFrame, requests[0].Frame);
            Assert.Equal(TestConstants.EmptyPage, requests[0].Frame.Url);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Network Events</playwright-describe>
        ///<playwright-it>Page.Events.Response</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
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
            Assert.Null(failedRequests[0].Response);
            Assert.Equal(ResourceType.StyleSheet, failedRequests[0].ResourceType);

            string error = string.Empty;

            if (TestConstants.IsChromium)
            {
                // It's not the same error as in Playwright but it works for testing purposes.
                error = "net::ERR_CONNECTION_REFUSED";
            }
            else if (TestConstants.IsWebKit)
            {
                if (TestConstants.IsMacOSX)
                {
                    error = "The network connection was lost.";
                }
                else if (TestConstants.IsWindows)
                {
                    error = "Unsupported protocol";
                }
                else
                {
                    error = "Message Corrupt";
                }
            }
            else
            {
                error = "NS_ERROR_FAILURE";
            }

            Assert.Equal(error, failedRequests[0].Failure);
            Assert.NotNull(failedRequests[0].Frame);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Network Events</playwright-describe>
        ///<playwright-it>Page.Events.RequestFinished</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task PageEventsRequestFinished()
        {
            var requests = new List<IRequest>();
            Page.RequestFinished += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Single(requests);
            Assert.Equal(TestConstants.EmptyPage, requests[0].Url);
            Assert.NotNull(requests[0].Response);
            Assert.Equal(HttpMethod.Get, requests[0].Method);
            Assert.Equal(Page.MainFrame, requests[0].Frame);
            Assert.Equal(TestConstants.EmptyPage, requests[0].Frame.Url);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Network Events</playwright-describe>
        ///<playwright-it>should fire events in proper order</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFireEventsInProperOrder()
        {
            var events = new List<string>();
            Page.Request += (sender, e) => events.Add("request");
            Page.Response += (sender, e) => events.Add("response");
            Page.RequestFinished += (sender, e) => events.Add("requestfinished");
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(new[] { "request", "response", "requestfinished" }, events);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Network Events</playwright-describe>
        ///<playwright-it>should support redirects</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
            Assert.Equal(new[] {
                $"GET {FOO_URL}",
                $"302 {FOO_URL}",
                $"DONE {FOO_URL}",
                $"GET {TestConstants.EmptyPage}",
                $"200 {TestConstants.EmptyPage}",
                $"DONE {TestConstants.EmptyPage}"
            }, events);

            // Check redirect chain
            var redirectChain = response.Request.RedirectChain;
            Assert.Single(redirectChain);
            Assert.Contains("/foo.html", redirectChain[0].Url);
        }
    }
}
