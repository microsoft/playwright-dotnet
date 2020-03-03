using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Event args for <see cref="IPage.Websocket"/>.
    /// </summary>
    public class WebsocketEventArgs : EventArgs
    {
        /// <summary>
        /// Websocket.
        /// </summary>
        public IWebsocket Websocket { get; }
    }
}
