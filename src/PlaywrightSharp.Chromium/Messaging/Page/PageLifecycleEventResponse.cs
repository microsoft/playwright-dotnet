namespace PlaywrightSharp.Chromium.Messaging.Page
{
    internal class PageLifecycleEventResponse
    {
        public string FrameId { get; set; }

        public string LoaderId { get; set; }

        public string Name { get; set; }

        public double Timestamp { get; set; }
    }
}
