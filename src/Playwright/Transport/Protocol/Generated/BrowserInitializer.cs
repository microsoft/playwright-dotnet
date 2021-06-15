using System.Collections.Generic;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class BrowserInitializer
    {
        public string Version { get; set; }

        public string Name { get; set; }
    }
}
