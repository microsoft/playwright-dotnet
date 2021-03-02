using System;
using Microsoft.Playwright.Protocol.Contracts;

namespace Microsoft.Playwright.Protocol.Channels
{
    internal class Channel<T> : ChannelBase, IChannel<T>
        where T : ChannelOwnerBase, IChannelOwner<T>
    {
        public Channel(string guid, Connection connection, T owner) : base(guid, connection)
        {
            Object = owner;
        }

        public virtual T Object { get; set; }
    }
}
