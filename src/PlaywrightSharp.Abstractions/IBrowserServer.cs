using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Manage the lifecycle of a browser process.
    /// </summary>
    public interface IBrowserServer
    {
        /// <summary>
        /// Triggered when the <see cref="IBrowserServer"/> gets closed.
        /// </summary>
        public event EventHandler<BrowserAppClosedEventArgs> Closed;

        /// <summary>
        /// Browser process.
        /// </summary>
        Process Process { get; }

        /// <summary>
        /// WebSocket endpoint to connect to the browser.
        /// </summary>
        string WebSocketEndpoint { get; }

        /// <summary>
        /// Kills the browser's process.
        /// </summary>
        /// <returns>A task that completes when the process was killed.</returns>
        Task KillAsync();

        /// <summary>
        /// Gracefully close the browser.
        /// </summary>
        /// <returns>A task that completes when the close message was sent to the browser.</returns>
        Task CloseAsync();
    }
}
