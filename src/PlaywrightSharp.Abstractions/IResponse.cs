using System.Collections.Generic;
using System.Net;

namespace PlaywrightSharp
{
    /// <summary>
    /// Represents responses which are received by page.
    /// </summary>
    public interface IResponse
    {
        /// <summary>
        /// Status code of the response.
        /// </summary>
        public HttpStatusCode Status { get; }

        /// <summary>
        /// An <see cref="IFrame"/> that initiated this response, or null if navigating to error pages.
        /// </summary>
        IFrame Frame { get; }

        /// <summary>
        /// Contains the URL of the response.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Contains a boolean stating whether the response was successful (status in the range 200-299) or not.
        /// </summary>
        bool Ok { get; }

        /// <summary>
        /// A matching <see cref="IRequest"/> object.
        /// </summary>
        IRequest Request { get; }
    }
}
