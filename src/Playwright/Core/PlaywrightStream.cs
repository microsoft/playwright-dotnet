using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core
{
    // Using this name to avoid collitions with System.IO.Stream.
    internal class PlaywrightStream : ChannelOwnerBase, IChannelOwner<PlaywrightStream>
    {
        private readonly StreamChannel _channel;

        internal PlaywrightStream(IChannelOwner parent, string guid) : base(parent, guid)
        {
            _channel = new(guid, parent.Connection, this);
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<PlaywrightStream> IChannelOwner<PlaywrightStream>.Channel => _channel;
    }
}
