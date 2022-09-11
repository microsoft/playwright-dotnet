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

using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core;

internal class Tracing : ChannelOwnerBase, IChannelOwner<Tracing>, ITracing
{
    private readonly TracingChannel _channel;

    public Tracing(IChannelOwner parent, string guid) : base(parent, guid)
    {
        _channel = new(guid, parent.Connection, this);
    }

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<Tracing> IChannelOwner<Tracing>.Channel => _channel;

    public async Task StartAsync(TracingStartOptions options = default)
    {
        await _channel.Connection.WrapApiCallAsync(async () =>
        {
            await _channel.TracingStartAsync(
                    name: options?.Name,
                    title: options?.Title,
                    screenshots: options?.Screenshots,
                    snapshots: options?.Snapshots,
                    sources: options?.Sources).ConfigureAwait(false);
            await _channel.StartChunkAsync(options?.Title).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public Task StartChunkAsync(TracingStartChunkOptions options = default) => _channel.StartChunkAsync(title: options?.Title);

    public Task StopChunkAsync(TracingStopChunkOptions options = default) => DoStopChunkAsync(filePath: options?.Path);

    public async Task StopAsync(TracingStopOptions options = default)
    {
        await _channel.Connection.WrapApiCallAsync(async () =>
        {
            await StopChunkAsync(new() { Path = options?.Path }).ConfigureAwait(false);
            await _channel.TracingStopAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private async Task DoStopChunkAsync(string filePath)
    {
        bool isLocal = !_channel.Connection.IsRemote;

        var mode = "doNotSave";
        if (!string.IsNullOrEmpty(filePath))
        {
            if (isLocal)
            {
                mode = "compressTraceAndSources";
            }
            else
            {
                mode = "compressTrace";
            }
        }

        var (artifact, sourceEntries) = await _channel.StopChunkAsync(mode).ConfigureAwait(false);

        // Not interested in artifacts.
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        // The artifact may be missing if the browser closed while stopping tracing.
        if (artifact == null)
        {
            return;
        }

        // Save trace to the final local file.
        await artifact.SaveAsAsync(filePath).ConfigureAwait(false);
        await artifact.DeleteAsync().ConfigureAwait(false);

        // Add local sources to the remote trace if necessary.
        if (sourceEntries.Count > 0)
        {
            await _channel.Connection.LocalUtils.ZipAsync(filePath, sourceEntries).ConfigureAwait(false);
        }
    }
}
