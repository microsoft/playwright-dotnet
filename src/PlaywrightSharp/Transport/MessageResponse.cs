using System.Text.Json;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp.Transport
{
    internal class MessageResponse : IMessageResponse
    {
        public Channel Channel { get; set; }

        public JsonElement Result { get; set; }
    }
}
