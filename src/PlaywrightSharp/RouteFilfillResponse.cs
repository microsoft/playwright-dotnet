using System.Collections.Generic;
using System.Net;

namespace PlaywrightSharp
{
    /// <summary>
    ///  Response that will fulfill <see cref="Route.FulfillAsync(RouteFilfillResponse)"/>.
    /// </summary>
    public class RouteFilfillResponse
    {
        /// <summary>
        /// Status code of the response.
        /// </summary>
        public HttpStatusCode? Status { get; set; }

        /// <summary>
        /// Optional response headers.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// If set, equals to setting Content-Type response header.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Optional response body.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Optional response body.
        /// </summary>
        public byte[] BodyContent { get; set; }

        /// <summary>
        /// Optional file path to respond with. The content type will be inferred from file extension.
        /// If path is a relative path, then it is resolved relative to current working directory.
        /// </summary>
        public string Path { get; set; }
    }
}
