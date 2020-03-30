using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Protocol;
using PlaywrightSharp.Firefox.Protocol.Browser;
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

        internal FirefoxBrowser(IBrowserApp app, FirefoxConnection connection, string[] browserContextIds)
        {
            _app = app;
            _connection = connection;
            Contexts = browserContextIds.ToDictionary(id => id, id => (IBrowserContext)CreateBrowserContext(id));
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
        public IEnumerable<IBrowserContext> BrowserContexts
        {
            get
            {
                yield return DefaultContext;

                foreach (var context in Contexts.Values)
                {
                    yield return context;
                }
            }
        }

        /// <inheritdoc cref="IBrowser.DefaultContext"/>
        public IBrowserContext DefaultContext { get; }

        /// <summary>
        /// Default wait time in milliseconds. Defaults to 30 seconds.
        /// </summary>
        public int DefaultWaitForTimeout { get; set; } = Playwright.DefaultTimeout;

        /// <inheritdoc cref="IBrowser.IsConnected"/>
        public bool IsConnected => !_connection.IsClosed;

        internal ConcurrentDictionary<string, FirefoxTarget> TargetsMap { get; } = new ConcurrentDictionary<string, FirefoxTarget>();

        internal Dictionary<string, IBrowserContext> Contexts { get; }

        /// <inheritdoc cref="IBrowser.CloseAsync"/>
        public async Task CloseAsync()
        {
            var tsc = new TaskCompletionSource<bool>();
            void EventHandler(object sender, EventArgs e)
            {
                tsc.TrySetResult(true);
                _connection.Disconnected -= EventHandler;
            }

            _connection.Disconnected += EventHandler;
            await _connection.SendAsync(new BrowserCloseRequest()).ConfigureAwait(false);
            await tsc.Task.ConfigureAwait(false);
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
        public async Task<IBrowserContext> NewContextAsync(BrowserContextOptions options = null)
        {
            string browserContextId = (await _connection.SendAsync(new TargetCreateBrowserContextRequest()).ConfigureAwait(false)).BrowserContextId;
            if (options?.IgnoreHTTPSErrors == true)
            {
                await _connection.SendAsync(new BrowserSetIgnoreHTTPSErrorsRequest { Enabled = true }).ConfigureAwait(false);
            }

            var context = CreateBrowserContext(browserContextId, options);
            await context.InitializeAsync().ConfigureAwait(false);
            Contexts[browserContextId] = context;
            return context;
        }

        /// <inheritdoc cref="IBrowser.WaitForTargetAsync(Func{ITarget, bool}, WaitForOptions)"/>
        public Task<ITarget> WaitForTargetAsync(Func<ITarget, bool> predicate, WaitForOptions options = null)
        {
            int timeout = options?.Timeout ?? DefaultWaitForTimeout;
            var existingTarget = GetAllTargets().FirstOrDefault(predicate);
            if (existingTarget != null)
            {
                return Task.FromResult(existingTarget);
            }

            var tsc = new TaskCompletionSource<ITarget>();
            void TargetChangedHandler(object sender, TargetChangedArgs e)
            {
                tsc.TrySetResult(e.Target);
                TargetChanged -= TargetChangedHandler;
            }

            TargetChanged += TargetChangedHandler;
            return tsc.Task.WithTimeout(timeout);
        }

        /// <inheritdoc cref="IBrowser.GetPageTarget(IPage)" />
        public ITarget GetPageTarget(IPage page)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowser.StartTracingAsync(TracingOptions)" />
        public Task StartTracingAsync(TracingOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowser.StopTracingAsync" />
        public Task<string> StopTracingAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowser.GetTargets(IBrowserContext)" />
        public IEnumerable<ITarget> GetTargets(IBrowserContext context = null)
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

        internal IEnumerable<FirefoxTarget> GetAllTargets() => TargetsMap.Values;

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

            var context = browserContextId != null ? Contexts[browserContextId] : DefaultContext;
            var target = new FirefoxTarget(_connection, this, context, targetId, type, url, openerId);
            TargetsMap[targetId] = target;
            var opener = target.Opener;

            if (opener?.PageTask != null)
            {
                var page = await opener.PageTask.ConfigureAwait(false);
                if (page.HasPopupEventListeners)
                {
                    var popupPage = await target.CreatePageAsync().ConfigureAwait(false);
                    popupPage.OnPopup(this);
                }
            }

            TargetCreated?.Invoke(this, new TargetChangedArgs { Target = target });
        }

        private void OnTargetDestroyed(TargetTargetDestroyedFirefoxEvent payload)
        {
            string targetId = payload.TargetId;
            if (TargetsMap.TryRemove(targetId, out var target))
            {
                target.DidClose();
                TargetDestroyed?.Invoke(this, new TargetChangedArgs { Target = target });
            }
        }

        private void OnTargetInfoChanged(TargetTargetInfoChangedFirefoxEvent payload)
        {
            string targetId = payload.TargetId;
            TargetsMap[targetId].Url = payload.Url;
            var target = TargetsMap[targetId];

            TargetChanged?.Invoke(this, new TargetChangedArgs { Target = target });
        }

        private BrowserContext CreateBrowserContext(string browserContextId, BrowserContextOptions options = null)
            => new BrowserContext(new FirefoxBrowserContext(browserContextId, _connection, options ?? new BrowserContextOptions(), this), options);

        private void OnDisconnected(object sender, TransportClosedEventArgs e) => Disconnected?.Invoke(this, EventArgs.Empty);
    }
}
