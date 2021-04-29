using System;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class FileChooserChannelEventArgs : EventArgs
    {
        public ElementHandleChannel Element { get; set; }

        public bool IsMultiple { get; set; }
    }
}
