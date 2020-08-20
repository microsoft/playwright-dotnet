using System;
using System.Text.RegularExpressions;

namespace PlaywrightSharp
{
    internal class RouteSetting
    {
        public string Url { get; set; }

        public Regex Regex { get; set; }

        public Action<Route, IRequest> Handler { get; set; }
    }
}
