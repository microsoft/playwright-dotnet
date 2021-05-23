using System;

namespace Microsoft.Playwright
{
    internal class PageEventArgs : EventArgs
    {
        public IPage Page { get; internal set; }
    }
}
