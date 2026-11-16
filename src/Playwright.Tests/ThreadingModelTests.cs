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

using Microsoft.Playwright.Transport;

namespace Microsoft.Playwright.Tests;

/// <summary>
/// Pins the client's threading model; see "Threading model" in CLAUDE.md. Driver messages and
/// library code run on one dispatcher thread per connection, event handlers run on it too, an
/// awaited call resumes once the message that completed it has been handled in full, and the
/// caller's continuation never runs on the dispatcher thread.
/// </summary>
public class ThreadingModelTests : PageTestEx
{
    private static bool OnDispatcherThread => Thread.CurrentThread.Name == Dispatcher.ThreadName;

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
        // Messages are handled one at a time and a reply only queues the continuation,
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
    public async Task PageLoadHasFiredWhenWaitForLoadStateReturns()
    {
        // Frame.OnLoadState raises Frame.LoadState, which completes the wait, before Page.Load.
        // The continuation runs after the whole message has been handled, as a JS microtask would.
        bool loadFired = false;
        Page.Load += (_, _) => loadFired = true;
        var finishLoading = await GotoPageThatLoadsOnDemandAsync();
        var load = Page.WaitForLoadStateAsync(LoadState.Load);
        finishLoading();
        await load;
        Assert.True(loadFired);
    }

    [PlaywrightTest]
    public async Task ApiCallsResumeOffTheDispatcherThread()
    {
        // Whether a call completes with a reply or with an event, the caller continues on the
        // thread pool, so that it may block there.
        await Page.GotoAsync(Server.EmptyPage);
        Assert.False(OnDispatcherThread);

        var finishLoading = await GotoPageThatLoadsOnDemandAsync();
        var load = Page.WaitForLoadStateAsync(LoadState.Load);
        finishLoading();
        await load;
        Assert.False(OnDispatcherThread);

        await Page.RunAndWaitForResponseAsync(() => Page.EvaluateAsync("fetch('/empty.html')"), "**/empty.html");
        Assert.False(OnDispatcherThread);

        await Page.CloseAsync();
        Assert.False(OnDispatcherThread);
    }

    [PlaywrightTest]
    public async Task BlockingAfterAnEventDrivenWaitDoesNotStallTheConnection()
    {
        var finishLoading = await GotoPageThatLoadsOnDemandAsync();
        var load = Page.WaitForLoadStateAsync(LoadState.Load);
        finishLoading();
        await load;
        var evaluate = Page.EvaluateAsync<int>("1 + 1");
        Assert.True(evaluate.Wait(5000));
        Assert.AreEqual(2, await evaluate);
    }

    [PlaywrightTest]
    public async Task EventHandlersRunOnTheDispatcherThread()
    {
        bool? onDispatcher = null;
        Page.Console += (_, _) => onDispatcher = OnDispatcherThread;
        await Page.EvaluateAsync("console.log('hi')");
        Assert.True(onDispatcher);
    }

    [PlaywrightTest]
    public async Task AwaitingInsideAnEventHandlerResumesOffTheDispatcherThread()
    {
        // Only the synchronous part of a handler runs on the dispatcher thread. A call that the
        // handler awaits returns on the thread pool, so the handler may block from there on.
        var result = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        Page.Console += async (_, _) =>
        {
            try
            {
                Assert.True(OnDispatcherThread);
                var value = await Page.EvaluateAsync<int>("1 + 1");
                Assert.False(OnDispatcherThread);
                var blocking = Page.EvaluateAsync<int>("2 + 2");
                Assert.True(blocking.Wait(5000));
                result.SetResult(value + blocking.Result);
            }
            catch (Exception ex)
            {
                result.SetException(ex);
            }
        };
        await Page.EvaluateAsync("console.log('hi')");
        Assert.AreEqual(6, await result.Task);
    }

    [PlaywrightTest]
    public async Task BlockingInAnEventHandlerStallsTheConnection()
    {
        // The one rule for users: a synchronous wait on a Playwright call in the synchronous
        // part of a handler blocks the thread that would deliver its reply.
        bool completed = true;
        Page.Console += (_, _) => completed = Page.EvaluateAsync<int>("1 + 1").Wait(1000);
        await Page.EvaluateAsync("console.log('hi')");
        Assert.False(completed);
    }

    [PlaywrightTest]
    public async Task RunAndWaitForActionRunsOffTheDispatcherThread()
    {
        bool? onDispatcher = null;
        await Page.RunAndWaitForConsoleMessageAsync(async () =>
        {
            onDispatcher = OnDispatcherThread;
            await Page.EvaluateAsync("console.log('hi')");
            onDispatcher |= OnDispatcherThread;
        });
        Assert.False(onDispatcher);
    }

