using System;
using System.Diagnostics;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Server
{
    /// <summary>
    /// Manage the lifecycle of a browser process.
    /// </summary>
    public class BrowserServer : IBrowserServer
    {
        private readonly Func<Task> _gracefullyClose;
        private readonly Func<Task> _kill;

        internal BrowserServer(LaunchResult launchResult)
        {
            Process = launchResult.Process;
            WebSocketEndpoint = launchResult.WebSocketEndpoint;
            _gracefullyClose = launchResult.GracefullyCloseFunction;
            _kill = launchResult.KillFunction;
        }

        /// <inheritdoc cref="IBrowserServer.Closed"/>
        public event EventHandler<BrowserAppClosedEventArgs> Closed;

        /// <summary>
        /// Browser process.
        /// </summary>
        public Process Process { get; }

        /// <summary>
        /// WebSocket endpoint to connect to the browser.
        /// </summary>
        public string WebSocketEndpoint { get; }

        /// <summary>
        /// Kills the browser's process.
        /// </summary>
        /// <returns>A task that completes when the process was killed.</returns>
        public Task KillAsync() => _kill() ?? Task.CompletedTask;

        /// <summary>
        /// Gracefully close the browser.
        /// </summary>
        /// <returns>A task that completes when the close message was sent to the browser.</returns>
        public Task CloseAsync() => _gracefullyClose() ?? Task.CompletedTask;

        internal async Task CloseOrKillAsync(int timeout)
        {
            try
            {
                await CloseAsync().WithTimeout(timeout).ConfigureAwait(false);
            }
            catch
            {
                try
                {
                    await KillAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    throw;
                }
            }
        }

        internal void OnClose(int exitCode) => Closed?.Invoke(this, new BrowserAppClosedEventArgs { ExitCode = exitCode });
    }
}
