using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    internal class NormalizedFulfillResponse
    {
        public int Status { get; set; }

        public HeaderEntry[] Headers { get; set; }

        public string Body { get; set; }

        public bool IsBase64 { get; set; }
    }
}
