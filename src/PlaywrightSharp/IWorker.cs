using System;
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
        /// Raised when worker gets closed.
        /// </summary>
        event EventHandler<EventArgs> Close;

        /// <summary>
        /// Gets the Worker URL.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Executes a function in browser context, passing the current <see cref="IElementHandle"/> as the first argument.
        /// </summary>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// <see cref="IJSHandle"/> instances can be passed as arguments.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script is executed, yielding the return value of that script.</returns>
        Task<IJSHandle> EvaluateHandleAsync(string expression);

        /// <summary>
        /// Executes a function in browser context, passing the current <see cref="IElementHandle"/> as the first argument.
        /// </summary>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// <see cref="IJSHandle"/> instances can be passed as arguments.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script is executed, yielding the return value of that script.</returns>
        Task<IJSHandle> EvaluateHandleAsync(string expression, object arg);

        /// <summary>
        /// Executes a script in the frame context.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>Task that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(string expression);

        /// <summary>
        /// Executes a script in the frame context.
        /// </summary>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script as an row Json element.</returns>
        Task<JsonElement?> EvaluateAsync(string expression);

        /// <summary>
        /// Executes a script in the frame context.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IPage.EvaluateAsync{T}(string, object)"/>
        /// <returns>Task that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(string expression, object arg);

        /// <summary>
        /// Executes a script in the frame context.
        /// </summary>
        /// <param name="expression">Script to be evaluated in browser context.</param>
        /// <param name="arg">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IPage.EvaluateAsync(string, object)"/>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script as an row Json element.</returns>
        Task<JsonElement?> EvaluateAsync(string expression, object arg);
    }
}
