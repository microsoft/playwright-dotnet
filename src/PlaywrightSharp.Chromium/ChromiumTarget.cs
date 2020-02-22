using System;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Helpers;
using PlaywrightSharp.Chromium.Messaging.Target;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="ITarget"/>
    public class ChromiumTarget : ITarget
    {
        private readonly Func<Task<ChromiumSession>> _sessionFactory;
        private readonly TaskCompletionSource<bool> _initializedTaskWrapper = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task<Worker> _workerTask;
        private ChromiumPage _page;

        internal ChromiumTarget(
            TargetInfo targetInfo,
            Func<Task<ChromiumSession>> sessionFactory,
            ChromiumBrowserContext browserContext)
        {
            TargetInfo = targetInfo;
            _sessionFactory = sessionFactory;
            BrowserContext = browserContext;
            PageTask = null;

            _ = _initializedTaskWrapper.Task.ContinueWith(
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

                    var popupPage = await PageAsync().ConfigureAwait(false);
                    openerPage.OnPopup(popupPage);
                },
                CancellationToken.None,
                TaskContinuationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);

            CloseTaskWrapper = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            IsInitialized = TargetInfo.Type != TargetType.Page || !string.IsNullOrEmpty(TargetInfo.Url);

            if (IsInitialized)
            {
                _initializedTaskWrapper.TrySetResult(true);
            }
        }

        /// <inheritdoc cref="ITarget"/>
        public string Url => TargetInfo.Url;

        /// <inheritdoc cref="ITarget"/>
        public TargetType Type => TargetInfo.Type;

        internal bool IsInitialized { get; set; }

        internal TargetInfo TargetInfo { get; set; }

        internal string TargetId => TargetInfo.TargetId;

        internal ChromiumTarget Opener => TargetInfo.OpenerId != null ? Browser.TargetsMap.GetValueOrDefault(TargetInfo.OpenerId) : null;

        internal ChromiumBrowser Browser => BrowserContext.Browser;

        internal ChromiumBrowserContext BrowserContext { get; }

        internal Task<bool> InitializedTask => _initializedTaskWrapper.Task;

        internal Task CloseTask => CloseTaskWrapper.Task;

        internal TaskCompletionSource<bool> CloseTaskWrapper { get; }

        internal Task<ChromiumPage> PageTask { get; set; }

        /// <summary>
        /// If the target is not of type `"service_worker"` or `"shared_worker"`, returns `null`.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the worker is resolved, yielding the <see cref="Worker"/>.</returns>
        public Task<Worker> WorkerAsync()
        {
            if (TargetInfo.Type != TargetType.ServiceWorker && TargetInfo.Type != TargetType.SharedWorker)
            {
                return Task.FromResult<Worker>(null);
            }

            if (_workerTask == null)
            {
                _workerTask = WorkerInternalAsync();
            }

            return _workerTask;
        }

        internal Task<ChromiumPage> PageAsync()
        {
            if ((TargetInfo.Type == TargetType.Page || TargetInfo.Type == TargetType.BackgroundPage) && PageTask == null)
            {
                PageTask = CreatePageAsync();
            }

            return PageTask ?? Task.FromResult<ChromiumPage>(null);
        }

        internal void TargetInfoChanged(TargetInfo targetInfo)
        {
            TargetInfo = targetInfo;

            if (!IsInitialized && (TargetInfo.Type != TargetType.Page || !string.IsNullOrEmpty(TargetInfo.Url)))
            {
                IsInitialized = true;
                _initializedTaskWrapper.TrySetResult(true);
                return;
            }
        }

        private static Task<Worker> WorkerInternalAsync() => Task.FromResult<Worker>(null);

        private async Task<ChromiumPage> CreatePageAsync()
        {
            var client = await _sessionFactory().ConfigureAwait(false);
            _page = new ChromiumPage(client, Browser, BrowserContext);
            _page.Target = this;

            client.Disconnected += (sender, e) => _page.DidDisconnected();

            client.MessageReceived += (sender, e) =>
            {
                if (e.MessageID == "Target.attachedToTarget")
                {
                    var response = e.MessageData.Value.ToObject<TargetAttachToTargetResponse>();
                    if (response.TargetInfo.Type != TargetType.ServiceWorker)
                    {
                        _ = client.SendAsync("Target.detachFromTarget", new TargetDetachFromTargetRequest { SessionId = response.SessionId });
                    }
                }
            };

            await _page.InitializeAsync();
            await client.send('Target.setAutoAttach', { autoAttach: true, waitForDebuggerOnStart: false, flatten: true});
            return page;
        }
    }
}