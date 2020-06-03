using System.Collections.Generic;
using System.Net.Http;

namespace PlaywrightSharp
{
    /// <summary>
    /// Payload information.
    /// </summary>
    /// <see cref="IRequest.ContinueAsync(Payload)"/>
    public class Payload
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

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        public string Url { get; set; }
    }
}
