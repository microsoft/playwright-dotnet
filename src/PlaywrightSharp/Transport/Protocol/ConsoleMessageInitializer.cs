using System.Collections.Generic;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class ConsoleMessageInitializer
    {
        public string Type { get; set; }

        public IEnumerable<ChannelBase> Args { get; set; }

        public ConsoleMessageLocation Location { get; set; }

        public string Text { get; set; }
    }
}
