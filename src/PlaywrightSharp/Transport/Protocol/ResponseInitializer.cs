using System.Collections.Generic;
using System.Net;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp.Transport.Protocol
{
    internal class ResponseInitializer
    {
        public HttpStatusCode Status { get; set; }

        public string Url { get; set; }

        public string StatusText { get; set; }

        public IDictionary<string, string> Headers { get; set; }

        public RequestChannel Request { get; set; }
    }
}
