using System.Text.Json;

namespace PlaywrightSharp.Transport
{
    internal class PlaywrightSharpServerParams
    {
        public ChannelOwnerType Type { get; set; }

        public JsonElement? Initializer { get; set; }
    }
}
