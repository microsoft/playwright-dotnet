using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Arguments used by <see cref="IPage"/> events.
    /// </summary>
    /// <seealso cref="Page.Request"/>
    public class RequestEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>The request.</value>
        public IRequest Request { get; set; }
    }
}