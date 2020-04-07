using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Accessibility;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IPage"/>
    public sealed class Page : IPage, IDisposable
    {
        private readonly TaskCompletionSource<bool> _closeTsc = new TaskCompletionSource<bool>();
        private readonly Dictionary<string, Delegate> _pageBindings = new Dictionary<string, Delegate>();
        private bool _disconnected;

        /// <inheritdoc cref="IPage"/>
        internal Page(IPageDelegate pageDelegate, IBrowserContext browserContext)
        {
            FrameManager = new FrameManager(this);
            Delegate = pageDelegate;
            BrowserContext = browserContext;
            Keyboard = new Keyboard(Delegate.RawKeyboard);
            Mouse = new Mouse(Delegate.RawMouse, Keyboard);

            PageState = new PageState { Viewport = browserContext.Options.Viewport };

            Screenshotter = new Screenshotter(this);
        }

        /// <inheritdoc cref="IPage.Console"/>
        public event EventHandler<ConsoleEventArgs> Console;

        /// <inheritdoc cref="IPage.Popup"/>
        public event EventHandler<PopupEventArgs> Popup;

        /// <inheritdoc cref="IPage.Request"/>
        public event EventHandler<RequestEventArgs> Request;

        /// <inheritdoc cref="IPage.RequestFinished"/>
        public event EventHandler<RequestEventArgs> RequestFinished;

        /// <inheritdoc cref="IPage.RequestFailed"/>
        public event EventHandler<RequestEventArgs> RequestFailed;

        /// <inheritdoc cref="IPage.Dialog"/>
        public event EventHandler<DialogEventArgs> Dialog;

        /// <inheritdoc cref="IPage.FrameAttached"/>
        public event EventHandler<FrameEventArgs> FrameAttached;

        /// <inheritdoc cref="IPage.FrameDetached"/>
        public event EventHandler<FrameEventArgs> FrameDetached;

        /// <inheritdoc cref="IPage.FrameNavigated"/>
        public event EventHandler<FrameEventArgs> FrameNavigated;

        /// <inheritdoc cref="IPage.Load"/>
        public event EventHandler Load;

        /// <inheritdoc cref="IPage.DOMContentLoaded"/>
        public event EventHandler DOMContentLoaded;

        /// <inheritdoc cref="IPage.Response"/>
        public event EventHandler<ResponseEventArgs> Response;

        /// <inheritdoc cref="IPage.FileChooser"/>
        public event EventHandler<FileChooserEventArgs> FileChooser;

        /// <inheritdoc cref="IPage.Close"/>
        public event EventHandler Close;

        /// <inheritdoc cref="IPage.Error"/>
        public event EventHandler<ErrorEventArgs> Error;

        /// <inheritdoc cref="IPage.PageError"/>
        public event EventHandler<PageErrorEventArgs> PageError;

        /// <inheritdoc cref="IPage.WorkerCreated"/>
        public event EventHandler<WorkerEventArgs> WorkerCreated;

        /// <inheritdoc cref="IPage.WorkerDestroyed"/>
        public event EventHandler<WorkerEventArgs> WorkerDestroyed;

        /// <inheritdoc cref="IPage.Websocket"/>
        public event EventHandler<WebsocketEventArgs> Websocket;

        /// <inheritdoc cref="IPage.MainFrame"/>
        public IFrame MainFrame => FrameManager.MainFrame;

        /// <inheritdoc cref="IPage.BrowserContext"/>
        public IBrowserContext BrowserContext { get; internal set; }

        /// <inheritdoc cref="IPage.Viewport"/>
        public Viewport Viewport => PageState.Viewport;

        /// <inheritdoc cref="IPage.Accessibility"/>
        public IAccessibility Accessibility => null;

        /// <inheritdoc cref="IPage.Mouse"/>
        public IMouse Mouse { get; }

        /// <inheritdoc cref="IPage.Url"/>
        public string Url => MainFrame.Url;

        /// <inheritdoc cref="IPage.Frames"/>
        public IFrame[] Frames => FrameManager.GetFrames();

        /// <inheritdoc cref="IPage.Keyboard"/>
        public IKeyboard Keyboard { get; }

        /// <inheritdoc cref="IPage.DefaultTimeout"/>
        public int DefaultTimeout { get; set; }

        /// <inheritdoc cref="IPage.DefaultNavigationTimeout"/>
        public int DefaultNavigationTimeout { get; set; }

        /// <inheritdoc cref="IPage.IsClosed"/>
        public bool IsClosed { get; private set; }

        /// <inheritdoc cref="IPage.Workers"/>
        public IWorker[] Workers => null;

        /// <inheritdoc cref="IPage.Coverage"/>
        public ICoverage Coverage => null;

        internal FrameManager FrameManager { get; }

        internal bool HasPopupEventListeners => Popup?.GetInvocationList().Length > 0;

        internal PageState PageState { get; }

        internal IPageDelegate Delegate { get; }

        internal Screenshotter Screenshotter { get; }

        /// <inheritdoc cref="IPage.AddScriptTagAsync(AddTagOptions)"/>
        public Task<IElementHandle> AddScriptTagAsync(AddTagOptions options) => MainFrame.AddScriptTagAsync(options);

        /// <inheritdoc cref="IPage.AuthenticateAsync(Credentials)"/>
        public Task AuthenticateAsync(Credentials credentials)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.ClickAsync(string, ClickOptions)"/>
        public Task ClickAsync(string selector, ClickOptions options = null) => MainFrame.ClickAsync(selector, options);

        /// <inheritdoc cref="IPage.DoubleClickAsync(string, ClickOptions)"/>
        public Task DoubleClickAsync(string selector, ClickOptions options = null) => MainFrame.DoubleClickAsync(selector, options);

        /// <inheritdoc cref="IPage.TripleClickAsync(string, ClickOptions)"/>
        public Task TripleClickAsync(string selector, ClickOptions options = null) => MainFrame.TripleClickAsync(selector, options);

        /// <inheritdoc cref="IPage.CloseAsync(PageCloseOptions)"/>
        public async Task CloseAsync(PageCloseOptions options = null)
        {
            if (IsClosed)
            {
                return;
            }

            if (_disconnected)
            {
                throw new TargetClosedException("Protocol error: Connection closed. Most likely the page has been closed.");
            }

            bool runBeforeUnload = options?.RunBeforeUnload ?? false;
            await Delegate.ClosePageAsync(runBeforeUnload).ConfigureAwait(false);
            if (!runBeforeUnload)
            {
                await _closeTsc.Task.ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="IPage.EmulateMediaAsync(EmulateMedia)"/>
        public Task EmulateMediaAsync(EmulateMedia options)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.EvaluateAsync{T}(string, object[])"/>
        public Task<T> EvaluateAsync<T>(string script, params object[] args) => MainFrame.EvaluateAsync<T>(script, args);

        /// <inheritdoc cref="IPage.EvaluateAsync(string, object[])"/>
        public Task<JsonElement?> EvaluateAsync(string script, params object[] args) => MainFrame.EvaluateAsync<JsonElement?>(script, args);

        /// <inheritdoc cref="IPage.EvaluateHandleAsync(string, object[])"/>
        public Task<IJSHandle> EvaluateHandleAsync(string pageFunction, params object[] args) => MainFrame.EvaluateHandleAsync(pageFunction, args);

        /// <inheritdoc cref="IPage.EvaluateOnNewDocumentAsync(string, object[])"/>
        public Task EvaluateOnNewDocumentAsync(string pageFunction, params object[] args)
        {
            string source = GetEvaluationString(pageFunction, args);
            return Delegate.EvaluateOnNewDocumentAsync(source);
        }

        /// <inheritdoc cref="IPage.ExposeFunctionAsync(string, Action)"/>
        public Task ExposeFunctionAsync(string name, Action playwrightFunction)
            => ExposeFunctionAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc cref="IPage.ExposeFunctionAsync{TResult}(string, Func{TResult})"/>
        public Task ExposeFunctionAsync<TResult>(string name, Func<TResult> playwrightFunction)
            => ExposeFunctionAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc cref="IPage.ExposeFunctionAsync{T, TResult}(string, Func{T, TResult})"/>
        public Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> playwrightFunction)
            => ExposeFunctionAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc cref="IPage.ExposeFunctionAsync{T1, T2, TResult}(string, Func{T1, T2, TResult})"/>
        public Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> playwrightFunction)
            => ExposeFunctionAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc cref="IPage.ExposeFunctionAsync{T1, T2, T3, TResult}(string, Func{T1, T2, T3, TResult})"/>
        public Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> playwrightFunction)
            => ExposeFunctionAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc cref="IPage.ExposeFunctionAsync{T1, T2, T3, T4, TResult}(string, Func{T1, T2, T3, T4, TResult})"/>
        public Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> playwrightFunction)
            => ExposeFunctionAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc cref="IPage.FocusAsync(string)"/>
        public Task FocusAsync(string selector)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.GoBackAsync(NavigationOptions)"/>
        public Task<IResponse> GoBackAsync(NavigationOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.GoForwardAsync(NavigationOptions)"/>
        public Task<IResponse> GoForwardAsync(NavigationOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.GoToAsync(string, GoToOptions)"/>
        public Task<IResponse> GoToAsync(string url, GoToOptions options = null) => MainFrame.GoToAsync(url, options);

        /// <inheritdoc cref="IPage.GoToAsync(string, int, WaitUntilNavigation[])"/>
        public Task<IResponse> GoToAsync(string url, int timeout, params WaitUntilNavigation[] waitUntil)
             => MainFrame.GoToAsync(url, new GoToOptions { Timeout = timeout, WaitUntil = waitUntil });

        /// <inheritdoc cref="IPage.GoToAsync(string, WaitUntilNavigation[])"/>
        public Task<IResponse> GoToAsync(string url, params WaitUntilNavigation[] waitUntil)
             => MainFrame.GoToAsync(url, new GoToOptions { WaitUntil = waitUntil });

        /// <inheritdoc cref="IPage.HoverAsync(string)"/>
        public Task HoverAsync(string selector)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.QuerySelectorAsync(string)"/>
        public Task<IElementHandle> QuerySelectorAsync(string selector) => MainFrame.QuerySelectorAsync(selector);

        /// <inheritdoc cref="IPage.QuerySelectorEvaluateAsync(string, string, object[])"/>
        public Task QuerySelectorEvaluateAsync(string selector, string script, params object[] args) => MainFrame.QuerySelectorEvaluateAsync(selector, script, args);

        /// <inheritdoc cref="IPage.QuerySelectorEvaluateAsync{T}(string, string, object[])"/>
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, params object[] args) => MainFrame.QuerySelectorEvaluateAsync<T>(selector, script, args);

        /// <inheritdoc cref="IPage.ReloadAsync(NavigationOptions)"/>
        public async Task<IResponse> ReloadAsync(NavigationOptions options = null)
        {
            var waitTask = WaitForNavigationAsync(new WaitForNavigationOptions(options));
            await Delegate.ReloadAsync().ConfigureAwait(false);
            return await waitTask.ConfigureAwait(false);
        }

        /// <inheritdoc cref="IPage.ScreenshotAsync(ScreenshotOptions)"/>
        public Task<byte[]> ScreenshotAsync(ScreenshotOptions options = null)
            => Screenshotter.ScreenshotPageAsync(options);

        /// <inheritdoc cref="IPage.SetCacheEnabledAsync(bool)"/>
        public Task SetCacheEnabledAsync(bool enabled = true)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.SetContentAsync(string, NavigationOptions)"/>
        public Task SetContentAsync(string html, NavigationOptions options = null) => MainFrame.SetContentAsync(html, options);

        /// <inheritdoc cref="IPage.SetContentAsync(string, WaitUntilNavigation)"/>
        public Task SetContentAsync(string html, WaitUntilNavigation waitUntil)
            => SetContentAsync(html, new NavigationOptions { WaitUntil = new[] { waitUntil } });

        /// <inheritdoc cref="IPage.SetExtraHttpHeadersAsync(IDictionary{string, string})"/>
        public Task SetExtraHttpHeadersAsync(IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            PageState.ExtraHTTPHeaders.Clear();
            foreach (var header in headers)
            {
                PageState.ExtraHTTPHeaders[header.Key.ToLower()] = header.Value;
            }

            return Delegate.SetExtraHttpHeadersAsync(headers);
        }

        /// <inheritdoc cref="IPage.SetOfflineModeAsync(bool)"/>
        public Task SetOfflineModeAsync(bool value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.SetRequestInterceptionAsync(bool)"/>
        public Task SetRequestInterceptionAsync(bool value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.SetViewportAsync(PlaywrightSharp.Viewport)"/>
        public async Task SetViewportAsync(Viewport viewport)
        {
            if (viewport == null)
            {
                throw new ArgumentNullException(nameof(viewport));
            }

            bool oldIsMobile = PageState.Viewport?.IsMobile ?? false;
            bool newIsMobile = viewport.IsMobile;
            PageState.Viewport = viewport.Clone();

            await Delegate.SetViewportAsync(viewport).ConfigureAwait(false);

            if (oldIsMobile != newIsMobile)
            {
                await ReloadAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="IPage.TypeAsync(string, string, TypeOptions)"/>
        public Task TypeAsync(string selector, string text, TypeOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.GetTitleAsync"/>
        public Task<string> GetTitleAsync() => MainFrame.GetTitleAsync();

        /// <inheritdoc cref="IPage.GetOpenerAsync"/>
        public Task<IPage> GetOpenerAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.WaitForEvent{T}(PageEvent, WaitForEventOptions{T})"/>
        public Task<T> WaitForEvent<T>(PageEvent e, WaitForEventOptions<T> options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.WaitForLoadStateAsync(NavigationOptions)"/>
        public Task WaitForLoadStateAsync(NavigationOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.WaitForNavigationAsync(WaitForNavigationOptions)"/>
        public Task<IResponse> WaitForNavigationAsync(WaitForNavigationOptions options = null) => MainFrame.WaitForNavigationAsync(options);

        /// <inheritdoc cref="IPage.WaitForNavigationAsync(WaitUntilNavigation)"/>
        public Task<IResponse> WaitForNavigationAsync(WaitUntilNavigation waitUntil)
            => MainFrame.WaitForNavigationAsync(waitUntil);

        /// <inheritdoc cref="IPage.WaitForRequestAsync(Regex, WaitForOptions)"/>
        public Task<IRequest> WaitForRequestAsync(Regex regex, WaitForOptions options = null)
        {
            int timeout = options?.Timeout ?? DefaultTimeout;
            var tsc = new TaskCompletionSource<IRequest>();
            void RequestEventHandler(object sender, RequestEventArgs e)
            {
                if (regex.IsMatch(e.Request.Url))
                {
                    tsc.TrySetResult(e.Request);
                    Request -= RequestEventHandler;
                }
            }

            Request += RequestEventHandler;
            return tsc.Task.WithTimeout(timeout);
        }

        /// <inheritdoc cref="IPage.WaitForRequestAsync(string, WaitForOptions)"/>
        public Task<IRequest> WaitForRequestAsync(string url, WaitForOptions options = null)
        {
            int timeout = options?.Timeout ?? DefaultTimeout;
            var tsc = new TaskCompletionSource<IRequest>();
            void RequestEventHandler(object sender, RequestEventArgs e)
            {
                if (url.Equals(e.Request.Url))
                {
                    tsc.TrySetResult(e.Request);
                    Request -= RequestEventHandler;
                }
            }

            Request += RequestEventHandler;
            return tsc.Task.WithTimeout(timeout);
        }

        /// <inheritdoc cref="IPage.WaitForResponseAsync(string, WaitForOptions)"/>
        public Task<IResponse> WaitForResponseAsync(string url, WaitForOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.GetPdfAsync(string)"/>
        public Task GetPdfAsync(string file) => throw new NotImplementedException();

        /// <inheritdoc cref="IPage.WaitForFunctionAsync(string, WaitForFunctionOptions, object[])"/>
        public Task<IJSHandle> WaitForFunctionAsync(string pageFunction, WaitForFunctionOptions options = null, params object[] args)
            => MainFrame.WaitForFunctionAsync(pageFunction, options, args);

        /// <inheritdoc cref="IPage.WaitForFunctionAsync(string, object[])"/>
        public Task<IJSHandle> WaitForFunctionAsync(string pageFunction, params object[] args) => WaitForFunctionAsync(pageFunction, null, args);

        /// <inheritdoc cref="IPage.QuerySelectorAllEvaluateAsync(string, string, object[])"/>
        public Task QuerySelectorAllEvaluateAsync(string selector, string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.QuerySelectorAllEvaluateAsync{T}(string, string, object[])"/>
        public Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.FillAsync(string, string, WaitForSelectorOptions)"/>
        public Task FillAsync(string selector, string text, WaitForSelectorOptions options = null) => MainFrame.FillAsync(selector, text, options);

        /// <inheritdoc cref="IPage.SelectAsync(string, string[])"/>
        public Task<string[]> SelectAsync(string selector, params string[] values)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.SelectAsync(string, SelectOption[])"/>
        public Task<string[]> SelectAsync(string selector, params SelectOption[] values)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.SelectAsync(string, IElementHandle[])"/>
        public Task<string[]> SelectAsync(string selector, params IElementHandle[] values)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.GetContentAsync"/>
        public Task<string> GetContentAsync() => MainFrame.GetContentAsync();

        /// <inheritdoc cref="IPage.QuerySelectorAllAsync(string)"/>
        public Task<IElementHandle[]> QuerySelectorAllAsync(string selector)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.AddStyleTagAsync(AddTagOptions)"/>
        public Task<IElementHandle> AddStyleTagAsync(AddTagOptions options)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.WaitForEvent(PageEvent)"/>
        public Task WaitForEvent(PageEvent e)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.WaitForTimeoutAsync(int)"/>
        public Task WaitForTimeoutAsync(int timeout) => Task.Delay(timeout);

        /// <inheritdoc cref="IPage.WaitForSelectorAsync(string, WaitForSelectorOptions)"/>
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null)
            => MainFrame.WaitForSelectorAsync(selector, options);

        /// <inheritdoc cref="IPage.WaitForSelectorEvaluateAsync(string, string, WaitForSelectorOptions, object[])"/>
        public Task<IElementHandle> WaitForSelectorEvaluateAsync(string selector, string script, WaitForSelectorOptions options = null, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.ScreenshotBase64Async(ScreenshotOptions)"/>
        public Task<string> ScreenshotBase64Async(ScreenshotOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose() => Screenshotter?.Dispose();

        internal static string GetEvaluationString(string fun, params object[] args)
        {
            return $"({fun})({string.Join(",", args.Select(SerializeArgument))})";

            string SerializeArgument(object arg)
            {
                return arg == null
                    ? "undefined"
                    : JsonSerializer.Serialize(arg, JsonHelper.DefaultJsonSerializerOptions);
            }
        }

        internal void RemoveWorker(string workerId)
        {
            throw new NotImplementedException();
        }

        internal void AddWorker(string workerId, Worker worker)
        {
            throw new NotImplementedException();
        }

        internal void AddConsoleMessage(ConsoleType type, IJSHandle[] args, ConsoleMessageLocation location, string text = null)
        {
            var message = new ConsoleMessage(type, text, args, location);
            bool intercepted = FrameManager.InterceptConsoleMessage(message);
            if (intercepted || Console?.GetInvocationList()?.Length == 0)
            {
                foreach (var arg in args)
                {
                    arg.DisposeAsync();
                }
            }
            else
            {
                Console?.Invoke(this, new ConsoleEventArgs(message));
            }
        }

        internal void OnPopup(object parent) => Popup?.Invoke(parent, new PopupEventArgs(this));

        internal void OnRequest(IRequest request) => Request?.Invoke(this, new RequestEventArgs(request));

        internal void OnRequestFinished(IRequest request) => RequestFinished?.Invoke(this, new RequestEventArgs(request));

        internal void DidDisconnected() => _disconnected = true;

        internal void DidClose()
        {
            if (IsClosed)
            {
                throw new PlaywrightSharpException("Page closed twice");
            }

            IsClosed = true;
            Close?.Invoke(this, EventArgs.Empty);
            _closeTsc.TrySetResult(true);
        }

        internal void OnDOMContentLoaded() => DOMContentLoaded?.Invoke(this, new EventArgs());

        internal void OnFrameAttached(IFrame frame) => FrameAttached?.Invoke(this, new FrameEventArgs(frame));

        internal void OnFrameDetached(IFrame frame) => FrameDetached?.Invoke(this, new FrameEventArgs(frame));

        internal void OnFrameNavigated(Frame frame) => FrameNavigated?.Invoke(this, new FrameEventArgs(frame));

        internal void OnLoad() => Load?.Invoke(this, new EventArgs());

        internal void OnPageError(PageErrorEventArgs args) => PageError?.Invoke(this, args);

        internal async Task OnBindingCalledAsync(string payload, IFrameExecutionContext context)
        {
            var bindingPayload = JsonSerializer.Deserialize<BindingPayload>(payload, JsonHelper.DefaultJsonSerializerOptions);
            const string taskResultPropertyName = "Result";
            const string deliverResult = @"function deliverResult(name, seq, result) {
                window[name]['callbacks'].get(seq).resolve(result);
                window[name]['callbacks'].delete(seq);
            }";

            const string deliverError = @"function deliverError(name, seq, message, stack) {
                const error = new Error(message);
                error.stack = stack;
                window[name]['callbacks'].get(seq).reject(error);
                window[name]['callbacks'].delete(seq);
            }";

            string expression;

            try
            {
                var binding = _pageBindings[bindingPayload.Name];
                var methodParams = binding.Method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
                object[] args = bindingPayload.Args.Select((arg, i) => JsonSerializer.Deserialize(arg.GetRawText(), methodParams[0], JsonHelper.DefaultJsonSerializerOptions)).ToArray();

                object result = binding.DynamicInvoke(args);
                if (result is Task taskResult)
                {
                    await taskResult.ConfigureAwait(false);

                    if (taskResult.GetType().IsGenericType)
                    {
                        // the task is already awaited and therefore the call to property Result will not deadlock
                        result = taskResult.GetType().GetProperty(taskResultPropertyName).GetValue(taskResult);
                    }
                }

                expression = GetEvaluationString(deliverResult, bindingPayload.Name, bindingPayload.Seq, result);
            }
            catch (Exception ex)
            {
                expression = GetEvaluationString(deliverError, bindingPayload.Name, bindingPayload.Seq, ex.Message, ex.StackTrace);
            }

            try
            {
                await context.EvaluateAsync(expression).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private async Task ExposeFunctionAsync(string name, Delegate puppeteerFunction)
        {
            if (_pageBindings.ContainsKey(name))
            {
                throw new PlaywrightSharpException($"Failed to add page binding with name {name}: window['{name}'] already exists!");
            }

            _pageBindings.Add(name, puppeteerFunction);

            const string addPageBinding = @"function addPageBinding(bindingName) {
                const binding = window[bindingName];
                window[bindingName] = (...args) => {
                    const me = window[bindingName];
                    let callbacks = me['callbacks'];
                    if (!callbacks) {
                        callbacks = new Map();
                        me['callbacks'] = callbacks;
                    }
                    const seq = (me['lastSeq'] || 0) + 1;
                    me['lastSeq'] = seq;
                    const promise = new Promise((resolve, reject) => callbacks.set(seq, { resolve, reject }));
                    binding(JSON.stringify({ name: bindingName, seq, args }));
                    return promise;
                };
            }";

            await Delegate.ExposeBindingAsync(name, GetEvaluationString(addPageBinding, name)).ConfigureAwait(false);
        }

        private class BindingPayload
        {
            public string Name { get; set; }

            public JsonElement[] Args { get; set; }

            public int Seq { get; set; }
        }
    }
}
