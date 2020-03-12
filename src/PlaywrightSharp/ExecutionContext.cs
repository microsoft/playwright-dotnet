using System;
using System.Text.Json;

namespace PlaywrightSharp
{
    internal class ExecutionContext
    {
        internal virtual IJSHandle CreateHandle(IRemoteObject remoteObject) => new JSHandle(this, remoteObject);
    }
}