using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class RequestInitializer
    {
        public string Url { get; set; }

        public HttpMethod Method { get; set; }

        public IEnumerable<HeaderEntry> Headers { get; set; }

        public string PostData { get; set; }

        public bool IsNavigationRequest { get; set; }

        public Frame Frame { get; set; }

        public RequestChannel RedirectedFrom { get; set; }

        public string ResourceType { get; set; }
    }
}
