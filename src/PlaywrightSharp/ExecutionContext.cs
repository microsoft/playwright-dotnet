using System;
using System.Text.Json;

namespace PlaywrightSharp
{
    internal class ExecutionContext
    {
        public ExecutionContext(IExecutionContextDelegate executionContextDelegate)
        {
            Delegate = executionContextDelegate;
        }

        internal IExecutionContextDelegate Delegate { get; }

        internal virtual IJSHandle CreateHandle(IRemoteObject remoteObject) => new JSHandle(this, remoteObject);
    }
}