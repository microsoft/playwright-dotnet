using System.Text.Json.Serialization;

namespace PlaywrightSharp.Chromium.Messaging
{
    internal class WSEndpointResponse
    {
        [JsonPropertyName("webSocketDebuggerUrl")]
        public string WebSocketDebuggerUrl { get; set; }
    }
}