    [PlaywrightTest]
    public async Task ToPassCallbackRunsOffTheDispatcherThread()
    {
        // Every attempt runs on the thread pool, also when a handler starts the retries in its
        // synchronous part, so the callback may block and a zero interval does not spin there.
        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Page.Console += async (_, _) =>
        {
            try
            {
                Assert.True(OnDispatcherThread);
                int attempts = 0;
                bool onDispatcher = false;
                await Expect(() =>
                {
                    onDispatcher |= OnDispatcherThread;
                    if (!Page.EvaluateAsync<int>("1 + 1").Wait(5000) || ++attempts < 3)
                    {
                        throw new InvalidOperationException("Retry");
                    }
                }).ToPassAsync(new() { Intervals = [0] });
                Assert.False(OnDispatcherThread);
                result.SetResult(onDispatcher);
            }
            catch (Exception ex)
            {
                result.SetException(ex);
            }
        };
        await Page.EvaluateAsync("console.log('hi')");
        Assert.False(await result.Task);
    }

    [PlaywrightTest]
    public async Task SubscribingRightAfterStartingAWaitKeepsBothHandlers()
    {
        // The wait subscribes on the dispatcher thread after the call has returned, and the
        // caller subscribes to the same event on its own thread in the meantime. The handler
        // list is updated atomically, so neither subscription overwrites the other.
        await Page.GotoAsync(Server.EmptyPage);
        for (int i = 0; i < 100; i++)
        {
            int seen = 0;
            EventHandler<IRequest> handler = (_, _) => Interlocked.Increment(ref seen);
            var wait = Page.WaitForRequestAsync($"**/empty.html?{i}", new() { Timeout = 5000 });
            Page.Request += handler;
            await Page.EvaluateAsync("i => fetch('/empty.html?' + i).then(r => r.text())", i);
            await wait;
            Assert.AreEqual(1, Volatile.Read(ref seen), $"iteration {i}");
            Page.Request -= handler;
        }
    }

    [PlaywrightTest]
    public async Task UnsubscribingRightAfterStartingAWaitKeepsTheWait()
    {
        // Removing the last handler disables the event on the server, and the wait that was
        // started just before needs it enabled. Only the dispatcher thread tells the server,
        // from the handlers as they are at that point, so the event stays enabled.
        await Page.GotoAsync(Server.EmptyPage);
        for (int i = 0; i < 100; i++)
        {
            EventHandler<IRequest> handler = (_, _) => { };
            Page.Request += handler;
            var wait = Page.WaitForRequestAsync($"**/empty.html?{i}", new() { Timeout = 5000 });
            Page.Request -= handler;
            await Page.EvaluateAsync("i => fetch('/empty.html?' + i).then(r => r.text())", i);
            await wait;
        }
    }

    [PlaywrightTest]
    public async Task RemovedHandlerIsNotCalledForEventsThatAreAlreadyQueued()
    {
        // Removing a handler takes effect at once, as with any C# event. Here the dispatcher
        // thread is held inside a `request` handler while the page logs to the console, so the
        // `console` messages wait in its queue. The handler removed meanwhile must not get them.
        await Page.GotoAsync(Server.EmptyPage);
        int calls = 0;
        EventHandler<IConsoleMessage> removed = (_, _) => Interlocked.Increment(ref calls);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Page.Request += (_, _) =>
        {
            entered.Set();
            release.Wait(TimeSpan.FromSeconds(10));
        };
        Page.Console += removed;

        var evaluate = Page.EvaluateAsync(@"async () => {
            await fetch('/empty.html').then(r => r.text());
            for (let i = 0; i < 5; i++)
                console.log(i);
        }");
        Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
        // Let the page finish and its console messages reach the queue.
        await Task.Delay(500);
        Page.Console -= removed;
        release.Set();
        await evaluate;

        // A reply follows the events that preceded it, so all of them have been handled.
        Assert.AreEqual(0, Volatile.Read(ref calls));
    }

