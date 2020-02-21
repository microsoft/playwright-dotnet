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
    }
}
