using System;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Protocol.Contracts
{
    /// <summary>
    /// Transport interface.
    /// </summary>
    internal interface IConnectionTransport
    {
        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        event EventHandler<string> MessageReceived;

        /// <summary>
        /// Occurs when a log message is received.
        /// </summary>
        event EventHandler<string> LogReceived;

        /// <summary>
        /// Occurs when the transport is closed.
        /// </summary>
        event EventHandler<string> TransportClosed;

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
