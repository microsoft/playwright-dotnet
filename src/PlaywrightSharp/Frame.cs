using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IFrame"/>
    public class Frame : IFrame
    {
        private readonly Frame _parentFrame;
        private readonly IDictionary<ContextType, ContextData> _contextData;
        private readonly bool _detached = false;

        internal Frame(Page page, string frameId, Frame parentFrame)
        {
            Page = page;
            Id = frameId;
            _parentFrame = parentFrame;

            _contextData = new Dictionary<ContextType, ContextData>
            {
                [ContextType.Main] = new ContextData(),
                [ContextType.Utility] = new ContextData(),
            };
            SetContext(ContextType.Main, null);
            SetContext(ContextType.Utility, null);

            if (_parentFrame != null)
            {
                _parentFrame.ChildFrames.Add(this);
            }
        }

        /// <inheritdoc cref="IFrame.ChildFrames"/>
        IFrame[] IFrame.ChildFrames => ChildFrames.ToArray();

        /// <inheritdoc cref="IFrame.Name"/>
        public string Name { get; set; }

        /// <inheritdoc cref="IFrame.Url"/>
        public string Url { get; set; }

        /// <inheritdoc cref="IFrame.ParentFrame"/>
        public IFrame ParentFrame => null;

        /// <inheritdoc cref="IFrame.Detached"/>
        public bool Detached { get; set; }

        /// <inheritdoc cref="IFrame.Id"/>
        public string Id { get; set; }

        internal Page Page { get; }

        internal IList<string> FiredLifecycleEvents { get; } = new List<string>();

        internal List<Frame> ChildFrames { get; } = new List<Frame>();

        internal string LastDocumentId { get; set; }

        /// <inheritdoc cref="IFrame.AddScriptTagAsync(AddTagOptions)"/>
        public Task<IElementHandle> AddScriptTagAsync(AddTagOptions options)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IFrame.ClickAsync(string, ClickOptions)"/>
        public async Task ClickAsync(string selector, ClickOptions options = null)
        {
            var handle = await OptionallyWaitForSelectorInUtilityContextAsync(selector, options).ConfigureAwait(false);
            await handle.ClickAsync(options).ConfigureAwait(false);
            await handle.DisposeAsync().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IFrame.EvaluateAsync{T}(string, object[])"/>
        public async Task<T> EvaluateAsync<T>(string script, params object[] args)
        {
            var context = await GeMainContextAsync().ConfigureAwait(false);
            return await context.EvaluateAsync<T>(script, args).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IFrame.EvaluateAsync(string, object[])"/>
        public Task<JsonElement?> EvaluateAsync(string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IFrame.EvaluateHandleAsync(string)"/>
        public Task<IJSHandle> EvaluateHandleAsync(string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IFrame.EvaluateHandleAsync(string)"/>
        public Task<IJSHandle> EvaluateHandleAsync(string expression)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IFrame.FillAsync(string, string, WaitForSelectorOptions)"/>
        public Task FillAsync(string selector, string text, WaitForSelectorOptions options = null)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IFrame.GoToAsync(string, GoToOptions)"/>
        public async Task<IResponse> GoToAsync(string url, GoToOptions options = null)
        {
            Page.PageState.ExtraHTTPHeaders.TryGetValue("referer", out string referer);

            if (options?.Referer != null)
            {
                if (referer != null && referer != options.Referer)
                {
                    throw new ArgumentException("\"referer\" is already specified as extra HTTP header");
                }

                referer = options.Referer;
            }

            using var watcher = new LifecycleWatcher(this, options);

            try
            {
                var navigateTask = Page.Delegate.NavigateFrameAsync(this, url, referer);
                var task = await Task.WhenAny(
                    watcher.TimeoutOrTerminationTask,
                    navigateTask).ConfigureAwait(false);

                await task.ConfigureAwait(false);

                var tasks = new List<Task>() { watcher.TimeoutOrTerminationTask };
                if (!string.IsNullOrEmpty(navigateTask.Result.NewDocumentId))
                {
                    watcher.SetExpectedDocumentId(navigateTask.Result.NewDocumentId, url);
                    tasks.Add(watcher.NewDocumentNavigationTask);
                }
                else if (navigateTask.Result.IsSameDocument)
                {
                    tasks.Add(watcher.SameDocumentNavigationTask);
                }
                else
                {
                    tasks.AddRange(new[] { watcher.SameDocumentNavigationTask, watcher.NewDocumentNavigationTask });
                }

                task = await Task.WhenAny(tasks).ConfigureAwait(false);

                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new NavigationException(ex.Message, ex);
            }

            return watcher.NavigationResponse;
        }

        /// <inheritdoc cref="IFrame.GoToAsync(string, WaitUntilNavigation)"/>
        public Task<IResponse> GoToAsync(string url, WaitUntilNavigation waitUntil)
            => GoToAsync(url, new GoToOptions { WaitUntil = new[] { waitUntil } });

        /// <inheritdoc cref="IJSHandle.GetPropertyAsync"/>
        public Task<IElementHandle> QuerySelectorAsync(string selector)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IFrame.QuerySelectorEvaluateAsync(string, string, object[])"/>
        public Task QuerySelectorEvaluateAsync(string selector, string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IFrame.QuerySelectorEvaluateAsync{T}(string, string, object[])"/>
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, params object[] args)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IFrame.SetContentAsync(string, NavigationOptions)"/>
        public Task SetContentAsync(string html, NavigationOptions options = null)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IFrame.WaitForNavigationAsync(WaitForNavigationOptions)"/>
        public Task<IResponse> WaitForNavigationAsync(WaitForNavigationOptions options = null)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IFrame.WaitForNavigationAsync(WaitUntilNavigation)"/>
        public Task<IResponse> WaitForNavigationAsync(WaitUntilNavigation waitUntil)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IFrame.WaitForSelectorAsync(string, WaitForSelectorOptions)"/>
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null)
        {
            throw new System.NotImplementedException();
        }

        internal Task<IFrameExecutionContext> GetUtilityContextAsync()
        {
            throw new NotImplementedException();
        }

        internal void OnDetached()
        {
        }

        internal void ContextCreated(ContextType contextType, FrameExecutionContext context)
        {
            var data = _contextData[contextType];

            // In case of multiple sessions to the same target, there's a race between
            // connections so we might end up creating multiple isolated worlds.
            // We can use either.
            if (data.Context != null)
            {
                SetContext(contextType, null);
            }

            SetContext(contextType, context);
        }

        internal void ContextDestroyed(FrameExecutionContext context)
        {
            foreach (var contextType in _contextData.Keys)
            {
                var data = _contextData[contextType];
                if (data.Context == context)
                {
                    SetContext(contextType, null);
                }
            }
        }

        private void SetContext(ContextType contextType, IFrameExecutionContext context)
        {
            var data = _contextData[contextType];
            data.Context = context;

            if (context != null)
            {
                data.ContextTsc.TrySetResult(context);

                foreach (var rerunnableTask in data.RerunnableTasks)
                {
                    _ = rerunnableTask.RerunAsync(context);
                }
            }
            else
            {
                data.ContextTsc = new TaskCompletionSource<IFrameExecutionContext>();
            }
        }

        private Task<IFrameExecutionContext> GeMainContextAsync() => GetContextAsync(ContextType.Main);

        private Task<IFrameExecutionContext> GetContextAsync(ContextType contextType)
        {
            if (_detached)
            {
                throw new PlaywrightSharpException($"Execution Context is not available in detached frame \"{Url}\" (are you trying to evaluate ?)");
            }

            return _contextData[contextType].ContextTask;
        }

        private async Task<IElementHandle> OptionallyWaitForSelectorInUtilityContextAsync(string selector, ClickOptions options)
        {
            options ??= new ClickOptions();
            options.Timeout ??= Page.DefaultTimeout;

            IElementHandle handle;

            if (options.WaitFor != WaitForOption.NoWait)
            {
                var maybeHandle = await WaitForSelectorInUtilityContextAsync(selector, options.WaitFor, options.Timeout).ConfigureAwait(false);

                if (maybeHandle == null)
                {
                    throw new PlaywrightSharpException($"No node found for selector: {SelectorToString(selector, options.WaitFor)}");
                }

                handle = maybeHandle;
            }
            else
            {
                var context = await GetContextAsync(ContextType.Utility).ConfigureAwait(false);
                var maybeHandle = await context.QuerySelectorAsync(selector).ConfigureAwait(false);

                if (maybeHandle == null)
                {
                    throw new PlaywrightSharpException($"No node found for selector: {selector}");
                }

                handle = maybeHandle!;
            }

            return handle;
        }

        private object SelectorToString(string selector, WaitForOption waitFor)
        {
            string label = waitFor switch
            {
                WaitForOption.Visible => "[visible] ",
                WaitForOption.Hidden => "[hidden] ",
                _ => string.Empty,
            };
            return $"{label}{selector}";
        }

        private async Task<ElementHandle> WaitForSelectorInUtilityContextAsync(string selector, WaitForOption waitFor, int? timeout)
        {
            var task = Dom.GetWaitForSelectorFunction(selector, waitFor, timeout);
            var result = await ScheduleRerunnableTaskAsync(task, ContextType.Utility, timeout, $"selector \"{SelectorToString(selector, waitFor)}\"").ConfigureAwait(false);

            if (!(result is ElementHandle))
            {
                await result.DisposeAsync().ConfigureAwait(false);
                return null;
            }

            return result as ElementHandle;
        }

        private Task<IJSHandle> ScheduleRerunnableTaskAsync(Func<IFrameExecutionContext, Task<IJSHandle>> task, ContextType contextType, int? timeout, string title)
        {
            var data = _contextData[contextType];
            var rerunnableTask = new RerunnableTask(data, task, timeout, title);
            data.RerunnableTasks.Add(rerunnableTask);
            if (data.Context != null)
            {
                _ = rerunnableTask.RerunAsync(data.Context);
            }

            return rerunnableTask.Task;
        }
    }
}
