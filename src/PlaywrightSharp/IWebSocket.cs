using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Represents websocket connections in the page.
    /// </summary>
    public interface IWebSocket
    {
        /// <summary>
        /// Contains the URL of the WebSocket.
        /// </summary>
        string Url { get; }

        event EventHandler<EventArgs> Close;
    }
}
