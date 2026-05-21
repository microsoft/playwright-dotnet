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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Tracing : ChannelOwner, ITracing
{
    private readonly Dictionary<string, HarRecorder> _harRecorders = new();
    internal string? _tracesDir;
    private bool _includeSources;
    private string? _stacksId;
    private bool _isTracing;
    private string? _harId;

    public Tracing(ChannelOwner parent, string guid) : base(parent, guid)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task StartAsync(TracingStartOptions? options = default)
    {
        _includeSources = options?.Sources == true;
        await SendMessageToServerAsync(
        "tracingStart",
        new Dictionary<string, object?>
        {
            ["name"] = options?.Name,
            ["title"] = options?.Title,
            ["screenshots"] = options?.Screenshots,
            ["snapshots"] = options?.Snapshots,
            ["sources"] = options?.Sources,
            ["live"] = options?.Live,
        }).ConfigureAwait(false);
        var traceName = (await SendMessageToServerAsync("tracingStartChunk", new Dictionary<string, object?>
        {
            ["title"] = options?.Title,
            ["name"] = options?.Name,
        }).ConfigureAwait(false))!.Value.GetProperty("traceName").ToString();
        await StartCollectingStacksAsync(traceName).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task StartChunkAsync(TracingStartChunkOptions? options = default)
    {
        var traceName = (await SendMessageToServerAsync("tracingStartChunk", new Dictionary<string, object?>
        {
            ["title"] = options?.Title,
            ["name"] = options?.Name,
        }).ConfigureAwait(false))!.Value.GetProperty("traceName").ToString();
        await StartCollectingStacksAsync(traceName).ConfigureAwait(false);
    }

    private async Task StartCollectingStacksAsync(string traceName)
    {
        if (!_isTracing)
        {
            _isTracing = true;
            _connection.SetIsTracing(true);
        }
        if (_connection.LocalUtils != null)
        {
            _stacksId = await _connection.LocalUtils.TracingStartedAsync(tracesDir: _tracesDir, traceName: traceName).ConfigureAwait(false);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task StopChunkAsync(TracingStopChunkOptions? options = default) => DoStopChunkAsync(filePath: options?.Path);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task StopAsync(TracingStopOptions? options = default)
    {
        await StopChunkAsync(new() { Path = options?.Path }).ConfigureAwait(false);
        await SendMessageToServerAsync("tracingStop").ConfigureAwait(false);
    }

    private async Task DoStopChunkAsync(string? filePath)
    {
        ResetStackCounter();

        if (string.IsNullOrEmpty(filePath) || filePath == null)
        {
            // Not interested in artifacts.
            await _TracingStopChunkAsync("discard").ConfigureAwait(false);
            if (!_stacksId.IsNullOrEmpty())
            {
                if (_connection.LocalUtils != null)
                {
                    await _connection.LocalUtils.TraceDiscardedAsync(stacksId: _stacksId).ConfigureAwait(false);
                }
            }
            return;
        }

        bool isLocal = !_connection.IsRemote;

        if (isLocal)
        {
            var (_, sourceEntries) = await _TracingStopChunkAsync("entries").ConfigureAwait(false);
            if (_connection.LocalUtils != null)
            {
                await _connection.LocalUtils.ZipAsync(filePath, sourceEntries!, "write", _stacksId, _includeSources).ConfigureAwait(false);
            }
            return;
        }

        var (artifact, _) = await _TracingStopChunkAsync("archive").ConfigureAwait(false);

        // The artifact may be missing if the browser closed while stopping tracing.
        if (artifact == null)
        {
            if (!_stacksId.IsNullOrEmpty())
            {
                if (_connection.LocalUtils != null)
                {
                    await _connection.LocalUtils.TraceDiscardedAsync(stacksId: _stacksId).ConfigureAwait(false);
                }
            }
            return;
        }

        // Save trace to the final local file.
        await artifact.SaveAsAsync(filePath).ConfigureAwait(false);
        await artifact.DeleteAsync().ConfigureAwait(false);

        if (_connection.LocalUtils != null)
        {
            await _connection.LocalUtils.ZipAsync(filePath, new(), "append", _stacksId, _includeSources).ConfigureAwait(false);
        }
    }

    internal async Task<(Artifact? Artifact, List<NameValue?>? Entries)> _TracingStopChunkAsync(string mode)
    {
        var result = await SendMessageToServerAsync("tracingStopChunk", new Dictionary<string, object?>
        {
            ["mode"] = mode,
        }).ConfigureAwait(false);

        var artifact = result.GetObject<Artifact>("artifact", _connection);
        List<NameValue?> entries = new();
        if (result!.Value.TryGetProperty("entries", out var entriesElement))
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

    public async Task<IAsyncDisposable> GroupAsync(string name, TracingGroupOptions? options = null)
    {
        await SendMessageToServerAsync("tracingGroup", new Dictionary<string, object?>
        {
            ["name"] = name,
            ["location"] = options?.Location,
        }).ConfigureAwait(false);
        return new DisposableStub(async () =>
        {
            await GroupEndAsync().ConfigureAwait(false);
        });
    }

    public Task GroupEndAsync()
        => SendMessageToServerAsync("tracingGroupEnd");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IAsyncDisposable> StartHarAsync(string path, TracingStartHarOptions? options = default)
    {
        if (_harId != null)
        {
            throw new PlaywrightException("HAR recording has already been started");
        }
        var defaultContent = path.EndsWith(".zip", StringComparison.Ordinal) ? HarContentPolicy.Attach : HarContentPolicy.Embed;
        _harId = await RecordIntoHarAsync(
            path,
            null,
            options?.Content ?? defaultContent,
            options?.UrlFilter ?? options?.UrlFilterString,
            options?.UrlFilterRegex,
            options?.Mode ?? HarMode.Full,
            null).ConfigureAwait(false);
        return new DisposableStub(() => StopHarAsync());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task StopHarAsync()
    {
        var harId = _harId;
        if (harId == null)
        {
            throw new PlaywrightException("HAR recording has not been started");
        }
        _harId = null;
        await ExportHarAsync(harId).ConfigureAwait(false);
    }

    internal async Task<string> RecordIntoHarAsync(string har, Page? page, HarContentPolicy? content, string? urlGlob, Regex? urlRegex, HarMode? mode, string? resourcesDir)
    {
        var isZip = har.EndsWith(".zip", StringComparison.Ordinal);
        var recordHarArgs = new Dictionary<string, object?>();
        var contentPolicy = content ?? HarContentPolicy.Attach;
        recordHarArgs["content"] = contentPolicy;
        if (!isZip)
        {
            recordHarArgs["harPath"] = har;
        }
        if (!string.IsNullOrEmpty(resourcesDir))
        {
            recordHarArgs["resourcesDir"] = resourcesDir;
        }
        if (!string.IsNullOrEmpty(urlGlob))
        {
            recordHarArgs["urlGlob"] = urlGlob;
        }
        if (urlRegex != null)
        {
            var (source, flags) = urlRegex.GetSourceAndFlags();
            recordHarArgs["urlRegexSource"] = source;
            recordHarArgs["urlRegexFlags"] = flags;
        }
        recordHarArgs["mode"] = mode ?? HarMode.Minimal;

        var harId = (await SendMessageToServerAsync("harStart", new Dictionary<string, object?>
        {
            ["page"] = page,
            ["options"] = recordHarArgs,
        }).ConfigureAwait(false))!.Value.GetProperty("harId").ToString();
        _harRecorders.Add(harId, new(har, contentPolicy, resourcesDir));
        return harId;
    }

    internal async Task ExportAllHarsAsync()
    {
        foreach (var harId in _harRecorders.Keys.ToArray())
        {
            await ExportHarAsync(harId).ConfigureAwait(false);
        }
    }

    private async Task ExportHarAsync(string harId)
    {
        if (!_harRecorders.TryGetValue(harId, out var recorder))
        {
            return;
        }
        _harRecorders.Remove(harId);
        var isZip = recorder.Path.EndsWith(".zip", StringComparison.Ordinal);
        var isLocal = !_connection.IsRemote;

        if (isLocal)
        {
            var entriesResult = await SendMessageToServerAsync("harExport", new Dictionary<string, object?>
            {
                ["harId"] = harId,
                ["mode"] = "entries",
            }).ConfigureAwait(false);
            if (!isZip)
            {
                // Server wrote HAR and resources to the user's chosen paths.
                return;
            }
            if (_connection.LocalUtils == null)
            {
                throw new PlaywrightException("Cannot save zipped HAR in thin clients");
            }
            List<NameValue> entries = new();
            if (entriesResult!.Value.TryGetProperty("entries", out var entriesElement))
            {
                foreach (var current in entriesElement.EnumerateArray())
                {
                    var entry = current.Deserialize<NameValue>();
                    if (entry != null)
                    {
                        entries.Add(entry);
                    }
                }
            }
            await _connection.LocalUtils.ZipAsync(recorder.Path, entries, "write", null, false).ConfigureAwait(false);
            return;
        }

        var archiveResult = await SendMessageToServerAsync("harExport", new Dictionary<string, object?>
        {
            ["harId"] = harId,
            ["mode"] = "archive",
        }).ConfigureAwait(false);
        var artifact = archiveResult.GetObject<Artifact>("artifact", _connection);
        if (artifact == null)
        {
            return;
        }
        // The server always returns a zipped artifact in archive mode.
        // Unzip when the target file is not a .zip.
        if (!isZip)
        {
            if (_connection.LocalUtils == null)
            {
                throw new PlaywrightException("Uncompressed HAR is not supported in thin clients");
            }
            await artifact.SaveAsAsync(recorder.Path + ".tmp").ConfigureAwait(false);
            await _connection.LocalUtils.HarUnzipAsync(recorder.Path + ".tmp", recorder.Path, recorder.ResourcesDir).ConfigureAwait(false);
        }
        else
        {
            await artifact.SaveAsAsync(recorder.Path).ConfigureAwait(false);
        }
        await artifact.DeleteAsync().ConfigureAwait(false);
    }

    internal class HarRecorder
    {
        internal HarRecorder(string path, HarContentPolicy? content, string? resourcesDir)
        {
            Path = path;
            Content = content;
            ResourcesDir = resourcesDir;
        }

        internal string Path { get; }

        internal HarContentPolicy? Content { get; }

        internal string? ResourcesDir { get; }
    }
}
