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

namespace Microsoft.Playwright.Tests.Fixtures;

public static partial class Utils
{
    /// <summary>
    /// <c>setInterval(() => { ... }, interval)</c>: the callback runs again and again, <paramref name="interval"/>
    /// milliseconds after its last run ended, never twice at the same time. The result goes to <see cref="ClearInterval"/>.
    /// </summary>
    public static CancellationTokenSource SetInterval(Action callback, int interval)
    {
        var cancellation = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    // Node.js makes an interval of 0 an interval of 1.
                    await Task.Delay(Math.Max(interval, 1), cancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                try
                {
                    callback();
                }
                catch (Exception e)
                {
                    await Console.Error.WriteLineAsync($"A setInterval callback failed: {e}").ConfigureAwait(false);
                }
            }
        });
        return cancellation;
    }

    /// <summary><c>clearInterval(timer)</c>.</summary>
    public static void ClearInterval(CancellationTokenSource timer) => timer?.Cancel();
}
