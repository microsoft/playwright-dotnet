using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>network-request.spec.js</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class NetworkRequestTest : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public NetworkRequestTest(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("network-request.spec.js", "should work for main frame navigation request")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForMainFrameNavigationRequests()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Single(requests);
            Assert.Equal(Page.MainFrame, requests[0].Frame);
        }

        [PlaywrightTest("network-request.spec.js", "should work for subframe navigation request")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForSubframeNavigationRequest()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);

            await Page.GoToAsync(TestConstants.EmptyPage);

            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            Assert.Equal(2, requests.Count);
            Assert.Equal(Page.FirstChildFrame(), requests[1].Frame);
        }

        [PlaywrightTest("network-request.spec.js", "should work for fetch requests")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForFetchRequests()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.EvaluateAsync("fetch('/digits/1.png')");
            Assert.Single(requests.Where(r => !r.Url.Contains("favicon")));
            Assert.Equal(Page.MainFrame, requests[0].Frame);
        }

        [PlaywrightTest("network-request.spec.js", "should return headers")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnHeaders()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Contains(
                TestConstants.Product switch
                {
                    TestConstants.ChromiumProduct => "Chrome",
                    TestConstants.FirefoxProduct => "Firefox",
                    TestConstants.WebkitProduct => "WebKit",
                    _ => "None"
                },
                response.Request.Headers["user-agent"]);
        }

        [PlaywrightTest("network-request.spec.js", "Request.headers", "should get the same headers as the server")]
        [Fact(Skip = "We don't need to test this")]
        public void ShouldGetTheSameHeadersAsTheServer()
        {
        }

        [PlaywrightTest("network-request.spec.js", "should return postData")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnPostData()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", context => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (sender, e) => request = e.Request;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: JSON.stringify({ foo: 'bar'})})");
            Assert.NotNull(request);
            Assert.Equal("{\"foo\":\"bar\"}", request.PostData);
        }

        [PlaywrightTest("network-request.spec.js", "should work with binary post data")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBinaryPostData()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", context => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (sender, e) => request = e.Request;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(256).keys())) })");
            Assert.NotNull(request);
            byte[] data = request.PostDataBuffer;
            Assert.Equal(256, data.Length);

            for (int index = 0; index < data.Length; index++)
            {
                Assert.Equal(index, data[index]);
            }
        }

        [PlaywrightTest("network-request.spec.js", "should work with binary post data and interception")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBinaryPostDataAndInterception()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", context => Task.CompletedTask);
            await Page.RouteAsync("/post", (route, _) => route.ContinueAsync());
            IRequest request = null;
            Page.Request += (sender, e) => request = e.Request;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(256).keys())) })");
            Assert.NotNull(request);
            byte[] data = request.PostDataBuffer;
            Assert.Equal(256, data.Length);

            for (int index = 0; index < data.Length; index++)
            {
                Assert.Equal(index, data[index]);
            }
        }

        [PlaywrightTest("network-request.spec.js", "should be |undefined| when there is no post data")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeUndefinedWhenThereIsNoPostData()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Null(response.Request.PostData);
        }


        [PlaywrightTest("network-request.spec.js", "should parse the json post data")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldParseTheJsonPostData()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", context => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (sender, e) => request = e.Request;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: JSON.stringify({ foo: 'bar'})})");
            Assert.NotNull(request);
            Assert.Equal("bar", request.GetPostDataJson().RootElement.GetProperty("foo").ToString());
        }

        [PlaywrightTest("network-request.spec.js", "should parse the data if content-type is application/x-www-form-urlencoded")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldParseTheDataIfContentTypeIsApplicationXWwwFormUrlencoded()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", context => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (sender, e) => request = e.Request;
            await Page.SetContentAsync("<form method='POST' action='/post'><input type='text' name='foo' value='bar'><input type='number' name='baz' value='123'><input type='submit'></form>");
            await Page.ClickAsync("input[type=submit]");

            Assert.NotNull(request);
            var element = request.GetPostDataJson().RootElement;
            Assert.Equal("bar", element.GetProperty("foo").ToString());
            Assert.Equal("123", element.GetProperty("baz").ToString());
        }

        [PlaywrightTest("network-request.spec.js", "should be |undefined| when there is no post data")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeUndefinedWhenThereIsNoPostData2()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Null(response.Request.GetPostDataJson());
        }

        [PlaywrightTest("network-request.spec.js", "should return event source")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEventSource()
        {
            const string sseMessage = "{\"foo\": \"bar\"}";

            Server.SetRoute("/sse", async ctx =>
            {
                ctx.Response.Headers["content-type"] = "text/event-stream";
                ctx.Response.Headers["connection"] = "keep-alive";
                ctx.Response.Headers["cache-control"] = "no-cache";

                await ctx.Response.Body.FlushAsync();
                await ctx.Response.WriteAsync($"data: {sseMessage}\r\r");
                await ctx.Response.Body.FlushAsync();
            });

            await Page.GoToAsync(TestConstants.EmptyPage);
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);

            Assert.Equal(sseMessage, await Page.EvaluateAsync<string>(@"() => {
                const eventSource = new EventSource('/sse');
                return new Promise(resolve => {
                    eventSource.onmessage = e => resolve(e.data);
                });
            }"));

            Assert.Equal(ResourceType.EventSource, requests[0].ResourceType);
        }

        [PlaywrightTest("network.spec.js", "should return navigation bit")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNavigationBit()
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

        [PlaywrightTest("network.spec.js", "Request.isNavigationRequest", "should return navigation bit when navigating to image")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNavigationBitWhenNavigatingToImage()
        {
            var requests = new List<IRequest>();
            Page.Request += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.ServerUrl + "/pptr.png");
            Assert.True(requests[0].IsNavigationRequest);
        }
    }
}
