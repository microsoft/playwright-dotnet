using System.Collections.Generic;
using System.Net.Http;

namespace PlaywrightSharp.Har
{
    /// <summary>
    /// HAR entry HTTP request.
    /// </summary>
    public class HarEntryRequest
    {
        /// <summary>
        /// URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// HTTP Method.
        /// </summary>
        public HttpMethod Method { get; set; }

        /// <summary>
        /// HTTP Version.
        /// </summary>
        public string HttpVersion { get; set; }

        /// <summary>
        /// Headers Size.
        /// </summary>
        public decimal HeadersSize { get; set; }

        /// <summary>
        /// Body Size.
        /// </summary>
        public decimal BodySize { get; set; }

        /// <summary>
        /// HTTP Headers.
        /// </summary>
        public IEnumerable<HarEntryHeader> Headers { get; set; }

        /// <summary>
        /// QueryString values.
        /// </summary>
        public IEnumerable<(string Name, string Value)> QueryString { get; set; }

        /// <summary>
        /// POST Data.
        /// </summary>
        public HarPostData PostData { get; set; }

        /// <summary>
        /// Cookies.
        /// </summary>
        public IEnumerable<HarCookie> Cookies { get; set; }
    }
}
