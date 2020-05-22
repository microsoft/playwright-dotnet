using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Browser;
using PlaywrightSharp.Chromium.Protocol.Target;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium
{
    /// <summary>
    /// Chromium browser.
    /// </summary>
    public sealed class ChromiumBrowser : IBrowser
    {
        private readonly IBrowserApp _app;
        private readonly ChromiumConnection _connection;
        private readonly ChromiumSession _session;
        private readonly Dictionary<string, IBrowserContext> _contexts;
        private bool _isClosed;

        private ChromiumBrowser(IBrowserApp app, ChromiumConnection connection, string[] browserContextIds)
        {
            _app = app;
            _connection = connection;
            _session = connection.RootSession;

            DefaultContext = new BrowserContext(new ChromiumBrowserContext(connection.RootSession, this));

            _contexts = browserContextIds.ToDictionary(
                contextId => contextId,
                contextId => (IBrowserContext)new BrowserContext(new ChromiumBrowserContext(connection.RootSession, this, contextId, null)));

            _session.MessageReceived += Session_MessageReceived;
            _connection.Disconnected += (sender, e) => Disconnected?.Invoke(this, EventArgs.Empty);
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

                foreach (var context in _contexts.Values)
                {
                    yield return context;
                }
            }
        }

        /// <inheritdoc cref="IBrowser.Disconnected"/>
        public IBrowserContext DefaultContext { get; }

        /// <summary>
        /// Default wait time in milliseconds. Defaults to 30 seconds.
        /// </summary>
        public int DefaultWaitForTimeout { get; set; } = Playwright.DefaultTimeout;

        /// <inheritdoc cref="IBrowser.IsConnected"/>
        public bool IsConnected => false;

        internal ConcurrentDictionary<string, ChromiumTarget> TargetsMap { get; } = new ConcurrentDictionary<string, ChromiumTarget>();

        /// <inheritdoc cref="IBrowser.CloseAsync"/>
        public async Task CloseAsync()
        {
            if (!_isClosed)
            {
                _isClosed = true;
                var disconnectedTcs = new TaskCompletionSource<bool>();
                Disconnected += (sender, e) => disconnectedTcs.TrySetResult(true);

                await _connection.RootSession.SendAsync(new BrowserCloseRequest()).ConfigureAwait(false);
                await disconnectedTcs.Task.ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="IBrowser.DisconnectAsync"/>
        public Task DisconnectAsync()
        {
            var disconnectedTcs = new TaskCompletionSource<bool>();
            Disconnected += (sender, e) => disconnectedTcs.TrySetResult(true);
            _connection.Close("Browser disconnected");
            return disconnectedTcs.Task;
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose() => _ = CloseAsync();

        /// <inheritdoc cref="IAsyncDisposable.DisposeAsync"/>
        public ValueTask DisposeAsync() => new ValueTask(CloseAsync());

        /// <inheritdoc cref="IBrowser.NewContextAsync"/>
        public async Task<IBrowserContext> NewContextAsync(BrowserContextOptions options = null)
        {
            string browserContextId = (await _session.SendAsync(new TargetCreateBrowserContextRequest()).ConfigureAwait(false)).BrowserContextId;
            var context = CreateBrowserContext(browserContextId, options);
            await context.InitializeAsync().ConfigureAwait(false);
            _contexts.Add(browserContextId, context);
            return context;
        }

        /// <inheritdoc cref="IBrowser.WaitForTargetAsync"/>
        public async Task<ITarget> WaitForTargetAsync(Func<ITarget, bool> predicate, WaitForOptions options = null)
        {
            int timeout = options?.Timeout ?? DefaultWaitForTimeout;
            var existingTarget = GetAllTargets().FirstOrDefault(predicate);

            if (existingTarget != null)
            {
                return existingTarget;
            }

            var targetCompletionSource = new TaskCompletionSource<ITarget>(TaskCreationOptions.RunContinuationsAsynchronously);

            void TargetHandler(object sender, TargetChangedArgs e)
            {
                if (predicate(e.Target))
                {
                    targetCompletionSource.TrySetResult(e.Target);
                }
            }

            try
            {
                TargetCreated += TargetHandler;
                TargetChanged += TargetHandler;

                return await targetCompletionSource.Task.WithTimeout(timeout).ConfigureAwait(false);
            }
            finally
            {
                TargetCreated -= TargetHandler;
                TargetChanged -= TargetHandler;
            }
        }

        /// <inheritdoc cref="IBrowser.GetTargets"/>
        public IEnumerable<ITarget> GetTargets(IBrowserContext context = null)
            => context == null ? GetAllTargets() : GetAllTargets().Where(t => t.BrowserContext == context);

        /// <inheritdoc cref="IBrowser.GetServiceWorkerAsync"/>
        public Task<IWorker> GetServiceWorkerAsync(ITarget target)
        {
            if (target == null)
            {
                return Task.FromResult<IWorker>(null);
            }

            return target.GetWorkerAsync();
        }

        /// <inheritdoc cref="IBrowser.GetPageTarget(IPage)"/>
        public ITarget GetPageTarget(IPage page)
        {
            if (page == null)
            {
                return null;
            }

            return ChromiumTarget.FromPage(page as Page);
        }

        /// <inheritdoc cref="IBrowser.StartTracingAsync(TracingOptions)"/>
        public Task StartTracingAsync(TracingOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowser.StopTracingAsync"/>
        public Task<string> StopTracingAsync()
        {
            throw new NotImplementedException();
        }

        internal static Task<IBrowser> ConnectAsync(ConnectOptions options) => ConnectAsync(null, options);

        internal static async Task<IBrowser> ConnectAsync(IBrowserApp app, ConnectOptions options)
        {
            var transport = await BrowserHelper.CreateTransportAsync(options).ConfigureAwait(false);
            var connection = new ChromiumConnection(transport);
            var response = await connection.RootSession.SendAsync(new TargetGetBrowserContextsRequest()).ConfigureAwait(false);
            var browser = new ChromiumBrowser(app, connection, response.BrowserContextIds);
            await connection.RootSession.SendAsync(new TargetSetDiscoverTargetsRequest { Discover = true }).ConfigureAwait(false);
            await browser.WaitForTargetAsync(t => t.Type == TargetType.Page).ConfigureAwait(false);
            return browser;
        }

        internal IEnumerable<ChromiumTarget> GetAllTargets() => TargetsMap.Values.Where(t => t.IsInitialized);

        internal Task ClosePageAsync(Page page)
            => _session.SendAsync(new TargetCloseTargetRequest { TargetId = ChromiumTarget.FromPage(page).TargetId });

        internal void RemoveContext(string contextId) => _contexts.Remove(contextId);

        private async void Session_MessageReceived(object sender, IChromiumEvent e)
        {
            try
            {
                switch (e)
                {
                    case TargetTargetCreatedChromiumEvent targetCreated:
                        await CreateTargetAsync(targetCreated).ConfigureAwait(false);
                        return;

                    case TargetTargetDestroyedChromiumEvent targetDestroyed:
                        await DestroyTargetAsync(targetDestroyed).ConfigureAwait(false);
                        return;

                    case TargetTargetInfoChangedChromiumEvent targetInfoChanged:
                        TargetInfoChanged(targetInfoChanged);
                        return;
                }
            }
            catch (Exception ex)
            {
                string message = $"Browser failed to process {e.InternalName}. {ex.Message}. {ex.StackTrace}";

                // TODO Add Logger _logger.LogError(ex, message);
                _connection.Close(message);
            }
        }

        private async Task CreateTargetAsync(TargetTargetCreatedChromiumEvent e)
        {
            var targetInfo = e.TargetInfo;
            string browserContextId = targetInfo.BrowserContextId;

            if (!(browserContextId != null && _contexts.TryGetValue(browserContextId, out var context)))
            {
                context = DefaultContext;
            }

            var target = new ChromiumTarget(
                e.TargetInfo,
                this,
                () => _connection.CreateSessionAsync(targetInfo),
                context);

            if (TargetsMap.ContainsKey(e.TargetInfo.TargetId))
            {
                // TODO add logger
                // _logger.LogError("Target should not exist before targetCreated");
            }

            TargetsMap[e.TargetInfo.TargetId] = target;

            if (await target.InitializedTask.ConfigureAwait(false))
            {
                var args = new TargetChangedArgs { Target = target };
                TargetCreated?.Invoke(this, args);
            }
        }

        private async Task DestroyTargetAsync(TargetTargetDestroyedChromiumEvent e)
        {
            if (!TargetsMap.ContainsKey(e.TargetId))
            {
                throw new PlaywrightSharpException("Target should exists before DestroyTarget");
            }

            var target = TargetsMap[e.TargetId];
            TargetsMap.TryRemove(e.TargetId, out _);
            target.DidClose();

            target.CloseTaskWrapper.TrySetResult(true);

            if (await target.InitializedTask.ConfigureAwait(false))
            {
                var args = new TargetChangedArgs { Target = target };
                TargetDestroyed?.Invoke(this, args);
            }
        }

        private void TargetInfoChanged(TargetTargetInfoChangedChromiumEvent e)
        {
            if (!TargetsMap.TryGetValue(e.TargetInfo.TargetId, out var target))
            {
                throw new PlaywrightSharpException("Target should exists before ChangeTargetInfo");
            }

            string previousUrl = target.Url;
            bool wasInitialized = target.IsInitialized;
            target.TargetInfoChanged(e.TargetInfo);

            if (wasInitialized && previousUrl != target.Url)
            {
                TargetChanged?.Invoke(this, new TargetChangedArgs { Target = target });
            }
        }

        private BrowserContext CreateBrowserContext(string contextId, BrowserContextOptions options = null)
            => new BrowserContext(new ChromiumBrowserContext(_session, this, contextId, options), options);
    }
}
