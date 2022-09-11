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
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Worker : ChannelOwnerBase, IChannelOwner<Worker>, IWorker
{
    private readonly WorkerChannel _channel;
    private readonly WorkerInitializer _initializer;

    public Worker(IChannelOwner parent, string guid, WorkerInitializer initializer) : base(parent, guid)
    {
        _channel = new(guid, parent.Connection, this);
        _initializer = initializer;

        _channel.Close += (_, _) =>
        {
            if (Page != null)
            {
                Page._workers.Remove(this);
            }

            if (Context != null)
            {
                Context._serviceWorkers.Remove(this);
            }

            Close?.Invoke(this, this);
            ClosedTcs.SetResult(true);
        };
    }

    public event EventHandler<IWorker> Close;

    public string Url => _initializer.Url;

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<Worker> IChannelOwner<Worker>.Channel => _channel;

    internal Page Page { get; set; }

    internal BrowserContext Context { get; set; }

    internal TaskCompletionSource<bool> ClosedTcs { get; } = new();

    public async Task<T> EvaluateAsync<T>(string expression, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
            expression,
            null,
            ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

    public async Task<IJSHandle> EvaluateHandleAsync(string expression, object arg = null)
        => await _channel.EvaluateExpressionHandleAsync(
            expression,
            null,
            ScriptsHelper.SerializedArgument(arg))
        .ConfigureAwait(false);

    public async Task<IWorker> WaitForCloseAsync(Func<Task> action = default, float? timeout = default)
    {
        using var waiter = new Waiter(this, "worker.WaitForCloseAsync");
        var waiterResult = waiter.GetWaitForEventTask<IWorker>(this, nameof(Close), null);
        var result = waiterResult.Task.WithTimeout(Convert.ToInt32(timeout ?? 0));
        if (action != null)
        {
            await WrapApiBoundaryAsync(() => Task.WhenAll(result, action())).ConfigureAwait(false);
        }
        else
        {
            await result.ConfigureAwait(false);
        }

        return this;
    }
}
