using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class ExecutionContext
    {
        protected ExecutionContext(IExecutionContextDelegate executionContextDelegate) => Delegate = executionContextDelegate;

        internal IExecutionContextDelegate Delegate { get; }

        public virtual IJSHandle CreateHandle(IRemoteObject remoteObject) => new JSHandle(this, remoteObject);

        public virtual Task<T> EvaluateAsync<T>(string pageFunction, params object[] args)
            => EvaluateCoreAsync<T>(false, pageFunction, args);

        public virtual Task<JSHandle> EvaluateHandleAsync<T>(string pageFunction, params object[] args)
            => EvaluateCoreAsync<JSHandle>(true, pageFunction, args);

        private Task<T> EvaluateCoreAsync<T>(bool returnByValue, string pageFunction, params object[] args)
            => Delegate.EvaluateAsync<T>(this, returnByValue, pageFunction, args);
    }
}
