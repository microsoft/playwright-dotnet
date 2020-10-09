using System;

namespace PlaywrightSharp.Transport
{
    /// <summary>
    /// Log received event arguments.
    /// <see cref="IConnectionTransport.LogReceived"/>.
    /// </summary>
    public class LogReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        public LogReceivedEventArgs(string message) => Message = message;

        /// <summary>
        /// Transport message.
        /// </summary>
        public string Message { get; }
    }
}
