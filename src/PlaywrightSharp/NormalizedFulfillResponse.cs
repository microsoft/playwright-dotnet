using System.Collections.Generic;
using System.Net;

namespace PlaywrightSharp
{
    internal class NormalizedFulfillResponse
    {
        public int Status { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public string Body { get; set; }

        public bool IsBase64 { get; set; }
    }
}
