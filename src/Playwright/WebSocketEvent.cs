using System;
using System.Collections.Generic;

namespace Microsoft.Playwright
{
    public static class WebSocketEvent
    {
        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IWebSocket.Close"/>.
        /// </summary>
        public static PlaywrightEvent<EventArgs> Close => (PlaywrightEvent<EventArgs>)Events["Close"];

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IWebSocket.FrameReceived"/>.
        /// </summary>
        public static PlaywrightEvent<IWebSocketFrame> FrameReceived => (PlaywrightEvent<IWebSocketFrame>)Events["FrameReceived"];

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IWebSocket.FrameSent"/>.
        /// </summary>
        public static PlaywrightEvent<IWebSocketFrame> FrameSent => (PlaywrightEvent<IWebSocketFrame>)Events["FrameSent"];

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IWebSocket.SocketError"/>.
        /// </summary>
        public static PlaywrightEvent<string> SocketError => (PlaywrightEvent<string>)Events["SocketError"];

        internal static Dictionary<string, IEvent> Events { get; } = new Dictionary<string, IEvent>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["Close"] = new PlaywrightEvent<EventArgs>() { Name = "Close" },
            ["FrameReceived"] = new PlaywrightEvent<IWebSocketFrame>() { Name = "FrameReceived" },
            ["FrameSent"] = new PlaywrightEvent<IWebSocketFrame>() { Name = "FrameSent" },
            ["SocketError"] = new PlaywrightEvent<string>() { Name = "SocketError" },
        };
    }
}
