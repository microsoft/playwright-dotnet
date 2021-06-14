using System.Collections.Generic;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class BindingCallInitializer
    {
        public Frame Frame { get; set; }

        public string Name { get; set; }

        public List<System.Text.Json.JsonElement> Args { get; set; }

        public JSHandle Handle { get; set; }
    }
}
