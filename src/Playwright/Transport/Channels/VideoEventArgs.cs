using System;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class VideoEventArgs : EventArgs
    {
        public string RelativePath { get; set; }
    }
}
