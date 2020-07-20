using System;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    internal class Dialog : IChannelOwner<Dialog>
    {
        private readonly ConnectionScope _scope;
        private readonly DialogChannel _channel;

        public Dialog(ConnectionScope scope, string guid, DialogInitializer initializer)
        {
            _scope = scope;
            _channel = new DialogChannel(guid, scope, this);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Dialog> IChannelOwner<Dialog>.Channel => _channel;
    }
}
