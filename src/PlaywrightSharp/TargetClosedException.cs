namespace PlaywrightSharp
{
    /// <summary>
    /// Exception thrown by a connection when it detects that the target was closed.
    /// </summary>
    public class TargetClosedException : PlaywrightSharpException
    {
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
