using System;
using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Protocol.Target;

namespace PlaywrightSharp.Firefox
{
    /// <inheritdoc cref="ITarget"/>
    internal class FirefoxTarget : ITarget
    {
        private readonly FirefoxConnection _connection;
        private readonly FirefoxBrowser _browser;
        private readonly IBrowserContext _context;
        private readonly string _targetId;
        private readonly string _openerId;
        private Page _page;

        public FirefoxTarget(FirefoxConnection connection, FirefoxBrowser firefoxBrowser, IBrowserContext context, string targetId, TargetInfoType type, string url, string openerId)
        {
            _connection = connection;
            _browser = firefoxBrowser;
            _context = context;
            _targetId = targetId;
            _openerId = openerId;

            Type = type switch
            {
                TargetInfoType.Browser => TargetType.Browser,
                TargetInfoType.Page => TargetType.Page,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
            Url = url;
        }

        /// <inheritdoc cref="ITarget.Url"/>
        public string Url { get; internal set; }

        /// <inheritdoc cref="ITarget.Type"/>
        public TargetType Type { get; }

        public IBrowserContext BrowserContext => throw new NotImplementedException();

        ITarget ITarget.Opener => Opener;

        internal bool IsInitialized { get; set; }

        internal FirefoxTarget Opener => _openerId != null ? _browser.TargetsMap[_openerId] : null;

        internal Task<Page> PageTask => CreatePageAsync();

        /// <inheritdoc cref="ITarget.GetPageAsync"/>
        public Task<IPage> GetPageAsync() => Task.FromResult<IPage>(null);

        public Task<IWorker> GetWorkerAsync()
        {
            throw new NotImplementedException();
        }

        internal void DidClose() => _page?.DidClose();

        private async Task<Page> CreatePageAsync()
        {
            if (Type != TargetType.Page)
            {
                throw new PlaywrightSharpException($"Cannot create page for \"{Type}\" target");
            }

            FirefoxPage firefoxPage;
            if (PageTask == null)
            {
                var session = await _connection.CreateSessionAsync(_targetId).ConfigureAwait(false);
                firefoxPage = new FirefoxPage(session, _context, () =>
                {
                    var openerTarget = Opener;
                    if (openerTarget == null)
                    {
                        return Task.FromResult<Page>(null);
                    }

                    return openerTarget.PageTask;
                });
                _page = firefoxPage.Page;
            }

            return _page;
        }
    }
}
