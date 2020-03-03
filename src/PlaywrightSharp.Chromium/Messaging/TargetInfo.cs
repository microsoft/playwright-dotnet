namespace PlaywrightSharp.Chromium.Messaging
{
    internal class TargetInfo
    {
        public TargetType Type { get; set; }

        public string BrowserContextId { get; set; }

        public string TargetId { get; set; }

        public string Url { get; set; }

        public string OpenerId { get; set; }
    }
}