    [PlaywrightTest]
    public async Task HandlersAddedAndRemovedFromManyThreadsAreAllKept()
    {
        // The handler list is updated atomically: no thread overwrites the update of another,
        // and the server is told to send the event exactly while there are handlers.
        const int threads = 8;
        const int perThread = 50;
        int calls = 0;
        var handlers = Enumerable.Range(0, threads * perThread)
            .Select(_ => (EventHandler<IConsoleMessage>)((_, _) => Interlocked.Increment(ref calls)))
            .ToArray();

        await Task.WhenAll(Enumerable.Range(0, threads).Select(t => Task.Run(() =>
        {
            for (int i = 0; i < perThread; i++)
            {
                Page.Console += handlers[(t * perThread) + i];
            }
        })));
        await Page.EvaluateAsync("console.log('hi')");
        Assert.AreEqual(threads * perThread, Volatile.Read(ref calls));

        await Task.WhenAll(Enumerable.Range(0, threads).Select(t => Task.Run(() =>
        {
            for (int i = 0; i < perThread; i++)
            {
                Page.Console -= handlers[(t * perThread) + i];
            }
        })));
        await Page.EvaluateAsync("console.log('hi')");
        Assert.AreEqual(threads * perThread, Volatile.Read(ref calls));

        // The event still works after its last handler was removed.
        var message = await Page.RunAndWaitForConsoleMessageAsync(() => Page.EvaluateAsync("console.log('again')"));
        Assert.AreEqual("again", message.Text);
    }

    [PlaywrightTest]
    public async Task RunAndWaitForActionThatThrowsAfterTheEventDoesNotBreakTheConnection()
    {
        // The event arrives first, and the action then fails on the thread pool. The waiter is
        // cleaned up on the dispatcher thread, and the page keeps working.
        var exception = await PlaywrightAssert.ThrowsAsync<InvalidOperationException>(() => Page.RunAndWaitForConsoleMessageAsync(async () =>
        {
            await Page.EvaluateAsync("console.log('hi')");
            await Task.Delay(50);
            throw new InvalidOperationException("boom");
        }));
        Assert.AreEqual("boom", exception.Message);
        Assert.AreEqual(2, await Page.EvaluateAsync<int>("1 + 1"));
    }

    [PlaywrightTest]
    public async Task RouteHandlerMayResumeOnAnotherThread()
    {
        // A handler that awaits something foreign resumes on the thread pool; the library
        // switches back before it touches its state, and the second handler still runs.
        var log = new List<string>();
        await Page.RouteAsync("**/empty.html", async route =>
        {
            await Task.Delay(10);
            log.Add("second:" + OnDispatcherThread);
            await route.FulfillAsync(new() { ContentType = "text/html", Body = "<title>routed</title>" });
        });
        await Page.RouteAsync("**/empty.html", async route =>
        {
            await Task.Delay(10);
            log.Add("first:" + OnDispatcherThread);
            await route.FallbackAsync();
        });
        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("routed", await Page.TitleAsync());
        Assert.AreEqual(new[] { "first:False", "second:False" }, log);
    }

    [PlaywrightTest]
    public async Task CallerSynchronizationContextIsHonored()
    {
        // The hop off the dispatcher completes on the thread pool, so an awaiter that captured
        // a SynchronizationContext is posted back to it, as with any other Task.
        var context = new CountingSynchronizationContext();
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            await Page.EvaluateAsync("1");
            Assert.AreEqual(1, context.Posts);
            Assert.False(OnDispatcherThread);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [PlaywrightTest]
    public async Task ConcurrentCallsAndReadsDoNotCorruptState()
    {
        // Pages opened from many threads while another thread enumerates Context.Pages.
        using var cts = new CancellationTokenSource();
        var reader = Task.Run(() =>
        {
            int seen = 0;
            while (!cts.Token.IsCancellationRequested)
            {
                foreach (var page in Context.Pages)
                {
                    seen++;
                }
            }
            return seen;
        });
        var pages = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => Context.NewPageAsync())));
        Assert.AreEqual(11, Context.Pages.Count);
        await Task.WhenAll(pages.Select(page => Task.Run(() => page.CloseAsync())));
        Assert.AreEqual(1, Context.Pages.Count);
        cts.Cancel();
        Assert.Greater(await reader, 0);
    }

    [PlaywrightTest]
    public async Task ThrowingInAnEventHandlerClosesTheConnection()
    {
        // Handlers run inside message dispatch, whose catch-all closes the connection, so one
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

    private sealed class CountingSynchronizationContext : SynchronizationContext
    {
        private int _posts;

        public int Posts => Volatile.Read(ref _posts);

        public override void Post(SendOrPostCallback d, object state)
        {
            Interlocked.Increment(ref _posts);
            ThreadPool.QueueUserWorkItem(_ => d(state));
        }
    }
}
