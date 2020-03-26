using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class FrameExecutionContext : ExecutionContext, IFrameExecutionContext
    {
        private int _injectedGeneration = -1;
        private Task<IJSHandle> _injectedTask;

        internal FrameExecutionContext(IExecutionContextDelegate executionContextDelegate, Frame frame) : base(executionContextDelegate)
        {
            Frame = frame;
        }

        public Frame Frame { get; set; }

        public Task<T> EvaluateAsync<T>(string script, params object[] args) => EvaluateAsync<T>(true, script, args);

        public async Task<T> EvaluateAsync<T>(bool returnByValue, string script, params object[] args)
        {
            Func<object, bool> needsAdoption = (object value) => value is ElementHandle elementHandle && elementHandle.Context != this;

            if (!args.Any(needsAdoption))
            {
                return await Delegate.EvaluateAsync<T>(this, returnByValue, script, args).ConfigureAwait(false);
            }

            List<Task<ElementHandle>> toDispose = new List<Task<ElementHandle>>();

            var adoptedTasks = args.Select<object, Task<object>>(arg =>
            {
                if (!needsAdoption(arg))
                {
                    return Task.FromResult(arg);
                }

                var adopted = Frame.Page.Delegate.AdoptElementHandleAsync(arg as ElementHandle, this);
                toDispose.Add(adopted);
                return adopted.ContinueWith(t => (object)t.Result, TaskScheduler.Default);
            });

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

        public Task<IJSHandle> EvaluateHandleAsync(string script, params object[] args) => EvaluateAsync<IJSHandle>(false, script, args);

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

        public async Task<IJSHandle> GetInjectedAsync()
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
                      {string.Join(",\n", await selectors.GetSourcesAsync().ConfigureAwait(false))},
                    ])
                  ";
                _injectedTask = EvaluateHandleAsync(source);
                _injectedGeneration = selectors.Generation;
            }

            return await _injectedTask.ConfigureAwait(false);
        }

        internal override IJSHandle CreateHandle(IRemoteObject remoteObject)
        {
            if (Frame.Page.Delegate.IsElementHandle(remoteObject))
            {
                return new ElementHandle(this, remoteObject);
            }

            return base.CreateHandle(remoteObject);
        }

        private async Task<string> GetInjectedSource()
        {
            using var stream = typeof(FrameExecutionContext).Assembly.GetManifestResourceStream("PlaywrightSharp.Resources.injectedSource.ts");
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}
