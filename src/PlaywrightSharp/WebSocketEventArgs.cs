using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// See <seealso cref="IPage.WebSocket"/>.
    /// </summary>
    public class WebSocketEventArgs : EventArgs
    {
        /// <summary>
        /// The <see cref="IWebSocket"/>.
        /// </summary>
        public IWebSocket WebSocket { get; set; }
    }
}
