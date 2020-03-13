using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IJSHandle"/>
    public class JSHandle : IJSHandle
    {
        private bool _disposed;

        internal JSHandle(ExecutionContext context, IRemoteObject remoteObject)
        {
            RemoteObject = remoteObject;
            Context = context;
        }

        internal ExecutionContext Context { get; set; }

        internal bool Disposed { get; }

        internal IRemoteObject RemoteObject { get; set; }

        /// <inheritdoc cref="IJSHandle.DisposeAsync"/>
        public Task DisposeAsync()
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            _disposed = true;
            return Context.Delegate.ReleaseHandleAsync(this);
        }

        /// <inheritdoc cref="IJSHandle.GetJsonValueAsync{T}"/>
        public Task<T> GetJsonValueAsync<T>()
        {
            throw new System.NotImplementedException();
        }

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