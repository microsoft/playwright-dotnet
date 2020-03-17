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
        private FirefoxPage _page;

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

        internal FirefoxTarget Opener => _openerId != null ?
            _browser.TargetsMap[_openerId] : null;

        internal TaskCompletionSource<Page> PageTsc { get; private set; }

        internal Task<Page> PageAsync()
        {
            throw new NotImplementedException();
        }

        internal void DidClose()
        {
            _page?.DidClose();
        }
    }
}
