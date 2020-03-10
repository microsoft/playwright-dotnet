using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IJSHandle"/>
    public class JSHandle : IJSHandle
    {
        internal bool Disposed { get; }

        internal FrameExecutionContext Context { get; set; }

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