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
/// The dispatcher is the thread on which a connection handles driver messages and runs library
/// code; see <see cref="Dispatcher"/>. These tests pin the three rules the rest of the client
/// depends on: items run on that thread in order, a completion source completed through the
/// dispatcher resumes its awaiter after the current item and on the dispatcher thread, and the
/// two hops (onto the thread and off it) land where they say.
/// </summary>
public class DispatcherTests
{
    private Dispatcher _dispatcher = null!;

    private static bool OnDispatcherThread => Thread.CurrentThread.Name == Dispatcher.ThreadName;

    [SetUp]
    public void SetUp() => _dispatcher = new Dispatcher();

    [TearDown]
    public void TearDown() => _dispatcher.Dispose();

    [Test]
    public async Task PostRunsItemsOnTheDispatcherThreadInOrder()
    {
        var log = new List<string>();
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        for (int i = 0; i < 100; i++)
        {
            int n = i;
            _dispatcher.Post(() =>
            {
                Assert.True(_dispatcher.IsCurrent);
                Assert.True(OnDispatcherThread);
                log.Add($"item{n}");
            });
        }
        _dispatcher.Post(() => done.SetResult(true));
        await done.Task;
        Assert.AreEqual(Enumerable.Range(0, 100).Select(i => $"item{i}").ToList(), log);
    }

    [Test]
    public async Task SwitchToLandsOnTheDispatcherThreadAndIsSynchronousThere()
    {
        Assert.False(_dispatcher.IsCurrent);
        Assert.False(_dispatcher.SwitchTo().IsCompleted);
        await _dispatcher.SwitchTo();
        Assert.True(_dispatcher.IsCurrent);
        Assert.True(_dispatcher.SwitchTo().IsCompleted);
        // A second hop from the dispatcher thread is a no-op.
        await _dispatcher.SwitchTo();
        Assert.True(_dispatcher.IsCurrent);
        await Dispatcher.ExitToThreadPool();
        Assert.False(_dispatcher.IsCurrent);
    }

    [Test]
    public async Task SwitchToFlowsTheExecutionContext()
    {
        var local = new AsyncLocal<string>();
        local.Value = "from the caller";
        await _dispatcher.SwitchTo();
        Assert.AreEqual("from the caller", local.Value);
        await Dispatcher.ExitToThreadPool();
        Assert.AreEqual("from the caller", local.Value);
    }

    [Test]
    public async Task CompleteRunsTheContinuationAfterTheCurrentItemOnTheDispatcherThread()
    {
        // The library's await: ConfigureAwait(false) on a source that the dispatcher completes.
        var tcs = new TaskCompletionSource<int>();
        var itemFinished = false;
        var continuation = Task.Run(async () =>
        {
            var value = await tcs.Task.ConfigureAwait(false);
            Assert.True(itemFinished, "the continuation must run after the item that completed the source");
            Assert.True(_dispatcher.IsCurrent, "the continuation must run inline on the dispatcher thread");
            return value;
        });
        // Give the awaiter time to subscribe before the source is completed.
        await Task.Delay(50);
        _dispatcher.Post(() =>
        {
            _dispatcher.Complete(tcs, 42);
            Assert.False(tcs.Task.IsCompleted, "completion is deferred until the item has finished");
            itemFinished = true;
        });
        Assert.AreEqual(42, await continuation);
    }

    [Test]
    public async Task MicrotasksRunBeforeTheNextItem()
    {
        var log = new List<string>();
        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        // Subscribe from the dispatcher thread, so that the continuations run inline there.
        await _dispatcher.SwitchTo();
        _ = tcs1.Task.ContinueWith(
            _ =>
            {
                log.Add("continuation1");
                // A microtask queued by a microtask still runs before the next item.
                _dispatcher.Complete(tcs2, true);
            },
            TaskContinuationOptions.ExecuteSynchronously);
        _ = tcs2.Task.ContinueWith(_ => log.Add("continuation2"), TaskContinuationOptions.ExecuteSynchronously);
        _dispatcher.Post(() =>
        {
            log.Add("item1");
            _dispatcher.Complete(tcs1, true);
            log.Add("item1 end");
        });
        _dispatcher.Post(() => log.Add("item2"));
        _dispatcher.Post(() => done.SetResult(true));
        await Dispatcher.ExitToThreadPool();
        await done.Task;
        Assert.AreEqual(new[] { "item1", "item1 end", "continuation1", "continuation2", "item2" }, log);
    }

    [Test]
    public async Task CompleteFromAnotherThreadResumesTheAwaiterOnTheDispatcherThread()
    {
        var tcs = new TaskCompletionSource<bool>();
        var continuation = Task.Run(async () =>
        {
            await tcs.Task.ConfigureAwait(false);
            return _dispatcher.IsCurrent;
        });
        await Task.Delay(50);
        Assert.False(_dispatcher.IsCurrent);
        _dispatcher.Complete(tcs, true);
        Assert.True(await continuation);
    }

    [Test]
    public async Task FailFaultsTheSourceAfterTheCurrentItem()
    {
        var tcs = new TaskCompletionSource<bool>();
        var continuation = Task.Run(async () =>
        {
            try
            {
                await tcs.Task.ConfigureAwait(false);
                return null;
            }
            catch (InvalidOperationException ex)
            {
                Assert.True(_dispatcher.IsCurrent);
                return ex.Message;
            }
        });
        await Task.Delay(50);
        _dispatcher.Post(() => _dispatcher.Fail(tcs, new InvalidOperationException("boom")));
        Assert.AreEqual("boom", await continuation);
    }

    [Test]
    public async Task InlineContinuationChainStaysOnTheDispatcherThread()
    {
        // Nested library methods awaiting each other with ConfigureAwait(false): every level
        // resumes on the dispatcher thread when the innermost source is completed there.
        var tcs = new TaskCompletionSource<bool>();
        var threads = new List<bool>();

        async Task Level(int depth)
        {
            if (depth == 0)
            {
                await tcs.Task.ConfigureAwait(false);
            }
            else
            {
                await Level(depth - 1).ConfigureAwait(false);
            }
            threads.Add(_dispatcher.IsCurrent);
        }

        var chain = Task.Run(() => Level(20));
        await Task.Delay(50);
        _dispatcher.Post(() => _dispatcher.Complete(tcs, true));
        await chain;
        Assert.AreEqual(21, threads.Count);
        Assert.True(threads.All(onDispatcher => onDispatcher));
    }

    [Test]
    public async Task ExitToThreadPoolNeverCompletesSynchronously()
    {
        Assert.False(Dispatcher.ExitToThreadPool().IsCompleted);
        var thread = Thread.CurrentThread;
        await Dispatcher.ExitToThreadPool();
        Assert.True(Thread.CurrentThread.IsThreadPoolThread);
        Assert.False(_dispatcher.IsCurrent);
    }

    [Test]
    public async Task PostAfterDisposeStillRunsTheAction()
    {
        _dispatcher.Dispose();
        var ran = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcher.Post(() => ran.SetResult(true));
        Assert.True(await ran.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        // The hop still resolves, on some thread, so that a call made after close can throw.
        await _dispatcher.SwitchTo();
    }

    [Test]
    public async Task UnhandledExceptionInAnItemIsReportedAndTheThreadSurvives()
    {
        Exception reported = null;
        _dispatcher.UnhandledException += (_, ex) => reported = ex;
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcher.Post(() => throw new InvalidOperationException("boom"));
        _dispatcher.Post(() => done.SetResult(true));
        await done.Task;
        Assert.AreEqual("boom", reported?.Message);
    }
}
