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

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class PageWaitForUrlTests : PageTestEx
{
    [PlaywrightTest("page-wait-for-url.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync("url => window.location.href = url", Server.Prefix + "/grid.html");
        await Page.WaitForURLAsync("**/grid.html");
    }

    [PlaywrightTest("page-wait-for-url.spec.ts", "should respect timeout")]
    public async Task ShouldRespectTimeout()
    {
        var task = Page.WaitForURLAsync("**/frame.html", new() { Timeout = 2500 });
        await Page.GotoAsync(Server.EmptyPage);
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => task);
        StringAssert.Contains("Timeout 2500ms exceeded.", exception.Message);
    }

    [PlaywrightTest("page-wait-for-url.spec.ts", "should work with both domcontentloaded and load")]
    public async Task UrlShouldWorkWithBothDomcontentloadedAndLoad()
    {
        var responseTask = new TaskCompletionSource<bool>();
        Server.SetRoute("/one-style.css", async (ctx) =>
        {
            if (await responseTask.Task)
            {
                await ctx.Response.WriteAsync("Some css");
            }
        });

        var waitForRequestTask = Server.WaitForRequest("/one-style.css");
        var navigationTask = Page.GotoAsync(Server.Prefix + "/one-style.html");
        var domContentLoadedTask = Page.WaitForURLAsync("**/one-style.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var bothFiredTask = Task.WhenAll(
            Page.WaitForURLAsync("**/one-style.html", new() { WaitUntil = WaitUntilState.Load }),
            domContentLoadedTask);

        await waitForRequestTask;
        await domContentLoadedTask;
        Assert.False(bothFiredTask.IsCompleted);
        responseTask.TrySetResult(true);
        await bothFiredTask;
        await navigationTask;
    }

    [PlaywrightTest("page-wait-for-url.spec.ts", "should work with clicking on anchor links")]
    public async Task ShouldWorkWithClickingOnAnchorLinks()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a href='#foobar'>foobar</a>");
        await Page.ClickAsync("a");
        await Page.WaitForURLAsync("**/*#foobar");
    }

    [PlaywrightTest("page-wait-for-url.spec.ts", "should work with history.pushState()")]
    public async Task ShouldWorkWithHistoryPushState()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
                <a onclick='javascript:replaceState()'>SPA</a>
                <script>
                  function replaceState() { history.replaceState({}, '', '/replaced.html') }
                </script>
            ");
        await Page.ClickAsync("a");
        await Page.WaitForURLAsync("**/replaced.html");
        Assert.AreEqual(Server.Prefix + "/replaced.html", Page.Url);
    }

    [PlaywrightTest("page-wait-for-url.spec.ts", "should work with DOM history.back()/history.forward()")]
    public async Task ShouldWorkWithDOMHistoryBackHistoryForward()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
                <a id=back onclick='javascript:goBack()'>back</a>
                <a id=forward onclick='javascript:goForward()'>forward</a>
                <script>
                  function goBack() { history.back(); }
                  function goForward() { history.forward(); }
                  history.pushState({}, '', '/first.html');
                  history.pushState({}, '', '/second.html');
                </script>
            ");
        Assert.AreEqual(Server.Prefix + "/second.html", Page.Url);

        await Task.WhenAll(Page.WaitForURLAsync("**/first.html"), Page.ClickAsync("a#back"));
        Assert.AreEqual(Server.Prefix + "/first.html", Page.Url);

        await Task.WhenAll(Page.WaitForURLAsync("**/second.html"), Page.ClickAsync("a#forward"));
        Assert.AreEqual(Server.Prefix + "/second.html", Page.Url);
    }

    [PlaywrightTest("page-wait-for-url.spec.ts", "should work with url match for same document navigations")]
    public async Task ShouldWorkWithUrlMatchForSameDocumentNavigations()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var waitPromise = Page.WaitForURLAsync(new Regex("third\\.html"));
        Assert.False(waitPromise.IsCompleted);

        await Page.EvaluateAsync(@"() => {
                history.pushState({}, '', '/first.html');
            }");
        Assert.False(waitPromise.IsCompleted);

        await Page.EvaluateAsync(@"() => {
                history.pushState({}, '', '/second.html');
            }");
        Assert.False(waitPromise.IsCompleted);

        await Page.EvaluateAsync(@"() => {
                history.pushState({}, '', '/third.html');
            }");
        await waitPromise;
    }

    [PlaywrightTest("page-wait-for-url.spec.ts", "should work on frame")]
    public async Task ShouldWorkOnFrame()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        var frame = Page.Frames.ElementAt(1);

        await TaskUtils.WhenAll(
            frame.WaitForURLAsync("**/grid.html"),
            frame.EvaluateAsync("url => window.location.href = url", Server.Prefix + "/grid.html"));
        ;
    }
}
