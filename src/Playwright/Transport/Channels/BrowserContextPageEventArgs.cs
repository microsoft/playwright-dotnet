using System;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class BrowserContextPageEventArgs : EventArgs
    {
        public PageChannel PageChannel { get; set; }
    }
}
