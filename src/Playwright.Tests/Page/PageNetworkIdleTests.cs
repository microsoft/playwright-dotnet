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

using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-network-idle.spec.ts.</summary>
public class PageNetworkIdleTests
{
    [UpstreamTest("NE-070 should navigate to empty page with networkidle")]
    public async Task ShouldNavigateToEmptyPageWithNetworkidle(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.EmptyPage, new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.That(response.Status, Is.EqualTo(200));
    }

    private static async Task NetworkIdleTest(IFrame frame, Server server, Func<Task> action, bool isSetContent = false)
    {
        var waitForRequest = (string suffix) =>
        {
            return PromiseAll(
                server.WaitForRequest(suffix),
                frame.Page.WaitForRequestAsync(server.Prefix + suffix));
        };

        ServerResponse responseA = null;
        ServerResponse responseB = null;
        // Hold on to a bunch of requests without answering.
        server.SetRoute("/fetch-request-a.js", (req, res) => responseA = res);
        var firstFetchResourceRequested = waitForRequest("/fetch-request-a.js");
        server.SetRoute("/fetch-request-b.js", (req, res) => responseB = res);
        var secondFetchResourceRequested = waitForRequest("/fetch-request-b.js");

        var waitForLoadPromise = isSetContent ? Task.CompletedTask : frame.WaitForNavigationAsync(new() { WaitUntil = WaitUntilState.Load });

        // Navigate to a page which loads immediately and then does a bunch of
        // requests via javascript's fetch method.
        var actionPromise = action();

        // Track when the action gets completed.
        var actionFinished = false;
        // .NET: actionPromise.then(() => actionFinished = true) is a local function that is called in its place.
        async Task TrackAction()
        {
            await actionPromise;
            actionFinished = true;
        }
        IgnoreErrors(TrackAction());

        // Wait for the frame's 'load' event.
        await waitForLoadPromise;
        Assert.That(actionFinished, Is.EqualTo(false));

        // Wait for the initial resource to be requested.
        await firstFetchResourceRequested;
        Assert.That(actionFinished, Is.EqualTo(false));

        // Trigger the second request.
        await frame.Page.EvaluateAsync("() => window['fetchSecond']()");
        // Finish the first request.
        responseA.StatusCode = 404;
        responseA.End("File not found");

        // Wait for the second round to be requested.
        await secondFetchResourceRequested;
        Assert.That(actionFinished, Is.EqualTo(false));

        // Finishing the second response should trigger networkidle.
        var timerTriggered = false;
        var timer = SetTimeout(() => timerTriggered = true, 500);
        responseB.StatusCode = 404;
        responseB.End("File not found");

        // .NET: const response = await actionPromise: the action gives a response, or nothing for setContent.
        await actionPromise;
        var response = actionPromise is Task<IResponse> responsePromise ? await responsePromise : null;
        ClearTimeout(timer);
        Assert.That(timerTriggered, Is.EqualTo(true));
        if (!isSetContent)
        {
            Assert.That(response.Ok, Is.EqualTo(true));
        }
    }

    [UpstreamTest("NE-071 should wait for networkidle to succeed navigation")]
    public async Task ShouldWaitForNetworkidleToSucceedNavigation(IPage page, Server server)
    {
        await NetworkIdleTest(page.MainFrame, server, () =>
        {
            return page.GotoAsync(server.Prefix + "/networkidle.html", new() { WaitUntil = WaitUntilState.NetworkIdle });
        });
    }

