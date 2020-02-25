using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Helpers;
using PlaywrightSharp.Chromium.Messaging.Target;
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
        private readonly string[] _browserContextIds;
        private readonly ChromiumSession _session;
        private readonly Dictionary<string, IBrowserContext> _contexts;

        internal ChromiumBrowser(IBrowserApp app, ChromiumConnection connection, string[] browserContextIds)
        {
            _app = app;
            _connection = connection;
            _browserContextIds = browserContextIds;
            _session = connection.RootSession;

            DefaultContext = new BrowserContext(new ChromiumBrowserContext(connection.RootSession, this, null, null));

            _contexts = browserContextIds.ToDictionary(
                contextId => contextId,
                contextId => (IBrowserContext)new BrowserContext(new ChromiumBrowserContext(connection.RootSession, this, contextId, null)));

            _session.MessageReceived += Session_MessageReceived;
        }

        /// <inheritdoc cref="IBrowser"/>
        public event EventHandler<TargetChangedArgs> TargetChanged;

        /// <inheritdoc cref="IBrowser"/>
        public event EventHandler<TargetChangedArgs> TargetCreated;

        /// <inheritdoc cref="IBrowser"/>
        public event EventHandler<TargetChangedArgs> TargetDestroyed;

        /// <inheritdoc cref="IBrowser"/>
        public event EventHandler Disconnected;

        /// <inheritdoc cref="IBrowser"/>
        public IBrowserContext[] BrowserContexts => null;

        /// <inheritdoc cref="IBrowser"/>
        public IBrowserContext DefaultContext { get; }

        /// <summary>
        /// Dafault wait time in milliseconds. Defaults to 30 seconds.
        /// </summary>
        public int DefaultWaitForTimeout { get; set; } = Playwright.DefaultTimeout;

        /// <inheritdoc cref="IBrowser"/>
        public bool IsConnected => false;

        internal IDictionary<string, ChromiumTarget> TargetsMap { get; } = new ConcurrentDictionary<string, ChromiumTarget>();

        /// <inheritdoc cref="IBrowser"/>
        public Task CloseAsync() => _app?.CloseAsync();

        /// <inheritdoc cref="IBrowser"/>
        public Task DisconnectAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowser"/>
        public void Dispose()
        {
        }

        /// <inheritdoc cref="IBrowser"/>
        public Task<IBrowserContext> NewContextAsync(BrowserContextOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowser"/>
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

        internal static Task<IBrowser> ConnectAsync(ConnectOptions options) => ConnectAsync(null, options);

        internal static async Task<IBrowser> ConnectAsync(IBrowserApp app, ConnectOptions options)
        {
            var transport = await BrowserHelper.CreateTransportAsync(options).ConfigureAwait(false);
            var connection = new ChromiumConnection(transport);
            var response = await connection.RootSession.SendAsync<TargetGetBrowserContextsResponse>("Target.getBrowserContexts").ConfigureAwait(false);
            var browser = new ChromiumBrowser(app, connection, response.BrowserContextIds);
            await connection.RootSession.SendAsync("Target.setDiscoverTargets", new TargetSetDiscoverTargetsRequest { Discover = true }).ConfigureAwait(false);
            await browser.WaitForTargetAsync(t => t.Type == TargetType.Page).ConfigureAwait(false);
            return browser;
        }

        private async void Session_MessageReceived(object sender, MessageEventArgs e)
        {
            try
            {
                switch (e.MessageID)
                {
                    case "Target.targetCreated":
                        await CreateTargetAsync(e.MessageData?.ToObject<TargetCreatedResponse>()).ConfigureAwait(false);
                        return;

                    case "Target.targetDestroyed":
                        await DestroyTargetAsync(e.MessageData?.ToObject<TargetDestroyedResponse>()).ConfigureAwait(false);
                        return;

                    case "Target.targetInfoChanged":
                        ChangeTargetInfo(e.MessageData?.ToObject<TargetCreatedResponse>());
                        return;
                }
            }

            // We need to silence exceptions on async void events.
#pragma warning disable CA1031 // Do not catch general exception types.
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types.
            {
                string message = $"Browser failed to process {e.MessageID}. {ex.Message}. {ex.StackTrace}";

                // TODO Add Logger _logger.LogError(ex, message);
                _connection.Close(message);
            }
        }

        private async Task CreateTargetAsync(TargetCreatedResponse e)
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

        private async Task DestroyTargetAsync(TargetDestroyedResponse e)
        {
            if (!TargetsMap.ContainsKey(e.TargetId))
            {
                throw new PlaywrightSharpException("Target should exists before DestroyTarget");
            }

            var target = TargetsMap[e.TargetId];
            TargetsMap.Remove(e.TargetId);

            target.CloseTaskWrapper.TrySetResult(true);

            if (await target.InitializedTask.ConfigureAwait(false))
            {
                var args = new TargetChangedArgs { Target = target };
                TargetDestroyed?.Invoke(this, args);
            }
        }

        private void ChangeTargetInfo(TargetCreatedResponse e)
        {
            if (!TargetsMap.ContainsKey(e.TargetInfo.TargetId))
            {
                throw new PlaywrightSharpException("Target should exists before ChangeTargetInfo");
            }

            var target = TargetsMap[e.TargetInfo.TargetId];
            target.TargetInfoChanged(e.TargetInfo);
        }

        private IEnumerable<ChromiumTarget> GetAllTargets() => TargetsMap.Values.Where(t => t.IsInitialized);
    }
}
