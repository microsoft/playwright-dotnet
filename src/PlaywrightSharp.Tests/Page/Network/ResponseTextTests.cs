using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Response.text</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ResponseTextTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ResponseTextTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("network.spec.js", "Response.text", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/simple.json");
            Assert.Equal("{\"foo\": \"bar\"}", (await response.GetTextAsync()).Trim());
        }

        [PlaywrightTest("network.spec.js", "Response.text", "should return uncompressed text")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnUncompressedText()
        {
            Server.EnableGzip("/simple.json");
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/simple.json");
            Assert.Equal("gzip", response.Headers["content-encoding"]);
            Assert.Equal("{\"foo\": \"bar\"}", (await response.GetTextAsync()).Trim());
        }

        [PlaywrightTest("network.spec.js", "Response.text", "should throw when requesting body of redirected response")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenRequestingBodyOfRedirectedResponse()
        {
            Server.SetRedirect("/foo.html", "/empty.html");
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/foo.html");
            var redirectedFrom = response.Request.RedirectedFrom;
            Assert.NotNull(redirectedFrom);
            var redirected = await redirectedFrom.GetResponseAsync();
            Assert.Equal(HttpStatusCode.Redirect, redirected.Status);

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(async () => await redirected.GetTextAsync());
            Assert.Contains("Response body is unavailable for redirect responses", exception.Message);
        }

        [PlaywrightTest("network.spec.js", "Response.text", "should wait until response completes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitUntilResponseCompletes()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            // Setup server to trap request.
            var serverResponseCompletion = new TaskCompletionSource<bool>();
            HttpResponse serverResponse = null;
            Server.SetRoute("/get", context =>
            {
                serverResponse = context.Response;
                context.Response.Headers["Content-Type"] = "text/plain; charset=utf-8";
                context.Response.WriteAsync("hello ");
                return serverResponseCompletion.Task;
            });

            // Setup page to trap response.
            bool requestFinished = false;
            Page.RequestFinished += (sender, e) => requestFinished = requestFinished || e.Request.Url.Contains("/get");
            // send request and wait for server response
            var (pageResponse, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Response),
                Page.EvaluateAsync("fetch('./get', { method: 'GET'})"),
                Server.WaitForRequest("/get")
            );

            Assert.NotNull(serverResponse);
            Assert.NotNull(pageResponse);
            Assert.Equal(HttpStatusCode.OK, pageResponse.Response.Status);
            Assert.False(requestFinished);

            var responseText = pageResponse.Response.GetTextAsync();
            // Write part of the response and wait for it to be flushed.
            await serverResponse.WriteAsync("wor");
            // Finish response.
            await serverResponse.WriteAsync("ld!");
            serverResponseCompletion.SetResult(true);
            Assert.Equal("hello world!", await responseText);
        }
    }
}
