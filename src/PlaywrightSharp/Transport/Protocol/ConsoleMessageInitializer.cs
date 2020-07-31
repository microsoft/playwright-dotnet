using System.Collections.Generic;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp.Transport.Protocol
{
    internal class ConsoleMessageInitializer
    {
        public string Type { get; set; }

        public IEnumerable<ChannelBase> Args { get; set; }

        public ConsoleMessageLocation Location { get; set; }

        public string Text { get; set; }
    }
}
