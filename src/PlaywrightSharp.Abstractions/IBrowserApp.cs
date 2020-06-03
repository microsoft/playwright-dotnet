using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Browser process manager.
    /// </summary>
    public interface IBrowserApp : IDisposable
    {
        /// <summary>
        /// Triggered when the <see cref="IBrowserApp"/> gets closed.
        /// </summary>
        event EventHandler<BrowserAppClosedEventArgs> Closed;

        /// <summary>
        /// Browser websocket endpoint which can be used as an argument to <see cref="IBrowserType.ConnectAsync(ConnectOptions)"/> to establish connection to the browser.
        /// </summary>
        string WebSocketEndpoint { get; }

        /// <summary>
        /// Gets the spawned browser process. Returns <c>null</c> if the browser instance was created with <see cref="IBrowserType.ConnectAsync(ConnectOptions)"/> method.
        /// </summary>
        Process Process { get; }

        /// <summary>
        /// This options object can be passed to <see cref="IBrowserType.ConnectAsync(ConnectOptions)"/> to establish connection to the browser.
        /// </summary>
        ConnectOptions ConnectOptions { get; }

        /// <summary>
        /// Closes browser and all of its pages (if any were opened).
        /// The Browser object itself is considered to be disposed and cannot be used anymore.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the brwoser is closed.</returns>
        Task CloseAsync();

        /// <summary>
        /// Kills the browser process.
        /// </summary>
        void Kill();
    }
}
