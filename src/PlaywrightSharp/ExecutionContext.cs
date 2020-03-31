namespace PlaywrightSharp
{
    internal class ExecutionContext
    {
        protected ExecutionContext(IExecutionContextDelegate executionContextDelegate)
        {
            Delegate = executionContextDelegate;
        }

        internal IExecutionContextDelegate Delegate { get; }

        internal virtual IJSHandle CreateHandle(IRemoteObject remoteObject) => new JSHandle(this, remoteObject);
    }
}
