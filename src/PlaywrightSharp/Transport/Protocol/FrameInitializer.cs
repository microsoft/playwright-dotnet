namespace PlaywrightSharp.Transport.Protocol
{
    internal class FrameInitializer
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public Frame ParentFrame { get; set; }
    }
}
