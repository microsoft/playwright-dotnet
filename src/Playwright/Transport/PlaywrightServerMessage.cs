using System.Text.Json;

namespace Microsoft.Playwright.Transport
{
    internal class PlaywrightServerMessage
    {
        public int? Id { get; set; }

        public string Guid { get; set; }

        public string Method { get; set; }

        public JsonElement? Params { get; set; }

        public JsonElement? Result { get; set; }

        public ErrorEntry Error { get; set; }
    }
}
