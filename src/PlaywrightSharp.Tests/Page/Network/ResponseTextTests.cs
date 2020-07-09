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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class ResponseTextTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ResponseTextTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Response.text</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/simple.json");
            Assert.Equal("{\"foo\": \"bar\"}", (await response.GetTextAsync()).Trim());
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Response.text</playwright-describe>
        ///<playwright-it>should return uncompressed text</playwright-it>
        [Retry]
        public async Task ShouldReturnUncompressedText()
        {
            Server.EnableGzip("/simple.json");
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/simple.json");
            Assert.Equal("gzip", response.Headers["Content-Encoding"]);
            Assert.Equal("{\"foo\": \"bar\"}", (await response.GetTextAsync()).Trim());
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Response.text</playwright-describe>
        ///<playwright-it>should throw when requesting body of redirected response</playwright-it>
        [Retry]
        public async Task ShouldThrowWhenRequestingBodyOfRedirectedResponse()
        {
            Server.SetRedirect("/foo.html", "/empty.html");
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/foo.html");
            var redirectChain = response.Request.RedirectChain;
            Assert.Single(redirectChain);
            var redirected = redirectChain[0].Response;
            Assert.Equal(HttpStatusCode.Redirect, redirected.Status);

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(async () => await redirected.GetTextAsync());
            Assert.Contains("Response body is unavailable for redirect responses", exception.Message);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Response.text</playwright-describe>
        ///<playwright-it>should wait until response completes</playwright-it>
        [Retry]
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
                Page.WaitForEvent<ResponseEventArgs>(PageEvent.Response),
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
