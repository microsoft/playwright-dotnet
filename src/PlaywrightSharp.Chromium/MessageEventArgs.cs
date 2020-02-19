using System.Text.Json;

namespace PlaywrightSharp.Chromium
{
    internal class MessageEventArgs
    {
        public string MessageID { get; set; }

        public JsonElement? MessageData { get; set; }
    }
}