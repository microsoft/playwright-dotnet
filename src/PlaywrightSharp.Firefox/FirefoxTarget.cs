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
        private readonly string _targetId;
        private readonly string _openerId;
        private Page _page;

        public FirefoxTarget(FirefoxConnection connection, FirefoxBrowser firefoxBrowser, IBrowserContext context, string targetId, TargetInfoType type, string url, string openerId)
        {
            _connection = connection;
            _browser = firefoxBrowser;
            _targetId = targetId;
            _openerId = openerId;

            BrowserContext = context;
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

        public IBrowserContext BrowserContext { get; }

        ITarget ITarget.Opener => Opener;

        internal bool IsInitialized { get; set; }

        internal FirefoxTarget Opener => _openerId != null ? _browser.TargetsMap[_openerId] : null;

        internal Task<Page> PageTask { get; set; }

        /// <inheritdoc cref="ITarget.GetPageAsync"/>
        public async Task<IPage> GetPageAsync() => await CreatePageAsync().ConfigureAwait(false);

        public Task<IWorker> GetWorkerAsync()
        {
            throw new NotImplementedException();
        }

        internal void DidClose() => _page?.DidClose();

        internal Task<Page> CreatePageAsync()
        {
            if (Type != TargetType.Page)
            {
                throw new PlaywrightSharpException($"Cannot create page for \"{Type}\" target");
            }

            if (PageTask == null)
            {
                async Task<Page> CreatePageInternalAsync()
                {
                    if (_page != null)
                    {
                        return _page;
                    }

                    var session = await _connection.CreateSessionAsync(_targetId).ConfigureAwait(false);
                    FirefoxPage firefoxPage = new FirefoxPage(session, BrowserContext, () =>
                    {
                        var openerTarget = Opener;
                        if (openerTarget == null)
                        {
                            return Task.FromResult<Page>(null);
                        }

                        return openerTarget.PageTask;
                    });
                    _page = firefoxPage.Page;
                    void DisconnectedEventHandler(object sender, EventArgs e)
                    {
                        _page.DidDisconnected();
                        session.Disconnected -= DisconnectedEventHandler;
                    }

                    session.Disconnected += DisconnectedEventHandler;
                    await firefoxPage.InitializeAsync().ConfigureAwait(false);
                    return _page;
                }

                PageTask = CreatePageInternalAsync();
            }

            return PageTask;
        }
    }
}
