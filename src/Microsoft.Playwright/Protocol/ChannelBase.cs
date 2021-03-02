using System;
using System.Text.Json;

namespace Microsoft.Playwright.Protocol
{
    internal class ChannelBase
    {
        public ChannelBase(string guid, Connection connection)
        {
            Guid = guid;
            Connection = connection;
        }

        public string Guid { get; }

        public Connection Connection { get; }

        internal virtual void OnMessage(string method, JsonElement? serverParams)
        {
        }
    }
}
