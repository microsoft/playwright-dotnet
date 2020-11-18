using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    internal class WebSocket : ChannelOwnerBase, IChannelOwner<WebSocket>, IWebSocket
    {
        private readonly WebSocketChannel _channel;
        private readonly WebSocketInitializer _initializer;

        public WebSocket(IChannelOwner parent, string guid, WebSocketInitializer initializer) : base(parent, guid)
        {
            _channel = new WebSocketChannel(guid, parent.Connection, this);
            _initializer = initializer;
        }

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<WebSocket> IChannelOwner<WebSocket>.Channel => _channel;

        internal Page Page { get; set; }

        internal BrowserContext BrowserContext { get; set; }
    }
}
