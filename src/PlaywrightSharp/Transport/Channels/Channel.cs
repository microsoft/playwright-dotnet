using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class Channel<T> : ChannelBase, IChannel<T>
        where T : IChannelOwner<T>
    {
        public Channel(string guid, ConnectionScope scope, T owner) : base(guid, scope)
        {
            Object = owner;
        }

        public virtual T Object { get; set; }
    }
}
