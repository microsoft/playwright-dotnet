using System;
using System.Threading.Tasks;
using PlaywrightSharp.Input;

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

        public IRawKeyboard RawKeyboard => throw new NotImplementedException();

        public IRawMouse RawMouse => throw new NotImplementedException();

        internal Page Page { get; }

        public Task<IElementHandle> AdoptElementHandleAsync(object arg, FrameExecutionContext frameExecutionContext)
        {
            throw new System.NotImplementedException();
        }

        public Task ClosePageAsync(bool runBeforeUnload)
        {
            throw new System.NotImplementedException();
        }

        public Task<Quad[][]> GetContentQuadsAsync(ElementHandle elementHandle)
        {
            throw new NotImplementedException();
        }

        public Task<LayoutMetric> GetLayoutViewportAsync()
        {
            throw new NotImplementedException();
        }

        public bool IsElementHandle(IRemoteObject remoteObject)
        {
            throw new NotImplementedException();
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
