/**
 * Copyright 2018 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-event-network.spec.ts.</summary>
public class PageEventNetworkTests
{
    [UpstreamTest("NE-084 Page.Events.Request @smoke")]
    public async Task PageEventsRequest(IPage page, Server server)
    {
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        await page.GotoAsync(server.EmptyPage);
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(requests[0].Url, Is.EqualTo(server.EmptyPage));
        Assert.That(requests[0].ResourceType, Is.EqualTo("document"));
        Assert.That(requests[0].Method, Is.EqualTo("GET"));
        Assert.That(await requests[0].ResponseAsync(), Is.Not.Null);
        Assert.That(requests[0].Frame == page.MainFrame, Is.EqualTo(true));
        Assert.That(requests[0].Frame.Url, Is.EqualTo(server.EmptyPage));
    }

    [UpstreamTest("NE-085 Page.Events.Response @smoke")]
    public async Task PageEventsResponse(IPage page, Server server)
    {
        var responses = new List<IResponse>();
        page.Response += (_, response) => responses.Add(response);
        await page.GotoAsync(server.EmptyPage);
        Assert.That(responses.Count, Is.EqualTo(1));
        Assert.That(responses[0].Url, Is.EqualTo(server.EmptyPage));
        Assert.That(responses[0].Status, Is.EqualTo(200));
        Assert.That(responses[0].Ok, Is.EqualTo(true));
        Assert.That(responses[0].Request, Is.Not.Null);
    }

    [UpstreamTest("NE-086 Page.Events.RequestFailed @smoke")]
    public async Task PageEventsRequestFailed(IPage page, Server server, string browserName, string platform, string channel)
    {
        server.SetRoute("/one-style.css", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/css");
            res.Connection.Destroy();
        });
        var failedRequests = new List<IRequest>();
        page.RequestFailed += (_, request) => failedRequests.Add(request);
        await page.GotoAsync(server.Prefix + "/one-style.html");
        Assert.That(failedRequests.Count, Is.EqualTo(1));
        Assert.That(failedRequests[0].Url, Does.Contain("one-style.css"));
        Assert.That(await failedRequests[0].ResponseAsync(), Is.Null);
        Assert.That(failedRequests[0].ResourceType, Is.EqualTo("stylesheet"));
        if (browserName == "chromium")
        {
            Assert.That(failedRequests[0].Failure, Is.EqualTo("net::ERR_EMPTY_RESPONSE"));
        }
        else if (browserName == "webkit")
        {
            if (platform == "linux" || channel == "webkit-wsl")
            {
                Assert.That(failedRequests[0].Failure, Does.Match("(Message Corrupt)|(Connection terminated unexpectedly)").IgnoreCase);
            }
            else if (platform == "darwin")
            {
                Assert.That(failedRequests[0].Failure, Is.EqualTo("The network connection was lost."));
            }
            else if (platform == "win32")
            {
                Assert.That(failedRequests[0].Failure, Is.EqualTo("Server returned nothing (no headers, no data)"));
            }
        }
        else
        {
            Assert.That(failedRequests[0].Failure, Is.EqualTo("NS_ERROR_NET_RESET"));
        }
        Assert.That(failedRequests[0].Frame, Is.Not.Null);
    }

    [UpstreamTest("NE-087 Page.Events.RequestFinished @smoke")]
    public async Task PageEventsRequestFinished(IPage page, Server server)
    {
        var (response, _) = await PromiseAll(
            page.GotoAsync(server.EmptyPage),
            page.WaitForRequestFinishedAsync());
        var request = response.Request;
        Assert.That(request.Url, Is.EqualTo(server.EmptyPage));
        Assert.That(await request.ResponseAsync(), Is.Not.Null);
        Assert.That(request.Frame == page.MainFrame, Is.EqualTo(true));
        Assert.That(request.Frame.Url, Is.EqualTo(server.EmptyPage));
        Assert.That(request.Failure, Is.Null);
    }

    [UpstreamTest("NE-088 should fire events in proper order")]
    public async Task ShouldFireEventsInProperOrder(IPage page, Server server)
    {
        var events = new List<string>();
        page.Request += (_, request) => events.Add("request");
        page.Response += (_, response) => events.Add("response");
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(await response.FinishedAsync(), Is.Null);
        events.Add("requestfinished");
        Assert.That(events, Is.EqualTo(new[] { "request", "response", "requestfinished" }));
    }

    [UpstreamTest("NE-089 should support redirects")]
    public async Task ShouldSupportRedirects(IPage page, Server server)
    {
        var FOO_URL = server.Prefix + "/foo.html";
        var events = new Dictionary<string, List<object>>();
        events[server.EmptyPage] = [];
        events[FOO_URL] = [];
        page.Request += (_, request) => events[request.Url].Add(request.Method);
        page.Response += (_, response) => events[response.Url].Add(response.Status);
        page.RequestFinished += (_, request) => events[request.Url].Add("DONE");
        page.RequestFailed += (_, request) => events[request.Url].Add("FAIL");

        server.SetRedirect("/foo.html", "/empty.html");
        var response = await page.GotoAsync(FOO_URL);
        await response.FinishedAsync();

        var expected = new Dictionary<string, List<object>>();
        expected[FOO_URL] = ["GET", 302, "DONE"];
        expected[server.EmptyPage] = ["GET", 200, "DONE"];
        ExpectToEqual(events, expected);
        var redirectedFrom = response.Request.RedirectedFrom;
        Assert.That(redirectedFrom.Url, Does.Contain("/foo.html"));
        Assert.That(redirectedFrom.RedirectedFrom, Is.Null);
        Assert.That(redirectedFrom.RedirectedTo, Is.SameAs(response.Request));
    }

    [UpstreamTest("NE-090 should resolve responses after a cross-process navigation")]
    public async Task ShouldResolveResponsesAfterACrossProcessNavigation(IPage page, Server server, string browserName)
    {
        var responseFromServerPromise = new TaskCompletionSource<ServerResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/foo", (message, response) =>
        {
            responseFromServerPromise.SetResult(response);
        });
        await page.GotoAsync(server.EmptyPage);
        var requestPromise = page.WaitForRequestAsync(_ => true);
        // start a long running request and wait until it hits the server
        await page.EvaluateAsync("url => void fetch(url)", server.Prefix + "/foo");
        var responseFromServer = await responseFromServerPromise.Task;
        var request = await requestPromise;
        var responsePromise = request.ResponseAsync();
        var failedPromise = Once<IRequest>(h => page.RequestFailed += h, h => page.RequestFailed -= h);
        // navigate, which should cancel the request
        await page.GotoAsync(server.CrossProcessPrefix);
        // make sure we aren't stalling this request on the server
        responseFromServer.End("done");
        // the response should resolve to null, because the page navigated.
        Assert.That(await responsePromise, Is.Null);
        // the request should fail with the browser's "canceled" error.
        Assert.That(await failedPromise, Is.SameAs(request));
        Assert.That(request.Failure, Does.Contain(new Dictionary<string, string> { ["chromium"] = "net::ERR_ABORTED", ["firefox"] = "NS_BINDING_ABORTED", ["webkit"] = "cancelled" }[browserName]));
    }

    [UpstreamTest("NE-143 should resolve responses after a same-process navigation")]
    public async Task ShouldResolveResponsesAfterASameProcessNavigation(IPage page, Server server, string browserName)
    {
        var responseFromServerPromise = new TaskCompletionSource<ServerResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/foo", (message, response) =>
        {
            responseFromServerPromise.SetResult(response);
        });
        server.SetRoute("/streaming", (message, response) =>
        {
            response.WriteHead(200, new() { ["Content-Type"] = "text/plain" });
            response.Write("partial body");
        });
        await page.GotoAsync(server.EmptyPage);
        var requestPromise = page.WaitForRequestAsync(server.Prefix + "/foo");
        var streamingResponsePromise = page.WaitForResponseAsync(server.Prefix + "/streaming");
        // start a long running request and wait until it hits the server
        await page.EvaluateAsync("url => void fetch(url)", server.Prefix + "/foo");
        // start a request that never finishes streaming its body
        await page.EvaluateAsync("url => void fetch(url)", server.Prefix + "/streaming");
        var responseFromServer = await responseFromServerPromise.Task;
        var request = await requestPromise;
        var responsePromise = request.ResponseAsync();
        var streamingResponse = await streamingResponsePromise;
        var streamingTextPromise = Catch(() => streamingResponse.TextAsync());
        var failedRequests = new List<IRequest>();
        // .NET: the parameter of the handler is `request` upstream, which is a variable of the test here.
        page.RequestFailed += (_, r) => failedRequests.Add(r);
        // navigate within the same process, which should cancel the requests
        await page.GotoAsync(server.Prefix + "/title.html");
        // make sure we aren't stalling this request on the server
        responseFromServer.End("done");
        // the response should resolve to null, because the page navigated.
        Assert.That(await responsePromise, Is.Null);
        // both requests should fail with the browser's "canceled" error.
        var errorText = new Dictionary<string, string> { ["chromium"] = "net::ERR_ABORTED", ["firefox"] = "NS_BINDING_ABORTED", ["webkit"] = "cancelled" }[browserName];
        Assert.That(failedRequests.Select(r => r.Url).Order(StringComparer.Ordinal), Is.EqualTo(new[] { server.Prefix + "/foo", server.Prefix + "/streaming" }));
        Assert.That(request.Failure, Does.Contain(errorText));
        Assert.That(streamingResponse.Request.Failure, Does.Contain(errorText));
        // the partial body should not be available anymore.
        Assert.That((await streamingTextPromise).Message, Does.Contain("navigated away"));
    }
}
