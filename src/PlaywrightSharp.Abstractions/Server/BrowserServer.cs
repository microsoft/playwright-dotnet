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

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserServer"/> class.
        /// </summary>
        /// <param name="process">Browser process.</param>
        /// <param name="gracefullyClose">Browser close func.</param>
        /// <param name="kill">Process kill func.</param>
        /// <param name="webSocketEndpoint">WebSocket endpoint.</param>
        public BrowserServer(Process process, Func<Task> gracefullyClose, Func<Task> kill, string webSocketEndpoint)
        {
            Process = process;
            WebSocketEndpoint = webSocketEndpoint;
            _gracefullyClose = gracefullyClose;
            _kill = kill;
        }

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
    }
}
