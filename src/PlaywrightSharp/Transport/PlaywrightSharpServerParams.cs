using System.Text.Json;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp.Transport
{
    internal class PlaywrightSharpServerParams
    {
        public string Guid { get; set; }

        public ChannelOwnerType Type { get; set; }

        public JsonElement? Initializer { get; set; }
    }
}
