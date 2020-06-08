using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Exception thrown by a connection when it detects that the target was closed.
    /// </summary>
    public class TargetClosedException : PlaywrightSharpException
    {
        /// <inheritdoc cref="Exception"/>
        public TargetClosedException()
        {
        }

        /// <inheritdoc cref="Exception"/>
        public TargetClosedException(string message) : base(message)
        {
        }

        /// <inheritdoc cref="Exception"/>
        public TargetClosedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetClosedException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="closeReason">Close reason.</param>
        public TargetClosedException(string message, string closeReason) : base($"{message} ({closeReason})")
            => CloseReason = closeReason;

        /// <summary>
        /// Close Reason.
        /// </summary>
        /// <value>The close reason.</value>
        public string CloseReason { get; }
    }
}
