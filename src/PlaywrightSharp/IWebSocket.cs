using System;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Represents websocket connections in the page.
    /// </summary>
    public interface IWebSocket
    {
        /// <summary>
        /// Raised when the <see cref="IWebSocket"/> closes.
        /// </summary>
        event EventHandler<EventArgs> Close;

        /// <summary>
        /// Raised when the <see cref="IWebSocket"/> recieves a frame.
        /// </summary>
        event EventHandler<WebSocketFrameEventArgs> FrameReceived;

        /// <summary>
        /// Raised when the <see cref="IWebSocket"/> sends a frame.
        /// </summary>
        event EventHandler<WebSocketFrameEventArgs> FrameSent;

        /// <summary>
        /// Raised when the <see cref="IWebSocket"/> has an error.
        /// </summary>
        event EventHandler<WebSocketErrorEventArgs> SocketError;

        /// <summary>
        /// Contains the URL of the WebSocket.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Indicates that the web socket has been closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Waits for event to fire and passes its value into the predicate function. Resolves when the predicate returns truthy value.
        /// Will throw an Exception if the <see cref="IWebSocket"/> is closed before the event is fired.
        /// </summary>
        /// <param name="webSocketEvent">Event to wait for.</param>
        /// <param name="predicate">Receives the event data and resolves when the waiting should resolve.</param>
        /// <param name="timeout">Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or <see cref="IPage.DefaultTimeout"/>.</param>
        /// <typeparam name="T">Resulting event args.</typeparam>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// // wait for console event:
        /// var console = await page.WaitForEvent(PageEvent.Console);
        ///
        /// // wait for popup event:
        /// var popup = await page.WaitForEvent(PageEvent.Popup);
        ///
        /// // wait for dialog event:
        /// var dialog = await page.WaitForEvent(PageEvent.Dialog);
        ///
        /// // wait for request event:
        /// var request = await page.WaitForEvent(PageEvent.Request);
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task"/> that completes when the predicate returns truthy value. Yielding the information of the event.</returns>
        Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> webSocketEvent, Func<T, bool> predicate = null, int? timeout = null)
            where T : EventArgs;
    }
}
