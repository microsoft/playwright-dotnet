using System.Collections.Generic;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class BrowserTypeInitializer
    {
        public string ExecutablePath { get; set; }

        public string Name { get; set; }
    }
}
