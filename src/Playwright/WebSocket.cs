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

namespace Microsoft.Playwright
{
    /// <inheritdoc cref="IWebSocket"/>
    public class WebSocket : ChannelOwnerBase, IChannelOwner<WebSocket>, IWebSocket
    {
        private readonly WebSocketChannel _channel;
        private readonly WebSocketInitializer _initializer;
        private readonly Page _page;

        internal WebSocket(IChannelOwner parent, string guid, WebSocketInitializer initializer) : base(parent, guid)
        {
            _channel = new WebSocketChannel(guid, parent.Connection, this);
            _initializer = initializer;

            _channel.Close += (_, _) =>
            {
                IsClosed = true;
                Close?.Invoke(this, this);
            };
            _page = parent as Page;
            _channel.FrameReceived += (_, e) => FrameReceived?.Invoke(this, e);
            _channel.FrameSent += (_, e) => FrameSent?.Invoke(this, e);
            _channel.SocketError += (_, e) => SocketError?.Invoke(this, e);
        }

        /// <inheritdoc/>
        public event EventHandler<IWebSocket> Close;

        /// <inheritdoc/>
        public event EventHandler<IWebSocketFrame> FrameSent;

        /// <inheritdoc/>
        public event EventHandler<IWebSocketFrame> FrameReceived;

        /// <inheritdoc/>
        public event EventHandler<string> SocketError;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<WebSocket> IChannelOwner<WebSocket>.Channel => _channel;

        /// <inheritdoc/>
        public string Url => _initializer.Url;

        /// <inheritdoc/>
        public bool IsClosed { get; internal set; }

        /// <inheritdoc/>
        public async Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> webSocketEvent, Func<T, bool> predicate = null, float? timeout = null)
        {
            if (webSocketEvent == null)
            {
                throw new ArgumentException("WebSocket event is required", nameof(webSocketEvent));
            }

            timeout ??= _page.DefaultTimeout;
            using var waiter = new Waiter();
            waiter.RejectOnTimeout(Convert.ToInt32(timeout), $"Timeout while waiting for event \"{typeof(T)}\"");

            if (webSocketEvent.Name != WebSocketEvent.SocketError.Name)
            {
                waiter.RejectOnEvent<string>(this, WebSocketEvent.SocketError.Name, new PlaywrightSharpException("Socket error"));
            }

            if (webSocketEvent.Name != WebSocketEvent.Close.Name)
            {
                waiter.RejectOnEvent<IWebSocket>(this, WebSocketEvent.Close.Name, new PlaywrightSharpException("Socket closed"));
            }

            waiter.RejectOnEvent<IPage>(_page, PageEvent.Close.Name, new TargetClosedException("Page closed"));

            return await waiter.WaitForEventAsync(this, webSocketEvent.Name, predicate).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<object> WaitForEventAsync(string @event, float? timeout = null)
        {
            if (@event == null)
            {
                throw new ArgumentException("WebSocket event is required", nameof(@event));
            }

            timeout ??= _page.DefaultTimeout;
            using var waiter = new Waiter();
            waiter.RejectOnTimeout(Convert.ToInt32(timeout), $"Timeout while waiting for event \"{@event}\"");

            if (@event != WebSocketEvent.SocketError.Name)
            {
                waiter.RejectOnEvent<string>(this, WebSocketEvent.SocketError.Name, new PlaywrightSharpException("Socket error"));
            }

            if (@event != WebSocketEvent.Close.Name)
            {
                waiter.RejectOnEvent<EventArgs>(this, WebSocketEvent.Close.Name, new PlaywrightSharpException("Socket closed"));
            }

            waiter.RejectOnEvent<EventArgs>(_page, PageEvent.Close.Name, new TargetClosedException("Page closed"));
            await waiter.WaitForEventAsync(this, @event).ConfigureAwait(false);
            return this;
        }

        /// <inheritdoc/>
        public Task<IWebSocketFrame> WaitForFrameReceivedAsync(Func<IWebSocketFrame, bool> predicate = null, float? timeout = null)
            => WaitForEventAsync(WebSocketEvent.FrameReceived, predicate, timeout);

        /// <inheritdoc/>
        public Task<IWebSocketFrame> WaitForFrameSentAsync(Func<IWebSocketFrame, bool> predicate = null, float? timeout = null)
            => WaitForEventAsync(WebSocketEvent.FrameSent, predicate, timeout);
    }
}
