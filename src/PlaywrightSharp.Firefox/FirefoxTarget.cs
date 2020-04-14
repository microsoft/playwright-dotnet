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

        internal Page Page { get; private set; }

        /// <inheritdoc cref="ITarget.GetPageAsync"/>
        async Task<IPage> ITarget.GetPageAsync() => await GetPageAsync().ConfigureAwait(false);

        /// <inheritdoc cref="ITarget.GetWorkerAsync"/>
        public Task<IWorker> GetWorkerAsync()
        {
            throw new NotImplementedException();
        }

        internal Task<Page> GetPageAsync() => CreatePageAsync();

        internal void DidClose() => Page?.DidClose();

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
                    if (Page != null)
                    {
                        return Page;
                    }

                    var session = await _connection.CreateSessionAsync(_targetId).ConfigureAwait(false);
                    var firefoxPage = new FirefoxPage(session, BrowserContext, () =>
                    {
                        var openerTarget = Opener;
                        if (openerTarget == null)
                        {
                            return Task.FromResult<Page>(null);
                        }

                        return openerTarget.PageTask;
                    });
                    Page = firefoxPage.Page;
                    void DisconnectedEventHandler(object sender, EventArgs e)
                    {
                        Page.DidDisconnected();
                        session.Disconnected -= DisconnectedEventHandler;
                    }

                    session.Disconnected += DisconnectedEventHandler;
                    await firefoxPage.InitializeAsync().ConfigureAwait(false);
                    return Page;
                }

                PageTask = CreatePageInternalAsync();
            }

            return PageTask;
        }
    }
}
