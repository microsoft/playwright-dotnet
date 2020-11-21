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
    /// <inheritdoc cref="IWebSocket"/>
    public class WebSocket : ChannelOwnerBase, IChannelOwner<WebSocket>, IWebSocket
    {
        private readonly WebSocketChannel _channel;
        private readonly WebSocketInitializer _initializer;
        private Page _page;

        internal WebSocket(IChannelOwner parent, string guid, WebSocketInitializer initializer) : base(parent, guid)
        {
            _channel = new WebSocketChannel(guid, parent.Connection, this);
            _initializer = initializer;

            _channel.Close += (_, e) =>
            {
                IsClosed = true;
                Close?.Invoke(this, EventArgs.Empty);
            };
            _page = parent as Page;
            _channel.FrameReceived += (_, e) => FrameReceived?.Invoke(this, e);
            _channel.FrameSent += (_, e) => FrameSent?.Invoke(this, e);
            _channel.SocketError += (_, e) => SocketError?.Invoke(this, e);
        }

        /// <inheritdoc/>
        public event EventHandler<EventArgs> Close;

        /// <inheritdoc/>
        public event EventHandler<WebSocketFrameEventArgs> FrameSent;

        /// <inheritdoc/>
        public event EventHandler<WebSocketFrameEventArgs> FrameReceived;

        /// <inheritdoc/>
        public event EventHandler<WebSocketErrorEventArgs> SocketError;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<WebSocket> IChannelOwner<WebSocket>.Channel => _channel;

        /// <inheritdoc/>
        public string Url => _initializer.Url;

        /// <inheritdoc/>
        public bool IsClosed { get; internal set; }

        /// <inheritdoc/>
        public async Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> webSocketEvent, Func<T, bool> predicate = null, int? timeout = null)
            where T : EventArgs
        {
            if (webSocketEvent == null)
            {
                throw new ArgumentException("WebSocket event is required", nameof(webSocketEvent));
            }

            timeout ??= _page.TimeoutSettings.Timeout;
            using var waiter = new Waiter();
            waiter.RejectOnTimeout(timeout, $"Timeout while waiting for event \"{typeof(T)}\"");

            if (webSocketEvent.Name != WebSocketEvent.SocketError.Name)
            {
                waiter.RejectOnEvent<WebSocketErrorEventArgs>(this, WebSocketEvent.SocketError.Name, new PlaywrightSharpException("Socket error"));
            }

            if (webSocketEvent.Name != WebSocketEvent.Close.Name)
            {
                waiter.RejectOnEvent<EventArgs>(this, WebSocketEvent.Close.Name, new PlaywrightSharpException("Socket closed"));
            }

            waiter.RejectOnEvent<EventArgs>(_page, PageEvent.Close.Name, new TargetClosedException("Page closed"));

            return await waiter.WaitForEventAsync(this, webSocketEvent.Name, predicate).ConfigureAwait(false);
        }
    }
}
