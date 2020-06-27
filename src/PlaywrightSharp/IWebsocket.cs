using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Websocket created by the page.
    /// </summary>
    public interface IWebsocket
    {
        /// <summary>
        /// Raised when the websocket is opened.
        /// </summary>
        event EventHandler Open;

        /// <summary>
        /// Raised when the websocket gets closed.
        /// </summary>
        event EventHandler Close;

        /// <summary>
        /// Websocket URL.
        /// </summary>
        string Url { get; }
    }
}
