using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>network-request.spec.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class PageNetworkRequestTest : PageTestEx
    {
        [PlaywrightTest("page-network-request.spec.ts", "should work for main frame navigation request")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForMainFrameNavigationRequests()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.GotoAsync(Server.EmptyPage);
            Assert.That(requests, Has.Count.EqualTo(1));
            Assert.AreEqual(Page.MainFrame, requests[0].Frame);
        }

        [PlaywrightTest("page-network-request.spec.ts", "should work for subframe navigation request")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForSubframeNavigationRequest()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);

            await Page.GotoAsync(Server.EmptyPage);

            await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
            Assert.AreEqual(2, requests.Count);
            Assert.AreEqual(Page.FirstChildFrame(), requests[1].Frame);
        }

        [PlaywrightTest("page-network-request.spec.ts", "should work for fetch requests")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForFetchRequests()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.EvaluateAsync("fetch('/digits/1.png')");
            Assert.AreEqual(1, requests.Where(r => !r.Url.Contains("favicon")).Count());
            Assert.AreEqual(Page.MainFrame, requests[0].Frame);
        }

        [PlaywrightTest("page-network-request.spec.ts", "should return headers")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnHeaders()
        {
            var response = await Page.GotoAsync(Server.EmptyPage);

            string expected = TestConstants.BrowserName switch
            {
                "chromium" => "Chrome",
                "firefox" => "Firefox",
                "webkit" => "WebKit",
                _ => "None"
            };

            StringAssert.Contains(expected, response.Request.Headers["user-agent"]);
        }

        [PlaywrightTest("page-network-request.spec.ts", "Request.headers", "should get the same headers as the server")]
        [Test, Ignore("We don't need to test this")]
        public void ShouldGetTheSameHeadersAsTheServer()
        {
        }

        [PlaywrightTest("page-network-request.spec.ts", "should return postData")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            Server.SetRoute("/post", _ => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (_, e) => request = e;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: JSON.stringify({ foo: 'bar'})})");
            Assert.NotNull(request);
            Assert.AreEqual("{\"foo\":\"bar\"}", request.PostData);
        }

        [PlaywrightTest("page-network-request.spec.ts", "should work with binary post data")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBinaryPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            Server.SetRoute("/post", _ => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (_, e) => request = e;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(256).keys())) })");
            Assert.NotNull(request);
            byte[] data = request.PostDataBuffer;
            Assert.AreEqual(256, data.Length);

            for (int index = 0; index < data.Length; index++)
            {
                Assert.AreEqual(index, data[index]);
            }
        }

        [PlaywrightTest("page-network-request.spec.ts", "should work with binary post data and interception")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBinaryPostDataAndInterception()
        {
            await Page.GotoAsync(Server.EmptyPage);
            Server.SetRoute("/post", _ => Task.CompletedTask);
            await Page.RouteAsync("/post", (route) => route.ContinueAsync());
            IRequest request = null;
            Page.Request += (_, e) => request = e;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(256).keys())) })");
            Assert.NotNull(request);
            byte[] data = request.PostDataBuffer;
            Assert.AreEqual(256, data.Length);

            for (int index = 0; index < data.Length; index++)
            {
                Assert.AreEqual(index, data[index]);
            }
        }

        [PlaywrightTest("page-network-request.spec.ts", "should be |undefined| when there is no post data")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeUndefinedWhenThereIsNoPostData()
        {
            var response = await Page.GotoAsync(Server.EmptyPage);
            Assert.Null(response.Request.PostData);
        }


        [PlaywrightTest("page-network-request.spec.ts", "should parse the json post data")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldParseTheJsonPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            Server.SetRoute("/post", _ => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (_, e) => request = e;
            await Page.EvaluateHandleAsync("fetch('./post', { method: 'POST', body: JSON.stringify({ foo: 'bar'})})");
            Assert.NotNull(request);
            Assert.AreEqual("bar", request.PostDataJSON()?.GetProperty("foo").ToString());
        }

        [PlaywrightTest("page-network-request.spec.ts", "should parse the data if content-type is application/x-www-form-urlencoded")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldParseTheDataIfContentTypeIsApplicationXWwwFormUrlencoded()
        {
            await Page.GotoAsync(Server.EmptyPage);
            Server.SetRoute("/post", _ => Task.CompletedTask);
            IRequest request = null;
            Page.Request += (_, e) => request = e;
            await Page.SetContentAsync("<form method='POST' action='/post'><input type='text' name='foo' value='bar'><input type='number' name='baz' value='123'><input type='submit'></form>");
            await Page.ClickAsync("input[type=submit]");

            Assert.NotNull(request);
            var element = request.PostDataJSON();
            Assert.AreEqual("bar", element?.GetProperty("foo").ToString());
            Assert.AreEqual("123", element?.GetProperty("baz").ToString());
        }

        [PlaywrightTest("page-network-request.spec.ts", "should be |undefined| when there is no post data")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeUndefinedWhenThereIsNoPostData2()
        {
            var response = await Page.GotoAsync(Server.EmptyPage);
            Assert.Null(response.Request.PostDataJSON());
        }

        [PlaywrightTest("page-network-request.spec.ts", "should return event source")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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

            await Page.GotoAsync(Server.EmptyPage);
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);

            Assert.AreEqual(sseMessage, await Page.EvaluateAsync<string>(@"() => {
                const eventSource = new EventSource('/sse');
                return new Promise(resolve => {
                    eventSource.onmessage = e => resolve(e.data);
                });
            }"));

            Assert.AreEqual("eventsource", requests[0].ResourceType);
        }

        [PlaywrightTest("page-network-request.spec.ts", "should return navigation bit")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNavigationBit()
        {
            var requests = new Dictionary<string, IRequest>();
            Page.Request += (_, e) => requests[e.Url.Split('/').Last()] = e;
            Server.SetRedirect("/rrredirect", "/frames/one-frame.html");
            await Page.GotoAsync(Server.Prefix + "/rrredirect");
            Assert.True(requests["rrredirect"].IsNavigationRequest);
            Assert.True(requests["one-frame.html"].IsNavigationRequest);
            Assert.True(requests["frame.html"].IsNavigationRequest);
            Assert.False(requests["script.js"].IsNavigationRequest);
            Assert.False(requests["style.css"].IsNavigationRequest);
        }

        [PlaywrightTest("page-network-request.spec.ts", "Request.isNavigationRequest", "should return navigation bit when navigating to image")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNavigationBitWhenNavigatingToImage()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);
            await Page.GotoAsync(Server.Prefix + "/pptr.png");
            Assert.True(requests[0].IsNavigationRequest);
        }
    }
}
