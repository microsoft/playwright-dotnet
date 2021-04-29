using System;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright
{
    internal class SelectorsOwner : ChannelOwnerBase, IChannelOwner<SelectorsOwner>
    {
        private readonly SelectorsChannel _channel;

        public SelectorsOwner(IChannelOwner parent, string guid) : base(parent, guid)
        {
            _channel = new SelectorsChannel(guid, parent.Connection, this);
        }

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<SelectorsOwner> IChannelOwner<SelectorsOwner>.Channel => _channel;

        internal Task RegisterAsync(SelectorsRegisterParams registration) => _channel.RegisterAsync(registration);
    }
}
