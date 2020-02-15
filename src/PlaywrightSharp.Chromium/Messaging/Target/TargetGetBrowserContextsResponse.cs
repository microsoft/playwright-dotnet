using System;

namespace PlaywrightSharp.Chromium.Messaging.Target
{
    internal class TargetGetBrowserContextsResponse
    {
        public string[] BrowserContextIds { get; set; }
    }
}
