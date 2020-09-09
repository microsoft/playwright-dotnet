using System.Collections.Generic;
using System.Net;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    internal class NormalizedFulfillResponse
    {
        public int Status { get; set; }

        public HeaderEntry[] Headers { get; set; }

        public string Body { get; set; }

        public bool IsBase64 { get; set; }
    }
}
