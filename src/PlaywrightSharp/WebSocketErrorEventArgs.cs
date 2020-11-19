using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// See <see cref="IWebSocket.SocketError"/>.
    /// </summary>
    public class WebSocketErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Error Message.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
