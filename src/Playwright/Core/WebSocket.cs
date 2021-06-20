using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    internal class WebSocket : ChannelOwnerBase, IChannelOwner<WebSocket>, IWebSocket
    {
        private readonly WebSocketChannel _channel;
        private readonly WebSocketInitializer _initializer;

        internal WebSocket(IChannelOwner parent, string guid, WebSocketInitializer initializer) : base(parent, guid)
        {
            _channel = new(guid, parent.Connection, this);
            _initializer = initializer;

            _channel.Close += (_, _) =>
            {
                IsClosed = true;
                Close?.Invoke(this, this);
            };
            _channel.FrameReceived += (_, e) => FrameReceived?.Invoke(this, e);
            _channel.FrameSent += (_, e) => FrameSent?.Invoke(this, e);
            _channel.SocketError += (_, e) => SocketError?.Invoke(this, e);
        }

        public event EventHandler<IWebSocket> Close;

        public event EventHandler<IWebSocketFrame> FrameSent;

        public event EventHandler<IWebSocketFrame> FrameReceived;

        public event EventHandler<string> SocketError;

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<WebSocket> IChannelOwner<WebSocket>.Channel => _channel;

        public string Url => _initializer.Url;

        public bool IsClosed { get; internal set; }
    }
}
