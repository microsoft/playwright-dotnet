namespace PlaywrightSharp.Chromium.Messaging.Page
{
    internal class PageGetFrameTreeItem
    {
        public FramePayload Frame { get; set; }

        public PageGetFrameTreeItem[] ChildFrames { get; set; }
    }
}
