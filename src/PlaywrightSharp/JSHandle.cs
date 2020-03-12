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

        internal bool Disposed { get; }

        internal IRemoteObject RemoteObject { get; set; }

        internal ExecutionContext Context { get; set; }

        /// <inheritdoc cref="IJSHandle.DisposeAsync"/>
        public Task DisposeAsync()
        {
            throw new System.NotImplementedException();
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