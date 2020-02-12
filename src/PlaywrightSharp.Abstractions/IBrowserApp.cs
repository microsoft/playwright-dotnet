using System;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Browser process manager.
    /// </summary>
    public interface IBrowserApp : IDisposable
    {
        /// <summary>
        /// Browser websocket endpoint which can be used as an argument to <see cref="IBrowserType.ConnectAsync(ConnectOptions)"/> to establish connection to the browser.
        /// </summary>
        string WebSocketEndpoint { get; }

        /// <summary>
        /// Process id.
        /// </summary>
        int ProcessId { get; }

        /// <summary>
        /// Closes browser and all of its pages (if any were opened).
        /// The Browser object itself is considered to be disposed and cannot be used anymore.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the brwoser is closed.</returns>
        Task CloseAsync();

        /// <summary>
        /// This options object can be passed to <see cref="IBrowserType.ConnectAsync(ConnectOptions)"/> to establish connection to the browser.
        /// </summary>
        /// <returns><see cref="ConnectOptions"/> to connect to this <see cref="IBrowserApp"/>.</returns>
        ConnectOptions GetConnectOptions();
    }
}
