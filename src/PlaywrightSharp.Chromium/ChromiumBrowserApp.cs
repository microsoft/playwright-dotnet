using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumBrowserApp : IBrowserApp
    {
        private readonly ChromiumProcessManager _processManager;

        public ChromiumBrowserApp(ChromiumProcessManager processManager)
        {
            _processManager = processManager;
        }

        public event EventHandler<BrowserAppClosedEventArgs> Closed;

        public string WebSocketEndpoint => null;

        public Process Process => _processManager.Process;

        public Task CloseAsync()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _processManager.KillAsync().GetAwaiter().GetResult();
        }

        public ConnectOptions GetConnectOptions()
        {
            throw new NotImplementedException();
        }

        public void Kill()
        {
            throw new NotImplementedException();
        }

        public void ProcessKilled(int exitCode) => Closed?.Invoke(this, new BrowserAppClosedEventArgs { ExitCode = exitCode });
    }
}