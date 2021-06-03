using System;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class BrowserContextChannelResponseEventArgs : EventArgs
    {
        public Page Page { get; set; }

        public Response Response { get; set; }
    }
}
