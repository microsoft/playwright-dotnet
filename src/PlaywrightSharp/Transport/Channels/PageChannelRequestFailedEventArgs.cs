using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class PageChannelRequestFailedEventArgs : EventArgs
    {
        public RequestChannel Request { get; set; }

        public string FailureText { get; set; }
    }
}
