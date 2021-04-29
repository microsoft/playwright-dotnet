using System.Text.Json;

namespace Microsoft.Playwright.Transport.Converters
{
    internal class KeyValueObject
    {
        public string K { get; set; }

        public object V { get; set; }
    }
}
