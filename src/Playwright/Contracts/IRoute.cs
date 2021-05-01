using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IRoute
    {
        /// <inheritdoc cref="FulfillAsync(int?, IEnumerable{KeyValuePair{string, string}}, string, string, byte[], string)"/>
        Task FulfillAsync(HttpStatusCode status, IEnumerable<KeyValuePair<string, string>> headers = default, string contentType = default, string body = default, byte[] bodyBytes = default, string path = default);
    }
}
