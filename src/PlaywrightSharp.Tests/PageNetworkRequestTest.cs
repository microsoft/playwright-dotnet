using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Contracts.Constants;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>network-request.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageNetworkRequestTest : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageNetworkRequestTest(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-network-request.spec.ts", "should work for main frame navigation request")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForMainFrameNavigationRequests()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Single(requests);
            Assert.Equal(Page.MainFrame, requests[0].Frame);
        }

        [PlaywrightTest("page-network-request.spec.ts", "should work for subframe navigation request")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForSubframeNavigationRequest()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);

            await Page.GoToAsync(TestConstants.EmptyPage);

            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            Assert.Equal(2, requests.Count);
            Assert.Equal(Page.FirstChildFrame(), requests[1].Frame);
        }

        [PlaywrightTest("page-network-request.spec.ts", "should work for fetch requests")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForFetchRequests()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.EvaluateAsync("fetch('/digits/1.png')");
            Assert.Single(requests.Where(r => !r.Url.Contains("favicon")));
            Assert.Equal(Page.MainFrame, requests[0].Frame);
        }

        [PlaywrightTest("page-network-request.spec.ts", "should return headers")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnHeaders()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage);

            string expected = TestConstants.Product switch
            {
                TestConstants.ChromiumProduct => "Chrome",
                TestConstants.FirefoxProduct => "Firefox",
                TestConstants.WebkitProduct => "WebKit",
                _ => "None"
            };

            Assert.Contains(response.Request.GetHeaderValues("user-agent"), (f) => f.Contains(expected));
        }

        [PlaywrightTest("page-network-request.spec.ts", "Request.headers", "should get the same headers as the server")]
        [Fact(Skip = "We don't need to test this")]
        public void ShouldGetTheSameHeadersAsTheServer()
        {
        }

        [PlaywrightTest("page-network-request.spec.ts", "should return postData")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnPostData()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", _ => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (_, e) => request = e;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: JSON.stringify({ foo: 'bar'})})");
            Assert.NotNull(request);
            Assert.Equal("{\"foo\":\"bar\"}", request.PostData);
        }

        [PlaywrightTest("page-network-request.spec.ts", "should work with binary post data")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBinaryPostData()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", _ => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (_, e) => request = e;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(256).keys())) })");
            Assert.NotNull(request);
            byte[] data = request.PostDataBuffer;
            Assert.Equal(256, data.Length);

            for (int index = 0; index < data.Length; index++)
            {
                Assert.Equal(index, data[index]);
            }
        }

        [PlaywrightTest("page-network-request.spec.ts", "should work with binary post data and interception")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBinaryPostDataAndInterception()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", _ => Task.CompletedTask);
            await Page.RouteAsync("/post", (route) => route.ResumeAsync());
            IRequest request = null;
            Page.Request += (_, e) => request = e;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(256).keys())) })");
            Assert.NotNull(request);
            byte[] data = request.PostDataBuffer;
            Assert.Equal(256, data.Length);

            for (int index = 0; index < data.Length; index++)
            {
                Assert.Equal(index, data[index]);
            }
        }

        [PlaywrightTest("page-network-request.spec.ts", "should be |undefined| when there is no post data")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeUndefinedWhenThereIsNoPostData()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Null(response.Request.PostData);
        }


        [PlaywrightTest("page-network-request.spec.ts", "should parse the json post data")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldParseTheJsonPostData()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", _ => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (_, e) => request = e;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: JSON.stringify({ foo: 'bar'})})");
            Assert.NotNull(request);
            Assert.Equal("bar", request.GetPayloadAsJson().RootElement.GetProperty("foo").ToString());
        }

        [PlaywrightTest("page-network-request.spec.ts", "should parse the data if content-type is application/x-www-form-urlencoded")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldParseTheDataIfContentTypeIsApplicationXWwwFormUrlencoded()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/post", _ => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (_, e) => request = e;
            await Page.SetContentAsync("<form method='POST' action='/post'><input type='text' name='foo' value='bar'><input type='number' name='baz' value='123'><input type='submit'></form>");
            await Page.ClickAsync("input[type=submit]");

            Assert.NotNull(request);
            var element = request.GetPayloadAsJson().RootElement;
            Assert.Equal("bar", element.GetProperty("foo").ToString());
            Assert.Equal("123", element.GetProperty("baz").ToString());
        }

        [PlaywrightTest("page-network-request.spec.ts", "should be |undefined| when there is no post data")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeUndefinedWhenThereIsNoPostData2()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Null(response.Request.GetPayloadAsJson());
        }

        [PlaywrightTest("page-network-request.spec.ts", "should return event source")]
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
            Page.Request += (_, e) => requests.Add(e);

            Assert.Equal(sseMessage, await Page.EvaluateAsync<string>(@"() => {
                const eventSource = new EventSource('/sse');
                return new Promise(resolve => {
                    eventSource.onmessage = e => resolve(e.data);
                });
            }"));

            Assert.Equal(ResourceTypes.EventSource, requests[0].ResourceType);
        }

        [PlaywrightTest("page-network-request.spec.ts", "should return navigation bit")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNavigationBit()
        {
            var requests = new Dictionary<string, IRequest>();
            Page.Request += (_, e) => requests[e.Url.Split('/').Last()] = e;
            Server.SetRedirect("/rrredirect", "/frames/one-frame.html");
            await Page.GoToAsync(TestConstants.ServerUrl + "/rrredirect");
            Assert.True(requests["rrredirect"].IsNavigationRequest);
            Assert.True(requests["one-frame.html"].IsNavigationRequest);
            Assert.True(requests["frame.html"].IsNavigationRequest);
            Assert.False(requests["script.js"].IsNavigationRequest);
            Assert.False(requests["style.css"].IsNavigationRequest);
        }

        [PlaywrightTest("page-network-request.spec.ts", "Request.isNavigationRequest", "should return navigation bit when navigating to image")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNavigationBitWhenNavigatingToImage()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.GoToAsync(TestConstants.ServerUrl + "/pptr.png");
            Assert.True(requests[0].IsNavigationRequest);
        }
    }
}
