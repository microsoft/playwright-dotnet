/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Core;

internal class FunctionAssertions : IFunctionAssertions
{
    private static readonly int[] _defaultIntervals = { 100, 250, 500, 1000 };
    private static float _defaultTimeout = 30_000;
    private readonly Func<Task> _callback;
    private readonly string? _customMessage;

    public FunctionAssertions(Func<Task> callback, string? customMessage = null)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _customMessage = customMessage;
    }

    public FunctionAssertions(Action callback, string? customMessage = null)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }
        _callback = () =>
        {
            callback();
            return Task.CompletedTask;
        };
        _customMessage = customMessage;
    }

    internal static Func<Func<Task>, Task> AttemptRunner { get; set; } = callback => callback();

    public static void SetDefaultTimeout(float timeout) => _defaultTimeout = timeout;

    public async Task ToPassAsync(FunctionAssertionsToPassOptions? options = default)
    {
        var timeout = options?.Timeout ?? _defaultTimeout;
        var intervals = options?.Intervals?.ToArray() ?? _defaultIntervals;
        var stopwatch = Stopwatch.StartNew();
        Exception? lastError = null;
        for (var attempt = 0; ; attempt++)
        {
            var remaining = timeout - stopwatch.Elapsed.TotalMilliseconds;
            if (timeout > 0 && remaining <= 0)
            {
                break;
            }
            var task = RunCallbackAsync();
            if (timeout > 0 && !await CompletesWithinAsync(task, remaining).ConfigureAwait(false))
            {
                break;
            }
            try
            {
                await task.ConfigureAwait(false);
                return;
            }
            catch (Exception e)
            {
                lastError = e;
            }

            var interval = intervals.Length == 0 ? 1000 : intervals[Math.Min(attempt, intervals.Length - 1)];
            if (timeout > 0 && stopwatch.Elapsed.TotalMilliseconds + interval >= timeout)
            {
                break;
            }
            await Task.Delay(interval).ConfigureAwait(false);
        }

        var prefix = _customMessage != null ? _customMessage + "\n\n" : string.Empty;
        var message = $"Timeout {timeout}ms exceeded while waiting on the predicate";
        if (lastError == null)
        {
            throw new PlaywrightAssertionException($"{prefix}{message}");
        }
        throw new PlaywrightAssertionException($"{prefix}{lastError.Message}\nCall log:\n  - {message}", lastError);
    }

    private static async Task<bool> CompletesWithinAsync(Task task, double timeout)
    {
        using var cts = new CancellationTokenSource();
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromMilliseconds(timeout), cts.Token)).ConfigureAwait(false) == task;
        cts.Cancel();
        if (!completed)
        {
            _ = task.ContinueWith(t => t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }
        return completed;
    }

    private async Task RunCallbackAsync() => await AttemptRunner(_callback).ConfigureAwait(false);
}
