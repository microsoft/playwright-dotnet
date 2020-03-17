using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Protocol;
using PlaywrightSharp.Firefox.Protocol.Target;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Firefox
{
    /// <summary>
    /// Firefox browser.
    /// </summary>
    public sealed class FirefoxBrowser : IBrowser
    {
        private readonly IBrowserApp _app;
        private readonly FirefoxConnection _connection;
        private readonly Dictionary<string, IBrowserContext> _contexts;

        internal FirefoxBrowser(IBrowserApp app, FirefoxConnection connection, string[] browserContextIds)
        {
            _app = app;
            _connection = connection;
            _contexts = browserContextIds.ToDictionary(id => id, id => CreateBrowserContext(id));
            DefaultContext = CreateBrowserContext(null);

            _connection.Disconnected += OnDisconnected;
            _connection.MessageReceived += OnMessageReceived;
        }

        /// <inheritdoc cref="IBrowser.TargetChanged"/>
        public event EventHandler<TargetChangedArgs> TargetChanged;

        /// <inheritdoc cref="IBrowser.TargetCreated"/>
        public event EventHandler<TargetChangedArgs> TargetCreated;

        /// <inheritdoc cref="IBrowser.TargetDestroyed"/>
        public event EventHandler<TargetChangedArgs> TargetDestroyed;

        /// <inheritdoc cref="IBrowser.Disconnected"/>
        public event EventHandler Disconnected;

        /// <inheritdoc cref="IBrowser.BrowserContexts"/>
        public IBrowserContext[] BrowserContexts => null;

        /// <inheritdoc cref="IBrowser.DefaultContext"/>
        public IBrowserContext DefaultContext { get; }

        /// <inheritdoc cref="IBrowser.IsConnected"/>
        public bool IsConnected => !_connection.IsClosed;

        internal ConcurrentDictionary<string, FirefoxTarget> TargetsMap { get; } = new ConcurrentDictionary<string, FirefoxTarget>();

        /// <inheritdoc cref="IBrowser.CloseAsync"/>
        public Task CloseAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowser.DisconnectAsync"/>
        public Task DisconnectAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose() => _ = CloseAsync();

        /// <inheritdoc cref="IAsyncDisposable.DisposeAsync"/>
        public ValueTask DisposeAsync() => new ValueTask(CloseAsync());

        /// <inheritdoc cref="IBrowser.NewContextAsync(BrowserContextOptions)"/>
        public Task<IBrowserContext> NewContextAsync(BrowserContextOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowser.WaitForTargetAsync(Func{ITarget, bool}, WaitForOptions)"/>
        public Task<ITarget> WaitForTargetAsync(Func<ITarget, bool> predicate, WaitForOptions options = null)
        {
            throw new NotImplementedException();
        }

        internal static async Task<IBrowser> ConnectAsync(IBrowserApp app, ConnectOptions options)
        {
            var transport = await BrowserHelper.CreateTransportAsync(options).ConfigureAwait(false);
            var connection = new FirefoxConnection(transport);
            var response = await connection.SendAsync(new TargetGetBrowserContextsRequest()).ConfigureAwait(false);
            var browser = new FirefoxBrowser(app, connection, response.BrowserContextIds);
            await connection.SendAsync(new TargetEnableRequest()).ConfigureAwait(false);
            await browser.WaitForTargetAsync(t => t.Type == TargetType.Page).ConfigureAwait(false);
            return browser;
        }

        private async void OnMessageReceived(object sender, IFirefoxEvent e)
        {
            switch (e)
            {
                case TargetTargetCreatedFirefoxEvent targetCreated:
                    await OnTargetCreatedAsync(targetCreated).ConfigureAwait(false);
                    break;
                case TargetTargetDestroyedFirefoxEvent targetDestroyed:
                    OnTargetDestroyed(targetDestroyed);
                    break;
                case TargetTargetInfoChangedFirefoxEvent targetInfoChanged:
                    OnTargetInfoChanged(targetInfoChanged);
                    break;
            }
        }

        private async Task OnTargetCreatedAsync(TargetTargetCreatedFirefoxEvent payload)
        {
            string targetId = payload.TargetId;
            string url = payload.Url;
            string browserContextId = payload.BrowserContextId;
            string openerId = payload.OpenerId;
            var type = payload.Type;

            var context = browserContextId != null ? _contexts[browserContextId] : DefaultContext;
            var target = new FirefoxTarget(_connection, this, context, targetId, type, url, openerId);
            TargetsMap[targetId] = target;
            var opener = target.Opener;

            if (opener?.PageTsc != null)
            {
                var page = await opener.PageTsc.Task.ConfigureAwait(false);
                if (page.HasPopupEventListeners)
                {
                    var popupPage = await target.PageAsync().ConfigureAwait(false);
                    popupPage.OnPopup(this);
                }
            }
        }

        private void OnTargetDestroyed(TargetTargetDestroyedFirefoxEvent payload)
        {
            string targetId = payload.TargetId;
            if (TargetsMap.TryRemove(targetId, out var target))
            {
                target.DidClose();
            }
        }

        private void OnTargetInfoChanged(TargetTargetInfoChangedFirefoxEvent payload)
        {
            string targetId = payload.TargetId;
            string url = payload.Url;
            TargetsMap[targetId].Url = url;
        }

        private IBrowserContext CreateBrowserContext(string browserContextId, BrowserContextOptions options = null)
        {
            return new BrowserContext(new FirefoxBrowserContext(browserContextId, _connection, options ?? new BrowserContextOptions()));
        }

        private void OnDisconnected(object sender, TransportClosedEventArgs e) => Disconnected?.Invoke(this, EventArgs.Empty);
    }
}
