namespace PlaywrightSharp
{
    /// <summary>
    /// Exception thrown when an element selector returns null.
    /// </summary>
    public class SelectorException : PlaywrightSharpException
    {
        /// <summary>
        /// Gets the selector.
        /// </summary>
        /// <value>The selector.</value>
        public string Selector { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorException"/> class.
        /// </summary>
        /// <param name="message">Message.</param>
        public SelectorException(string message) : base(message)
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
    }
}
