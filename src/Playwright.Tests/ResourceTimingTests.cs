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

using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

///<playwright-file>resource-timing.spec.ts</playwright-file>
public class ResourceTimingTests : ContextTestEx
{
    private void VerifyConnectionTimingConsistency(RequestTimingResult timing)
    {
        static void verifyTimingValue(float value, float previous)
        {
            Assert.IsTrue(value == -1 || value > 0 && value >= previous);
        }

        verifyTimingValue(timing.DomainLookupStart, -1);
        verifyTimingValue(timing.DomainLookupEnd, timing.DomainLookupStart);
        verifyTimingValue(timing.ConnectStart, timing.DomainLookupEnd);
        verifyTimingValue(timing.SecureConnectionStart, timing.ConnectStart);
        verifyTimingValue(timing.ConnectEnd, timing.SecureConnectionStart);
    }

    [PlaywrightTest("resource-timing.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await using var context = await NewContext();
        var page = await context.NewPageAsync();
        var (request, _) = await TaskUtils.WhenAll(
            page.WaitForRequestFinishedAsync(),
            page.GotoAsync(Server.EmptyPage));

        var timing = request.Timing;

        VerifyConnectionTimingConsistency(timing);
        Assert.GreaterOrEqual(timing.RequestStart, timing.ConnectEnd);
        Assert.GreaterOrEqual(timing.ResponseStart, timing.RequestStart);
        Assert.GreaterOrEqual(timing.ResponseEnd, timing.ResponseStart);
        Assert.Less(timing.ResponseEnd, 10000);
    }

    [PlaywrightTest("resource-timing.spec.ts", "should work for subresource")]
    public async Task ShouldWorkForSubresource()
    {
        await using var context = await NewContext();
        var page = await context.NewPageAsync();
        var requests = new List<IRequest>();

        page.RequestFinished += (_, e) => requests.Add(e);
        await page.GotoAsync(Server.Prefix + "/one-style.html");

        Assert.AreEqual(2, requests.Count);

        var timing = requests[1].Timing;

        VerifyConnectionTimingConsistency(timing);

        Assert.GreaterOrEqual(timing.RequestStart, 0);
        Assert.GreaterOrEqual(timing.ResponseStart, timing.RequestStart);
        Assert.GreaterOrEqual(timing.ResponseEnd, timing.ResponseStart);
        Assert.Less(timing.ResponseEnd, 10000);
    }

    [PlaywrightTest("resource-timing.spec.ts", "should work for SSL")]
    public async Task ShouldWorkForSSL()
    {
        await using var context = await NewContext(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        var (request, _) = await TaskUtils.WhenAll(
            page.WaitForRequestFinishedAsync(),
            page.GotoAsync(HttpsServer.Prefix + "/empty.html"));

        var timing = request.Timing;
        VerifyConnectionTimingConsistency(timing);
        Assert.GreaterOrEqual(timing.RequestStart, timing.ConnectEnd);
        Assert.GreaterOrEqual(timing.ResponseStart, timing.RequestStart);
        Assert.GreaterOrEqual(timing.ResponseEnd, timing.ResponseStart);
        Assert.Less(timing.ResponseEnd, 10000);
    }

    [PlaywrightTest("resource-timing.spec.ts", "should work for redirect")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldWorkForRedirect()
    {
        await using var context = await NewContext();
        var page = await context.NewPageAsync();
        Server.SetRedirect("/foo.html", "/empty.html");
        var responses = new List<IResponse>();

        page.Response += (_, e) => responses.Add(e);
        await page.GotoAsync(Server.Prefix + "/foo.html");

        // This is different on purpose, promises work different in TS.
        await responses[1].FinishedAsync();

        Assert.AreEqual(2, responses.Count);
        Assert.AreEqual(Server.Prefix + "/foo.html", responses[0].Url);
        Assert.AreEqual(Server.Prefix + "/empty.html", responses[1].Url);

        var timing1 = responses[0].Request.Timing;
        VerifyConnectionTimingConsistency(timing1);
        Assert.GreaterOrEqual(timing1.RequestStart, timing1.ConnectEnd);
        Assert.GreaterOrEqual(timing1.ResponseStart, timing1.RequestStart);
        Assert.GreaterOrEqual(timing1.ResponseEnd, timing1.ResponseStart);
        Assert.Less(timing1.ResponseEnd, 10000);


        var timing2 = responses[1].Request.Timing;
        VerifyConnectionTimingConsistency(timing2);
        Assert.GreaterOrEqual(timing2.RequestStart, timing2.ConnectEnd);
        Assert.GreaterOrEqual(timing2.ResponseStart, timing2.RequestStart);
        Assert.GreaterOrEqual(timing2.ResponseEnd, timing2.ResponseStart);
        Assert.Less(timing2.ResponseEnd, 10000);
    }

    [PlaywrightTest("resource-timing.spec.ts", "should work when serving from memory cache")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWorkWhenServingFromMemoryCache()
    {
        Server.SetRoute("/one-style.css", async (ctx) =>
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/css";
            ctx.Response.Headers["Cache-Control"] = "public, max-age=10031518";
            await ctx.Response.WriteAsync("body { background: red }");
        });

        await using var context = await NewContext();
        var page = await context.NewPageAsync();

        await page.GotoAsync(Server.Prefix + "/one-style.html");
        var (response, _) = await TaskUtils.WhenAll(
            page.WaitForResponseAsync("**/one-style.css"),
            page.ReloadAsync());

        await response.FinishedAsync();

        var timing = response.Request.Timing;
        VerifyConnectionTimingConsistency(timing);

        Assert.AreEqual(timing.ResponseStart, timing.ResponseEnd);
        Assert.Less(timing.ResponseEnd, 1000);
    }

}
