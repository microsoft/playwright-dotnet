using System;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class BindingCallEventArgs : EventArgs
    {
        public BindingCall BidingCall { get; set; }
    }
}
