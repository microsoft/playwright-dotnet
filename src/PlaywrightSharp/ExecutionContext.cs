using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class ExecutionContext
    {
        protected ExecutionContext(IExecutionContextDelegate executionContextDelegate)
        {
            Delegate = executionContextDelegate;
        }

        internal IExecutionContextDelegate Delegate { get; }

        public virtual Task<T> EvaluateAsync<T>(bool returnByValue, string script, params object[] args)
            => Delegate.EvaluateAsync<T>(this, returnByValue, script, args);

        public Task<T> EvaluateAsync<T>(string script, params object[] args) => EvaluateAsync<T>(true, script, args);

        public Task<JsonElement?> EvaluateAsync(string script, params object[] args) => EvaluateAsync<JsonElement?>(true, script, args);

        public Task<IJSHandle> EvaluateHandleAsync(string script, params object[] args) => EvaluateAsync<IJSHandle>(false, script, args);

        internal virtual IJSHandle CreateHandle(IRemoteObject remoteObject) => new JSHandle(this, remoteObject);
    }
}
