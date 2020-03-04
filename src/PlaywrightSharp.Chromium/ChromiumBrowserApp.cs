using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    /// <summary>
    /// Chromium implementation for <see cref="IBrowserApp"/>.
    /// </summary>
    public class ChromiumBrowserApp : IBrowserApp
    {
        private readonly ChromiumProcessManager _processManager;

        internal ChromiumBrowserApp(ChromiumProcessManager processManager, ConnectOptions options)
        {
            _processManager = processManager;
            ConnectOptions = options;
        }

        /// <inheritdoc cref="IDisposable"/>
        ~ChromiumBrowserApp() => Dispose(false);

        /// <inheritdoc cref="IBrowserApp"/>
        public event EventHandler<BrowserAppClosedEventArgs> Closed;

        /// <inheritdoc cref="IBrowserApp"/>
        public ConnectOptions ConnectOptions { get; }

        /// <inheritdoc cref="IBrowserApp"/>
        public string WebSocketEndpoint => null;

        /// <inheritdoc cref="IBrowserApp"/>
        public Process Process => _processManager.Process;

        /// <inheritdoc cref="IBrowserApp"/>
        public Task CloseAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserApp"/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc cref="IBrowserApp"/>
        public void Kill()
        {
            throw new NotImplementedException();
        }

        internal void ProcessKilled(int exitCode) => Closed?.Invoke(this, new BrowserAppClosedEventArgs { ExitCode = exitCode });

        /// <inheritdoc cref="IDisposable"/>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _processManager.KillAsync().GetAwaiter().GetResult();
            }
        }
    }
}
