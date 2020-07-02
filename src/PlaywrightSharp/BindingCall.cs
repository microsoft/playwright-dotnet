using System;
using System.Text.Json;
using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp
{
    internal class BindingCall : ChannelOwnerBase
    {
        public BindingCall(PlaywrightClient client, Channel channel, BindingCallInitializer initializer) : base(channel)
        {
            throw new NotImplementedException();
        }
    }
}
