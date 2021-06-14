using System;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class RouteEventArgs : EventArgs
    {
        public Route Route { get; set; }

        public IRequest Request { get; set; }
    }
}
