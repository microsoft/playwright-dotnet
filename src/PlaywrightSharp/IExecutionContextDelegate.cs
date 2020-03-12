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
        /// <param name="script">Script to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to script.</param>
        /// <typeparam name="T">Return type.</typeparam>
        /// <remarks>
        /// If the script, returns a Promise, then the method would wait for the promise to resolve and return its value.
        /// </remarks>
        /// <seealso cref="IFrame.EvaluateAsync{T}(string, object[])"/>
        /// <returns>A <see cref="Task"/>  that completes when the script finishes or the promise is resolved, yielding the result of the script.</returns>
        Task<T> EvaluateAsync<T>(FrameExecutionContext frameExecutionContext, bool returnByValue, string script, object[] args);
    }
}
