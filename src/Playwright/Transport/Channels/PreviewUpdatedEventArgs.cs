using System;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class PreviewUpdatedEventArgs : EventArgs
    {
        public string Preview { get; set; }
    }
}
