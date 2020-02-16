using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Exception thrown when an element selector returns null.
    /// </summary>
    public class SelectorException : PlaywrightSharpException
    {
        /// <inheritdoc cref="Exception"/>
        public SelectorException()
        {
        }

        /// <inheritdoc cref="Exception"/>
        public SelectorException(string message) : base(message)
        {
        }

        /// <inheritdoc cref="Exception"/>
        public SelectorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="selector">Selector.</param>
        public SelectorException(string message, string selector) : base(message)
        {
            Selector = selector;
        }

        /// <summary>
        /// Gets the selector.
        /// </summary>
        /// <value>The selector.</value>
        public string Selector { get; }
    }
}
