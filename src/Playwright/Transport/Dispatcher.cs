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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Transport;

/// <summary>
/// The one thread on which a connection handles driver messages and runs its own code.
/// <para>
/// The JS client runs on the event loop: a message is handled synchronously, and the promise
/// continuations it resolves run as microtasks after it, before the next message. Nothing can
/// observe a half-applied message or subscribe to an event between a check and the event.
/// The dispatcher restores that model in .NET:
/// </para>
/// <list type="bullet">
/// <item>Messages and library continuations run on the dispatcher thread, one item at a time.</item>
/// <item>A <see cref="TaskCompletionSource{T}"/> that library code awaits is completed through
/// <see cref="Complete{T}"/> or <see cref="Fail{T}"/>: after the current item, before the next one.
/// Its continuations run inline on the dispatcher thread, so a <c>ConfigureAwait(false)</c> chain
/// that starts there stays there.</item>
/// <item>A public API call hops onto the dispatcher with <see cref="SwitchTo"/> before it touches
/// state, and hops off with <see cref="ExitToThreadPool"/> before it returns, so that user code
/// never runs on the dispatcher thread by accident. Library code that awaits something completed
/// elsewhere (a user callback, a timer, pipe I/O) switches back with <see cref="SwitchTo"/>.</item>
/// </list>
/// </summary>
internal sealed class Dispatcher : IDisposable
{
    internal const string ThreadName = "Playwright dispatcher";

    // Continuation chains run inline and can nest a few dozen async frames deep.
    private const int StackSize = 16 * 1024 * 1024;

    private readonly BlockingCollection<Action> _queue = new(new ConcurrentQueue<Action>());
    private readonly Queue<Action> _microtasks = new();
    private readonly Thread _thread;
    private volatile bool _disposed;

    internal Dispatcher()
    {
        _thread = new Thread(Run, StackSize)
        {
            IsBackground = true,
            Name = ThreadName,
        };
        _thread.Start();
    }

    internal event EventHandler<Exception>? UnhandledException;

    internal bool IsCurrent => Thread.CurrentThread == _thread;

    /// <summary>The API call that the current async flow is inside of; see <see cref="Connection.ApiZone"/>.</summary>
    internal AsyncLocal<ApiZone?> ApiZone { get; } = new();

    /// <summary>Awaitable that continues on a thread pool thread. Never completes synchronously.</summary>
    internal static ThreadPoolAwaitable ExitToThreadPool() => default;

    /// <summary>Runs <paramref name="action"/> on the dispatcher thread as its own item, after the items already queued.</summary>
    internal void Post(Action action)
    {
        if (!_disposed)
        {
            try
            {
                _queue.Add(action);
                return;
            }
            catch (InvalidOperationException)
            {
                // Disposed concurrently; ObjectDisposedException derives from it.
            }
        }

        // The thread is gone. Run the action on the thread pool so that nothing waits forever;
        // after the connection has closed there is no state left to protect.
        ThreadPool.UnsafeQueueUserWorkItem(static state => ((Action)state)(), action);
    }

    /// <summary>
    /// Runs <paramref name="action"/> on the dispatcher thread: at once when already there, as its
    /// own item otherwise. For synchronous API that has work for the dispatcher thread and cannot
    /// wait for it: the work is ordered with the caller's earlier and later calls.
    /// </summary>
    internal void RunOrPost(Action action)
    {
        if (IsCurrent)
        {
            action();
        }
        else
        {
            Post(action);
        }
    }

    /// <summary>
    /// Runs <paramref name="action"/> on the dispatcher thread after the current item and before
    /// the next one: the .NET form of a microtask. Called from another thread, it posts.
    /// </summary>
    internal void RunAfterCurrentItem(Action action)
    {
        if (IsCurrent)
        {
            _microtasks.Enqueue(action);
        }
        else
        {
            Post(action);
        }
    }

    /// <summary>Completes <paramref name="tcs"/> after the current item; see <see cref="RunAfterCurrentItem"/>.</summary>
    internal void Complete<T>(TaskCompletionSource<T> tcs, T result)
        => RunAfterCurrentItem(() => tcs.TrySetResult(result));

    /// <summary>Faults <paramref name="tcs"/> after the current item; see <see cref="RunAfterCurrentItem"/>.</summary>
    internal void Fail<T>(TaskCompletionSource<T> tcs, Exception exception)
        => RunAfterCurrentItem(() => tcs.TrySetException(exception));

    /// <summary>Awaitable that continues on the dispatcher thread. Completes synchronously when already there.</summary>
    internal SwitchToAwaitable SwitchTo() => new(this);

    [Conditional("DEBUG")]
    internal void AssertCurrent()
    {
        // After the connection has closed the thread is gone, and calls run on the thread pool.
        if (!IsCurrent && !_disposed)
        {
            throw new InvalidOperationException("Playwright state must only be touched on the dispatcher thread.");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        // The thread runs what is already queued and exits.
        _queue.CompleteAdding();
    }

    private void Run()
    {
        foreach (var item in _queue.GetConsumingEnumerable())
        {
            RunItem(item);
            while (_microtasks.Count > 0)
            {
                RunItem(_microtasks.Dequeue());
            }
        }
        _queue.Dispose();
    }

    private void RunItem(Action item)
    {
        try
        {
            item();
        }
        catch (Exception ex)
        {
            UnhandledException?.Invoke(this, ex);
        }

        // A continuation is only inlined on a thread without a SynchronizationContext. Drop one
        // that user code installed here, or library continuations would move to the thread pool.
        if (SynchronizationContext.Current != null)
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }

    internal readonly struct SwitchToAwaitable : ICriticalNotifyCompletion
    {
        private readonly Dispatcher _dispatcher;

        internal SwitchToAwaitable(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public bool IsCompleted => _dispatcher.IsCurrent;

        public SwitchToAwaitable GetAwaiter() => this;

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation)
        {
            var context = ExecutionContext.Capture();
            if (context == null)
            {
                _dispatcher.Post(continuation);
            }
            else
            {
                _dispatcher.Post(() => ExecutionContext.Run(context, static state => ((Action)state)(), continuation));
            }
        }

        public void UnsafeOnCompleted(Action continuation) => _dispatcher.Post(continuation);
    }

    internal readonly struct ThreadPoolAwaitable : ICriticalNotifyCompletion
    {
        public bool IsCompleted => false;

        public ThreadPoolAwaitable GetAwaiter() => this;

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation)
            => ThreadPool.QueueUserWorkItem(static state => ((Action)state)(), continuation);

        public void UnsafeOnCompleted(Action continuation)
            => ThreadPool.UnsafeQueueUserWorkItem(static state => ((Action)state)(), continuation);
    }
}
