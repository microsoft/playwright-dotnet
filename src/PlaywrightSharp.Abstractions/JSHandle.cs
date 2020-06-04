using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

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

        internal IRemoteObject RemoteObject { get; }

        /// <inheritdoc cref="IJSHandle.DisposeAsync"/>
        public Task DisposeAsync()
        {
            if (Disposed)
            {
                return Task.CompletedTask;
            }

            Disposed = true;
            return Context.Delegate.ReleaseHandleAsync(this);
        }

        /// <inheritdoc cref="IJSHandle.EvaluateAsync{T}(string, object[])"/>
        public Task<T> EvaluateAsync<T>(string pageFunction, params object[] args)
            => Context.EvaluateAsync<T>(pageFunction, args.Prepend(this));

        /// <inheritdoc cref="IJSHandle.EvaluateAsync(string, object[])"/>
        public Task<JsonElement?> EvaluateAsync(string pageFunction, params object[] args)
            => EvaluateAsync<JsonElement?>(pageFunction, args);

        /// <inheritdoc cref="IJSHandle.GetJsonValueAsync{T}"/>
        public Task<T> GetJsonValueAsync<T>() => Context.Delegate.HandleJSONValueAsync<T>(this);

        /// <inheritdoc cref="IJSHandle.GetPropertiesAsync"/>
        public Task<IDictionary<string, IJSHandle>> GetPropertiesAsync() => Context.Delegate.GetPropertiesAsync(this);

        /// <inheritdoc cref="IJSHandle.GetPropertyAsync(string)"/>
        public async Task<IJSHandle> GetPropertyAsync(string propertyName)
        {
            var objectHandle = await EvaluateHandleAsync(
                @"(object, propertyName) => {
                    const result = { __proto__: null };
                    result[propertyName] = object[propertyName];
                    return result;
                }",
                propertyName).ConfigureAwait(false);

            var properties = await objectHandle.GetPropertiesAsync().ConfigureAwait(false);
            properties.TryGetValue(propertyName, out var result);
            await objectHandle.DisposeAsync().ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc cref="object.ToString()"/>
        public override string ToString() => Context.Delegate.HandleToString(this, true /* includeType */);

        private Task<JSHandle> EvaluateHandleAsync(string pageFunction, params object[] args)
            => Context.EvaluateHandleAsync(pageFunction, args.Prepend(this));
    }
}
