using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    // Using this name to avoid collitions with System.IO.Stream.
    internal class PlaywrightStream : ChannelOwnerBase, IChannelOwner<PlaywrightStream>
    {
        private readonly StreamChannel _channel;

        internal PlaywrightStream(IChannelOwner parent, string guid) : base(parent, guid)
        {
            _channel = new StreamChannel(guid, parent.Connection, this);
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<PlaywrightStream> IChannelOwner<PlaywrightStream>.Channel => _channel;
    }
}
