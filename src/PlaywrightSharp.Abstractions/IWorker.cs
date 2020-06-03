using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// The Worker class represents a <see href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API">WebWorker</see>.
    /// </summary>
    public interface IWorker
    {
        /// <summary>
        /// Gets the Worker URL.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Executes a script in the worker context.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the result to.</typeparam>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/>  that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(string script, params object[] args);

        /// <summary>
        /// Executes a script in the worker context.
        /// </summary>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/>  that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<JsonElement?> EvaluateAsync(string script, params object[] args);
    }
}
