using System.Text.Json;

namespace PlaywrightSharp.Transport.Converters
{
    internal class KeyJsonElementValueObject
    {
        public string K { get; set; }

        public JsonElement V { get; set; }
    }
}
