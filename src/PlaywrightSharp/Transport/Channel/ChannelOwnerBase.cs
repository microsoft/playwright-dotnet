using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp
{
    internal abstract class ChannelOwnerBase : IChannelOwner
    {
        public ChannelOwnerBase(Channel channel)
        {
            Channel = channel;
            Channel.Object = this;
        }

        public Channel Channel { get; }
    }
}
