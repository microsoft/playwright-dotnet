using System;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Arguments used by <see cref="IPage"/> events.
    /// </summary>
    /// <seealso cref="IPage.RequestFailed"/>
    internal class RequestFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the request.
        /// </summary>
        public IRequest Request { get; set; }

        /// <summary>
        /// Failure text.
        /// </summary>
        public string FailureText { get; set; }
    }
}
