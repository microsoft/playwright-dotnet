using System.Collections.Generic;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp.Transport.Protocol
{
    internal class BrowserContextInitializer
    {
        public IEnumerable<PageChannel> Pages { get; set; }

        public IEnumerable<PageChannel> CrBackgroundPages { get; set; }
    }
}
