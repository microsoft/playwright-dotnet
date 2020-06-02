using System.Collections.Generic;

namespace PlaywrightSharp
{
    internal class PageState
    {
        public Viewport Viewport { get; set; }

        public MediaType? MediaType { get; set; }

        public ColorScheme? ColorScheme { get; set; }

        public Dictionary<string, string> ExtraHTTPHeaders { get; set; } = new Dictionary<string, string>();

        public bool? CacheEnabled { get; set; }

        public bool? InterceptNetwork { get; set; }

        public bool? OfflineMode { get; set; }

        public Credentials Credentials { get; set; }

        public bool? HasTouch { get; set; }
    }
}
