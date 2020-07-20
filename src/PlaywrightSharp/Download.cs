using System;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    internal class Download : IChannelOwner<Download>
    {
        private readonly ConnectionScope _scope;
        private readonly DownloadChannel _channel;

        public Download(ConnectionScope scope, string guid, DownloadInitializer initializer)
        {
            _scope = scope;
            _channel = new DownloadChannel(guid, scope, this);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Download> IChannelOwner<Download>.Channel => _channel;
    }
}
