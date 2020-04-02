using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IJSHandle"/>
    public class JSHandle : IJSHandle
    {
        internal JSHandle(ExecutionContext context, IRemoteObject remoteObject)
        {
            RemoteObject = remoteObject;
            Context = context;
        }

        internal ExecutionContext Context { get; }

        internal bool Disposed { get; private set; }

        internal IRemoteObject RemoteObject { get; set; }

        /// <inheritdoc cref="IJSHandle.EvaluateAsync{T}(string, object[])"/>
        public Task<T> EvaluateAsync<T>(string pageFunction, params object[] args)
        {
            object[] newArgs = new object[args.Length + 1];
            newArgs[0] = this;
            args.CopyTo(newArgs, 1);
            return Context.EvaluateAsync<T>(pageFunction, newArgs);
        }

        /// <inheritdoc cref="IJSHandle.EvaluateAsync(string, object[])"/>
        public Task<JsonElement?> EvaluateAsync(string pageFunction, params object[] args)
            => EvaluateAsync<JsonElement?>(pageFunction, args);

        /// <inheritdoc cref="IJSHandle.DisposeAsync"/>
        Task IJSHandle.DisposeAsync()
        {
            if (Disposed)
            {
                return Task.CompletedTask;
            }

            Disposed = true;
            return Context.Delegate.ReleaseHandleAsync(this);
        }

        /// <inheritdoc cref="IJSHandle.GetJsonValueAsync{T}"/>
        public Task<T> GetJsonValueAsync<T>() => Context.Delegate.HandleJSONValueAsync<T>(this);

        /// <inheritdoc cref="IJSHandle.GetPropertiesAsync"/>
        public Task<IReadOnlyDictionary<string, IJSHandle>> GetPropertiesAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IJSHandle.GetPropertyAsync(string)"/>
        public Task<IJSHandle> GetPropertyAsync(string propertyName)
        {
            throw new System.NotImplementedException();
        }
    }
}
