using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Arguments used by <see cref="IPage"/> events.
    /// </summary>
    /// <seealso cref="IPage.Request"/>
    /// <seealso cref="IPage.RequestFailed"/>
    /// <seealso cref="IPage.RequestFinished"/>
    public class RequestEventArgs : EventArgs
    {
        internal RequestEventArgs(IRequest request) => Request = request;

        /// <summary>
        /// Gets the request.
        /// </summary>
        /// <value>The request.</value>
        public IRequest Request { get; }
    }
}
