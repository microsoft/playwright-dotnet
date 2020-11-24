using System.Collections.Generic;
using System.Net;

namespace PlaywrightSharp.Har
{
    /// <summary>
    /// HAR entry HTTP response.
    /// </summary>
    public class HarEntryResponse
    {
        /// <summary>
        /// HTTP Version.
        /// </summary>
        public string HttpVersion { get; set; }

        /// <summary>
        /// HTTP Headers.
        /// </summary>
        public IEnumerable<HarEntryHeader> Headers { get; set; }

        /// <summary>
        /// HTTP Status Code.
        /// </summary>
        public HttpStatusCode Status { get; set; }

        /// <summary>
        /// HTTP Status Text.
        /// </summary>
        public string StatusText { get; set; }

        /// <summary>
        /// Redirect URL.
        /// </summary>
        public string RedirectURL { get; set; }

        /// <summary>
        /// Cookies.
        /// </summary>
        public IEnumerable<HarCookie> Cookies { get; set; }

        /// <summary>
        /// Content.
        /// </summary>
        public HarContent Content { get; set; }
    }
}
