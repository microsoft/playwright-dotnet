using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class BindingCallInitializer
    {
        public string Name { get; set; }

        public Frame Frame { get; set; }

        public JsonElement[] Args { get; set; }

        public JSHandleChannel Handle { get; set; }
    }
}
