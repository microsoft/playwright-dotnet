using System;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Represents a browser process a client can connect to.
    /// </summary>
    public interface IBrowserServer : IAsyncDisposable
    {
        /// <summary>
        /// Raised when the browser is closed.
        /// </summary>
        event EventHandler Closed;

        /// <summary>
        /// Browser process ID.
        /// </summary>
        int ProcessId { get; }

        /// <summary>
        /// Browser websocket endpoint which can be used as an argument to <see cref="IBrowserType.ConnectAsync(string, int?, int?)"/> to establish connection to the browser.
        /// </summary>
        string WSEndpoint { get; }

        /// <summary>
        /// Closes the browser gracefully and makes sure the process is terminated.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the browser is closed.</returns>
        Task CloseAsync();

        /// <summary>
        /// Kills the browser process and waits for the process to exit.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the browser process is killed.</returns>
        Task KillAsync();
    }
}
