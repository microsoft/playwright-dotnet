using System;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class VideoEventArgs : EventArgs
    {
        public Artifact Artifact { get; set; }
    }
}
