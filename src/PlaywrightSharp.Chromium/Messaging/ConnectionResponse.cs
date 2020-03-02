using System.Text.Json;

namespace PlaywrightSharp.Chromium.Messaging
{
    internal class ConnectionResponse
    {
        public int? Id { get; set; }

        public ConnectionError Error { get; set; }

        public JsonElement Result { get; set; }

        public string Method { get; set; }

        public JsonElement Params { get; set; }

        public string SessionId { get; set; }
    }
}
