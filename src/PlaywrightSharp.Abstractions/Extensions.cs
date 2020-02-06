using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IJSHandle"/> and <see cref="IElementHandle"/> Extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Runs <paramref name="pageFunction"/> within the frame and passes it the outcome of <paramref name="elementHandleTask"/> as the first argument.
        /// </summary>
        /// <param name="elementHandleTask">A task that returns an <see cref="IElementHandle"/> that will be used as the first argument in <paramref name="pageFunction"/>.</param>
        /// <param name="pageFunction">Function to be evaluated in browser context.</param>
        /// <param name="args">Arguments to pass to <c>pageFunction</c>.</param>
        /// <returns>Task.</returns>
        public static async Task EvaluateAsync(this Task<IElementHandle> elementHandleTask, string pageFunction, params object[] args)
        {
            var elementHandle = await elementHandleTask.ConfigureAwait(false);
            if (elementHandle == null)
            {
                throw new SelectorException("Error: failed to find element matching selector");
            }

            await elementHandle.EvaluateAsync(pageFunction, args).ConfigureAwait(false);
        }
    }
}
