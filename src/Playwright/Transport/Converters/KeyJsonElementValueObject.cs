using System.Text.Json;

namespace Microsoft.Playwright.Transport.Converters
{
    internal class KeyJsonElementValueObject
    {
        public string K { get; set; }

        public JsonElement V { get; set; }
    }
}
