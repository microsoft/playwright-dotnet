using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class ExecutionContext
    {
        internal ExecutionContext(IExecutionContextDelegate executionContextDelegate) => Delegate = executionContextDelegate;

        internal IExecutionContextDelegate Delegate { get; }

        public virtual IJSHandle CreateHandle(IRemoteObject remoteObject) => new JSHandle(this, remoteObject);

        public virtual Task<T> EvaluateAsync<T>(string pageFunction, params object[] args)
            => EvaluateCoreAsync<T>(true, pageFunction, args);

        public virtual Task<JSHandle> EvaluateHandleAsync(string pageFunction, params object[] args)
            => EvaluateCoreAsync<JSHandle>(false, pageFunction, args);

        private Task<T> EvaluateCoreAsync<T>(bool returnByValue, string pageFunction, params object[] args)
            => Delegate.EvaluateAsync<T>(this, returnByValue, pageFunction, args);
    }
}
