using System;
using System.Runtime.Serialization;

namespace PlaywrightSharp
{
    /// <summary>
    /// Base exception used to identify any exception thrown by PlaywrightSharp.
    /// </summary>
    public class PlaywrightSharpException : Exception
    {
        internal const string BrowserClosedExceptionMessage = "Browser has been closed";
        internal const string BrowserOrContextClosedExceptionMessage = "Target page, context or browser has been closed";

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaywrightSharpException"/> class.
        /// </summary>
        public PlaywrightSharpException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaywrightSharpException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public PlaywrightSharpException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaywrightSharpException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public PlaywrightSharpException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaywrightSharpException"/> class.
        /// </summary>
        /// <param name="info">Info.</param>
        /// <param name="context">Context.</param>
        protected PlaywrightSharpException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        internal static string RewriteErrorMeesage(string message)
            => message.Contains("Cannot find context with specified id") || message.Contains("Inspected target navigated or close")
                ? "Execution context was destroyed, most likely because of a navigation."
                : message;
    }
}
