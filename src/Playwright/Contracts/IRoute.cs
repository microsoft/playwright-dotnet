using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IRoute
    {
        /// <inheritdoc cref="FulfillAsync(string, byte[], string, IEnumerable{KeyValuePair{string, string}}, string, int?)"/>
        Task FulfillAsync(HttpStatusCode status, string body = default, byte[] bodyBytes = default, string contentType = default, IEnumerable<KeyValuePair<string, string>> headers = default, string path = default);
    }
}
