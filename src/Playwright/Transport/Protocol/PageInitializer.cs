using System.Collections.Generic;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class PageInitializer
    {
        public Frame MainFrame { get; set; }

        public ViewportSize ViewportSize { get; set; }

        public bool IsClosed { get; set; }

        public Page Opener { get; set; }
    }
}
