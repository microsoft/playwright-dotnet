using System;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class BindingCallEventArgs : EventArgs
    {
        public BindingCall BidingCall { get; set; }
    }
}
