using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class PageChannelRequestEventArgs : EventArgs
    {
        public RequestChannel Request { get; set; }

        public string FailureText { get; set; }

        public decimal ResponseEndTiming { get; set; }
    }
}
