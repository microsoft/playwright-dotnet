using System;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Helpers;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Runtime;
using PlaywrightSharp.Chromium.Protocol.Target;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="ITarget"/>
    public class ChromiumTarget : ITarget
    {
        private readonly ChromiumBrowser _browser;
        private readonly Func<Task<ChromiumSession>> _sessionFactory;
        private readonly TaskCompletionSource<bool> _initializedTaskWrapper = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task<IWorker> _workerTask;

        internal ChromiumTarget(
            TargetInfo targetInfo,
            ChromiumBrowser browser,
            Func<Task<ChromiumSession>> sessionFactory,
            IBrowserContext browserContext)
        {
            TargetInfo = targetInfo;
            _browser = browser;
            _sessionFactory = sessionFactory;
            BrowserContext = browserContext;
            PageTask = null;

            _initializedTaskWrapper.Task.ContinueWith(
                async initializedTask =>
                {
                    bool success = initializedTask.Result;
                    if (!success)
                    {
                        return;
                    }

                    var openerPageTask = Opener?.PageTask;
                    if (openerPageTask == null || Type != TargetType.Page)
                    {
                        return;
                    }

                    var openerPage = await openerPageTask.ConfigureAwait(false);
                    if (!openerPage.HasPopupEventListeners)
                    {
                        return;
                    }

                    var popupPage = await GetPageAsync().ConfigureAwait(false);
                    openerPage.OnPopup(popupPage);
                },
                CancellationToken.None,
                TaskContinuationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);

            CloseTaskWrapper = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            IsInitialized = Type != TargetType.Page || !string.IsNullOrEmpty(TargetInfo.Url);

            if (IsInitialized)
            {
                _initializedTaskWrapper.TrySetResult(true);
            }
        }

        /// <inheritdoc cref="ITarget.Url"/>
        public string Url => TargetInfo.Url;

        /// <inheritdoc cref="ITarget.Type"/>
        public TargetType Type => TargetInfo.GetTargetType();

        /// <inheritdoc cref="ITarget.BrowserContext"/>
        public IBrowserContext BrowserContext { get; }

        /// <inheritdoc cref="ITarget.Opener"/>
        ITarget ITarget.Opener => Opener;

        internal bool IsInitialized { get; set; }

        internal TargetInfo TargetInfo { get; set; }

        internal string TargetId => TargetInfo.TargetId;

        internal ChromiumTarget Opener => TargetInfo.OpenerId != null ? _browser.TargetsMap.GetValueOrDefault(TargetInfo.OpenerId) : null;

        internal Task<bool> InitializedTask => _initializedTaskWrapper.Task;

        internal Task CloseTask => CloseTaskWrapper.Task;

        internal TaskCompletionSource<bool> CloseTaskWrapper { get; }

        internal Task<Page> PageTask { get; set; }

        internal ChromiumPage ChromiumPage { get; set; }

        /// <summary>
        /// If the target is not of type `"service_worker"` or `"shared_worker"`, returns `null`.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the worker is resolved, yielding the <see cref="Worker"/>.</returns>
        public Task<IWorker> GetWorkerAsync()
        {
            if (Type != TargetType.ServiceWorker && Type != TargetType.SharedWorker)
            {
                return Task.FromResult<IWorker>(null);
            }

            return _workerTask ??= WorkerInternalAsync();
        }

        /// <inheritdoc cref="ITarget.GetPageAsync"/>
        public async Task<IPage> GetPageAsync()
        {
            if ((Type == TargetType.Page || Type == TargetType.BackgroundPage) && PageTask == null)
            {
                PageTask = CreatePageAsync();
            }

            return await (PageTask ?? Task.FromResult<Page>(null)).ConfigureAwait(false);
        }

        internal static ChromiumTarget FromPage(Page page) => page.Target as ChromiumTarget;

        internal void TargetInfoChanged(TargetInfo targetInfo)
        {
            TargetInfo = targetInfo;

            if (!IsInitialized && (Type != TargetType.Page || !string.IsNullOrEmpty(TargetInfo.Url)))
            {
                IsInitialized = true;
                _initializedTaskWrapper.TrySetResult(true);
            }
        }

        internal Task<ChromiumSession> CreateCDPSessionAsync() => _sessionFactory();

        internal void DidClose() => ChromiumPage?.DidClose();

        private async Task<IWorker> WorkerInternalAsync()
        {
            var session = await _sessionFactory().ConfigureAwait(false);
            var worker = new Worker(TargetInfo.Url);

            void HandleRuntimeExecutionContextCreated(object sender, IChromiumEvent e)
            {
                if (e is RuntimeExecutionContextCreatedChromiumEvent runtimeExecutionContextCreated)
                {
                    worker.CreateExecutionContext(new ChromiumExecutionContext(session, runtimeExecutionContextCreated.Context));
                    session.MessageReceived -= HandleRuntimeExecutionContextCreated;
                }
            }

            session.MessageReceived += HandleRuntimeExecutionContextCreated;

            try
            {
                await session.SendAsync(new RuntimeEnableRequest()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return worker;
        }

        private async Task<Page> CreatePageAsync()
        {
            var client = await _sessionFactory().ConfigureAwait(false);
            ChromiumPage = new ChromiumPage(client, _browser, BrowserContext);
            var page = ChromiumPage.Page;
            page.Target = this;

            client.Disconnected += (sender, e) => page.DidDisconnected();

            client.MessageReceived += (sender, e) =>
            {
                if (e is TargetAttachedToTargetChromiumEvent response)
                {
                    if (response.TargetInfo.GetTargetType() != TargetType.Worker)
                    {
                        _ = client.SendAsync(new TargetDetachFromTargetRequest { SessionId = response.SessionId });
                    }
                }
            };

            await ChromiumPage.InitializeAsync().ConfigureAwait(false);
            await client.SendAsync(new TargetSetAutoAttachRequest
            {
                AutoAttach = true,
                WaitForDebuggerOnStart = false,
                Flatten = true,
            }).ConfigureAwait(false);

            return page;
        }
    }
}
