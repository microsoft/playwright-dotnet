using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport
{
    /// <summary>
    /// Transport interface.
    /// </summary>
    public interface IConnectionTransport
    {
        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Sends a message using the transport.
        /// </summary>
        /// <returns>The task.</returns>
        /// <param name="message">Message to send.</param>
        Task SendAsync(string message);

        /// <summary>
        /// Closes the connection.
        /// </summary>
        void Close();
    }
}
