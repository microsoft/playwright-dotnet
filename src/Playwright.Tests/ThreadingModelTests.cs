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

/// <summary>
/// Pins what the client's threading model does today; see "Threading model" in CLAUDE.md.
/// The first test is the guarantee the model gives. The others are its pitfalls, asserted as
/// they are rather than as they should be: when one of them starts failing, the model changed
/// and the documentation needs an update. The check-then-subscribe race inside the library is
/// covered by PageWaitForUrlTests.ShouldNotMissNavigationCommittedRightAfterTheCall (#3364).
/// </summary>
public class ThreadingModelTests : PageTestEx
{
    // Navigates to a page that commits at once (Firefox and WebKit need a few KB before they
    // commit) and finishes loading only when the returned action is invoked, so that a
    // WaitForLoadStateAsync started before that has to wait for the `load` event.
    private async Task<Action> GotoPageThatLoadsOnDemandAsync()
    {
        var finishBody = new TaskCompletionSource<bool>();
        Server.SetRoute("/slow.html", async ctx =>
        {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync("<!DOCTYPE html><html><body><!--" + new string('x', 4096) + "-->");
            await ctx.Response.Body.FlushAsync();
            await finishBody.Task;
            await ctx.Response.WriteAsync("</body></html>");
        });
        await Page.GotoAsync(Server.Prefix + "/slow.html", new() { WaitUntil = WaitUntilState.Commit });
        return () => finishBody.SetResult(true);
    }

    [PlaywrightTest]
    public async Task EventsBeforeTheReplyAreHandledBeforeTheMethodReturns()
    {
        // Messages are dispatched one at a time and a reply only queues the continuation,
        // so everything the `load` event did is visible when GotoAsync returns.
        int loads = 0;
        Page.Load += (_, _) => Interlocked.Increment(ref loads);
        for (int i = 1; i <= 20; i++)
        {
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(i, Volatile.Read(ref loads));
        }
    }

    [PlaywrightTest]
    public async Task EventDrivenWaitResumesOnTheReaderThread()
    {
        // Waiter completes a plain TaskCompletionSource, so the continuation runs synchronously
        // inside the event handler, inside Connection.Dispatch, until its next incomplete await.
        // A reply, by contrast, queues the continuation to the thread pool.
        var finishLoading = await GotoPageThatLoadsOnDemandAsync();
        var load = Page.WaitForLoadStateAsync(LoadState.Load);
        finishLoading();
        await load;
        StringAssert.Contains("Connection.Dispatch", Environment.StackTrace);

        await Page.GotoAsync(Server.EmptyPage);
        StringAssert.DoesNotContain("Connection.Dispatch", Environment.StackTrace);
    }

    [PlaywrightTest]
    public async Task PageLoadHasNotFiredWhenWaitForLoadStateReturns()
    {
        // Frame.OnLoadState raises Frame.LoadState, which completes the wait, before Page.Load.
        // In the JS client the continuation is a microtask and runs after both.
        bool loadFired = false;
        Page.Load += (_, _) => loadFired = true;
        var finishLoading = await GotoPageThatLoadsOnDemandAsync();
        var load = Page.WaitForLoadStateAsync(LoadState.Load);
        finishLoading();
        await load;
        Assert.False(loadFired);

        // The reply to this call is dispatched only after the reader has finished OnLoadState.
        await Page.EvaluateAsync("1");
        Assert.True(loadFired);
    }

    [PlaywrightTest]
    public async Task BlockingAfterAnEventDrivenWaitStallsTheConnection()
    {
        // Same cause: this code runs on the reader thread, so the reply to EvaluateAsync cannot
        // be dispatched until it yields. The same Wait() after a GotoAsync completes at once.
        var finishLoading = await GotoPageThatLoadsOnDemandAsync();
        var load = Page.WaitForLoadStateAsync(LoadState.Load);
        finishLoading();
        await load;
        var evaluate = Page.EvaluateAsync<int>("1 + 1");
        Assert.False(evaluate.Wait(1000));
        Assert.AreEqual(2, await evaluate);
    }

    [PlaywrightTest]
    public async Task BlockingInAnEventHandlerStallsTheConnection()
    {
        // Event handlers run on the reader thread too: a synchronous wait on any Playwright
        // call inside one is a deadlock until the wait gives up.
        bool completed = true;
        Page.Console += (_, _) => completed = Page.EvaluateAsync<int>("1 + 1").Wait(1000);
        await Page.EvaluateAsync("console.log('hi')");
        Assert.False(completed);
    }

    [PlaywrightTest]
    public async Task ThrowingInAnEventHandlerClosesTheConnection()
    {
        // Handlers run inside Connection.Dispatch, whose catch-all closes the connection, so one
        // throwing handler takes down every page and browser of the IPlaywright instance.
        // Uses its own instance so that the shared one survives; the driver exits with it.
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var browser = await playwright[BrowserName].LaunchAsync(await LaunchOptionsAsync());
        var page = await browser.NewPageAsync();
        page.Console += (_, _) => throw new InvalidOperationException("boom");

        var exception = Assert.ThrowsAsync<TargetClosedException>(() => page.EvaluateAsync("console.log('hi')"));
        StringAssert.Contains("boom", exception.Message);
        Assert.ThrowsAsync<TargetClosedException>(() => browser.NewPageAsync());
    }
}
