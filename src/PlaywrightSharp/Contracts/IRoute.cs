using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    public partial interface IRoute
    {
        /// <inheritdoc cref="FulfillAsync(int?, IEnumerable{KeyValuePair{string, string}}, string, string, byte[], string)"/>
        Task FulfillAsync(
            HttpStatusCode status,
            IEnumerable<KeyValuePair<string, string>> headers = null,
            string contentType = null,
            string body = null,
            byte[] bodyBytes = null,
            string path = null);
    }
}
