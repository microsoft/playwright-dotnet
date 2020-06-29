using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PlaywrightSharp.Transport;

namespace PlaywrightSharp
{
    /// <inheritdoc />
    public class PlaywrightClient : IPlaywrightClient, IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private Process _playwrightServerProcess;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaywrightClient"/> class.
        /// </summary>
        /// <param name="loggerFactory">Logger.</param>
        public PlaywrightClient(ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~PlaywrightClient() => Dispose(false);

        /// <summary>
        /// Playwright Server Path.
        /// </summary>
        public string PlaywrightServerPath { get; set; }

        /// <inheritdoc />
        public Task<IBrowserType> GetChromiumBrowserAsync() => GetBrowserTypeAsync(BrowserType.Chromium);

        /// <inheritdoc />
        public Task<IBrowserType> GetFirefoxBrowserAsync() => GetBrowserTypeAsync(BrowserType.Chromium);

        /// <inheritdoc />
        public Task<IBrowserType> GetWebkitTypeAsync() => GetBrowserTypeAsync(BrowserType.Chromium);

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task<IBrowserType> GetBrowserTypeAsync(string chromium)
        {
            if (_playwrightServerProcess == null)
            {
                LaunchPlaywrightServer();
            }
        }

        private void LaunchPlaywrightServer()
        {
            _playwrightServerProcess = LaunchProcess();
            var transport = new StdIOTransport(_playwrightServerProcess);
            transport.MessageReceived += TransportOnMessageReceived;
        }

        private void TransportOnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private Process LaunchProcess()
        {
            var process = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = GetExecutablePath(),
                    RedirectStandardOutput = true,
                },
            };

            process.Start();
            return process;
        }

        private string GetExecutablePath()
        {
            throw new System.NotImplementedException();
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _loggerFactory?.Dispose();

            if (_playwrightServerProcess?.HasExited == false)
            {
                _playwrightServerProcess.Kill();
            }

            _playwrightServerProcess?.Dispose();
        }
    }
}
