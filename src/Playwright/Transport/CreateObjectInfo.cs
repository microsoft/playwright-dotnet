using System.Text.Json;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Transport
{
    internal class CreateObjectInfo
    {
        public string Guid { get; set; }

        public ChannelOwnerType Type { get; set; }

        public JsonElement? Initializer { get; set; }
    }
}
