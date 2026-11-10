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

using System.Text.Json;
using System.Text.RegularExpressions;

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

    // See https://github.com/microsoft/playwright-dotnet/issues/3365
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
        try
        {
            await Page.WaitForURLAsync("**/one-style.html", new() { WaitUntil = WaitUntilState.Commit, Timeout = 1000 });
            Assert.False(bothFiredTask.IsCompleted);
        }
        finally
        {
            responseTask.TrySetResult(true);
            await bothFiredTask;
            await navigationTask;
        }
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

    // See https://github.com/microsoft/playwright-dotnet/issues/3364
    [PlaywrightTest]
    public async Task ShouldNotMissNavigationCommittedRightAfterTheCall()
    {
        // Page A navigates to B a few milliseconds after it loads. The wait must see that
        // navigation even when it commits right after WaitForURLAsync was called: the check of
        // the current URL and the subscription to the next navigation are one step.
        Server.SetRoute("/a.html", context =>
        {
            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync("<script>addEventListener('load', () => setTimeout(() => location.href = '/b.html', 5))</script>");
        });
        Server.SetRoute("/b.html", context =>
        {
            context.Response.ContentType = "text/html";
            return context.Response.WriteAsync("<h1>b</h1>");
        });
        for (int i = 0; i < 10; i++)
        {
            await Page.GotoAsync(Server.Prefix + "/a.html");
            await Page.WaitForURLAsync("**/b.html", new() { Timeout = 5000 });
        }
    }

    // See https://github.com/microsoft/playwright-dotnet/issues/3399
    [PlaywrightTest]
    public async Task ShouldNotMissLoadThatFiresRightAfterTheCall()
    {
        // Each page links to the next one and loads a script. The response arrives first, and
        // `load` follows a few milliseconds later: right when WaitForURLAsync checks the load
        // state of a URL that already matches and subscribes to the next one.
        Server.SetRoute("/slow-script.js", async context =>
        {
            context.Response.ContentType = "application/javascript";
            await Task.Delay(5);
            await context.Response.WriteAsync("window.resourceLoaded = true;");
        });
        for (int i = 0; i < 20; i++)
        {
            int next = i + 1;
            Server.SetRoute($"/p/{i}", context =>
            {
                context.Response.ContentType = "text/html";
                return context.Response.WriteAsync($"<a href='/p/{next}'>next</a><script src='/slow-script.js'></script>");
            });
        }
        await Page.GotoAsync(Server.Prefix + "/p/0");
        for (int i = 1; i < 20; i++)
        {
            var response = await Page.RunAndWaitForResponseAsync(
                () => Page.Locator("a").ClickAsync(),
                r => r.Request.IsNavigationRequest && r.Status == 200);
            await Page.WaitForURLAsync(response.Url, new() { WaitUntil = WaitUntilState.Load, Timeout = 5000 });
            Assert.True(await Page.EvaluateAsync<bool>("window.resourceLoaded === true"));
        }
    }

    [PlaywrightTest]
    public async Task ShouldNotBlockTheDispatcherWhileMatchingTheUrl()
    {
        // The predicate is user code and runs on the dispatcher thread, like an event handler.
        // Awaiting a call from there is fine; this test pins that the wait does not hold anything
        // that the reply to that call would need.
        await Page.GotoAsync(Server.EmptyPage);
        Task<JsonElement?> evaluate = null;
        await Page.WaitForURLAsync(url =>
        {
            evaluate ??= Page.EvaluateAsync("() => { document.body.appendChild(document.createElement('iframe')); }");
            return true;
        });
        await evaluate!;
        Assert.AreEqual(2, Page.Frames.Count);
    }
}
