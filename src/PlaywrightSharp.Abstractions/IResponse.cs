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
    }
}
