using System.Collections.Generic;
using System.Net.Http;

namespace PlaywrightSharp
{
    /// <summary>
    /// Request overrides for <see cref="Route.ContinueAsync(RouteContinueOverrides)"/>.
    /// </summary>
    public class RouteContinueOverrides
    {
        /// <summary>
        /// Gets or sets the HTTP method.
        /// </summary>
        public HttpMethod Method { get; set; }

        /// <summary>
        /// Gets or sets the post data.
        /// </summary>
        public string PostData { get; set; }

        /// <summary>
        /// Gets or sets the HTTP headers.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }
    }
}
