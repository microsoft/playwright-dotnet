using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class FrameExecutionContext : IFrameExecutionContext
    {
        private readonly IExecutionContextDelegate _delegate;

        internal FrameExecutionContext(IExecutionContextDelegate executionContextDelegate, Frame frame)
        {
            _delegate = executionContextDelegate;
            Frame = frame;
        }

        public Frame Frame { get; set; }

        public Task<T> EvaluateAsync<T>(string script, params object[] args) => EvaluateAsync<T>(true, script, args);

        public async Task<T> EvaluateAsync<T>(bool returnByValue, string script, params object[] args)
        {
            Func<object, bool> needsAdoption = (object value) => value is ElementHandle elementHandle && elementHandle.Context != this;

            if (!args.Any(needsAdoption))
            {
                return await _delegate.EvaluateAsync<T>(this, returnByValue, script, args).ConfigureAwait(false);
            }

            List<Task<ElementHandle>> toDispose = new List<Task<ElementHandle>>();

            var adoptedTasks = args.Select<object, Task<object>>(arg =>
            {
                if (!needsAdoption(arg))
                {
                    return Task.FromResult(arg);
                }

                var adopted = Frame.Page.Delegate.AdoptElementHandleAsync(arg, this);
                toDispose.Add(adopted);
                return adopted.ContinueWith(t => (object)t.Result, TaskScheduler.Default);
            });

            await Task.WhenAll(adoptedTasks).ConfigureAwait(false);

            T result;
            try
            {
                result = await _delegate.EvaluateAsync<T>(this, returnByValue, script, adoptedTasks.Select(t => t.Result).ToArray()).ConfigureAwait(false);
            }
            finally
            {
                await Task.WhenAll(toDispose
                    .Select(handlePromise => handlePromise.ContinueWith(handleTask => handleTask.Result.DisposeAsync(), TaskScheduler.Default)))
                    .ConfigureAwait(false);
            }

            return result;
        }

        public Task<IJSHandle> EvaluateHandleAsync(string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        internal JsonElement CreateHandle(JsonElement? remoteObject)
        {
            throw new NotImplementedException();
        }
    }
}