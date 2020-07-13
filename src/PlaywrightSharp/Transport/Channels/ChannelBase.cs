using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal abstract class ChannelBase
    {
        public ChannelBase(string guid, ConnectionScope scope)
        {
            Guid = guid;
            Scope = scope;
        }

        public string Guid { get; }

        public ConnectionScope Scope { get; }

        internal virtual void OnMessage(string method, PlaywrightSharpServerParams serverParams)
        {
        }
    }
}
