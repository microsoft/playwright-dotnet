using System;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    internal class Route : IChannelOwner
    {
        private readonly ConnectionScope _scope;
        private readonly RouteChannel _channel;

        public Route(ConnectionScope scope, string guid, RouteInitializer initializer)
        {
            _scope = scope;
            _channel = new RouteChannel(guid, scope);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        Channel IChannelOwner.Channel => _channel;
    }
}
