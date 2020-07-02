using System;

namespace PlaywrightSharp.Transport.Channel
{
    internal class Channel
    {
        public Channel(string guid, PlaywrightClient client)
        {
            Guid = guid;
            client.AddChannel(this);
        }

        internal event EventHandler<ChannelMessageEventArgs> MessageReceived;

        public string Guid { get; }

        public IChannelOwner Object { get; set; }

        internal void OnMessage(string method, PlaywrightSharpServerParams serverParams)
            => MessageReceived?.Invoke(this, new ChannelMessageEventArgs
            {
                Method = method,
                Params = serverParams,
            });
    }
}
