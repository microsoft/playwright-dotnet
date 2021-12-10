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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    public class PageRequestContinueTests : PageTestEx
    {
        [PlaywrightTest("page-request-continue.spec.ts", "should work")]
        public async Task ShouldWork()
        {
            await Page.RouteAsync("**/*", (route) => route.ContinueAsync());
            await Page.GotoAsync(Server.EmptyPage);
        }

        [PlaywrightTest("page-request-continue.spec.ts", "should amend HTTP headers")]
        public async Task ShouldAmendHTTPHeaders()
        {
            await Page.RouteAsync("**/*", (route) =>
            {
#pragma warning disable 0612
                var headers = new Dictionary<string, string>(route.Request.Headers.ToDictionary(x => x.Key, x => x.Value)) { ["FOO"] = "bar" };
#pragma warning restore 0612
                route.ContinueAsync(new() { Headers = headers });
            });
            await Page.GotoAsync(Server.EmptyPage);
            var requestTask = Server.WaitForRequest("/sleep.zzz", request => request.Headers["foo"]);
            await TaskUtils.WhenAll(
                requestTask,
                Page.EvaluateAsync("() => fetch('/sleep.zzz')")
            );
            Assert.AreEqual("bar", requestTask.Result);
        }

        [PlaywrightTest("page-request-continue.spec.ts", "should amend method on main request")]
        public async Task ShouldAmendMethodOnMainRequest()
        {
            var methodTask = Server.WaitForRequest("/empty.html", r => r.Method);
            await Page.RouteAsync("**/*", (route) => route.ContinueAsync(new() { Method = HttpMethod.Post.Method }));
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual("POST", await methodTask);
        }

        [PlaywrightTest("page-request-continue.spec.ts", "should amend post data")]
        public async Task ShouldAmendPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.RouteAsync("**/*", (route) =>
            {
                route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("doggo") });
            });
            var requestTask = Server.WaitForRequest("/sleep.zzz", request =>
            {
                using StreamReader reader = new StreamReader(request.Body);
                return reader.ReadToEndAsync().GetAwaiter().GetResult();
            });

            await TaskUtils.WhenAll(
                requestTask,
                Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })")
            );
            Assert.AreEqual("doggo", requestTask.Result);
        }
    }
}
