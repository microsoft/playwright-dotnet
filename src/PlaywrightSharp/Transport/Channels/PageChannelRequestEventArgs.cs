using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class PageChannelRequestEventArgs : EventArgs
    {
        public RequestChannel RequestChannel { get; set; }
    }
}
