using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    public partial interface IRoute
    {
        /// <summary>
        /// <para>Fulfills route's request with given response.</para>
        /// <para>An example of fulfilling all requests with 404 responses:</para>
        /// <para>An example of serving static file.</para>
        /// </summary>
        /// <param name="status">Response status code, defaults to <c>200</c>.</param>
        /// <param name="body">Optional response body as text.</param>
        /// <param name="bodyBytes">Optional response body as raw bytes.</param>
        /// <param name="contentType">If set, equals to setting <c>Content-Type</c> response header.</param>
        /// <param name="headers">Response headers. Header values will be converted to a string.</param>
        /// <param name="path">
        /// File path to respond with. The content type will be inferred from file extension.
        /// If <c>path</c> is a relative path, then it is resolved relative to the current working
        /// directory.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes when the message was sent.</returns>
        Task FulfillAsync(HttpStatusCode status, string body = default, byte[] bodyBytes = default, string contentType = default, IEnumerable<KeyValuePair<string, string>> headers = default, string path = default);
    }
}
