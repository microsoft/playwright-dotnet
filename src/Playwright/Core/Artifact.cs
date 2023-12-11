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
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Artifact : ChannelOwner
{
    internal Artifact(ChannelOwner parent, string guid, ArtifactInitializer initializer) : base(parent, guid)
    {
        AbsolutePath = initializer.AbsolutePath;
    }

    internal string AbsolutePath { get; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<string> PathAfterFinishedAsync()
    {
        if (_connection.IsRemote)
        {
            throw new PlaywrightException("Path is not available when connecting remotely. Use SaveAsAsync() to save a local copy.");
        }
        return (await SendMessageToServerAsync<JsonElement?>("pathAfterFinished")
            .ConfigureAwait(false)).GetString("value", true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task SaveAsAsync(string path)
    {
        if (!_connection.IsRemote)
        {
            await SendMessageToServerAsync(
            "saveAs",
            new Dictionary<string, object>
            {
                ["path"] = path,
            }).ConfigureAwait(false);
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
        var stream = (await SendMessageToServerAsync("saveAsStream")
            .ConfigureAwait(false)).GetObject<Stream>("stream", _connection);
        await using (stream.ConfigureAwait(false))
        {
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                await stream.StreamImpl.CopyToAsync(fileStream, bufferSize: 1024 * 1024).ConfigureAwait(false);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<System.IO.Stream> CreateReadStreamAsync()
    {
        var stream = (await SendMessageToServerAsync<JsonElement?>("stream")
            .ConfigureAwait(false))?.GetObject<Stream>("stream", _connection);
        return stream.StreamImpl;
    }

    internal Task CancelAsync() => SendMessageToServerAsync("cancel");

    internal async Task<string> FailureAsync() => (await SendMessageToServerAsync<JsonElement?>("failure")
            .ConfigureAwait(false)).GetString("error", true);

    internal Task DeleteAsync() => SendMessageToServerAsync("delete");
}
