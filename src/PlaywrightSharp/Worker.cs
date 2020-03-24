using System;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IWorker"/>
    public class Worker : IWorker
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Worker"/> class.
        /// </summary>
        /// <param name="url">The urls of thw worker.</param>
        public Worker(string url) => Url = url;

        /// <inheritdoc cref="IWorker.Url"/>
        public string Url { get; }

        internal ExecutionContext ExistingExecutionContext { get; private set; }

        /// <inheritdoc cref="IWorker.EvaluateAsync{T}(string, object[])"/>
        public Task<T> EvaluateAsync<T>(string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IWorker.EvaluateAsync(string, object[])"/>
        public Task EvaluateAsync(string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        internal void CreateExecutionContext(IExecutionContextDelegate executionContextDelegate)
        {
            throw new NotImplementedException();
        }
    }
}
