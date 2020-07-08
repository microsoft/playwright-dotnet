using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class Channel<T> : ChannelBase
        where T : IChannelOwner<T>
    {
        public Channel(string guid, ConnectionScope scope, T owner) : base(guid, scope)
        {
            Object = owner;
        }

        public T Object { get; set; }
    }
}
