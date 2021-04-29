using System;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class PageChannelRequestEventArgs : EventArgs
    {
        public RequestChannel Request { get; set; }

        public string FailureText { get; set; }

        public float ResponseEndTiming { get; set; }
    }
}
