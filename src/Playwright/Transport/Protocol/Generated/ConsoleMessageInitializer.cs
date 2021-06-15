using System.Collections.Generic;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class ConsoleMessageInitializer
    {
        public string Type { get; set; }

        public string Text { get; set; }

        public List<JSHandle> Args { get; set; }

        public ConsoleMessageLocation Location { get; set; }
    }
}
