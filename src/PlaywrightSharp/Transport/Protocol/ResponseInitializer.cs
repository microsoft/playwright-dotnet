using System.Collections.Generic;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class ResponseInitializer
    {
        public int Status { get; set; }

        public string Url { get; set; }

        public string StatusText { get; set; }

        public IEnumerable<HeaderEntry> Headers { get; set; }

        public RequestChannel Request { get; set; }

        public RequestTimingResult Timing { get; set; }
    }
}
