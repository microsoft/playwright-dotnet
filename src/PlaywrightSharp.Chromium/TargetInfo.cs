using System;

namespace PlaywrightSharp.Chromium
{
    internal class TargetInfo
    {
        public TargetInfo()
        {
        }

        public TargetType Type { get; set; }

        public string BrowserContextId { get; set; }

        public string TargetId { get; set; }

        public string Url { get; set; }

        public string OpenerId { get; set; }
    }
}
