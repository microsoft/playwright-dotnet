/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

namespace Microsoft.Playwright.Tests;

public class PageEventRequestTests : PageTestEx
{
    [PlaywrightTest("page-event-request.spec.ts", "should fire for navigation requests")]
    public async Task ShouldFireForNavigationRequests()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, e) => requests.Add(e);
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(requests, Has.Count.EqualTo(1));
    }

    [PlaywrightTest("page-event-request.spec.ts", "should fire for iframes")]
    public async Task ShouldFireForIframes()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, e) => requests.Add(e);
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        Assert.AreEqual(2, requests.Count);
    }

    [PlaywrightTest("page-event-request.spec.ts", "should fire for fetches")]
    public async Task ShouldFireForFetches()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, e) => requests.Add(e);
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync("fetch('/empty.html')");
        Assert.AreEqual(2, requests.Count);
    }

    [PlaywrightTest("page-event-request.spec.ts", "should report requests and responses handled by service worker")]
    public async Task ShouldReportRequestsAndResponsesHandledByServiceWorker()
    {
        await Page.GotoAsync(Server.Prefix + "/serviceworkers/fetchdummy/sw.html");
        await Page.EvaluateAsync("() => window.activationPromise");

        var (request, swResponse) = await TaskUtils.WhenAll(
            Page.WaitForRequestAsync("**/*"),
            Page.EvaluateAsync<string>("() => fetchDummy('foo')"));

        Assert.AreEqual("responseFromServiceWorker:foo", swResponse);
        Assert.AreEqual(Server.Prefix + "/serviceworkers/fetchdummy/foo", request.Url);
        var response = await request.ResponseAsync();
        Assert.AreEqual(Server.Prefix + "/serviceworkers/fetchdummy/foo", response.Url);
        Assert.AreEqual("responseFromServiceWorker:foo", await response.TextAsync());
    }

    [PlaywrightTest("page-event-request.spec.ts", "should return last requests")]
    public async Task ShouldReturnLastRequests()
    {
        await Page.GotoAsync(Server.Prefix + "/title.html");

        // Set up routes for 200 requests
        for (int i = 0; i < 200; ++i)
        {
            Server.SetRoute("/fetch-" + i, context =>
            {
                context.Response.StatusCode = 200;
                return context.Response.WriteAsync("url:" + Server.Prefix + context.Request.Path);
            });
        }

        // #0 is the navigation request, so start with #1.
        for (int i = 0; i < 99; ++i)
        {
            await Page.EvaluateAsync("url => fetch(url)", Server.Prefix + "/fetch-" + i);
        }
        var first99Requests = (await Page.RequestsAsync()).ToList();
        first99Requests.RemoveAt(0); // Remove the navigation request

        for (int i = 99; i < 199; ++i)
        {
            await Page.EvaluateAsync("url => fetch(url)", Server.Prefix + "/fetch-" + i);
        }
        var last100Requests = await Page.RequestsAsync();
        var allRequests = new List<IRequest>();
        allRequests.AddRange(first99Requests);
        allRequests.AddRange(last100Requests);

        // All 199 requests are fully functional.
        int index = 0;
        foreach (var request in allRequests)
        {
            var response = await request.ResponseAsync();
            Assert.AreEqual("url:" + Server.Prefix + "/fetch-" + index, await response.TextAsync());
            Assert.AreEqual(Server.Prefix + "/fetch-" + index, request.Url);
            index++;
        }
    }
}
