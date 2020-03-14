using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            _contexts = browserContextIds.ToDictionary(id => id, CreateBrowserContext);

            TargetChanged?.Invoke(this, new TargetChangedArgs());
            TargetCreated?.Invoke(this, new TargetChangedArgs());
            TargetDestroyed?.Invoke(this, new TargetChangedArgs());
            Disconnected?.Invoke(this, EventArgs.Empty);
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
        public IBrowserContext DefaultContext => null;

        /// <inheritdoc cref="IBrowser.IsConnected"/>
        public bool IsConnected => false;

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

        private IBrowserContext CreateBrowserContext(string browserContextId)
        {
            return null;
        }
    }
}
