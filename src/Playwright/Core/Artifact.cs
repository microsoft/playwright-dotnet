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
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
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

        public Task<string> PathAfterFinishedAsync() => _channel.PathAfterFinishedAsync();

        public Task SaveAsAsync(string path) => _channel.SaveAsAsync(path);

        public async Task<Stream> CreateReadStreamAsync()
        {
            var stream = (await _channel.GetStreamAsync().ConfigureAwait(false)).Stream;
            string base64 = await stream.ReadAsync().ConfigureAwait(false);
            await stream.CloseAsync().ConfigureAwait(false);
            return new MemoryStream(Convert.FromBase64String(base64));
        }

        internal Task<string> FailureAsync() => _channel.GetFailureAsync();

        internal Task DeleteAsync() => _channel.DeleteAsync();
    }
}
