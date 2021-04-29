using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Converters;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class BrowserContextInitializer
    {
        public IEnumerable<ChannelBase> Pages { get; set; }

        public IEnumerable<ChannelBase> CrBackgroundPages { get; set; }

        public IEnumerable<ChannelBase> CrServiceWorkers { get; set; }

        public bool IsChromium { get; set; }
    }
}
