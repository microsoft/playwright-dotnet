using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Firefox
{
    /// <inheritdoc cref="IPageDelegate"/>
    internal class FirefoxPage : IPageDelegate
    {
        private readonly FirefoxSession _session;
        private readonly IBrowserContext _context;
        private readonly Func<Task<Page>> _openerResolver;

        public FirefoxPage(FirefoxSession session, IBrowserContext context, Func<Task<Page>> openerResolver)
        {
            _session = session;
            _context = context;
            _openerResolver = openerResolver;

            Page = new Page(this, _context);
        }

        internal Page Page { get; }

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

        internal void DidClose() => Page.DidClose();
    }
}
