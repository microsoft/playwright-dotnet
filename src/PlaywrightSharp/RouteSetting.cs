using System;

namespace PlaywrightSharp
{
    internal class RouteSetting
    {
        public string Url { get; set; }

        public Action<Route, IRequest> Handler { get; set; }
    }
}
