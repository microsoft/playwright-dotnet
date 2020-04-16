using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class FrameExecutionContext : ExecutionContext, IFrameExecutionContext
    {
        private int _injectedGeneration = -1;
        private Task<JSHandle> _injectedTask;

        internal FrameExecutionContext(IExecutionContextDelegate executionContextDelegate, Frame frame) : base(executionContextDelegate)
        {
            Frame = frame;
        }

        public Frame Frame { get; }

        public override Task<T> EvaluateAsync<T>(string script, params object[] args) => EvaluateAsync<T>(true, script, args);

        public async Task<T> EvaluateAsync<T>(bool returnByValue, string script, params object[] args)
        {
            bool NeedsAdoption(object value) => value is ElementHandle elementHandle && elementHandle.Context != this;

            if (args == null || !args.Any(NeedsAdoption))
            {
                return await Delegate.EvaluateAsync<T>(this, returnByValue, script, args).ConfigureAwait(false);
            }

            List<Task<ElementHandle>> toDispose = new List<Task<ElementHandle>>();

            var adoptedTasks = args.Select<object, Task<object>>(async (arg) =>
            {
                if (!NeedsAdoption(arg))
                {
                    return Task.FromResult(arg);
                }

                var adopted = Frame.Page.Delegate.AdoptElementHandleAsync(arg as ElementHandle, this);
                toDispose.Add(adopted);
                await adopted.ConfigureAwait(false);
                return adopted.Result;
            }).ToArray();

            await Task.WhenAll(adoptedTasks).ConfigureAwait(false);

            T result;
            try
            {
                result = await Delegate.EvaluateAsync<T>(this, returnByValue, script, adoptedTasks.Select(t => t.Result).ToArray()).ConfigureAwait(false);
            }
            finally
            {
                await Task.WhenAll(toDispose
                    .Select(handlePromise => handlePromise.ContinueWith(handleTask => handleTask.Result.DisposeAsync(), TaskScheduler.Default)))
                    .ConfigureAwait(false);
            }

            return result;
        }

        public Task EvaluateAsync(string script, params object[] args) => EvaluateAsync<JsonElement?>(true, script, args);

        public async Task<IElementHandle> QuerySelectorAsync(string selector, IElementHandle scope = null)
        {
            var handle = await EvaluateHandleAsync(
                "(injected, selector, scope) => injected.querySelector(selector, scope || document)",
                await GetInjectedAsync().ConfigureAwait(false),
                Dom.NormalizeSelector(selector),
                scope).ConfigureAwait(false);

            if (!(handle is ElementHandle))
            {
                await handle.DisposeAsync().ConfigureAwait(false);
            }

            return handle as ElementHandle;
        }

        public async Task<JSHandle> GetInjectedAsync()
        {
            var selectors = Selectors.Instance.Value;
            if (_injectedTask != null && selectors.Generation != _injectedGeneration)
            {
                _ = _injectedTask.ContinueWith(handleTask => handleTask.Result.DisposeAsync(), TaskScheduler.Default);
                _injectedTask = null;
            }

            if (_injectedTask == null)
            {
                string source = $@"
                    new ({await GetInjectedSource().ConfigureAwait(false)})([
                      {string.Join(",\n", selectors.Sources)},
                    ])
                  ";
                _injectedTask = EvaluateHandleAsync(source);
                _injectedGeneration = selectors.Generation;
            }

            return await _injectedTask.ConfigureAwait(false);
        }

        public override IJSHandle CreateHandle(IRemoteObject remoteObject)
        {
            if (Frame.Page.Delegate.IsElementHandle(remoteObject))
            {
                return new ElementHandle(this, remoteObject);
            }

            return base.CreateHandle(remoteObject);
        }

        public async Task<IElementHandle[]> QuerySelectorAllAsync(string selector, ElementHandle scope = null)
        {
            var arrayHandle = await QuerySelectorArrayAsync(selector, scope).ConfigureAwait(false);
            var properties = await arrayHandle.GetPropertiesAsync().ConfigureAwait(false);
            await arrayHandle.DisposeAsync().ConfigureAwait(false);
            List<IElementHandle> result = new List<IElementHandle>();
            List<Task> disposeTasks = new List<Task>();

            foreach (var property in properties.Values)
            {
                if (property is ElementHandle handle)
                {
                    result.Add(handle);
                }
                else
                {
                    disposeTasks.Add(property.DisposeAsync());
                }
            }

            await Task.WhenAll(disposeTasks).ConfigureAwait(false);
            return result.ToArray();
        }

        internal async Task<JSHandle> QuerySelectorArrayAsync(string selector, ElementHandle scope = null)
            => await EvaluateHandleAsync(
                "(injected, selector, scope) => injected.querySelectorAll(selector, scope || document)",
                await GetInjectedAsync().ConfigureAwait(false),
                Dom.NormalizeSelector(selector),
                scope).ConfigureAwait(false);

        private async Task<string> GetInjectedSource()
        {
            using var stream = typeof(FrameExecutionContext).Assembly.GetManifestResourceStream("PlaywrightSharp.Resources.injectedSource.ts");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }
    }
}
