using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    /// <summary>
    /// Chromium browser.
    /// </summary>
    public sealed class ChromiumBrowser : IBrowser
    {
        private readonly IBrowserApp _app;

        internal ChromiumBrowser(IBrowserApp app) => _app = app;

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
        public Process Process => null;

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
        internal static Task<IBrowser> ConnectAsync(IBrowserApp app)
        {
            /*
            var transport = await CreateTransport(options);
            const connection = new CRConnection(transport);
            const { browserContextIds } = await connection.rootSession.send('Target.getBrowserContexts');
            const browser = new CRBrowser(connection, browserContextIds);
            await connection.rootSession.send('Target.setDiscoverTargets', { discover: true });
            await browser.waitForTarget(t => t.type() === 'page');
            return browser;
            */

            return Task.FromResult<IBrowser>(new ChromiumBrowser(app));
        }
    }
}
