namespace PlaywrightSharp
{
    internal class Frame : IFrame
    {
        private PageBase _page;
        private string frameId;
        private IFrame parentFrame;

        public Frame(PageBase page, string frameId, IFrame parentFrame)
        {
            _page = page;
            this.frameId = frameId;
            this.parentFrame = parentFrame;
        }
    }
}