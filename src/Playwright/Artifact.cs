using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    internal class Artifact : ChannelOwnerBase, IChannelOwner<Artifact>
    {
        private readonly Connection _connection;
        private readonly ArtifactChannel _channel;

        internal Artifact(IChannelOwner parent, string guid, ArtifactInitializer initializer) : base(parent, guid)
        {
            _connection = parent.Connection;
            _channel = new ArtifactChannel(guid, parent.Connection, this);
            AbsolutePath = initializer.AbsolutePath;
        }

        /// <inheritdoc/>
        Connection IChannelOwner.Connection => _connection;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
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
