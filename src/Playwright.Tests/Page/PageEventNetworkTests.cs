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
public class PageEventNetworkTests : UpstreamPageTest
{
    [UpstreamTest("NE-084 Page.Events.Request @smoke")]
    public async Task PageEventsRequest()
    {
        var requests = new List<IRequest>();
        Page.Request += (_, request) => requests.Add(request);
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(requests[0].Url, Is.EqualTo(Server.EmptyPage));
        Assert.That(requests[0].ResourceType, Is.EqualTo("document"));
        Assert.That(requests[0].Method, Is.EqualTo("GET"));
        Assert.That(await requests[0].ResponseAsync(), Is.Not.Null);
        Assert.That(requests[0].Frame == Page.MainFrame, Is.EqualTo(true));
        Assert.That(requests[0].Frame.Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("NE-085 Page.Events.Response @smoke")]
    public async Task PageEventsResponse()
    {
        var responses = new List<IResponse>();
        Page.Response += (_, response) => responses.Add(response);
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(responses.Count, Is.EqualTo(1));
        Assert.That(responses[0].Url, Is.EqualTo(Server.EmptyPage));
        Assert.That(responses[0].Status, Is.EqualTo(200));
        Assert.That(responses[0].Ok, Is.EqualTo(true));
        Assert.That(responses[0].Request, Is.Not.Null);
    }

    [UpstreamTest("NE-086 Page.Events.RequestFailed @smoke")]
    public async Task PageEventsRequestFailed()
    {
        Server.SetRoute("/one-style.css", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/css");
            res.Connection.Destroy();
        });
        var failedRequests = new List<IRequest>();
        Page.RequestFailed += (_, request) => failedRequests.Add(request);
        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        Assert.That(failedRequests.Count, Is.EqualTo(1));
        Assert.That(failedRequests[0].Url, Does.Contain("one-style.css"));
        Assert.That(await failedRequests[0].ResponseAsync(), Is.Null);
        Assert.That(failedRequests[0].ResourceType, Is.EqualTo("stylesheet"));
        if (BrowserName == "chromium")
        {
            Assert.That(failedRequests[0].Failure, Is.EqualTo("net::ERR_EMPTY_RESPONSE"));
        }
        else if (BrowserName == "webkit")
        {
            if (Platform == "linux" || Channel == "webkit-wsl")
            {
                Assert.That(failedRequests[0].Failure, Does.Match("(Message Corrupt)|(Connection terminated unexpectedly)").IgnoreCase);
            }
            else if (Platform == "darwin")
            {
                Assert.That(failedRequests[0].Failure, Is.EqualTo("The network connection was lost."));
            }
            else if (Platform == "win32")
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
    public async Task PageEventsRequestFinished()
    {
        var (response, _) = await PromiseAll(
            Page.GotoAsync(Server.EmptyPage),
            Page.WaitForRequestFinishedAsync());
        var request = response.Request;
        Assert.That(request.Url, Is.EqualTo(Server.EmptyPage));
        Assert.That(await request.ResponseAsync(), Is.Not.Null);
        Assert.That(request.Frame == Page.MainFrame, Is.EqualTo(true));
        Assert.That(request.Frame.Url, Is.EqualTo(Server.EmptyPage));
        Assert.That(request.Failure, Is.Null);
    }

    [UpstreamTest("NE-088 should fire events in proper order")]
    public async Task ShouldFireEventsInProperOrder()
    {
        var events = new List<string>();
        Page.Request += (_, request) => events.Add("request");
        Page.Response += (_, response) => events.Add("response");
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(await response.FinishedAsync(), Is.Null);
        events.Add("requestfinished");
        Assert.That(events, Is.EqualTo(new[] { "request", "response", "requestfinished" }));
    }

    [UpstreamTest("NE-089 should support redirects")]
    public async Task ShouldSupportRedirects()
    {
        var FOO_URL = Server.Prefix + "/foo.html";
        var events = new Dictionary<string, List<object>>();
        events[Server.EmptyPage] = [];
        events[FOO_URL] = [];
        Page.Request += (_, request) => events[request.Url].Add(request.Method);
        Page.Response += (_, response) => events[response.Url].Add(response.Status);
        Page.RequestFinished += (_, request) => events[request.Url].Add("DONE");
        Page.RequestFailed += (_, request) => events[request.Url].Add("FAIL");

        Server.SetRedirect("/foo.html", "/empty.html");
        var response = await Page.GotoAsync(FOO_URL);
        await response.FinishedAsync();

        var expected = new Dictionary<string, List<object>>();
        expected[FOO_URL] = ["GET", 302, "DONE"];
        expected[Server.EmptyPage] = ["GET", 200, "DONE"];
        ExpectToEqual(events, expected);
        var redirectedFrom = response.Request.RedirectedFrom;
        Assert.That(redirectedFrom.Url, Does.Contain("/foo.html"));
        Assert.That(redirectedFrom.RedirectedFrom, Is.Null);
        Assert.That(redirectedFrom.RedirectedTo, Is.SameAs(response.Request));
    }

    [UpstreamTest("NE-090 should resolve responses after a cross-process navigation")]
    public async Task ShouldResolveResponsesAfterACrossProcessNavigation()
    {
        var responseFromServerPromise = new TaskCompletionSource<ServerResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        Server.SetRoute("/foo", (message, response) =>
        {
            responseFromServerPromise.SetResult(response);
        });
        await Page.GotoAsync(Server.EmptyPage);
        var requestPromise = Page.WaitForRequestAsync(_ => true);
        // start a long running request and wait until it hits the server
        await Page.EvaluateAsync("url => void fetch(url)", Server.Prefix + "/foo");
        var responseFromServer = await responseFromServerPromise.Task;
        var request = await requestPromise;
        var responsePromise = request.ResponseAsync();
        var failedPromise = Once<IRequest>(h => Page.RequestFailed += h, h => Page.RequestFailed -= h);
        // navigate, which should cancel the request
        await Page.GotoAsync(Server.CrossProcessPrefix);
        // make sure we aren't stalling this request on the server
        responseFromServer.End("done");
        // the response should resolve to null, because the page navigated.
        Assert.That(await responsePromise, Is.Null);
        // the request should fail with the browser's "canceled" error.
        Assert.That(await failedPromise, Is.SameAs(request));
        Assert.That(request.Failure, Does.Contain(new Dictionary<string, string> { ["chromium"] = "net::ERR_ABORTED", ["firefox"] = "NS_BINDING_ABORTED", ["webkit"] = "cancelled" }[BrowserName]));
    }

    [UpstreamTest("NE-143 should resolve responses after a same-process navigation")]
    public async Task ShouldResolveResponsesAfterASameProcessNavigation()
    {
        var responseFromServerPromise = new TaskCompletionSource<ServerResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        Server.SetRoute("/foo", (message, response) =>
        {
            responseFromServerPromise.SetResult(response);
        });
        Server.SetRoute("/streaming", (message, response) =>
        {
            response.WriteHead(200, new() { ["Content-Type"] = "text/plain" });
            response.Write("partial body");
        });
        await Page.GotoAsync(Server.EmptyPage);
        var requestPromise = Page.WaitForRequestAsync(Server.Prefix + "/foo");
        var streamingResponsePromise = Page.WaitForResponseAsync(Server.Prefix + "/streaming");
        // start a long running request and wait until it hits the server
        await Page.EvaluateAsync("url => void fetch(url)", Server.Prefix + "/foo");
        // start a request that never finishes streaming its body
        await Page.EvaluateAsync("url => void fetch(url)", Server.Prefix + "/streaming");
        var responseFromServer = await responseFromServerPromise.Task;
        var request = await requestPromise;
        var responsePromise = request.ResponseAsync();
        var streamingResponse = await streamingResponsePromise;
        var streamingTextPromise = Catch(() => streamingResponse.TextAsync());
        var failedRequests = new List<IRequest>();
        // .NET: the parameter of the handler is `request` upstream, which is a variable of the test here.
        Page.RequestFailed += (_, r) => failedRequests.Add(r);
        // navigate within the same process, which should cancel the requests
        await Page.GotoAsync(Server.Prefix + "/title.html");
        // make sure we aren't stalling this request on the server
        responseFromServer.End("done");
        // the response should resolve to null, because the page navigated.
        Assert.That(await responsePromise, Is.Null);
        // both requests should fail with the browser's "canceled" error.
        var errorText = new Dictionary<string, string> { ["chromium"] = "net::ERR_ABORTED", ["firefox"] = "NS_BINDING_ABORTED", ["webkit"] = "cancelled" }[BrowserName];
        Assert.That(failedRequests.Select(r => r.Url).Order(StringComparer.Ordinal), Is.EqualTo(new[] { Server.Prefix + "/foo", Server.Prefix + "/streaming" }));
        Assert.That(request.Failure, Does.Contain(errorText));
        Assert.That(streamingResponse.Request.Failure, Does.Contain(errorText));
        // the partial body should not be available anymore.
        Assert.That((await streamingTextPromise).Message, Does.Contain("navigated away"));
    }
}
