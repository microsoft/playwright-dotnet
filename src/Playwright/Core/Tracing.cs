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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Tracing : ChannelOwner, ITracing
{
    internal string _tracesDir;
    private bool _includeSources;
    private string _stacksId;
    private bool _isTracing;

    public Tracing(ChannelOwner parent, string guid) : base(parent, guid)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task StartAsync(
        TracingStartOptions options = default)
    {
        _includeSources = options?.Sources == true;
        var traceName = await _connection.WrapApiCallAsync(
            async () =>
        {
            await SendMessageToServerAsync(
            "tracingStart",
            new Dictionary<string, object>
            {
                ["name"] = options?.Name,
                ["title"] = options?.Title,
                ["screenshots"] = options?.Screenshots,
                ["snapshots"] = options?.Snapshots,
                ["sources"] = options?.Sources,
            }).ConfigureAwait(false);
            return (await SendMessageToServerAsync("tracingStartChunk", new Dictionary<string, object>
            {
                ["title"] = options?.Title,
                ["name"] = options?.Name,
            }).ConfigureAwait(false))?.GetProperty("traceName").ToString();
        },
            true).ConfigureAwait(false);
        await StartCollectingStacksAsync(traceName).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task StartChunkAsync(TracingStartChunkOptions options = default)
    {
        var traceName = (await SendMessageToServerAsync("tracingStartChunk", new Dictionary<string, object>
        {
            ["title"] = options?.Title,
            ["name"] = options?.Name,
        }).ConfigureAwait(false))?.GetProperty("traceName").ToString();
        await StartCollectingStacksAsync(traceName).ConfigureAwait(false);
    }

    private async Task StartCollectingStacksAsync(string traceName)
    {
        if (!_isTracing)
        {
            _isTracing = true;
            _connection.SetIsTracing(true);
        }
        _stacksId = await _connection.LocalUtils.TracingStartedAsync(tracesDir: _tracesDir, traceName: traceName).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task StopChunkAsync(TracingStopChunkOptions options = default) => _connection.WrapApiCallAsync(() => DoStopChunkAsync(filePath: options?.Path), true);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task StopAsync(TracingStopOptions options = default)
    {
        await _connection.WrapApiCallAsync(
            async () =>
            {
                await StopChunkAsync(new() { Path = options?.Path }).ConfigureAwait(false);
                await SendMessageToServerAsync("tracingStop").ConfigureAwait(false);
            },
            true).ConfigureAwait(false);
    }

    private async Task DoStopChunkAsync(string filePath)
    {
        ResetStackCounter();

        if (string.IsNullOrEmpty(filePath))
        {
            // Not interested in artifacts.
            await _TracingStopChunkAsync("discard").ConfigureAwait(false);
            if (!string.IsNullOrEmpty(_stacksId))
            {
                await _connection.LocalUtils.TraceDiscardedAsync(stacksId: _stacksId).ConfigureAwait(false);
            }
            return;
        }

        bool isLocal = !_connection.IsRemote;

        if (isLocal)
        {
            var (_, sourceEntries) = await _TracingStopChunkAsync("entries").ConfigureAwait(false);
            await _connection.LocalUtils.ZipAsync(filePath, sourceEntries, "write", _stacksId, _includeSources).ConfigureAwait(false);
            return;
        }

        var (artifact, _) = await _TracingStopChunkAsync("archive").ConfigureAwait(false);

        // The artifact may be missing if the browser closed while stopping tracing.
        if (artifact == null)
        {
            if (!string.IsNullOrEmpty(_stacksId))
            {
                await _connection.LocalUtils.TraceDiscardedAsync(stacksId: _stacksId).ConfigureAwait(false);
            }
            return;
        }

        // Save trace to the final local file.
        await artifact.SaveAsAsync(filePath).ConfigureAwait(false);
        await artifact.DeleteAsync().ConfigureAwait(false);

        await _connection.LocalUtils.ZipAsync(filePath, new(), "append", _stacksId, _includeSources).ConfigureAwait(false);
    }

    internal async Task<(Artifact Artifact, List<NameValue> Entries)> _TracingStopChunkAsync(string mode)
    {
        var result = await SendMessageToServerAsync("tracingStopChunk", new Dictionary<string, object>
        {
            ["mode"] = mode,
        }).ConfigureAwait(false);

        var artifact = result.GetObject<Artifact>("artifact", _connection);
        List<NameValue> entries = new();
        if (result.Value.TryGetProperty("entries", out var entriesElement))
        {
            foreach (var current in entriesElement.EnumerateArray())
            {
                entries.Add(current.Deserialize<NameValue>());
            }
        }
        return (artifact, entries);
    }

    internal void ResetStackCounter()
    {
        if (_isTracing)
        {
            _isTracing = false;
            _connection.SetIsTracing(false);
        }
    }
}
