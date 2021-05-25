using System;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright
{
    internal class FrameNavigatedEventArgs : EventArgs
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public string Error { get; set; }

        internal NavigateDocument NewDocument { get; set; }
    }
}
