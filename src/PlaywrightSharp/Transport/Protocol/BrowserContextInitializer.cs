using System.Collections.Generic;
using System.Text.Json.Serialization;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Converters;

namespace PlaywrightSharp.Transport.Protocol
{
    internal class BrowserContextInitializer
    {
        public IEnumerable<ChannelBase> Pages { get; set; }

        public IEnumerable<ChannelBase> CrBackgroundPages { get; set; }

        public IEnumerable<ChannelBase> CrServiceWorkers { get; set; }

        public bool IsChromium { get; set; }
    }
}
