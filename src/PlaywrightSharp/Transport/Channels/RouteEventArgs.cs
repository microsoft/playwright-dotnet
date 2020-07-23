using System;

namespace PlaywrightSharp.Transport.Channels
{
    internal class RouteEventArgs : EventArgs
    {
        public Route Route { get; set; }

        public Request Request { get; set; }
    }
}
