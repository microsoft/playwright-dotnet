using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class Channel
    {
        public Channel(string guid, ConnectionScope scope)
        {
            Guid = guid;
            Scope = scope;
        }

        internal event EventHandler<ChannelMessageEventArgs> MessageReceived;

        public string Guid { get; }

        public ConnectionScope Scope { get; }

        public IChannelOwner Object { get; set; }

        internal void OnMessage(string method, PlaywrightSharpServerParams serverParams)
            => MessageReceived?.Invoke(this, new ChannelMessageEventArgs
            {
                Method = method,
                Params = serverParams,
            });
    }
}
