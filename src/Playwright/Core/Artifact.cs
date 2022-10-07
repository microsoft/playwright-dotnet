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

using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Artifact : ChannelOwnerBase, IChannelOwner<Artifact>
{
    private readonly Connection _connection;
    private readonly ArtifactChannel _channel;

    internal Artifact(IChannelOwner parent, string guid, ArtifactInitializer initializer) : base(parent, guid)
    {
        _connection = parent.Connection;
        _channel = new(guid, parent.Connection, this);
        AbsolutePath = initializer.AbsolutePath;
    }

    Connection IChannelOwner.Connection => _connection;

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<Artifact> IChannelOwner<Artifact>.Channel => _channel;

    internal string AbsolutePath { get; }

    public async Task<string> PathAfterFinishedAsync()
    {
        if (_connection.IsRemote)
        {
            throw new PlaywrightException("Path is not available when connecting remotely. Use SaveAsAsync() to save a local copy.");
        }
        return await _channel.PathAfterFinishedAsync().ConfigureAwait(false);
    }

    public async Task SaveAsAsync(string path)
    {
        if (!_connection.IsRemote)
        {
            await _channel.SaveAsAsync(path).ConfigureAwait(false);
            return;
        }
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));
        var stream = await _channel.SaveAsStreamAsync().ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                await stream.StreamImpl.CopyToAsync(fileStream).ConfigureAwait(false);
            }
        }
    }

    public async Task<System.IO.Stream> CreateReadStreamAsync()
    {
        var stream = await _channel.StreamAsync().ConfigureAwait(false);
        return stream.StreamImpl;
    }

    internal Task CancelAsync() => _channel.CancelAsync();

    internal Task<string> FailureAsync() => _channel.FailureAsync();

    internal Task DeleteAsync() => _channel.DeleteAsync();
}
