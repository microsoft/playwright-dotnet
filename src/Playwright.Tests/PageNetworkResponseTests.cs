/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Playwright.MSTest;
using Microsoft.Playwright.Testing.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.Tests
{
    [TestClass]
    public class PageNetworkResponseTests : PageTestEx
    {
        [PlaywrightTest("page-network-response.spec.ts", "should return body")]
        public async Task ShouldReturnBody()
        {
            var response = await Page.GotoAsync(Server.Prefix + "/pptr.png");
            byte[] imageBuffer = File.ReadAllBytes(TestUtils.GetWebServerFile("pptr.png"));
            CollectionAssert.AreEqual(imageBuffer, await response.BodyAsync());
        }

        [PlaywrightTest("page-network-response.spec.ts", "should return body with compression")]
        public async Task ShouldReturnBodyWithCompression()
        {
            Server.EnableGzip("/pptr.png");
            var response = await Page.GotoAsync(Server.Prefix + "/pptr.png");
            byte[] imageBuffer = File.ReadAllBytes(TestUtils.GetWebServerFile("pptr.png"));
            CollectionAssert.AreEqual(imageBuffer, await response.BodyAsync());
        }

        [PlaywrightTest("page-network-response.spec.ts", "should work")]
        public async Task ShouldWork()
        {
            Server.SetRoute("/empty.html", (context) =>
            {
                context.Response.Headers["foo"] = "bar";
                return Task.CompletedTask;
            });

            var response = await Page.GotoAsync(Server.EmptyPage);
            StringAssert.Contains(response.Headers["foo"], "bar");
        }

        [PlaywrightTest("page-network-response.spec.ts", "should return json")]
        public async Task ShouldReturnJson()
        {
            var response = await Page.GotoAsync(Server.Prefix + "/simple.json");
            Assert.AreEqual("{\"foo\": \"bar\"}", (await response.JsonAsync())?.GetRawText());
        }

        public async Task ShouldWorkWithGenerics()
        {
            var response = await Page.GotoAsync(Server.Prefix + "/simple.json");
            var root = await response.JsonAsync();
            Assert.AreEqual("bar", root?.GetProperty("foo").GetString());
        }

        [PlaywrightTest("page-network-response.spec.ts", "should return status text")]
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
        public async Task ShouldReturnText()
        {
            var response = await Page.GotoAsync(Server.Prefix + "/simple.json");
            Assert.AreEqual("{\"foo\": \"bar\"}", (await response.TextAsync()).Trim());
        }

        [PlaywrightTest("page-network-response.spec.ts", "should return uncompressed text")]
        public async Task ShouldReturnUncompressedText()
        {
            Server.EnableGzip("/simple.json");
            var response = await Page.GotoAsync(Server.Prefix + "/simple.json");
            Assert.AreEqual("gzip", response.Headers["content-encoding"]);
            Assert.AreEqual("{\"foo\": \"bar\"}", (await response.TextAsync()).Trim());
        }

        [PlaywrightTest("page-network-response.spec.ts", "should throw when requesting body of redirected response")]
        public async Task ShouldThrowWhenRequestingBodyOfRedirectedResponse()
        {
            Server.SetRedirect("/foo.html", "/empty.html");
            var response = await Page.GotoAsync(Server.Prefix + "/foo.html");
            var redirectedFrom = response.Request.RedirectedFrom;
            Assert.IsNotNull(redirectedFrom);
            var redirected = await redirectedFrom.ResponseAsync();
            Assert.AreEqual((int)HttpStatusCode.Redirect, redirected.Status);

            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => redirected.TextAsync());
            StringAssert.Contains(exception.Message, "Response body is unavailable for redirect responses");
        }

        [PlaywrightTest("page-network-response.spec.ts", "should wait until response completes")]
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

            Assert.IsNotNull(serverResponse);
            Assert.IsNotNull(pageResponse);
            Assert.AreEqual((int)HttpStatusCode.OK, pageResponse.Status);
            Assert.IsFalse(requestFinished);

            var responseText = pageResponse.TextAsync();
            // Write part of the response and wait for it to be flushed.
            await serverResponse.WriteAsync("wor");
            // Finish response.
            await serverResponse.WriteAsync("ld!");
            serverResponseCompletion.SetResult(true);
            Assert.AreEqual("hello world!", await responseText);
        }

        [PlaywrightTest("har.spec.ts", "should return security details directly from response")]
        [Skip(TestTargets.Webkit | TestTargets.Linux)]
        public async Task ShouldReturnSecurityDetails()
        {
            var response = await Page.GotoAsync(HttpsServer.EmptyPage);
            var details = await response.SecurityDetailsAsync();

            var name = "puppeteer-tests";
            Assert.AreEqual(name, details.SubjectName);
            if (TestConstants.IsWebKit)
            {
                Assert.AreEqual(1550084863f, details.ValidFrom);
            }
            else
            {
                Assert.AreEqual(name, details.Issuer);
                Assert.AreEqual("TLS 1.2", details.Protocol);
            }
        }

        [PlaywrightTest("har.spec.ts", "should return server address directly from response")]
        public async Task ShouldReturnServerAddressFromResponse()
        {
            var response = await Page.GotoAsync(HttpsServer.EmptyPage);
            var details = await response.ServerAddrAsync();
            Assert.That.IsNotEmptyOrNull(details.IpAddress);
            Assert.That.IsGreater(details.Port, 0);
        }

        public override BrowserNewContextOptions ContextOptions => new() { IgnoreHTTPSErrors = true };
    }
}
