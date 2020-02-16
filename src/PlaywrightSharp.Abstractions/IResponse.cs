using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

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
        HttpStatusCode Status { get; }

        /// <summary>
        /// Contains the status text of the response (e.g. usually an "OK" for a success).
        /// </summary>
        string StatusText { get; }

        /// <summary>
        /// An <see cref="IFrame"/> that initiated this response, or null if navigating to error pages.
        /// </summary>
        IFrame Frame { get; }

        /// <summary>
        /// The URL of the response.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// An object with HTTP headers associated with the response. All header names are lower-case.
        /// </summary>
        IDictionary<string, string> Headers { get; }

        /// <summary>
        /// Whether the response was successful (status in the range 200-299) or not.
        /// </summary>
        bool Ok { get; }

        /// <summary>
        /// A matching <see cref="IRequest"/> object.
        /// </summary>
        IRequest Request { get; }

        /// <summary>
        /// A text representation of response body.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the text was processed, yielding to a text representation of response body.</returns>
        Task<string> GetTextAsync();
    }
}
