using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Browser process manager.
    /// </summary>
    public interface IBrowserApp
    {
        /// <summary>
        /// Browser websocket endpoint which can be used as an argument to <see cref="IBrowserType.ConnectAsync(ConnectOptions)"/> to establish connection to the browser.
        /// </summary>
        string WebSocketEndpoint { get; }

        /// <summary>
        /// Closes browser and all of its pages (if any were opened).
        /// The Browser object itself is considered to be disposed and cannot be used anymore.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the brwoser is closed.</returns>
        Task CloseAsync();
    }
}
