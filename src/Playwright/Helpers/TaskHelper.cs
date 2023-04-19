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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Helpers;

/// <summary>
/// Task helper.
/// </summary>
[SuppressMessage("Microsoft.VisualStudio.Threading.Analyzers", "VSTHRD200", Justification = "Calling the method WithTimeoutAsync doesn't make any sense.")]
internal static class TaskHelper
{
    private static readonly Func<TimeSpan, Exception> _defaultExceptionFactory =
        timeout => new TimeoutException($"Timeout of {timeout.TotalMilliseconds}ms exceeded");

    // Recipe from https://blogs.msdn.microsoft.com/pfxteam/2012/10/05/how-do-i-cancel-non-cancelable-async-operations/

    /// <summary>
    /// Cancels the <paramref name="task"/> after <paramref name="milliseconds"/> milliseconds.
    /// </summary>
    /// <returns>The task result.</returns>
    /// <param name="task">Task to wait for.</param>
    /// <param name="milliseconds">Milliseconds timeout.</param>
    /// <param name="exceptionFactory">Optional timeout exception factory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task WithTimeout(
        this Task task,
        int milliseconds,
        Func<TimeSpan, Exception> exceptionFactory = null,
        CancellationToken cancellationToken = default)
        => WithTimeout(task, TimeSpan.FromMilliseconds(milliseconds), exceptionFactory, cancellationToken);

    /// <summary>
    /// Cancels the <paramref name="task"/> after a given <paramref name="timeout"/> period.
    /// </summary>
    /// <returns>The task result.</returns>
    /// <param name="task">Task to wait for.</param>
    /// <param name="timeout">The timeout period.</param>
    /// <param name="exceptionFactory">Optional timeout exception factory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task WithTimeout(
        this Task task,
        TimeSpan timeout,
        Func<TimeSpan, Exception> exceptionFactory = null,
        CancellationToken cancellationToken = default)
        => task.WithTimeout(
            () => throw (exceptionFactory ?? _defaultExceptionFactory)(timeout),
            timeout,
            cancellationToken);

    /// <summary>
    /// Cancels the <paramref name="task"/> after <paramref name="timeout"/> milliseconds.
    /// </summary>
    /// <returns>The task result.</returns>
    /// <param name="task">Task to wait for.</param>
    /// <param name="timeoutAction">Action to be executed on Timeout.</param>
    /// <param name="timeout">Milliseconds timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task WithTimeout(
        this Task task,
        Func<Task> timeoutAction,
        int timeout = 1_000,
        CancellationToken cancellationToken = default)
        => WithTimeout(task, timeoutAction, TimeSpan.FromMilliseconds(timeout), cancellationToken);

    /// <summary>
    /// Cancels the <paramref name="task"/> after a given <paramref name="timeout"/> period.
    /// </summary>
    /// <returns>The task result.</returns>
    /// <param name="task">Task to wait for.</param>
    /// <param name="timeoutAction">Action to be executed on Timeout.</param>
    /// <param name="timeout">The timeout period.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task WithTimeout(this Task task, Func<Task> timeoutAction, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (task == null)
        {
            return;
        }

        if (await TimeoutTask(task, timeout).ConfigureAwait(false) && !cancellationToken.IsCancellationRequested && timeoutAction != null)
        {
            await timeoutAction().ConfigureAwait(false);
        }

        await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Cancels the <paramref name="task"/> after <paramref name="timeout"/> milliseconds.
    /// </summary>
    /// <returns>The task result.</returns>
    /// <param name="task">Task to wait for.</param>
    /// <param name="timeoutAction">Action to be executed on Timeout.</param>
    /// <param name="timeout">Milliseconds timeout.</param>
    /// <typeparam name="T">Return type.</typeparam>
    public static Task<T> WithTimeout<T>(this Task<T> task, Action timeoutAction, int timeout = 1_000)
        => WithTimeout(task, timeoutAction, TimeSpan.FromMilliseconds(timeout));

    /// <summary>
    /// Cancels the <paramref name="task"/> after a given <paramref name="timeout"/> period.
    /// </summary>
    /// <returns>The task result.</returns>
    /// <param name="task">Task to wait for.</param>
    /// <param name="timeoutAction">Action to be executed on Timeout.</param>
    /// <param name="timeout">The timeout period.</param>
    /// <typeparam name="T">Return type.</typeparam>
    public static async Task<T> WithTimeout<T>(this Task<T> task, Action timeoutAction, TimeSpan timeout)
    {
        if (task == null)
        {
            return default;
        }

        if (await TimeoutTask(task, timeout).ConfigureAwait(false) && timeoutAction != null)
        {
            timeoutAction();
            return default;
        }

        return await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Cancels the <paramref name="task"/> after <paramref name="milliseconds"/> milliseconds.
    /// </summary>
    /// <returns>The task result.</returns>
    /// <param name="task">Task to wait for.</param>
    /// <param name="milliseconds">Milliseconds timeout.</param>
    /// <param name="exceptionFactory">Optional timeout exception factory.</param>
    /// <typeparam name="T">Task return type.</typeparam>
    public static Task<T> WithTimeout<T>(this Task<T> task, int milliseconds = 1_000, Func<TimeSpan, Exception> exceptionFactory = null)
        => WithTimeout(task, TimeSpan.FromMilliseconds(milliseconds), exceptionFactory);

    /// <summary>
    /// Cancels the <paramref name="task"/> after a given <paramref name="timeout"/> period.
    /// </summary>
    /// <returns>The task result.</returns>
    /// <param name="task">Task to wait for.</param>
    /// <param name="timeout">The timeout period.</param>
    /// <param name="exceptionFactory">Optional timeout exception factory.</param>
    /// <typeparam name="T">Task return type.</typeparam>
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout, Func<TimeSpan, Exception> exceptionFactory = null)
    {
        if (task == null)
        {
            return default;
        }

        if (await TimeoutTask(task, timeout).ConfigureAwait(false))
        {
            throw (exceptionFactory ?? _defaultExceptionFactory)(timeout);
        }

        return await task.ConfigureAwait(false);
    }

    private static async Task<bool> TimeoutTask(Task task, TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            await task.ConfigureAwait(false);
            return false;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellationToken = new CancellationTokenSource(timeout);
        using (cancellationToken.Token.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
        {
            if (task != await Task.WhenAny(task, tcs.Task).ConfigureAwait(false))
            {
                task.IgnoreException();
                return true;
            }
            return false;
        }
    }

    public static void IgnoreException(this Task task)
    {
        _ = task.ContinueWith(t => t.Exception.Handle(_ => true), CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
    }
}
