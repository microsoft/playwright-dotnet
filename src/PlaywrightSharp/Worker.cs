using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IWorker"/>
    public class Worker : IWorker
    {
        /// <inheritdoc cref="IWorker.Url"/>
        public string Url => null;

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
    }
}
