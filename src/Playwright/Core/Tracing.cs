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

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core;

internal class Tracing : ChannelOwnerBase, IChannelOwner<Tracing>, ITracing
{
    internal string _tracesDir;
    private readonly TracingChannel _channel;
    private bool _includeSources;
    private string _stacksId;
    private bool _isTracing;

    public Tracing(IChannelOwner parent, string guid) : base(parent, guid)
    {
        _channel = new(guid, parent.Connection, this);
    }

    ChannelBase IChannelOwner.Channel => _channel;

    Channel<Tracing> IChannelOwner<Tracing>.Channel => _channel;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task StartAsync(
        TracingStartOptions options = default)
    {
        _includeSources = options?.Sources == true;
        var traceName = await _channel.Connection.WrapApiCallAsync(
            async () =>
        {
            await _channel.TracingStartAsync(
                name: options?.Name,
                title: options?.Title,
                screenshots: options?.Screenshots,
                snapshots: options?.Snapshots,
                sources: options?.Sources).ConfigureAwait(false);
            return await _channel.StartChunkAsync(title: options?.Title, name: options?.Name).ConfigureAwait(false);
        },
            true).ConfigureAwait(false);
        await StartCollectingStacksAsync(traceName).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task StartChunkAsync(TracingStartChunkOptions options = default)
    {
        var traceName = await _channel.StartChunkAsync(title: options?.Title, name: options?.Name).ConfigureAwait(false);
        await StartCollectingStacksAsync(traceName).ConfigureAwait(false);
    }

    private async Task StartCollectingStacksAsync(string traceName)
    {
        if (!_isTracing)
        {
            _isTracing = true;
            _channel.Connection.SetIsTracing(true);
        }
        _stacksId = await _channel.Connection.LocalUtils.TracingStartedAsync(tracesDir: _tracesDir, traceName: traceName).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task StopChunkAsync(TracingStopChunkOptions options = default) => _channel.Connection.WrapApiCallAsync(() => DoStopChunkAsync(filePath: options?.Path), true);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task StopAsync(TracingStopOptions options = default)
    {
        await _channel.Connection.WrapApiCallAsync(
            async () =>
            {
                await StopChunkAsync(new() { Path = options?.Path }).ConfigureAwait(false);
                await _channel.TracingStopAsync().ConfigureAwait(false);
            },
            true).ConfigureAwait(false);
    }

    private async Task DoStopChunkAsync(string filePath)
    {
        if (_isTracing)
        {
            _isTracing = false;
            _channel.Connection.SetIsTracing(false);
        }

        if (string.IsNullOrEmpty(filePath))
        {
            // Not interested in artifacts.
            await _channel.TracingStopChunkAsync("discard").ConfigureAwait(false);
            if (!string.IsNullOrEmpty(_stacksId))
            {
                await _channel.Connection.LocalUtils.TraceDiscardedAsync(stacksId: _stacksId).ConfigureAwait(false);
            }
            return;
        }

        bool isLocal = !_channel.Connection.IsRemote;

        if (isLocal)
        {
            var (_, sourceEntries) = await _channel.TracingStopChunkAsync("entries").ConfigureAwait(false);
            await _channel.Connection.LocalUtils.ZipAsync(filePath, sourceEntries, "write", _stacksId, _includeSources).ConfigureAwait(false);
            return;
        }

        var (artifact, entries) = await _channel.TracingStopChunkAsync("archive").ConfigureAwait(false);

        // The artifact may be missing if the browser closed while stopping tracing.
        if (artifact == null)
        {
            if (!string.IsNullOrEmpty(_stacksId))
            {
                await _channel.Connection.LocalUtils.TraceDiscardedAsync(stacksId: _stacksId).ConfigureAwait(false);
            }
            return;
        }

        // Save trace to the final local file.
        await artifact.SaveAsAsync(filePath).ConfigureAwait(false);
        await artifact.DeleteAsync().ConfigureAwait(false);

        await _channel.Connection.LocalUtils.ZipAsync(filePath, new(), "append", _stacksId, _includeSources).ConfigureAwait(false);
    }
}
