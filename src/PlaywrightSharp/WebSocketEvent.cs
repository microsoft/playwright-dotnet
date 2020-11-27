using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// WebSocket events for <see cref="IWebSocket.WaitForEventAsync{T}(PlaywrightEvent{T}, System.Func{T, bool}, int?)"/>.
    /// </summary>
    public static class WebSocketEvent
    {
        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IWebSocket.Close"/>.
        /// </summary>
        public static PlaywrightEvent<EventArgs> Close { get; } = new PlaywrightEvent<EventArgs>() { Name = "Close" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IWebSocket.FrameReceived"/>.
        /// </summary>
        public static PlaywrightEvent<WebSocketFrameEventArgs> FrameReceived { get; } = new PlaywrightEvent<WebSocketFrameEventArgs>() { Name = "FrameReceived" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IWebSocket.FrameSent"/>.
        /// </summary>
        public static PlaywrightEvent<WebSocketFrameEventArgs> FrameSent { get; } = new PlaywrightEvent<WebSocketFrameEventArgs>() { Name = "FrameSent" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IWebSocket.SocketError"/>.
        /// </summary>
        public static PlaywrightEvent<WebSocketErrorEventArgs> SocketError { get; } = new PlaywrightEvent<WebSocketErrorEventArgs>() { Name = "SocketError" };
    }
}
