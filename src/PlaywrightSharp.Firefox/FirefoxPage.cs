using System.Threading.Tasks;

namespace PlaywrightSharp.Firefox
{
    /// <inheritdoc cref="IPageDelegate"/>
    internal class FirefoxPage : IPageDelegate
    {
        private readonly Page _page;

        public Task<ElementHandle> AdoptElementHandleAsync(object arg, FrameExecutionContext frameExecutionContext)
        {
            throw new System.NotImplementedException();
        }

        public Task ClosePageAsync(bool runBeforeUnload)
        {
            throw new System.NotImplementedException();
        }

        public Task<GotoResult> NavigateFrameAsync(IFrame frame, string url, string referrer)
        {
            throw new System.NotImplementedException();
        }

        public Task SetViewportAsync(Viewport viewport)
        {
            throw new System.NotImplementedException();
        }

        internal void DidClose() => _page.DidClose();
    }
}
