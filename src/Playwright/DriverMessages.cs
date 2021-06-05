namespace Microsoft.Playwright
{
    /// <summary>
    /// Messages coming from the Playwright Driver.
    /// </summary>
    internal static class DriverMessages
    {
        /// <summary>
        /// Message used when the browser gets closed.
        /// </summary>
        public const string BrowserClosedExceptionMessage = "Browser has been closed";

        /// <summary>
        /// Message used when the browser or the context get closed.
        /// </summary>
        public const string BrowserOrContextClosedExceptionMessage = "Target page, context or browser has been closed";
    }
}
