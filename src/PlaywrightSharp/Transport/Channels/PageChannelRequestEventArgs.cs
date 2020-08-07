using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class PageChannelRequestEventArgs : EventArgs
    {
        public Request Request { get; set; }
    }
}
