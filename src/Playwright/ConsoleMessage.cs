using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    internal class ConsoleMessage : ChannelOwnerBase, IChannelOwner<ConsoleMessage>, IConsoleMessage
    {
        private readonly ConsoleMessageChannel _channel;
        private readonly ConsoleMessageInitializer _initializer;

        internal ConsoleMessage(IChannelOwner parent, string guid, ConsoleMessageInitializer initializer) : base(parent, guid)
        {
            _channel = new ConsoleMessageChannel(guid, parent.Connection, this);
            _initializer = initializer;
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<ConsoleMessage> IChannelOwner<ConsoleMessage>.Channel => _channel;

        public string Type => _initializer.Type;

        public IReadOnlyCollection<IJSHandle> Args => _initializer.Args.Select(a => ((JSHandleChannel)a).Object).ToList().AsReadOnly();

        public string Location => _initializer.Location.ToString();

        public string Text => _initializer.Text;
    }
}
