namespace PlaywrightSharp.Chromium.Messaging.Page
{
    internal class PageGetFrameTreeItem
    {
        public PageGetFrameTreeItemInfo Frame { get; set; }
        public PageGetFrameTreeItem[] ChildFrames { get; set; }
    }
}