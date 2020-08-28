using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// The CDPSession instances are used to talk raw Chrome Devtools Protocol.
    /// </summary>
    public interface ICDPSession
    {
        /// <summary>
        /// Raised when a new message is received.
        /// </summary>
        event EventHandler<CDPEventArgs> MessageReceived;

        /// <summary>
        /// Make a method call to the browser session.
        /// </summary>
        /// <param name="method">Method name.</param>
        /// <param name="args">Arguments.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser session, yielding the result.</returns>
        Task<JsonElement?> SendAsync(string method, object args);

        /// <summary>
        /// Make a method call to the browser session.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="method">Method name.</param>
        /// <param name="args">Arguments.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser session, yielding the result.</returns>
        Task<T> SendAsync<T>(string method, object args);

        /// <summary>
        /// Detaches the CDPSession from the target. Once detached, the CDPSession object won't emit any events and can't be used to send messages.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser session.</returns>
        Task DetachAsync();
    }
}
