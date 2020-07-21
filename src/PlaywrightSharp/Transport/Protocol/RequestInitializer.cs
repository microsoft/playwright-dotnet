using System.Collections.Generic;
using System.Net.Http;

namespace PlaywrightSharp.Transport.Protocol
{
    internal class RequestInitializer
    {
        public string Url { get; set; }

        public HttpMethod Method { get; set; }

        public IDictionary<string, string> Headers { get; set; }

        public string PostData { get; set; }

        public bool IsNavigationRequest { get; set; }

        public Frame Frame { get; set; }
    }
}
