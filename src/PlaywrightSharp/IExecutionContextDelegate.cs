using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Execution context delegate.
    /// </summary>
    internal interface IExecutionContextDelegate
    {
        /// <summary>
        /// Executes a script in browser context.
        /// </summary>
        /// <param name="frameExecutionContext">Execution context.</param>
        /// <param name="returnByValue">Return by value.</param>
        /// <param name="pageFunction">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <typeparam name="T">Return type.</typeparam>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IFrame.EvaluateAsync{T}(string, object[])"/>
        /// <returns>A <see cref="Task"/>  that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(ExecutionContext frameExecutionContext, bool returnByValue, string pageFunction, object[] args);

        /// <summary>
        /// Releases a <see cref="JSHandle"/>.
        /// </summary>
        /// <param name="handle">Handle to be released.</param>
        /// <returns>A <see cref="Task"/> that completes when the message is confirmed by the browser.</returns>
        Task ReleaseHandleAsync(JSHandle handle);

        /// <summary>
        /// Converts an <see cref="IJSHandle"/> to string.
        /// </summary>
        /// <param name="handle"><see cref="IJSHandle"/> to parse.</param>
        /// <param name="includeType">Whether to include the type or not.</param>
        /// <returns>An <see cref="string"/> representation of the handle.</returns>
        string HandleToString(IJSHandle handle, bool includeType);

        /// <summary>
        /// Get a JSON representation of an <see cref="IJSHandle"/>.
        /// </summary>
        /// <param name="jsHandle">Handle to evaluate.</param>
        /// <typeparam name="T">Type to convert the JSON object to.</typeparam>
        /// <returns>A <see cref="Task"/> that completes when the JSON value is obtained and parsed, yielding a parsed object.</returns>
        Task<T> HandleJSONValueAsync<T>(IJSHandle jsHandle);
    }
}
