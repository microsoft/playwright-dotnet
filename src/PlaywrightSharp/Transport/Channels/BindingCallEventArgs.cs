using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BindingCallEventArgs : EventArgs
    {
        public BindingCall BidingCall { get; set; }
    }
}
