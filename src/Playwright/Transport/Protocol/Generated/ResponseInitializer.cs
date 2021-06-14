using System.Collections.Generic;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class ResponseInitializer
    {
        public Request Request { get; set; }

        public string Url { get; set; }

        public int Status { get; set; }

        public string StatusText { get; set; }

        public List<HeaderEntry> RequestHeaders { get; set; }

        public List<HeaderEntry> Headers { get; set; }

        public RequestTimingResult Timing { get; set; }
    }
}
