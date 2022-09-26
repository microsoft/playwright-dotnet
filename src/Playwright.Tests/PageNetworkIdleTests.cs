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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

///<playwright-file>page-network-idle.spec.ts</playwright-file>
public class PageNetworkIdleTests : PageTestEx
{
    [PlaywrightTest("page-network-idle.spec.ts", "should navigate to empty page with networkidle")]
    public async Task ShouldNavigateToEmptyPageWithNetworkIdle()
    {
        var response = await Page.GotoAsync(Server.EmptyPage, new() { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle to succeed navigation")]
    public Task ShouldWaitForNetworkIdleToSucceedNavigation()
        => NetworkIdleTestAsync(Page.MainFrame, () => Page.GotoAsync(Server.Prefix + "/networkidle.html", new() { WaitUntil = WaitUntilState.NetworkIdle }));

    [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle to succeed navigation with request from previous navigation")]
    public async Task ShouldWaitForToSucceedNavigationWithRequestFromPreviousNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/foo.js", _ => Task.CompletedTask);
        await Page.SetContentAsync("<script>fetch('foo.js')</script>");
        await NetworkIdleTestAsync(Page.MainFrame, () => Page.GotoAsync(Server.Prefix + "/networkidle.html", new() { WaitUntil = WaitUntilState.NetworkIdle }));
    }

    [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle in waitForNavigation")]
    public Task ShouldWaitForInWaitForNavigation()
        => NetworkIdleTestAsync(
            Page.MainFrame,
            () =>
            {
                var task = Page.WaitForNavigationAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
                Page.GotoAsync(Server.Prefix + "/networkidle.html");
                return task;
            });

    [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle in setContent")]
    public async Task ShouldWaitForInSetContent()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await NetworkIdleTestAsync(
            Page.MainFrame,
            () => Page.SetContentAsync("<script src='networkidle.js'></script>", new() { WaitUntil = WaitUntilState.NetworkIdle }),
            true);
    }

    [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle in setContent with request from previous navigation")]
    public async Task ShouldWaitForNetworkIdleInSetContentWithRequestFromPreviousNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/foo.js", _ => Task.CompletedTask);
        await Page.SetContentAsync("<script>fetch('foo.js')</script>");
        await NetworkIdleTestAsync(
            Page.MainFrame,
            () => Page.SetContentAsync("<script src='networkidle.js'></script>", new() { WaitUntil = WaitUntilState.NetworkIdle }),
            true);
    }

    [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle when navigating iframe")]
    public async Task ShouldWaitForNetworkIdleWhenNavigatingIframe()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        var frame = Page.FirstChildFrame();
        await NetworkIdleTestAsync(
            frame,
            () => frame.GotoAsync(Server.Prefix + "/networkidle.html", new() { WaitUntil = WaitUntilState.NetworkIdle }));
    }

    [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle in setContent from the child frame")]
    public async Task ShouldWaitForInSetContentFromTheChildFrame()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await NetworkIdleTestAsync(
            Page.MainFrame,
            () => Page.SetContentAsync("<iframe src='networkidle.html'></iframe>", new() { WaitUntil = WaitUntilState.NetworkIdle }),
            true);
    }

    [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle from the child frame")]
    public Task ShouldWaitForFromTheChildFrame()
        => NetworkIdleTestAsync(
            Page.MainFrame,
            () => Page.GotoAsync(Server.Prefix + "/networkidle-frame.html", new() { WaitUntil = WaitUntilState.NetworkIdle }));

    [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle from the popup")]
    public async Task ShouldWaitForNetworkIdleFromThePopup()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
                <button id=box1 onclick=""window.open('./popup/popup.html')"">Button1</button>
                <button id=box2 onclick=""window.open('./popup/popup.html')"">Button2</button>
                <button id=box3 onclick=""window.open('./popup/popup.html')"">Button3</button>
                <button id=box4 onclick=""window.open('./popup/popup.html')"">Button4</button>
                <button id=box5 onclick=""window.open('./popup/popup.html')"">Button5</button>
            ");

        for (int i = 1; i < 6; i++)
        {
            var popupTask = Page.WaitForPopupAsync();
            await Task.WhenAll(
                Page.WaitForPopupAsync(),
                Page.ClickAsync("#box" + i));

            await popupTask.Result.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    private async Task NetworkIdleTestAsync(IFrame frame, Func<Task> action = default, bool isSetContent = false)
    {
        var lastResponseFinished = new Stopwatch();
        var responses = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
        var fetches = new Dictionary<string, TaskCompletionSource<bool>>();

        async Task RequestDelegate(HttpContext context)
        {
            var taskCompletion = new TaskCompletionSource<bool>();
            responses[context.Request.Path] = taskCompletion;
            fetches[context.Request.Path].TrySetResult(true);
            await taskCompletion.Task;
            lastResponseFinished.Restart();
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("File not found");
        }

        fetches["/fetch-request-a.js"] = new();
        Server.SetRoute("/fetch-request-a.js", RequestDelegate);

        var firstFetchResourceRequested = Server.WaitForRequest("/fetch-request-a.js");

        fetches["/fetch-request-b.js"] = new();
        Server.SetRoute("/fetch-request-b.js", RequestDelegate);
        var secondFetchResourceRequested = Server.WaitForRequest("/fetch-request-b.js");

        var waitForLoadTask = isSetContent ? Task.CompletedTask : frame.WaitForNavigationAsync(new() { WaitUntil = WaitUntilState.Load });

        var actionTask = action();

        await waitForLoadTask;
        Assert.False(actionTask.IsCompleted);

        await firstFetchResourceRequested;
        Assert.False(actionTask.IsCompleted);

        await fetches["/fetch-request-a.js"].Task;
        await frame.Page.EvaluateAsync("() => window['fetchSecond']()");

        // Finishing first response should leave 2 requests alive and trigger networkidle2.
        responses["/fetch-request-a.js"].TrySetResult(true);

        // Wait for the second round to be requested.
        await secondFetchResourceRequested;
        Assert.False(actionTask.IsCompleted);

        await fetches["/fetch-request-b.js"].Task;
        responses["/fetch-request-b.js"].TrySetResult(true);

        IResponse navigationResponse = null;
        if (!isSetContent)
        {
            navigationResponse = await (Task<IResponse>)actionTask;
        }
        else
        {
            await actionTask;
        }

        lastResponseFinished.Stop();
        if (!isSetContent)
        {
            Assert.AreEqual((int)HttpStatusCode.OK, navigationResponse.Status);
        }
    }
}
