using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageNetworkResponseTests : PageTestEx
    {
        [PlaywrightTest("page-network-response.spec.ts", "should return body")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnBody()
        {
            var response = await Page.GotoAsync(Server.Prefix + "/pptr.png");
            byte[] imageBuffer = File.ReadAllBytes(TestUtils.GetWebServerFile("pptr.png"));
            Assert.AreEqual(imageBuffer, await response.BodyAsync());
        }

        [PlaywrightTest("page-network-response.spec.ts", "should return body with compression")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnBodyWithCompression()
        {
            Server.EnableGzip("/pptr.png");
            var response = await Page.GotoAsync(Server.Prefix + "/pptr.png");
            byte[] imageBuffer = File.ReadAllBytes(TestUtils.GetWebServerFile("pptr.png"));
            Assert.AreEqual(imageBuffer, await response.BodyAsync());
        }

        [PlaywrightTest("page-network-response.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            Server.SetRoute("/empty.html", (context) =>
            {
                context.Response.Headers["foo"] = "bar";
                return Task.CompletedTask;
            });

            var response = await Page.GotoAsync(Server.EmptyPage);
            StringAssert.Contains("bar", response.Headers["foo"]);
        }

        [PlaywrightTest("page-network-response.spec.ts", "should return json")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnJson()
        {
            var response = await Page.GotoAsync(Server.Prefix + "/simple.json");
            Assert.AreEqual("{\"foo\": \"bar\"}", (await response.JsonAsync())?.GetRawText());
        }

        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithGenerics()
        {
            var response = await Page.GotoAsync(Server.Prefix + "/simple.json");
            var root = await response.JsonAsync();
            Assert.AreEqual("bar", root?.GetProperty("foo").GetString());
        }

        [PlaywrightTest("page-network-response.spec.ts", "should return status text")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnStatusText()
        {
            Server.SetRoute("/cool", (context) =>
            {
                context.Response.StatusCode = 200;
                //There are some debates about this on these issues
                //https://github.com/aspnet/HttpAbstractions/issues/395
                //https://github.com/aspnet/HttpAbstractions/issues/486
                //https://github.com/aspnet/HttpAbstractions/issues/794
                context.Features.Get<IHttpResponseFeature>().ReasonPhrase = "cool!";
                return Task.CompletedTask;
            });
            var response = await Page.GotoAsync(Server.Prefix + "/cool");
            Assert.AreEqual("cool!", response.StatusText);
        }

        [PlaywrightTest("page-network-response.spec.ts", "should return text")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnText()
        {
            var response = await Page.GotoAsync(Server.Prefix + "/simple.json");
            Assert.AreEqual("{\"foo\": \"bar\"}", (await response.TextAsync()).Trim());
        }

        [PlaywrightTest("page-network-response.spec.ts", "should return uncompressed text")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnUncompressedText()
        {
            Server.EnableGzip("/simple.json");
            var response = await Page.GotoAsync(Server.Prefix + "/simple.json");
            Assert.AreEqual("gzip", response.Headers["content-encoding"]);
            Assert.AreEqual("{\"foo\": \"bar\"}", (await response.TextAsync()).Trim());
        }

        [PlaywrightTest("page-network-response.spec.ts", "should throw when requesting body of redirected response")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenRequestingBodyOfRedirectedResponse()
        {
            Server.SetRedirect("/foo.html", "/empty.html");
            var response = await Page.GotoAsync(Server.Prefix + "/foo.html");
            var redirectedFrom = response.Request.RedirectedFrom;
            Assert.NotNull(redirectedFrom);
            var redirected = await redirectedFrom.ResponseAsync();
            Assert.AreEqual((int)HttpStatusCode.Redirect, redirected.Status);

            var exception = await AssertThrowsAsync<PlaywrightException>(() => redirected.TextAsync());
            StringAssert.Contains("Response body is unavailable for redirect responses", exception.Message);
        }

        [PlaywrightTest("page-network-response.spec.ts", "should wait until response completes")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitUntilResponseCompletes()
        {
            await Page.GotoAsync(Server.EmptyPage);
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
            Page.RequestFinished += (_, e) => requestFinished = requestFinished || e.Url.Contains("/get");
            // send request and wait for server response
            var (pageResponse, _) = await TaskUtils.WhenAll(
                Page.WaitForResponseAsync("**/*"),
                Page.EvaluateAsync("fetch('./get', { method: 'GET'})"),
                Server.WaitForRequest("/get")
            );

            Assert.NotNull(serverResponse);
            Assert.NotNull(pageResponse);
            Assert.AreEqual((int)HttpStatusCode.OK, pageResponse.Status);
            Assert.False(requestFinished);

            var responseText = pageResponse.TextAsync();
            // Write part of the response and wait for it to be flushed.
            await serverResponse.WriteAsync("wor");
            // Finish response.
            await serverResponse.WriteAsync("ld!");
            serverResponseCompletion.SetResult(true);
            Assert.AreEqual("hello world!", await responseText);
        }
    }
}
