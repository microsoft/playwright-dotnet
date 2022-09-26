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

using System.Text;

namespace Microsoft.Playwright.Tests;

public class PageRequestContinueTests : PageTestEx
{
    [PlaywrightTest("page-request-continue.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.RouteAsync("**/*", (route) => route.ContinueAsync());
        await Page.GotoAsync(Server.EmptyPage);
    }

    [PlaywrightTest("page-request-continue.spec.ts", "should amend HTTP headers")]
    public async Task ShouldAmendHTTPHeaders()
    {
        await Page.RouteAsync("**/*", (route) =>
        {
#pragma warning disable 0612
            var headers = new Dictionary<string, string>(route.Request.Headers.ToDictionary(x => x.Key, x => x.Value)) { ["FOO"] = "bar" };
#pragma warning restore 0612
            route.ContinueAsync(new() { Headers = headers });
        });
        await Page.GotoAsync(Server.EmptyPage);
        var requestTask = Server.WaitForRequest("/sleep.zzz", request => request.Headers["foo"]);
        await TaskUtils.WhenAll(
            requestTask,
            Page.EvaluateAsync("() => fetch('/sleep.zzz')")
        );
        Assert.AreEqual("bar", requestTask.Result);
    }

    [PlaywrightTest("page-request-continue.spec.ts", "should amend method on main request")]
    public async Task ShouldAmendMethodOnMainRequest()
    {
        var methodTask = Server.WaitForRequest("/empty.html", r => r.Method);
        await Page.RouteAsync("**/*", (route) => route.ContinueAsync(new() { Method = HttpMethod.Post.Method }));
        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("POST", await methodTask);
    }

    [PlaywrightTest("page-request-continue.spec.ts", "should amend post data")]
    public async Task ShouldAmendPostData()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/*", (route) =>
        {
            route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("doggo") });
        });
        var requestTask = Server.WaitForRequest("/sleep.zzz", request =>
        {
            using StreamReader reader = new(request.Body);
            return reader.ReadToEndAsync().GetAwaiter().GetResult();
        });

        await TaskUtils.WhenAll(
            requestTask,
            Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })")
        );
        Assert.AreEqual("doggo", requestTask.Result);
    }

    [PlaywrightTest("page-request-continue.spec.ts", "should not throw when continuing while page is closing")]
    public async Task ShouldNotThrowWhenContinuingWhilePageIsClosing()
    {
        Task done = null;
        await Page.RouteAsync("**/*", (route) =>
        {
            done = Task.WhenAll(
                route.ContinueAsync(),
                Page.CloseAsync()
            );
        });
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () =>
        {
            await Page.GotoAsync(Server.EmptyPage);
        });
        await done;
    }

    [PlaywrightTest("page-request-continue.spec.ts", "should not throw when continuing after page is closed")]
    public async Task ShouldNotThrowWhenContinuingAfterPageIsClosed()
    {
        var tsc = new TaskCompletionSource<bool>();
        Task done = null;
        await Page.RouteAsync("**/*", async (route) =>
        {
            await Page.CloseAsync();
            done = route.ContinueAsync();
            tsc.SetResult(true);
        });
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () =>
        {
            await Page.GotoAsync(Server.EmptyPage);
        });
        await tsc.Task;
        await done;
    }

    [PlaywrightTest("page-request-continue.spec.ts", "should not allow changing protocol when overriding url")]
    public async Task ShouldNotAllowChangingProtocolWhenOverridingUrl()
    {
        var tcs = new TaskCompletionSource<Exception>();
        await Page.RouteAsync("**/empty.html", async (route) =>
        {
            try
            {
                await route.ContinueAsync(new RouteContinueOptions { Url = "file:///tmp/foo" });
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetResult(ex);
            }
        });
        var gotoTask = Page.GotoAsync(Server.EmptyPage, new PageGotoOptions { Timeout = 5000 });
        var exception = await tcs.Task;
        Assert.IsInstanceOf<PlaywrightException>(exception);
        Assert.AreEqual("New URL must have same protocol as overridden URL", exception.Message);
        await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => gotoTask);
    }
}
