using System.Collections.Generic;

namespace Microsoft.Playwright.Transport.Protocol
{
    internal class FrameInitializer
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public Frame ParentFrame { get; set; }

        public List<LoadState> LoadStates { get; set; }
    }
}
