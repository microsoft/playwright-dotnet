using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PlaywrightSharp
{
    /// <summary>
    /// Response that will fulfill a request.
    /// </summary>
    public struct ResponseData
    {
        /// <summary>
        /// Response body (text content).
        /// </summary>
        public string Body
        {
            get => Encoding.UTF8.GetString(BodyData);
            set => BodyData = Encoding.UTF8.GetBytes(value);
        }

        /// <summary>
        /// Response body (binary content).
        /// </summary>
        public byte[] BodyData { get; set; }

        /// <summary>
        /// Response headers. Header values will be converted to a string.
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// If set, equals to setting <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Type"/> response header.
        /// </summary>
        /// <value>The Content-Type.</value>
        public string ContentType { get; set; }

        /// <summary>
        /// Response status code.
        /// </summary>
        /// <value>Status Code.</value>
        public HttpStatusCode? Status { get; set; }
    }
}
