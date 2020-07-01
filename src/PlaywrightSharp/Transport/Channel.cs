using System;

namespace PlaywrightSharp.Transport
{
    internal class Channel
    {
        public IChannelOwner Object { get; set; }

        public event EventHandler<ChannelMessageEventArgs> MessageReceived;

        public void OnMessage(string method, PlaywrightSharpServerParams serverParams)
            => MessageReceived?.Invoke(this, new ChannelMessageEventArgs
            {
                Method = method,
                Params = serverParams,
            });
    }
}
