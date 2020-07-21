using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserContextBindingCallEventArgs : EventArgs
    {
        public BindingCallChannel BidingCallChannel { get; set; }
    }
}