    [UpstreamTest("NE-072 should wait for networkidle to succeed navigation with request from previous navigation")]
    public async Task ShouldWaitForNetworkidleToSucceedNavigationWithRequestFromPreviousNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/foo.js", (req, res) => { });
        await page.SetContentAsync("<script>fetch('foo.js');</script>");
        await NetworkIdleTest(page.MainFrame, server, () =>
        {
            return page.GotoAsync(server.Prefix + "/networkidle.html", new() { WaitUntil = WaitUntilState.NetworkIdle });
        });
    }

    [UpstreamTest("NE-073 should wait for networkidle in waitForNavigation")]
    public async Task ShouldWaitForNetworkidleInWaitForNavigation(IPage page, Server server)
    {
        await NetworkIdleTest(page.MainFrame, server, () =>
        {
            var promise = page.WaitForNavigationAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
            IgnoreErrors(page.GotoAsync(server.Prefix + "/networkidle.html"));
            return promise;
        });
    }

    [UpstreamTest("NE-074 should wait for networkidle in setContent")]
    public async Task ShouldWaitForNetworkidleInSetContent(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await NetworkIdleTest(page.MainFrame, server, () =>
        {
            return page.SetContentAsync("<script src='networkidle.js'></script>", new() { WaitUntil = WaitUntilState.NetworkIdle });
        }, true);
    }

    [UpstreamTest("NE-075 should wait for networkidle in setContent with request from previous navigation")]
    public async Task ShouldWaitForNetworkidleInSetContentWithRequestFromPreviousNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/foo.js", (req, res) => { });
        await page.SetContentAsync("<script>fetch('foo.js');</script>");
        await NetworkIdleTest(page.MainFrame, server, () =>
        {
            return page.SetContentAsync("<script src='networkidle.js'></script>", new() { WaitUntil = WaitUntilState.NetworkIdle });
        }, true);
    }

    [UpstreamTest("NE-076 should wait for networkidle when navigating iframe")]
    public async Task ShouldWaitForNetworkidleWhenNavigatingIframe(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        var frame = page.MainFrame.ChildFrames[0];
        await NetworkIdleTest(frame, server, () => frame.GotoAsync(server.Prefix + "/networkidle.html", new() { WaitUntil = WaitUntilState.NetworkIdle }));
    }

    [UpstreamTest("NE-077 should wait for networkidle in setContent from the child frame")]
    public async Task ShouldWaitForNetworkidleInSetContentFromTheChildFrame(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await NetworkIdleTest(page.MainFrame, server, () =>
        {
            return page.SetContentAsync("<iframe src='networkidle.html'></iframe>", new() { WaitUntil = WaitUntilState.NetworkIdle });
        }, true);
    }

    [UpstreamTest("NE-078 should wait for networkidle from the child frame")]
    public async Task ShouldWaitForNetworkidleFromTheChildFrame(IPage page, Server server)
    {
        await NetworkIdleTest(page.MainFrame, server, () =>
        {
            return page.GotoAsync(server.Prefix + "/networkidle-frame.html", new() { WaitUntil = WaitUntilState.NetworkIdle });
        });
    }

    [UpstreamTest("NE-079 should wait for networkidle from the popup")]
    public async Task ShouldWaitForNetworkidleFromThePopup(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <button id=box1 onclick=""window.open('./popup/popup.html')"">Button1</button>
    <button id=box2 onclick=""window.open('./popup/popup.html')"">Button2</button>
    <button id=box3 onclick=""window.open('./popup/popup.html')"">Button3</button>
    <button id=box4 onclick=""window.open('./popup/popup.html')"">Button4</button>
    <button id=box5 onclick=""window.open('./popup/popup.html')"">Button5</button>
  ");
        for (var i = 1; i < 6; ++i)
        {
            var popup = await PromiseAll(
                page.WaitForPopupAsync(),
                page.ClickAsync("#box" + i));
            await popup.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    [UpstreamTest("NE-080 should wait for networkidle when iframe attaches and detaches")]
    public async Task ShouldWaitForNetworkidleWhenIframeAttachesAndDetaches(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) => { });
        var done = false;
        // .NET: page.setContent(...).then(() => done = true) is a local function that is called in its place.
        async Task SetContent()
        {
            await page.SetContentAsync($@"
    <body>
      <script>
        const iframe = document.createElement('iframe');
        iframe.src = {JsonSerializer.Serialize(server.EmptyPage)};
        document.body.appendChild(iframe);
      </script>
    </body>
  ", new() { WaitUntil = WaitUntilState.NetworkIdle });
            done = true;
        }
        var promise = SetContent();
        await page.WaitForTimeoutAsync(600);
        Assert.That(done, Is.EqualTo(false));
        await page.EvaluateAsync(@"() => {
    document.querySelector('iframe').remove();
  }");
        await promise;
        Assert.That(done, Is.EqualTo(true));
    }

    [UpstreamTest("NE-081 should work after repeated navigations in the same page")]
    public async Task ShouldWorkAfterRepeatedNavigationsInTheSamePage(IPage page, Server server)
    {
        var requestCount = 0;
        await page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                ContentType = "text/html",
                Body = @"
        <script>
          fetch('http://localhost:8000/sample').then(res => console.log(res.json()))
        </script>",
            }));
        });

        await page.RouteAsync("**/sample", route =>
        {
            requestCount++;
            IgnoreErrors(route.FulfillAsync(new()
            {
                ContentType = "application/json",
                Body = JsonSerializer.Serialize(new
                {
                    content = "sample",
                }),
            }));
        });

        await page.GotoAsync(server.EmptyPage, new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.That(requestCount, Is.EqualTo(1));
        await page.GotoAsync(server.EmptyPage, new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.That(requestCount, Is.EqualTo(2));
    }

    [UpstreamTest("NE-082 should not wait for an open EventSource connection")]
    public async Task ShouldNotWaitForAnOpenEventSourceConnection(IPage page, Server server)
    {
        server.SetRoute("/sse", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/event-stream",
                ["Connection"] = "keep-alive",
                ["Cache-Control"] = "no-cache",
            });
            res.Write("data: hello\n\n");
        });
        server.SetRoute("/sse-page.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End("<script>new EventSource('/sse');</script>");
        });

        var response = await page.GotoAsync(server.Prefix + "/sse-page.html", new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("NE-083 should not wait for an open EventSource connection in setContent")]
    public async Task ShouldNotWaitForAnOpenEventSourceConnectionInSetContent(IPage page, Server server)
    {
        server.SetRoute("/sse", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/event-stream",
                ["Connection"] = "keep-alive",
                ["Cache-Control"] = "no-cache",
            });
            res.Write("data: hello\n\n");
        });

        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"<script>
    window.__sseOpened = new Promise(resolve => {
      const es = new EventSource('/sse');
      es.onmessage = () => resolve();
    });
  </script>", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.EvaluateAsync("() => window['__sseOpened']");
    }
}
