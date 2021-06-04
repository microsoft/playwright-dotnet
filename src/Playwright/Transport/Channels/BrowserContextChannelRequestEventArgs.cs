using System;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class BrowserContextChannelRequestEventArgs : EventArgs
    {
        public Page Page { get; set; }

        public Request Request { get; set; }

        public string FailureText { get; set; }

        public float ResponseEndTiming { get; set; }
    }
}
