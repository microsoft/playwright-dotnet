using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Represents the <see cref="IFrame"/> execution context.
    /// </summary>
    internal interface IFrameExecutionContext
    {
        /// <summary>
        /// Executes a script in the frame's context.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="returnByValue">Return by value.</param>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IPage.EvaluateAsync{T}(string, object[])"/>
        /// <seealso cref="IFrame.EvaluateAsync{T}(string, object[])"/>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(bool returnByValue, string script, params object[] args);

        /// <summary>
        /// Executes a script in the frame's context.
        /// </summary>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IPage.EvaluateAsync{T}(string, object[])"/>
        /// <seealso cref="IFrame.EvaluateAsync{T}(string, object[])"/>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved.</returns>
        Task EvaluateAsync(string script, params object[] args);

        /// <summary>
        /// Returns the injected <see cref="JSHandle"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the injected <see cref="JSHandle"/> is resolved, yielding the <see cref="IJSHandle"/>.</returns>
        Task<JSHandle> GetInjectedAsync();

        /// <summary>
        /// Executes a script in the frame's context.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IPage.EvaluateAsync{T}(string, object[])"/>
        /// <seealso cref="IFrame.EvaluateAsync{T}(string, object[])"/>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(string script, params object[] args);

        /// <summary>
        /// Executes a function that returns a <see cref="IJSHandle"/>.
        /// </summary>
        /// <param name="script">Function to be evaluated in the frame context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IFrame.EvaluateHandleAsync(string, object[])"/>
        /// <returns>A <see cref="Task"/> that completes when the script finishes or the promise is resolved, yielding the result of the script as a <see cref="IJSHandle"/>.</returns>
        Task<JSHandle> EvaluateHandleAsync(string script, params object[] args);

        /// <summary>
        /// The method runs <c>document.querySelector</c> within the element. If no element matches the selector, the return value resolve to <c>null</c>.
        /// </summary>
        /// <param name="selector">A selector to query element for.</param>
        /// <param name="scope">Scope.</param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the javascript function finishes, yielding an <see cref="IElementHandle"/>.
        /// </returns>
        Task<IElementHandle> QuerySelectorAsync(string selector, IElementHandle scope = null);
    }
}
