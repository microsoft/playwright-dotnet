using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// See <see cref="IWebSocket.FrameSent"/> and <see cref="IWebSocket.FrameReceived"/>.
    /// </summary>
    public class WebSocketFrameEventArgs : EventArgs
    {
        /// <summary>
        /// Frame payload.
        /// </summary>
        public string Payload { get; set; }
    }
}
