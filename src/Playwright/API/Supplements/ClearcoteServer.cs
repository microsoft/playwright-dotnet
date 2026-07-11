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
 * IMPLIED, BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright;

/// <summary>
/// Handle for a standing Clearcote CDP endpoint.
/// Use <see cref="ClearcoteBrowser.ServeAsync"/> to create one.
/// </summary>
public sealed class ClearcoteServer : IAsyncDisposable
{
    private readonly Process _process;
    private readonly string _userDataDir;
    private readonly bool _ownsUserDataDir;
    private readonly LeaseSession? _lease;
    private bool _disposed;

    internal ClearcoteServer(Process process, string host, int port, string userDataDir, bool ownsUserDataDir, LeaseSession? lease = null)
    {
        _process = process;
        _userDataDir = userDataDir;
        _ownsUserDataDir = ownsUserDataDir;
        _lease = lease;
        Host = host;
        Port = port;
    }

    /// <summary><para>CDP bind host.</para></summary>
    public string Host { get; }

    /// <summary><para>CDP port.</para></summary>
    public int Port { get; }

    /// <summary><para>HTTP CDP base URL — pass to <c>connectOverCDP</c> / Chromium CDP clients.</para></summary>
    public string CdpUrl => $"http://{Host}:{Port}";

    /// <summary><para>Whether the browser process is still alive.</para></summary>
    public bool IsAlive => !_process.HasExited;

    /// <summary>
    /// Resolve the browser-level WebSocket debugger URL.
    /// </summary>
    /// <returns>The WebSocket debugger URL, or <c>null</c> if the endpoint is not reachable.</returns>
    public async Task<string?> WsUrlAsync()
    {
        try
        {
            using var client = Helpers.Clearcote.CreateHttpClient(TimeSpan.FromSeconds(5));
            var json = await client.GetStringAsync($"{CdpUrl}/json/version").ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("webSocketDebuggerUrl", out var ws)
                && ws.ValueKind == JsonValueKind.String)
            {
                return ws.GetString();
            }
        }
        catch
        {
        }

        return null;
    }

    /// <summary>
    /// Stop the browser and clean up the profile directory.
    /// </summary>
    /// <returns>A task that completes when the browser has exited and the profile directory has been cleaned.</returns>
    public async Task CloseAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _disposed = true;

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        catch
        {
        }
        finally
        {
            _process.Dispose();
        }

        if (_lease is not null)
        {
            try
            {
                await _lease.StopAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        }

        if (_ownsUserDataDir)
        {
            try
            {
                Directory.Delete(_userDataDir, recursive: true);
            }
            catch
            {
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CloseAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
