using System.Collections.Generic;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class DialogInitializer
    {
        public string Type { get; set; }

        public string Message { get; set; }

        public string DefaultValue { get; set; }
    }
}
