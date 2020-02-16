using System;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Transport interface.
    /// </summary>
    public interface IConnectionTransport
    {
        /// <summary>
        /// Occurs when the transport is closed.
        /// </summary>
        event EventHandler<TransportClosedEventArgs> Closed;

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Close the connection.
        /// </summary>
        /// <param name="closeReason">Close reason.</param>
        void Close(string closeReason = null);

        /// <summary>
        /// Sends a message using the transport.
        /// </summary>
        /// <returns>The task.</returns>
        /// <param name="message">Message to send.</param>
        Task SendAsync(string message);
    }
}
