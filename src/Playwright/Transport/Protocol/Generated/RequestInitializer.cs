using System.Collections.Generic;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class RequestInitializer
    {
        public Frame Frame { get; set; }

        public string Url { get; set; }

        public string ResourceType { get; set; }

        public string Method { get; set; }

        public string PostData { get; set; }

        public List<HeaderEntry> Headers { get; set; }

        public bool IsNavigationRequest { get; set; }

        public Request RedirectedFrom { get; set; }
    }
}
