using System;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Transport
{
    /// <summary>
    /// Transport interface.
    /// </summary>
    internal interface IConnectionTransport
    {
        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Occurs when a log message is received.
        /// </summary>
        event EventHandler<LogReceivedEventArgs> LogReceived;

        /// <summary>
        /// Occurs when the transport is closed.
        /// </summary>
        event EventHandler<TransportClosedEventArgs> TransportClosed;

        /// <summary>
        /// Sends a message using the transport.
        /// </summary>
        /// <returns>The task.</returns>
        /// <param name="message">Message to send.</param>
        Task SendAsync(string message);

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="closeReason">Close reason.</param>
        void Close(string closeReason);
    }
}
