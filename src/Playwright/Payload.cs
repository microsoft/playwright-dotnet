using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Playwright
{
    internal class Payload
    {
        public HttpMethod Method { get; set; }

        public string PostData { get; set; }

        public IDictionary<string, string> Headers { get; set; }

        public string Url { get; set; }
    }
}
