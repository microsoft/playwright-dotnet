using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    public partial interface IFrame
    {

        /// <summary>
        /// <para>Returns the return value of <paramref name="expression"/>.</para>
        /// <para>
        /// If the function passed to the <see cref="IFrame.EvaluateAsync"/> returns a <see
        /// cref="Promise"/>, then <see cref="IFrame.EvaluateAsync"/> would wait for the promise
        /// to resolve and return its value.
        /// </para>
        /// <para>
        /// If the function passed to the <see cref="IFrame.EvaluateAsync"/> returns a non-<see
        /// cref="Serializable"/> value, then <see cref="IFrame.EvaluateAsync"/> returns <c>undefined</c>.
        /// Playwright also supports transferring some additional values that are not serializable
        /// by <c>JSON</c>: <c>-0</c>, <c>NaN</c>, <c>Infinity</c>, <c>-Infinity</c>.
        /// </para>
        /// <para>A string can also be passed in instead of a function.</para>
        /// <para>
        /// <see cref="IElementHandle"/> instances can be passed as an argument to the <see
        /// cref="IFrame.EvaluateAsync"/>.
        /// </para>
        /// </summary>
        /// <param name="expression">
        /// JavaScript expression to be evaluated in the browser context. If it looks like a
        /// function declaration, it is interpreted as a function. Otherwise, evaluated as an
        /// expression.
        /// </param>
        /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
        /// <returns>A <see cref="Task"/> that will resolve when the evaluate function is executed by the browser.</returns>
        public Task<JsonElement?> EvaluateAsync(string expression, object arg = default);
    }
}
