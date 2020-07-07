using System;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    internal class Download : IChannelOwner
    {
        private readonly ConnectionScope _scope;
        private readonly DownloadChannel _channel;

        public Download(ConnectionScope scope, string guid, DownloadInitializer initializer)
        {
            _scope = scope;
            _channel = new DownloadChannel(guid, scope);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        Channel IChannelOwner.Channel => _channel;
    }
}
