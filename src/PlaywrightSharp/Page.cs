using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IPage"/>
    public sealed class Page : IPage, IDisposable
    {
        private static readonly Dictionary<PageEvent, EventInfo> _pageEventsMap = ((PageEvent[])Enum.GetValues(typeof(PageEvent)))
            .ToDictionary(x => x, x => typeof(Page).GetEvent(x.ToString()));

        private readonly TaskCompletionSource<bool> _closeTsc = new TaskCompletionSource<bool>();
        private readonly Dictionary<string, Delegate> _pageBindings = new Dictionary<string, Delegate>();
        private readonly ConcurrentDictionary<string, Worker> _workers = new ConcurrentDictionary<string, Worker>();
        private readonly TimeoutSettings _timeoutSettings = new TimeoutSettings();
        private readonly object _fileChooserEventLock = new object();

        private bool _disconnected;
        private EventHandler<FileChooserEventArgs> _fileChooserEventHandler;

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
            Accessibility = new Accessibility(Delegate.GetAccessibilityTreeAsync);
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
        public event EventHandler<FileChooserEventArgs> FileChooser
        {
            add
            {
                lock (_fileChooserEventLock)
                {
                    _fileChooserEventHandler += value;
                    _ = Delegate.SetFileChooserInterceptedAsync(true);
                }
            }

            remove
            {
                lock (_fileChooserEventLock)
                {
                    _fileChooserEventHandler -= value;
                    _ = Delegate.SetFileChooserInterceptedAsync(false);
                }
            }
        }

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

        internal event EventHandler ClientDisconnected;

        /// <inheritdoc cref="IPage.MainFrame"/>
        IFrame IPage.MainFrame => MainFrame;

        /// <inheritdoc cref="IPage.BrowserContext"/>
        public IBrowserContext BrowserContext { get; }

        /// <inheritdoc cref="IPage.Viewport"/>
        public Viewport Viewport => PageState.Viewport;

        /// <inheritdoc cref="IPage.Accessibility"/>
        public IAccessibility Accessibility { get; }

        /// <inheritdoc cref="IPage.Mouse"/>
        public IMouse Mouse { get; }

        /// <inheritdoc cref="IPage.Url"/>
        public string Url => MainFrame.Url;

        /// <inheritdoc cref="IPage.Frames"/>
        public IFrame[] Frames => FrameManager.GetFrames();

        /// <inheritdoc cref="IPage.Keyboard"/>
        public IKeyboard Keyboard { get; }

        /// <inheritdoc cref="IPage.DefaultTimeout"/>
        public int DefaultTimeout
        {
            get => _timeoutSettings.Timeout;
            set => _timeoutSettings.SetDefaultTimeout(value);
        }

        /// <inheritdoc cref="IPage.DefaultNavigationTimeout"/>
        public int DefaultNavigationTimeout
        {
            get => _timeoutSettings.NavigationTimeout;
            set => _timeoutSettings.SetDefaultNavigationTimeout(value);
        }

        /// <inheritdoc cref="IPage.IsClosed"/>
        public bool IsClosed { get; private set; }

        /// <inheritdoc cref="IPage.Workers"/>
        public IWorker[] Workers => _workers.Values.ToArray();

        /// <inheritdoc cref="IPage.Coverage"/>
        /// <summary>
        /// Browser-specific Coverage implementation, only available for Chromium atm.
        /// </summary>
        public ICoverage Coverage => Delegate.Coverage;

        internal Frame MainFrame => FrameManager.MainFrame;

        internal FrameManager FrameManager { get; }

        internal bool HasPopupEventListeners => Popup?.GetInvocationList().Length > 0;

        internal PageState PageState { get; }

        internal IPageDelegate Delegate { get; }

        internal Screenshotter Screenshotter { get; }

        internal ITarget Target { get; set; }

        /// <inheritdoc cref="IPage.AddScriptTagAsync(AddTagOptions)"/>
        public Task<IElementHandle> AddScriptTagAsync(AddTagOptions options) => MainFrame.AddScriptTagAsync(options);

        /// <inheritdoc cref="IPage.AuthenticateAsync(Credentials)"/>
        public Task AuthenticateAsync(Credentials credentials)
        {
            PageState.Credentials = credentials;
            return Delegate.AuthenticateAsync(credentials);
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
            options ??= new EmulateMedia();
            if (options.Media != null)
            {
                PageState.MediaType = options.Media.Value;
            }

            if (options.ColorScheme != null)
            {
                PageState.ColorScheme = options.ColorScheme.Value;
            }

            return Delegate.SetEmulateMediaAsync(PageState.MediaType, PageState.ColorScheme);
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

        /// <inheritdoc cref="IPage.FocusAsync(string, WaitForSelectorOptions)"/>
        public Task FocusAsync(string selector, WaitForSelectorOptions options = null)
            => MainFrame.FocusAsync(selector, options);

        /// <inheritdoc cref="IPage.GoBackAsync(NavigationOptions)"/>
        public async Task<IResponse> GoBackAsync(NavigationOptions options = null)
        {
            using var cts = new CancellationTokenSource();

            var waitTask = WaitForNavigationAsync(new WaitForNavigationOptions(options), cts.Token);
            bool result = await Delegate.GoBackAsync().ConfigureAwait(false);

            if (!result)
            {
                cts.Cancel();
                return null;
            }

            return await waitTask.ConfigureAwait(false);
        }

        /// <inheritdoc cref="IPage.GoForwardAsync(NavigationOptions)"/>
        public async Task<IResponse> GoForwardAsync(NavigationOptions options = null)
        {
            var waitTask = WaitForNavigationAsync(new WaitForNavigationOptions(options));
            bool result = await Delegate.GoForwardAsync().ConfigureAwait(false);
            if (!result)
            {
                _ = waitTask.ContinueWith(_ => { }, TaskScheduler.Default);
                return null;
            }

            return await waitTask.ConfigureAwait(false);
        }

        /// <inheritdoc cref="IPage.GoToAsync(string, GoToOptions)"/>
        public Task<IResponse> GoToAsync(string url, GoToOptions options = null) => MainFrame.GoToAsync(url, options);

        /// <inheritdoc cref="IPage.GoToAsync(string, int, WaitUntilNavigation[])"/>
        public Task<IResponse> GoToAsync(string url, int timeout, params WaitUntilNavigation[] waitUntil)
             => MainFrame.GoToAsync(url, new GoToOptions { Timeout = timeout, WaitUntil = waitUntil });

        /// <inheritdoc cref="IPage.GoToAsync(string, WaitUntilNavigation[])"/>
        public Task<IResponse> GoToAsync(string url, params WaitUntilNavigation[] waitUntil)
             => MainFrame.GoToAsync(url, new GoToOptions { WaitUntil = waitUntil });

        /// <inheritdoc cref="IPage.HoverAsync(string, WaitForSelectorOptions)"/>
        public Task HoverAsync(string selector, WaitForSelectorOptions options = null)
            => MainFrame.HoverAsync(selector, options);

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
            if (PageState.CacheEnabled == enabled)
            {
                return Task.CompletedTask;
            }

            PageState.CacheEnabled = enabled;
            return Delegate.SetCacheEnabledAsync(enabled);
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
        public Task SetOfflineModeAsync(bool enabled)
        {
            if (PageState.OfflineMode == enabled)
            {
                return Task.CompletedTask;
            }

            PageState.OfflineMode = enabled;
            return Delegate.SetOfflineModeAsync(enabled);
        }

        /// <inheritdoc cref="IPage.SetRequestInterceptionAsync(bool)"/>
        public async Task SetRequestInterceptionAsync(bool enabled)
        {
            if (PageState.InterceptNetwork == enabled)
            {
                return;
            }

            PageState.InterceptNetwork = enabled;
            await Delegate.SetRequestInterceptionAsync(enabled).ConfigureAwait(false);
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
            => MainFrame.TypeAsync(selector, text, options);

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
            var info = _pageEventsMap[e];
            ValidateArgumentsTypes();
            var eventTsc = new TaskCompletionSource<T>();
            void PageEventHandler(object sender, T e)
            {
                if (options?.Predicate == null || options.Predicate(e))
                {
                    eventTsc.TrySetResult(e);
                    info.RemoveEventHandler(this, (EventHandler<T>)PageEventHandler);
                }
            }

            info.AddEventHandler(this, (EventHandler<T>)PageEventHandler);
            return eventTsc.Task.WithTimeout(options?.Timeout ?? DefaultTimeout);

            void ValidateArgumentsTypes()
            {
                if ((info.EventHandlerType.GenericTypeArguments.Length == 0 && typeof(T) == typeof(EventArgs))
                    || info.EventHandlerType.GenericTypeArguments[0] == typeof(T))
                {
                    return;
                }

                throw new ArgumentOutOfRangeException(nameof(e), $"{e} - {typeof(T).FullName}");
            }
        }

        /// <inheritdoc cref="IPage.WaitForLoadStateAsync(NavigationOptions)"/>
        public Task WaitForLoadStateAsync(NavigationOptions options = null) => MainFrame.WaitForLoadStateAsync(options);

        /// <inheritdoc cref="IPage.WaitForNavigationAsync(WaitForNavigationOptions, CancellationToken)"/>
        public Task<IResponse> WaitForNavigationAsync(WaitForNavigationOptions options = null, CancellationToken token = default)
            => MainFrame.WaitForNavigationAsync(options, token);

        /// <inheritdoc cref="IPage.WaitForNavigationAsync(WaitUntilNavigation)"/>
        public Task<IResponse> WaitForNavigationAsync(WaitUntilNavigation waitUntil)
            => MainFrame.WaitForNavigationAsync(waitUntil);

        /// <inheritdoc cref="IPage.WaitForRequestAsync(Regex, WaitForOptions)"/>
        public async Task<IRequest> WaitForRequestAsync(Regex regex, WaitForOptions options = null)
        {
            var result = await WaitForEvent(PageEvent.Request, new WaitForEventOptions<RequestEventArgs>
            {
                Predicate = e => regex.IsMatch(e.Request.Url),
                Timeout = options?.Timeout,
            }).ConfigureAwait(false);
            return result.Request;
        }

        /// <inheritdoc cref="IPage.WaitForRequestAsync(string, WaitForOptions)"/>
        public async Task<IRequest> WaitForRequestAsync(string url, WaitForOptions options = null)
        {
            var result = await WaitForEvent(PageEvent.Request, new WaitForEventOptions<RequestEventArgs>
            {
                Predicate = e => e.Request.Url.Equals(url),
                Timeout = options?.Timeout,
            }).ConfigureAwait(false);
            return result.Request;
        }

        /// <inheritdoc cref="IPage.WaitForResponseAsync(string, WaitForOptions)"/>
        public async Task<IResponse> WaitForResponseAsync(string url, WaitForOptions options = null)
        {
            var result = await WaitForEvent(PageEvent.Response, new WaitForEventOptions<ResponseEventArgs>
            {
                Predicate = e => e.Response.Url.Equals(url),
                Timeout = options?.Timeout,
            }).ConfigureAwait(false);
            return result.Response;
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
            => MainFrame.QuerySelectorAllEvaluateAsync(selector, script, args);

        /// <inheritdoc cref="IPage.QuerySelectorAllEvaluateAsync{T}(string, string, object[])"/>
        public Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script, params object[] args)
            => MainFrame.QuerySelectorAllEvaluateAsync<T>(selector, script, args);

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
            => MainFrame.QuerySelectorAllAsync(selector);

        /// <inheritdoc cref="IPage.AddStyleTagAsync(AddTagOptions)"/>
        public Task<IElementHandle> AddStyleTagAsync(AddTagOptions options) => MainFrame.AddStyleTagAsync(options);

        /// <inheritdoc cref="IPage.WaitForTimeoutAsync(int)"/>
        public Task WaitForTimeoutAsync(int timeout) => Task.Delay(timeout);

        /// <inheritdoc cref="IPage.WaitForSelectorAsync(string, WaitForSelectorOptions)"/>
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null)
            => MainFrame.WaitForSelectorAsync(selector, options);

        /// <inheritdoc cref="IPage.WaitForSelectorEvaluateAsync(string, string, WaitForFunctionOptions, object[])"/>
        public Task<IJSHandle> WaitForSelectorEvaluateAsync(string selector, string script, WaitForFunctionOptions options = null, params object[] args)
            => MainFrame.WaitForSelectorEvaluateAsync(selector, script, options, args);

        /// <inheritdoc cref="IPage.ScreenshotBase64Async(ScreenshotOptions)"/>
        public async Task<string> ScreenshotBase64Async(ScreenshotOptions options = null)
            => Convert.ToBase64String(await ScreenshotAsync(options).ConfigureAwait(false));

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose() => Screenshotter?.Dispose();

        internal static string GetEvaluationString(string fun, params object[] args)
        {
            return !fun.IsJavascriptFunction() ? fun : $"({fun})({string.Join(",", args.Select(SerializeArgument))})";

            string SerializeArgument(object arg)
            {
                return arg == null
                    ? "undefined"
                    : JsonSerializer.Serialize(arg, JsonHelper.DefaultJsonSerializerOptions);
            }
        }

        internal void RemoveWorker(string workerId)
        {
            if (_workers.TryRemove(workerId, out var worker))
            {
                WorkerDestroyed?.Invoke(this, new WorkerEventArgs(worker));
            }
        }

        internal void AddWorker(string workerId, Worker worker)
        {
            _workers[workerId] = worker;
            WorkerCreated?.Invoke(this, new WorkerEventArgs(worker));
        }

        internal void ClearWorkers()
        {
            foreach (var kv in _workers)
            {
                WorkerDestroyed?.Invoke(this, new WorkerEventArgs(kv.Value));
                _workers.TryRemove(kv.Key, out _);
            }
        }

        internal void AddConsoleMessage(ConsoleType type, IJSHandle[] args, ConsoleMessageLocation location, string text = null)
        {
            var message = new ConsoleMessage(
                type,
                text,
                args,
                (handle, includeType) => ((JSHandle)handle).Context.Delegate.HandleToString(handle, includeType),
                location);
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

        internal void OnResponse(IResponse response) => Response?.Invoke(this, new ResponseEventArgs(response));

        internal void DidDisconnected()
        {
            _disconnected = true;
            ClientDisconnected?.Invoke(this, new EventArgs());
        }

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

        internal void OnRequestFailed(Request request) => RequestFailed?.Invoke(this, new RequestEventArgs(request));

        internal void OnLoad() => Load?.Invoke(this, new EventArgs());

        internal void OnPageError(PageErrorEventArgs args) => PageError?.Invoke(this, args);

        internal void OnDialog(Dialog dialog) => Dialog?.Invoke(this, new DialogEventArgs(dialog));

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
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        internal async Task OnFileChooserOpenedAsync(ElementHandle handle)
        {
            bool multiple = await handle.EvaluateAsync<bool>("element => !!element.multiple").ConfigureAwait(false);

            if (_fileChooserEventHandler?.GetInvocationList().Length == 0)
            {
                await handle.DisposeAsync().ConfigureAwait(false);
                return;
            }

            _fileChooserEventHandler?.Invoke(this, new FileChooserEventArgs(handle, multiple));
        }

        private async Task ExposeFunctionAsync(string name, Delegate playwrightFunction)
        {
            if (_pageBindings.ContainsKey(name))
            {
                throw new PlaywrightSharpException($"Failed to add page binding with name {name}: window['{name}'] already exists!");
            }

            _pageBindings.Add(name, playwrightFunction);

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
