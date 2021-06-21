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
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>resource-timing.spec.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class ResourceTimingTests : PageTestEx
    {
        [PlaywrightTest("resource-timing.spec.ts", "should work")]
        [Test]
        public async Task ShouldWork()
        {
            var (request, _) = await TaskUtils.WhenAll(
                Page.WaitForRequestFinishedAsync(),
                Page.GotoAsync(Server.EmptyPage));

            var timing = request.Timing;
            Assert.True(timing.DomainLookupStart >= -1);
            Assert.True(timing.DomainLookupEnd >= timing.DomainLookupStart);
            Assert.True(timing.ConnectStart >= timing.DomainLookupEnd);
            Assert.AreEqual(-1, timing.SecureConnectionStart);
            Assert.True(VerifyTimingValue(timing.ConnectEnd, timing.SecureConnectionStart));
            Assert.True(VerifyTimingValue(timing.RequestStart, timing.ConnectEnd));
            Assert.True(timing.ResponseStart > timing.RequestStart);
            Assert.True(timing.ResponseEnd >= timing.ResponseStart);
            Assert.True(timing.ResponseEnd < 10000);
        }

        [PlaywrightTest("resource-timing.spec.ts", "should work for subresource")]
        [Test]
        public async Task ShouldWorkForSubresource()
        {
            var requests = new List<IRequest>();

            Page.RequestFinished += (_, e) => requests.Add(e);
            await Page.GotoAsync(Server.Prefix + "/one-style.html");

            Assert.AreEqual(2, requests.Count);

            var timing = requests[1].Timing;
            if (TestConstants.IsWebKit && TestConstants.IsWindows)
            {
                Assert.True(timing.DomainLookupStart >= 0);
                Assert.True(timing.DomainLookupEnd >= timing.DomainLookupStart);
                Assert.True(timing.ConnectStart >= timing.DomainLookupEnd);
                Assert.AreEqual(-1, timing.SecureConnectionStart);
                Assert.True(timing.ConnectEnd > timing.SecureConnectionStart);
            }
            else
            {
                Assert.True(timing.DomainLookupStart == 0 || timing.DomainLookupStart == -1);
                Assert.AreEqual(-1, timing.DomainLookupEnd);
                Assert.AreEqual(-1, timing.ConnectStart);
                Assert.AreEqual(-1, timing.SecureConnectionStart);
                Assert.AreEqual(-1, timing.ConnectEnd);
            }

            Assert.True(timing.RequestStart >= 0);
            Assert.True(timing.ResponseStart >= timing.RequestStart);
            Assert.True(timing.ResponseEnd >= timing.ResponseStart);
            Assert.True(timing.ResponseEnd < 10000);
        }

        [PlaywrightTest("resource-timing.spec.ts", "should work for SSL")]
        [Test, Ignore("Fix me #1058")]
        public async Task ShouldWorkForSSL()
        {
            var page = await Browser.NewPageAsync(new() { IgnoreHTTPSErrors = true });
            var (request, _) = await TaskUtils.WhenAll(
                page.WaitForRequestFinishedAsync(),
                page.GotoAsync(HttpsServer.Prefix + "/empty.html"));

            var timing = request.Timing;
            if (!(TestConstants.IsWebKit && TestConstants.IsMacOSX))
            {
                Assert.True(timing.DomainLookupStart >= 0);
                Assert.True(timing.DomainLookupEnd >= timing.DomainLookupStart);
                Assert.True(timing.ConnectStart >= timing.DomainLookupEnd);
                Assert.True(timing.SecureConnectionStart >= timing.ConnectStart);
                Assert.True(timing.ConnectEnd > timing.SecureConnectionStart);
                Assert.True(timing.RequestStart >= timing.ConnectEnd);
            }

            Assert.True(timing.ResponseStart > timing.RequestStart);
            Assert.True(timing.ResponseEnd >= timing.ResponseStart);
            Assert.True(timing.ResponseEnd < 10000);
            await page.CloseAsync();
        }

        [PlaywrightTest("resource-timing.spec.ts", "should work for redirect")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldWorkForRedirect()
        {
            Server.SetRedirect("/foo.html", "/empty.html");
            var responses = new List<IResponse>();

            Page.Response += (_, e) => responses.Add(e);
            await Page.GotoAsync(Server.Prefix + "/foo.html");

            // This is different on purpose, promises work different in TS.
            await responses[1].FinishedAsync();

            Assert.AreEqual(2, responses.Count);
            Assert.AreEqual(Server.Prefix + "/foo.html", responses[0].Url);
            Assert.AreEqual(Server.Prefix + "/empty.html", responses[1].Url);

            var timing1 = responses[0].Request.Timing;
            Assert.True(timing1.DomainLookupStart >= 0);
            Assert.True(timing1.DomainLookupEnd >= timing1.DomainLookupStart);
            Assert.True(timing1.ConnectStart >= timing1.DomainLookupEnd);
            Assert.AreEqual(-1, timing1.SecureConnectionStart);
            Assert.True(timing1.ConnectEnd > timing1.SecureConnectionStart);
            Assert.True(timing1.RequestStart >= timing1.ConnectEnd);
            Assert.True(timing1.ResponseStart > timing1.RequestStart);
            Assert.True(timing1.ResponseEnd >= timing1.ResponseStart);
            Assert.True(timing1.ResponseEnd < 10000);

            var timing2 = responses[1].Request.Timing;
            Assert.AreEqual(-1, timing2.DomainLookupStart);
            Assert.AreEqual(-1, timing2.DomainLookupEnd);
            Assert.AreEqual(-1, timing2.ConnectStart);
            Assert.AreEqual(-1, timing2.SecureConnectionStart);
            Assert.AreEqual(-1, timing2.ConnectEnd);
            Assert.True(timing2.RequestStart >= 0);
            Assert.True(timing2.ResponseStart >= timing2.RequestStart);
            Assert.True(timing2.ResponseEnd >= timing2.ResponseStart);
            Assert.True(timing2.ResponseEnd < 10000);
        }

        private bool VerifyTimingValue(float value, float previous) => value == -1 || value > 0 && value >= previous;
    }
}
