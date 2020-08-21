using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserContextPageEventArgs : EventArgs
    {
        public PageChannel PageChannel { get; set; }
    }
}
