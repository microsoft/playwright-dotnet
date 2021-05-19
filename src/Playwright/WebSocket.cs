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
    internal partial class WebSocket : ChannelOwnerBase, IChannelOwner<WebSocket>, IWebSocket
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

        public event EventHandler<IWebSocket> Close;

        public event EventHandler<IWebSocketFrame> FrameSent;

        public event EventHandler<IWebSocketFrame> FrameReceived;

        public event EventHandler<string> SocketError;

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<WebSocket> IChannelOwner<WebSocket>.Channel => _channel;

        public string Url => _initializer.Url;

        public bool IsClosed { get; internal set; }

        public async Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> webSocketEvent, Func<Task> action = default, Func<T, bool> predicate = default, float? timeout = default)
        {
            if (webSocketEvent == null)
            {
                throw new ArgumentException("WebSocket event is required", nameof(webSocketEvent));
            }

            timeout ??= _page.DefaultTimeout;
            using var waiter = new Waiter(_channel, $"webSocket.WaitForEventAsync(\"{typeof(T)}\")");
            waiter.RejectOnTimeout(Convert.ToInt32(timeout), $"Timeout while waiting for event \"{typeof(T)}\"");

            if (webSocketEvent.Name != WebSocketEvent.SocketError.Name)
            {
                waiter.RejectOnEvent<string>(this, WebSocketEvent.SocketError.Name, new PlaywrightException("Socket error"));
            }

            if (webSocketEvent.Name != WebSocketEvent.Close.Name)
            {
                waiter.RejectOnEvent<IWebSocket>(this, WebSocketEvent.Close.Name, new PlaywrightException("Socket closed"));
            }

            waiter.RejectOnEvent<IPage>(_page, PageEvent.Close.Name, new TargetClosedException("Page closed"));

            var result = waiter.WaitForEventAsync(this, webSocketEvent.Name, predicate);
            if (action != null)
            {
                await Task.WhenAll(result, action()).ConfigureAwait(false);
            }

            return await result.ConfigureAwait(false);
        }

        public async Task<object> WaitForEventAsync(string @event, Func<Task> action = default, float? timeout = default)
        {
            if (@event == null)
            {
                throw new ArgumentException("WebSocket event is required", nameof(@event));
            }

            timeout ??= _page.DefaultTimeout;
            using var waiter = new Waiter(_channel, $"webSocket.WaitForEventAsync(\"{@event}\")");
            waiter.RejectOnTimeout(Convert.ToInt32(timeout), $"Timeout while waiting for event \"{@event}\"");

            if (@event != WebSocketEvent.SocketError.Name)
            {
                waiter.RejectOnEvent<string>(this, WebSocketEvent.SocketError.Name, new PlaywrightException("Socket error"));
            }

            if (@event != WebSocketEvent.Close.Name)
            {
                waiter.RejectOnEvent<EventArgs>(this, WebSocketEvent.Close.Name, new PlaywrightException("Socket closed"));
            }

            waiter.RejectOnEvent<EventArgs>(_page, PageEvent.Close.Name, new TargetClosedException("Page closed"));
            var result = waiter.WaitForEventAsync(this, @event);
            if (action != null)
            {
                await Task.WhenAll(result, action()).ConfigureAwait(false);
            }
            else
            {
                await result.ConfigureAwait(false);
            }

            return this;
        }

        public Task<IWebSocketFrame> WaitForFrameReceivedAsync(Func<Task> action = default, Func<IWebSocketFrame, bool> predicate = default, float? timeout = default)
            => WaitForEventAsync(WebSocketEvent.FrameReceived, action, predicate, timeout);

        public Task<IWebSocketFrame> WaitForFrameSentAsync(Func<Task> action = default, Func<IWebSocketFrame, bool> predicate = default, float? timeout = default)
            => WaitForEventAsync(WebSocketEvent.FrameSent, action, predicate, timeout);
    }
}
