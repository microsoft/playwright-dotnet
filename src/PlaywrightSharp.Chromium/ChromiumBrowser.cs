using System;
using System.Diagnostics;
using System.Threading.Tasks;
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

        internal ChromiumBrowser(IBrowserApp app, ChromiumConnection connection, string[] browserContextIds)
        {
            _app = app;
            _connection = connection;
            _browserContextIds = browserContextIds;
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
        public IBrowserContext DefaultContext => null;

        /// <inheritdoc cref="IBrowser"/>
        public bool IsConnected => false;

        /// <inheritdoc cref="IBrowser"/>
        public Task CloseAsync()
        {
            throw new NotImplementedException();
        }

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
        public Task<ITarget> WaitForTargetAsync(Func<ITarget, bool> predicate, WaitForOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowser"/>
        internal static async Task<IBrowser> ConnectAsync(IBrowserApp app)
        {
            var transport = await BrowserHelper.CreateTransportAsync(app.GetConnectOptions()).ConfigureAwait(false);
            var connection = new ChromiumConnection(transport);
            var response = await connection.RootSession.SendAsync<TargetGetBrowserContextsResponse>("Target.getBrowserContexts").ConfigureAwait(false);
            var browser = new ChromiumBrowser(app, connection, response.BrowserContextIds);
            await connection.RootSession.SendAsync("Target.setDiscoverTargets", new TargetSetDiscoverTargetsRequest { Discover = true }).ConfigureAwait(false);
            await browser.WaitForTargetAsync(t => t.Type == TargetType.Page).ConfigureAwait(false);
            return browser;
        }
    }
}
