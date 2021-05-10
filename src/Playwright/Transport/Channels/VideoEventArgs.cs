using System;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class VideoEventArgs : EventArgs
    {
        public Artifact Artifact { get; set; }
    }
}
