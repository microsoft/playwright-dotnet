using System;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    internal class BindingCall : IChannelOwner<BindingCall>
    {
        private readonly ConnectionScope _scope;
        private readonly BindingCallChannel _channel;

        public BindingCall(ConnectionScope scope, string guid, BindingCallInitializer initializer)
        {
            _scope = scope;
            _channel = new BindingCallChannel(guid, scope, this);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<BindingCall> IChannelOwner<BindingCall>.Channel => _channel;
    }
}
