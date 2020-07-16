using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserContextOnPageEventArgs : EventArgs
    {
        public PageChannel PageChannel { get; set; }
    }
}
