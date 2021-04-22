using System;
using System.Text.RegularExpressions;

namespace PlaywrightSharp
{
    internal class RouteSetting
    {
        public string Url { get; set; }

        public Regex Regex { get; set; }

        public Func<string, bool> Function { get; set; }

        public Action<IRoute> Handler { get; set; }
    }
}
