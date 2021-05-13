using System;
using System.Runtime.Serialization;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Base exception used to identify any exception thrown by PlaywrightSharp.
    /// </summary>
    public class PlaywrightException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaywrightException"/> class.
        /// </summary>
        public PlaywrightException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaywrightException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public PlaywrightException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaywrightException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public PlaywrightException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaywrightException"/> class.
        /// </summary>
        /// <param name="info">Info.</param>
        /// <param name="context">Context.</param>
        protected PlaywrightException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        internal static string RewriteErrorMeesage(string message)
            => message.Contains("Cannot find context with specified id") || message.Contains("Inspected target navigated or close")
                ? "Execution context was destroyed, most likely because of a navigation."
                : message;
    }
}
