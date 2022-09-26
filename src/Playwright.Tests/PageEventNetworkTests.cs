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

using System.Globalization;
using System.Net;
using Microsoft.Playwright.Tests.TestServer;

namespace Microsoft.Playwright.Tests;

public class PageEventNetworkTests : PageTestEx
{
    [PlaywrightTest("page-event-network.spec.ts", "Page.Events.Request")]
    public async Task PageEventsRequest()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, e) => requests.Add(e);
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(requests, Has.Count.EqualTo(1));
        Assert.AreEqual(Server.EmptyPage, requests[0].Url);
        Assert.AreEqual("document", requests[0].ResourceType);
        Assert.AreEqual(HttpMethod.Get.Method, requests[0].Method);
        Assert.NotNull(await requests[0].ResponseAsync());
        Assert.AreEqual(Page.MainFrame, requests[0].Frame);
        Assert.AreEqual(Server.EmptyPage, requests[0].Frame.Url);
    }

    [PlaywrightTest("page-event-network.spec.ts", "Page.Events.Response")]
    public async Task PageEventsResponse()
    {
        var responses = new List<IResponse>();
        Page.Response += (_, e) => responses.Add(e);
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(responses, Has.Count.EqualTo(1));
        Assert.AreEqual(Server.EmptyPage, responses[0].Url);
        Assert.AreEqual((int)HttpStatusCode.OK, responses[0].Status);
        Assert.True(responses[0].Ok);
        Assert.NotNull(responses[0].Request);
    }

    [PlaywrightTest("page-event-network.spec.ts", "Page.Events.RequestFailed")]
    public async Task PageEventsRequestFailed()
    {
        int port = Server.Port + 100;
        var disposableServer = new SimpleServer(port, Path.Combine(TestUtils.FindParentDirectory("Playwright.Tests.TestServer"), "assets"), false);
        await disposableServer.StartAsync();

        disposableServer.SetRoute("/one-style.css", async _ =>
        {
            await disposableServer.StopAsync();
        });
        var failedRequests = new List<IRequest>();

        Page.RequestFailed += (_, e) => failedRequests.Add(e);

        await Page.GotoAsync($"http://localhost:{port}/one-style.html");

        Assert.That(failedRequests, Has.Count.EqualTo(1));
        StringAssert.Contains("one-style.css", failedRequests[0].Url);
        Assert.Null(await failedRequests[0].ResponseAsync());
        Assert.AreEqual("stylesheet", failedRequests[0].ResourceType);

        //We just need to test that we had a failure.
        Assert.NotNull(failedRequests[0].Failure);
        Assert.NotNull(failedRequests[0].Frame);
    }

    [PlaywrightTest("page-event-network.spec.ts", "Page.Events.RequestFinished")]
    public async Task PageEventsRequestFinished()
    {
        var (_, response) = await TaskUtils.WhenAll(
            Page.WaitForRequestFinishedAsync(),
            Page.GotoAsync(Server.EmptyPage));

        var request = response.Request;
        Assert.AreEqual(Server.EmptyPage, request.Url);
        Assert.NotNull(await request.ResponseAsync());
        Assert.AreEqual(HttpMethod.Get.Method, request.Method);
        Assert.AreEqual(Page.MainFrame, request.Frame);
        Assert.AreEqual(Server.EmptyPage, request.Frame.Url);
    }

    [PlaywrightTest("page-event-network.spec.ts", "should fire events in proper order")]
    public async Task ShouldFireEventsInProperOrder()
    {
        var events = new List<string>();
        Page.Request += (_, _) => events.Add("request");
        Page.Response += (_, _) => events.Add("response");
        var response = await Page.GotoAsync(Server.EmptyPage);
        await response.FinishedAsync();
        events.Add("requestfinished");
        Assert.AreEqual(new[] { "request", "response", "requestfinished" }, events);
    }

    [PlaywrightTest("page-event-network.spec.ts", "should support redirects")]
    public async Task ShouldSupportRedirects()
    {
        string FOO_URL = Server.Prefix + "/foo.html";
        string EMPTY_URL = Server.EmptyPage;

        var events = new Dictionary<string, List<string>>()
        {
            [FOO_URL] = new(),
            [EMPTY_URL] = new()
        };

        Page.Request += (_, e) => events[e.Url].Add(e.Method);
        Page.Response += (_, e) => events[e.Url].Add(e.Status.ToString(CultureInfo.InvariantCulture));
        Page.RequestFinished += (_, e) => events[e.Url].Add("DONE");
        Page.RequestFailed += (_, e) => events[e.Url].Add("FAIL");
        Server.SetRedirect("/foo.html", "/empty.html");
        var response = await Page.GotoAsync(FOO_URL);
        await response.FinishedAsync();

        var expected = new Dictionary<string, List<string>>
        {
            [FOO_URL] = new() { "GET", "302", "DONE" },
            [EMPTY_URL] = new() { "GET", "200", "DONE" }
        };

        Assert.AreEqual(expected, events);

        // Check redirect chain
        var redirectedFrom = response.Request.RedirectedFrom;

        StringAssert.Contains("/foo.html", redirectedFrom.Url);
        Assert.NotNull(redirectedFrom.RedirectedTo);
        Assert.AreEqual(response.Request, redirectedFrom.RedirectedTo);
    }
}
