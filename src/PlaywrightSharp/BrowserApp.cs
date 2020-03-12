using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    /// <summary>
    /// Chromium implementation for <see cref="IBrowserApp"/>.
    /// </summary>
    public class BrowserApp : IBrowserApp
    {
        private readonly IProcessManager _processManager;
        private readonly Func<Task> _gracefullyClose;

        internal BrowserApp(IProcessManager processManager, Func<Task> gracefullyClose, ConnectOptions options)
        {
            _processManager = processManager;
            _gracefullyClose = gracefullyClose;
            ConnectOptions = options;
        }

        /// <inheritdoc cref="IDisposable"/>
        ~BrowserApp() => Dispose(false);

        /// <inheritdoc cref="IBrowserApp"/>
        public event EventHandler<BrowserAppClosedEventArgs> Closed;

        /// <inheritdoc cref="IBrowserApp"/>
        public ConnectOptions ConnectOptions { get; }

        /// <inheritdoc cref="IBrowserApp"/>
        public string WebSocketEndpoint => ConnectOptions.BrowserWSEndpoint;

        /// <inheritdoc cref="IBrowserApp"/>
        public Process Process => _processManager.Process;

        /// <inheritdoc cref="IBrowserApp"/>
        public Task CloseAsync() => _gracefullyClose();

        /// <inheritdoc cref="IBrowserApp"/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc cref="IBrowserApp"/>
        public void Kill()
        {
            if (Process?.HasExited == false)
            {
                int pid = Process.Id;
                using var process = new Process();

                // We need to kill the process tree manually
                // See: https://github.com/dotnet/corefx/issues/26234
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    process.StartInfo.FileName = "taskkill";
                    process.StartInfo.Arguments = $"-pid {pid} -t -f";
                }
                else
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = $"-c \"kill -s 9 {pid}\"";
                }

                process.Start();
                process.WaitForExit();
            }
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
