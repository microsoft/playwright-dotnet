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

using System.Diagnostics;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>The constructs of JavaScript promises and event emitters that the upstream tests use.</summary>
public static partial class Utils
{
    private static readonly System.Text.Json.JsonSerializerOptions _jsonStringifyOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// <c>await Promise.all([a, b])</c>. Unlike <c>Task.WhenAll</c> it fails as soon as one of the tasks fails,
    /// as <c>Promise.all</c> does, and does not wait for the others.
    /// </summary>
    public static async Task PromiseAll(params Task[] tasks)
    {
        var remaining = tasks.ToList();
        try
        {
            while (remaining.Count > 0)
            {
                var done = await Task.WhenAny(remaining).ConfigureAwait(false);
                remaining.Remove(done);
                await done.ConfigureAwait(false);
            }
        }
        finally
        {
            foreach (var task in remaining)
            {
                _ = task.ContinueWith(t => _ = t.Exception, TaskScheduler.Default);
            }
        }
    }

    /// <summary><c>const values = await Promise.all(promises)</c>.</summary>
    public static async Task<T[]> PromiseAll<T>(params Task<T>[] tasks)
    {
        await PromiseAll(tasks.Cast<Task>().ToArray()).ConfigureAwait(false);
        return tasks.Select(task => task.Result).ToArray();
    }

    /// <summary><c>const values = await Promise.all(promises)</c>.</summary>
    public static Task<T[]> PromiseAll<T>(IEnumerable<Task<T>> tasks) => PromiseAll(tasks.ToArray());

    /// <summary><c>await Promise.all(promises)</c>.</summary>
    public static Task PromiseAll(IEnumerable<Task> tasks) => PromiseAll(tasks.ToArray());

    /// <summary><c>const [a, b] = await Promise.all([promiseA, promiseB])</c>.</summary>
    public static async Task<(T1 First, T2 Second)> PromiseAll<T1, T2>(Task<T1> first, Task<T2> second)
    {
        await PromiseAll((Task)first, second).ConfigureAwait(false);
        return (first.Result, second.Result);
    }

    /// <summary><c>const [a] = await Promise.all([promiseA, action])</c>.</summary>
    public static async Task<T1> PromiseAll<T1>(Task<T1> first, Task second)
    {
        await PromiseAll((Task)first, second).ConfigureAwait(false);
        return first.Result;
    }

    /// <summary><c>const [, b] = await Promise.all([action, promiseB])</c>.</summary>
    public static async Task<T2> PromiseAll<T2>(Task first, Task<T2> second)
    {
        await PromiseAll(first, (Task)second).ConfigureAwait(false);
        return second.Result;
    }

    /// <summary><c>const [, b] = await Promise.all([action1, promiseB, action2])</c>.</summary>
    public static async Task<T2> PromiseAll<T2>(Task first, Task<T2> second, Task third)
    {
        await PromiseAll(first, (Task)second, third).ConfigureAwait(false);
        return second.Result;
    }

    /// <summary><c>const [a, b, c] = await Promise.all([promiseA, promiseB, promiseC])</c>.</summary>
    public static async Task<(T1 First, T2 Second, T3 Third)> PromiseAll<T1, T2, T3>(Task<T1> first, Task<T2> second, Task<T3> third)
    {
        await PromiseAll((Task)first, second, third).ConfigureAwait(false);
        return (first.Result, second.Result, third.Result);
    }

    /// <summary><c>const [a, b, c] = await Promise.all([promiseA, promiseB, promiseC, action])</c>.</summary>
    public static async Task<(T1 First, T2 Second, T3 Third)> PromiseAll<T1, T2, T3>(Task<T1> first, Task<T2> second, Task<T3> third, Task fourth)
    {
        await PromiseAll((Task)first, second, third, fourth).ConfigureAwait(false);
        return (first.Result, second.Result, third.Result);
    }

    /// <summary><c>const [a, b, c, d] = await Promise.all([promiseA, promiseB, promiseC, promiseD])</c>.</summary>
    public static async Task<(T1 First, T2 Second, T3 Third, T4 Fourth)> PromiseAll<T1, T2, T3, T4>(Task<T1> first, Task<T2> second, Task<T3> third, Task<T4> fourth)
    {
        await PromiseAll((Task)first, second, third, fourth).ConfigureAwait(false);
        return (first.Result, second.Result, third.Result, fourth.Result);
    }

    /// <summary><c>const [a, b] = await Promise.all([promiseA, promiseB, action])</c>.</summary>
    public static async Task<(T1 First, T2 Second)> PromiseAll<T1, T2>(Task<T1> first, Task<T2> second, Task third)
    {
        await PromiseAll((Task)first, second, third).ConfigureAwait(false);
        return (first.Result, second.Result);
    }

    /// <summary><c>const [a] = await Promise.all([promiseA, action1, action2])</c>.</summary>
    public static async Task<T1> PromiseAll<T1>(Task<T1> first, Task second, Task third)
    {
        await PromiseAll((Task)first, second, third).ConfigureAwait(false);
        return first.Result;
    }

