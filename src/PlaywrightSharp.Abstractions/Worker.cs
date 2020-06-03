using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IWorker"/>
    public class Worker : IWorker
    {
        private readonly TaskCompletionSource<ExecutionContext> _executionContextTcs = new TaskCompletionSource<ExecutionContext>();
        private ExecutionContext _existingExecutionContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="Worker"/> class.
        /// </summary>
        /// <param name="url">The urls of thw worker.</param>
        public Worker(string url) => Url = url;

        /// <inheritdoc cref="IWorker.Url"/>
        public string Url { get; }

        /// <inheritdoc cref="IWorker.EvaluateAsync{T}(string, object[])"/>
        public async Task<T> EvaluateAsync<T>(string script, params object[] args)
            => await (await GetExistingExecutionContextAsync().ConfigureAwait(false)).EvaluateAsync<T>(script, args).ConfigureAwait(false);

        /// <inheritdoc cref="IWorker.EvaluateAsync(string, object[])"/>
        public async Task<JsonElement?> EvaluateAsync(string script, params object[] args)
            => await (await GetExistingExecutionContextAsync().ConfigureAwait(false)).EvaluateAsync<JsonElement?>(script, args).ConfigureAwait(false);

        internal Task<ExecutionContext> GetExistingExecutionContextAsync() => _executionContextTcs.Task;

        internal void CreateExecutionContext(IExecutionContextDelegate executionContextDelegate)
        {
            _existingExecutionContext = new ExecutionContext(executionContextDelegate);
            _executionContextTcs.TrySetResult(_existingExecutionContext);
        }
    }
}
