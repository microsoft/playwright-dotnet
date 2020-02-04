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
        public bool Ok { get; }

        /// <summary>
        /// A text representation of response body.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the text was processed, yielding to a text representation of response body.</returns>
        Task<string> GetTextAsync();
    }
}