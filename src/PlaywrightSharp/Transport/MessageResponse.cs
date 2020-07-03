using System.Text.Json;

namespace PlaywrightSharp.Transport
{
    internal class MessageResponse : IMessageResponse
    {
        public Channel.Channel Channel { get; set; }

        public JsonElement Result { get; set; }
    }
}