    /// <summary>
    /// <c>new Promise(f => emitter.once('event', f))</c>: the argument of the next event.
    /// <c>Once&lt;IDialog&gt;(h => page.Dialog += h, h => page.Dialog -= h)</c>.
    /// </summary>
    public static Task<T> Once<T>(Action<EventHandler<T>> add, Action<EventHandler<T>> remove) => Once(add, remove, null);

    /// <summary><c>new Promise(f => emitter.once('event', f))</c> for an event without an argument.</summary>
    public static Task Once(Action<EventHandler> add, Action<EventHandler> remove)
    {
        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler handler = null;
        handler = (_, _) =>
        {
            remove(handler);
            result.TrySetResult(true);
        };
        add(handler);
        return result.Task;
    }

    /// <summary>
    /// <c>emitter.waitForEvent('event', predicate)</c> for an event that has no wait method in .NET:
    /// the argument of the next event that the predicate accepts.
    /// </summary>
    public static Task<T> Once<T>(Action<EventHandler<T>> add, Action<EventHandler<T>> remove, Func<T, bool> predicate)
    {
        var result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<T> handler = null;
        handler = (_, value) =>
        {
            try
            {
                if (predicate != null && !predicate(value))
                {
                    return;
                }
            }
            catch (Exception e)
            {
                remove(handler);
                result.TrySetException(e);
                return;
            }
            remove(handler);
            result.TrySetResult(value);
        };
        add(handler);
        return result.Task;
    }

    /// <summary><c>setTimeout(() => { ... }, timeout)</c> that upstream does not await: the callback runs later, on its own. The result goes to <see cref="ClearTimeout"/>.</summary>
    public static TimeoutHandle SetTimeout(Action callback, int timeout) => SetTimeout(
        () =>
        {
            callback();
            return Task.CompletedTask;
        },
        timeout);

    /// <summary><c>setTimeout(async () => { ... }, timeout)</c> that upstream does not await.</summary>
    public static TimeoutHandle SetTimeout(Func<Task> callback, int timeout) => new(callback, timeout);

    /// <summary>
    /// <c>clearTimeout(timer)</c>. A timer that is due runs first: the event loop of Node.js runs the due
    /// timers before the code that waited for a message of the driver.
    /// </summary>
    public static void ClearTimeout(TimeoutHandle timer) => timer?.Clear();

    /// <summary>The result of <see cref="SetTimeout(Action, int)"/>.</summary>
    public sealed class TimeoutHandle
    {
        private readonly Func<Task> _callback;
        private readonly long _due;
        private readonly CancellationTokenSource _cancellation = new();
        private int _done;

        internal TimeoutHandle(Func<Task> callback, int timeout)
        {
            _callback = callback;
            _due = Stopwatch.GetTimestamp() + (timeout * Stopwatch.Frequency / 1000);
            _ = Task.Delay(timeout, _cancellation.Token).ContinueWith(
                _ => Fire(),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }

        internal void Clear()
        {
            if (Stopwatch.GetTimestamp() >= _due)
            {
                Fire();
            }
            Interlocked.Exchange(ref _done, 1);
            _cancellation.Cancel();
        }

        private void Fire()
        {
            if (Interlocked.Exchange(ref _done, 1) == 0)
            {
                _ = RunAsync();
            }
        }

        private async Task RunAsync()
        {
            try
            {
                await _callback().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync($"A setTimeout callback failed: {e}").ConfigureAwait(false);
            }
        }
    }

    /// <summary><c>JSON.stringify(value)</c>: unlike <c>JsonSerializer.Serialize</c> it does not escape non-ASCII characters, nor &lt;, &gt;, &amp;, ' and +.</summary>
    public static string JsonStringify(object value) => System.Text.Json.JsonSerializer.Serialize(value, _jsonStringifyOptions);

    /// <summary><c>string.replace(search, replacement)</c> of JavaScript with a string: replaces the first occurrence only.</summary>
    public static string ReplaceFirst(string text, string search, string replacement)
    {
        var index = text.IndexOf(search, StringComparison.Ordinal);
        return index == -1 ? text : text.Substring(0, index) + replacement + text.Substring(index + search.Length);
    }

    /// <summary>
    /// <c>emitter.waitForEvent('event').then(value => { ... })</c>: the callback runs when the event is
    /// raised, before the driver goes on, as the callback of a promise runs before the next message in
    /// JavaScript. The task ends after the callback.
    /// </summary>
    public static Task OnceThen<T>(Action<EventHandler<T>> add, Action<EventHandler<T>> remove, Action<T> then)
    {
        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<T> handler = null;
        handler = (_, value) =>
        {
            remove(handler);
            try
            {
                then(value);
                result.TrySetResult(true);
            }
            catch (Exception e)
            {
                result.TrySetException(e);
            }
        };
        add(handler);
        return result.Task;
    }

    /// <summary><c>promise.catch(() => {})</c> that upstream does not await: the call runs, its failure is ignored.</summary>
    public static void IgnoreErrors(Task task) => _ = task.ContinueWith(t => _ = t.Exception, TaskScheduler.Default);
}
