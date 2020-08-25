using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IPage" />
    public class Page : IChannelOwner<Page>, IPage
    {
        private static readonly Dictionary<PageEvent, EventInfo> _pageEventsMap = ((PageEvent[])Enum.GetValues(typeof(PageEvent)))
            .ToDictionary(x => x, x => typeof(Page).GetEvent(x.ToString()));

        private readonly ConnectionScope _scope;
        private readonly PageChannel _channel;
        private readonly List<Frame> _frames = new List<Frame>();
        private readonly List<(PageEvent pageEvent, TaskCompletionSource<bool> waitTcs)> _waitForCancellationTcs = new List<(PageEvent pageEvent, TaskCompletionSource<bool> waitTcs)>();
        private readonly TimeoutSettings _timeoutSettings = new TimeoutSettings();
        private readonly object _fileChooserEventLock = new object();

        private List<RouteSetting> _routes = new List<RouteSetting>();
        private EventHandler<FileChooserEventArgs> _fileChooserEventHandler;
        private bool _fileChooserIntercepted;

        internal Page(ConnectionScope scope, string guid, PageInitializer initializer)
        {
            _scope = scope;
            _channel = new PageChannel(guid, scope, this);

            MainFrame = initializer.MainFrame.Object;
            MainFrame.Page = this;
            _frames.Add(MainFrame);
            Viewport = initializer.ViewportSize;
            Accessibility = new Accesibility(_channel);
            Keyboard = new Keyboard(_channel);
            Mouse = new Mouse(_channel);
            _channel.Closed += Channel_Closed;
            _channel.Crashed += Channel_Crashed;
            _channel.Popup += (sender, e) => Popup?.Invoke(this, new PopupEventArgs(e.Page));
            _channel.RequestFailed += (sender, e) =>
            {
                e.Request.Object.Failure = e.FailureText;

                RequestFailed?.Invoke(this, new RequestFailedEventArgs
                {
                    Request = e.Request.Object,
                    FailureText = e.FailureText,
                });
            };

            _channel.Request += (sender, e) => Request?.Invoke(this, e);
            _channel.RequestFinished += (sender, e) => RequestFinished?.Invoke(this, e);
            _channel.Response += (sender, e) => Response?.Invoke(this, e);
            _channel.BindingCall += Channel_BindingCall;
            _channel.Route += Channel_Route;
            _channel.FrameNavigated += Channel_FrameNavigated;
            _channel.FrameAttached += Channel_FrameAttached;
            _channel.FrameDetached += Channel_FrameDetached;
            _channel.Dialog += (sender, e) => Dialog?.Invoke(this, e);
            _channel.Console += (sender, e) => Console?.Invoke(this, e);
            _channel.DOMContentLoaded += (sender, e) => DOMContentLoaded?.Invoke(this, e);
            _channel.Download += (sender, e) => Download?.Invoke(this, e);
            _channel.PageError += (sender, e) => PageError?.Invoke(this, e);
            _channel.Load += (sender, e) => Load?.Invoke(this, e);
            _channel.FileChooser += (sender, e) =>
            {
                _fileChooserEventHandler?.Invoke(this, new FileChooserEventArgs(e.Element.Object, e.IsMultiple));
            };
            _channel.Worker += (sender, e) =>
            {
                WorkersList.Add(e.WorkerChannel.Object);
                e.WorkerChannel.Object.Page = this;
                Worker?.Invoke(this, new WorkerEventArgs(e.WorkerChannel.Object));
            };
        }

        /// <inheritdoc />
        public event EventHandler<ConsoleEventArgs> Console;

        /// <inheritdoc />
        public event EventHandler<PopupEventArgs> Popup;

        /// <inheritdoc />
        public event EventHandler<RequestEventArgs> Request;

        /// <inheritdoc />
        public event EventHandler<ResponseEventArgs> Response;

        /// <inheritdoc />
        public event EventHandler<RequestEventArgs> RequestFinished;

        /// <inheritdoc />
        public event EventHandler<RequestFailedEventArgs> RequestFailed;

        /// <inheritdoc />
        public event EventHandler<DialogEventArgs> Dialog;

        /// <inheritdoc />
        public event EventHandler<FrameEventArgs> FrameAttached;

        /// <inheritdoc />
        public event EventHandler<FrameEventArgs> FrameDetached;

        /// <inheritdoc />
        public event EventHandler<FrameEventArgs> FrameNavigated;

        /// <inheritdoc />
        public event EventHandler<FileChooserEventArgs> FileChooser
        {
            add
            {
                lock (_fileChooserEventLock)
                {
                    _fileChooserEventHandler += value;
                    _fileChooserIntercepted = true;
                    _ = _channel.SetFileChooserInterceptedNoReplyAsync(true);
                }
            }

            remove
            {
                lock (_fileChooserEventLock)
                {
                    _fileChooserEventHandler -= value;

                    if (_fileChooserIntercepted)
                    {
                        _fileChooserIntercepted = false;
                        _ = _channel.SetFileChooserInterceptedNoReplyAsync(false);
                    }
                }
            }
        }

        /// <inheritdoc />
        public event EventHandler<EventArgs> Load;

        /// <inheritdoc />
        public event EventHandler<EventArgs> DOMContentLoaded;

        /// <inheritdoc />
        public event EventHandler<EventArgs> Closed;

        /// <inheritdoc />
        public event EventHandler<EventArgs> Crashed;

        /// <inheritdoc />
        public event EventHandler<PageErrorEventArgs> PageError;

        /// <inheritdoc />
        public event EventHandler<WorkerEventArgs> Worker;

        /// <inheritdoc />
        public event EventHandler<WorkerEventArgs> WorkerDestroyed;

        /// <inheritdoc />
        public event EventHandler<WebsocketEventArgs> Websocket;

        /// <inheritdoc />
        public event EventHandler<DownloadEventArgs> Download;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Page> IChannelOwner<Page>.Channel => _channel;

        /// <inheritdoc />
        public bool IsClosed { get; private set; }

        /// <inheritdoc />
        IFrame IPage.MainFrame => MainFrame;

        /// <inheritdoc cref="IPage.MainFrame" />
        public Frame MainFrame { get; }

        /// <inheritdoc />
        IBrowserContext IPage.Context => BrowserContext;

        /// <inheritdoc cref="IPage.Context" />
        public BrowserContext BrowserContext { get; internal set; }

        /// <inheritdoc />
        public ViewportSize Viewport { get; private set; }

        /// <inheritdoc />
        public IAccessibility Accessibility { get; }

        /// <inheritdoc />
        public IMouse Mouse { get; }

        /// <inheritdoc />
        public string Url => MainFrame.Url;

        /// <inheritdoc />
        public IFrame[] Frames => _frames.ToArray();

        /// <inheritdoc />
        public IKeyboard Keyboard { get; }

        /// <inheritdoc/>
        public int DefaultTimeout
        {
            get
            {
                return _timeoutSettings.Timeout;
            }

            set
            {
                _timeoutSettings.SetDefaultTimeout(value);
                _ = _channel.SetDefaultTimeoutNoReplyAsync(value);
            }
        }

        /// <inheritdoc/>
        public int DefaultNavigationTimeout
        {
            get
            {
                return _timeoutSettings.NavigationTimeout;
            }

            set
            {
                _timeoutSettings.SetDefaultNavigationTimeout(value);
                _ = _channel.SetDefaultNavigationTimeoutNoReplyAsync(value);
            }
        }

        /// <inheritdoc />
        public IWorker[] Workers { get; }

        /// <inheritdoc />
        public ICoverage Coverage { get; }

        internal BrowserContext OwnedContext { get; set; }

        internal Dictionary<string, Delegate> Bindings { get; } = new Dictionary<string, Delegate>();

        internal List<Worker> WorkersList { get; } = new List<Worker>();

        /// <inheritdoc />
        public Task<string> GetTitleAsync() => MainFrame.GetTitleAsync();

        /// <inheritdoc />
        public async Task<IPage> GetOpenerAsync() => (await _channel.GetOpenerAsync().ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public Task SetCacheEnabledAsync(bool enabled = true) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task EmulateMediaAsync()
            => _channel.EmulateMediaAsync(new Dictionary<string, object>());

        /// <inheritdoc />
        public Task EmulateMediaAsync(MediaType? media)
            => _channel.EmulateMediaAsync(new Dictionary<string, object> { ["media"] = media });

        /// <inheritdoc />
        public Task EmulateMediaAsync(ColorScheme? colorScheme)
            => _channel.EmulateMediaAsync(new Dictionary<string, object> { ["colorScheme"] = colorScheme });

        /// <inheritdoc />
        public Task EmulateMediaAsync(MediaType? media, ColorScheme? colorScheme)
            => _channel.EmulateMediaAsync(new Dictionary<string, object>
            {
                ["media"] = media,
                ["colorScheme"] = colorScheme,
            });

        /// <inheritdoc />
        public Task<IResponse> GoToAsync(string url, LifecycleEvent? waitUntil = null, string referer = null, int? timeout = null)
            => MainFrame.GoToAsync(true, url, waitUntil, referer, timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(LifecycleEvent? waitUntil = null, string url = null, int? timeout = null) => MainFrame.WaitForNavigationAsync(true, waitUntil, url, timeout);

        /// <inheritdoc />
        public async Task<IRequest> WaitForRequestAsync(string url, WaitForOptions options = null)
        {
            var result = await WaitForEvent<RequestEventArgs>(
                PageEvent.Request,
                e => e.Request.Url.Equals(url),
                options?.Timeout).ConfigureAwait(false);
            return result.Request;
        }

        /// <inheritdoc />
        public async Task<IRequest> WaitForRequestAsync(Regex regex, WaitForOptions options = null)
        {
            var result = await WaitForEvent<RequestEventArgs>(
                PageEvent.Request,
                e => regex.IsMatch(e.Request.Url),
                options?.Timeout).ConfigureAwait(false);
            return result.Request;
        }

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(
            string pageFunction,
            int? timeout = null,
            WaitForFunctionPollingOption? polling = null,
            int? pollingInterval = null)
            => MainFrame.WaitForFunctionAsync(true, pageFunction, timeout, polling, pollingInterval);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(
            string pageFunction,
            object args,
            int? timeout = null,
            WaitForFunctionPollingOption? polling = null,
            int? pollingInterval = null)
            => MainFrame.WaitForFunctionAsync(true, pageFunction, args, timeout, polling, pollingInterval);

        /// <inheritdoc />
        public async Task<T> WaitForEvent<T>(PageEvent e, Func<T, bool> predicate = null, int? timeout = null)
        {
            var info = _pageEventsMap[e];
            ValidateArgumentsTypes();
            var eventTsc = new TaskCompletionSource<T>();
            void PageEventHandler(object sender, T e)
            {
                if (predicate == null || predicate(e))
                {
                    info.RemoveEventHandler(this, (EventHandler<T>)PageEventHandler);
                    eventTsc.TrySetResult(e);
                }
            }

            info.AddEventHandler(this, (EventHandler<T>)PageEventHandler);
            var disconnectedTcs = new TaskCompletionSource<bool>();
            _waitForCancellationTcs.Add((e, disconnectedTcs));
            await Task.WhenAny(eventTsc.Task, disconnectedTcs.Task).WithTimeout(timeout ?? DefaultTimeout).ConfigureAwait(false);
            if (disconnectedTcs.Task.IsCompleted)
            {
                await disconnectedTcs.Task.ConfigureAwait(false);
            }

            return await eventTsc.Task.ConfigureAwait(false);

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

        /// <inheritdoc />
        public async Task CloseAsync(PageCloseOptions options = null)
        {
            await _channel.CloseAsync(options).ConfigureAwait(false);
            if (OwnedContext != null)
            {
                await OwnedContext.CloseAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string script) => MainFrame.EvaluateAsync<T>(true, script);

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string script, object args) => MainFrame.EvaluateAsync<T>(true, script, args);

        /// <inheritdoc />
        public Task QuerySelectorEvaluateAsync(string selector, string script) => MainFrame.QuerySelectorEvaluateAsync(true, selector, script);

        /// <inheritdoc />
        public Task QuerySelectorEvaluateAsync(string selector, string script, object args) => MainFrame.QuerySelectorEvaluateAsync(true, selector, script, args);

        /// <inheritdoc />
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script) => MainFrame.QuerySelectorEvaluateAsync<T>(true, selector, script);

        /// <inheritdoc />
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, object args) => MainFrame.QuerySelectorEvaluateAsync<T>(true, selector, script, args);

        /// <inheritdoc />
        public Task QuerySelectorAllEvaluateAsync(string selector, string script, object args) => MainFrame.QuerySelectorAllEvaluateAsync(true, selector, script, args);

        /// <inheritdoc />
        public Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script, object args) => MainFrame.QuerySelectorAllEvaluateAsync<T>(true, selector, script, args);

        /// <inheritdoc />
        public Task QuerySelectorAllEvaluateAsync(string selector, string script) => MainFrame.QuerySelectorAllEvaluateAsync(true, selector, script);

        /// <inheritdoc />
        public Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script) => MainFrame.QuerySelectorAllEvaluateAsync<T>(true, selector, script);

        /// <inheritdoc />
        public Task FillAsync(string selector, string text, NavigatingActionWaitOptions options = null) => MainFrame.FillAsync(true, selector, text, options);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, string file) => SetInputFilesAsync(selector, new[] { file });

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, string[] files) => MainFrame.SetInputFilesAsync(true, selector, files);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, FilePayload file) => SetInputFilesAsync(selector, new[] { file });

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, FilePayload[] files) => MainFrame.SetInputFilesAsync(true, selector, files);

        /// <inheritdoc />
        public Task TypeAsync(string selector, string text, int delay = 0) => MainFrame.TypeAsync(true, selector, text, delay);

        /// <inheritdoc />
        public Task FocusAsync(string selector, int? timeout = null) => MainFrame.FocusAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task HoverAsync(
            string selector,
            Point? position = null,
            Modifier[] modifiers = null,
            bool force = false,
            int? timeout = null) => MainFrame.HoverAsync(true, selector, position, modifiers, force, timeout);

        /// <inheritdoc />
        public Task PressAsync(string selector, string text, int delay = 0, bool? noWaitAfter = null, int? timeout = null) => MainFrame.PressAsync(true, selector, text, delay, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, bool? noWaitAfter = null, int? timeout = null) => MainFrame.SelectOptionAsync(true, selector, null, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, string value, bool? noWaitAfter = null, int? timeout = null) => MainFrame.SelectOptionAsync(true, selector, value, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, SelectOption value, bool? noWaitAfter = null, int? timeout = null) => MainFrame.SelectOptionAsync(true, selector, value, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, IElementHandle value, bool? noWaitAfter = null, int? timeout = null) => MainFrame.SelectOptionAsync(true, selector, value, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, string[] values, bool? noWaitAfter = null, int? timeout = null) => MainFrame.SelectOptionAsync(true, selector, values, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, SelectOption[] values, bool? noWaitAfter = null, int? timeout = null) => MainFrame.SelectOptionAsync(true, selector, values, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, IElementHandle[] values, bool? noWaitAfter = null, int? timeout = null) => MainFrame.SelectOptionAsync(true, selector, values.Cast<ElementHandle>(), noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, params string[] values) => SelectOptionAsync(selector, values, null, null);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, params SelectOption[] values) => SelectOptionAsync(selector, values, null, null);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, params IElementHandle[] values) => SelectOptionAsync(selector, values, null, null);

        /// <inheritdoc />
        public Task WaitForTimeoutAsync(int timeout) => Task.Delay(timeout);

        /// <inheritdoc />
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForState? state = null, int? timeout = null) => MainFrame.WaitForSelectorAsync(true, selector, state, timeout);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string script) => MainFrame.EvaluateAsync(true, script);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string script, object args) => MainFrame.EvaluateAsync(true, script, args);

        /// <inheritdoc />
        public Task<byte[]> ScreenshotAsync(bool fullPage) => ScreenshotAsync(null, fullPage);

        /// <inheritdoc />
        public Task<byte[]> ScreenshotAsync(Rect clip) => ScreenshotAsync(null, false, clip);

        /// <inheritdoc />
        public async Task<byte[]> ScreenshotAsync(
            string path = null,
            bool fullPage = false,
            Rect clip = null,
            bool omitBackground = false,
            ScreenshotFormat? type = null,
            int? quality = null,
            int? timeout = null)
            => Convert.FromBase64String(await _channel.ScreenshotAsync(path, fullPage, clip, omitBackground, type, quality, timeout).ConfigureAwait(false));

        /// <inheritdoc />
        public Task SetContentAsync(string html, LifecycleEvent? waitUntil = null, int? timeout = null) => MainFrame.SetContentAsync(true, html, waitUntil, timeout);

        /// <inheritdoc />
        public Task<string> GetContentAsync() => MainFrame.GetContentAsync(true);

        /// <inheritdoc />
        public Task SetExtraHttpHeadersAsync(IDictionary<string, string> headers) => _channel.SetExtraHttpHeadersAsync(headers);

        /// <inheritdoc />
        public Task<IElementHandle> QuerySelectorAsync(string selector) => MainFrame.QuerySelectorAsync(true, selector);

        /// <inheritdoc />
        public Task<IEnumerable<IElementHandle>> QuerySelectorAllAsync(string selector) => MainFrame.QuerySelectorAllAsync(true, selector);

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string pageFunction) => MainFrame.EvaluateHandleAsync(pageFunction);

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string pageFunction, object args) => MainFrame.EvaluateHandleAsync(pageFunction, args);

        /// <inheritdoc />
        public Task<IElementHandle> AddScriptTagAsync(AddTagOptions options) => MainFrame.AddScriptTagAsync(true, options);

        /// <inheritdoc />
        public Task<IElementHandle> AddStyleTagAsync(AddTagOptions options) => MainFrame.AddStyleTagAsync(true, options);

        /// <inheritdoc />
        public Task ClickAsync(string selector, ClickOptions options = null) => MainFrame.ClickAsync(true, selector, options);

        /// <inheritdoc />
        public Task DoubleClickAsync(string selector, ClickOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public async Task<IResponse> GoBackAsync(int? timeout = null, LifecycleEvent? waitUntil = null)
            => (await _channel.GoBackAsync(timeout, waitUntil).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IResponse> GoForwardAsync(int? timeout = null, LifecycleEvent? waitUntil = null)
            => (await _channel.GoForwardAsync(timeout, waitUntil).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IResponse> ReloadAsync(int? timeout = null, LifecycleEvent? waitUntil = null)
            => (await _channel.ReloadAsync(timeout, waitUntil).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public Task SetOfflineModeAsync(bool enabled) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task ExposeBindingAsync(string name, Action<BindingSource> playwrightFunction)
            => ExposeBindingAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> playwrightFunction)
            => ExposeBindingAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (Delegate)playwrightFunction);

        /// <inheritdoc/>
        public Task ExposeFunctionAsync(string name, Action playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _) => playwrightFunction());

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T>(string name, Action<T> playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _, T t) => playwrightFunction(t));

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<TResult>(string name, Func<TResult> playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _) => playwrightFunction());

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _, T t) => playwrightFunction(t));

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2) => playwrightFunction(t1, t2));

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2, T3 t3) => playwrightFunction(t1, t2, t3));

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2, T3 t3, T4 t4) => playwrightFunction(t1, t2, t3, t4));

        /// <inheritdoc />
        public async Task<IResponse> WaitForResponseAsync(string url, WaitForOptions options = null)
        {
            var result = await WaitForEvent<ResponseEventArgs>(
                PageEvent.Response,
                e => e.Response.Url.Equals(url),
                options?.Timeout).ConfigureAwait(false);
            return result.Response;
        }

        /// <inheritdoc />
        public Task GetPdfAsync(string file) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task GetPdfAsync(string file, PdfOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<Stream> GetPdfStreamAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<Stream> GetPdfStreamAsync(PdfOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<byte[]> GetPdfDataAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<byte[]> GetPdfDataAsync(PdfOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task AddInitScriptAsync(string script, params object[] args)
            => _channel.AddInitScriptAsync(ScriptsHelper.SerializeScriptCall(script, args));

        /// <inheritdoc />
        public Task AddInitScriptAsync(AddInitScriptOptions options, params object[] args)
            => AddInitScriptAsync(ScriptsHelper.EvaluationScript(options?.Content, options?.Path), args);

        /// <inheritdoc />
        public Task RouteAsync(string url, Action<Route, IRequest> handler)
        {
            _routes.Add(new RouteSetting
            {
                Url = url,
                Handler = handler,
            });

            if (_routes.Count == 1)
            {
                return _channel.SetNetworkInterceptionEnabledAsync(true);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RouteAsync(Regex regex, Action<Route, IRequest> handler)
        {
            _routes.Add(new RouteSetting
            {
                Regex = regex,
                Handler = handler,
            });

            if (_routes.Count == 1)
            {
                return _channel.SetNetworkInterceptionEnabledAsync(true);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task UnrouteAsync(string url, Action<Route, IRequest> handler = null)
        {
            var newRoutesList = new List<RouteSetting>();
            newRoutesList.AddRange(_routes.Where(r => r.Url != url || (handler != null && r.Handler != handler)));
            _routes = newRoutesList;

            if (_routes.Count == 0)
            {
                return _channel.SetNetworkInterceptionEnabledAsync(false);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task UnrouteAsync(Regex regex, Action<Route, IRequest> handler = null)
        {
            var newRoutesList = new List<RouteSetting>();
            newRoutesList.AddRange(_routes.Where(r => r.Regex != regex || (handler != null && r.Handler != handler)));
            _routes = newRoutesList;

            if (_routes.Count == 0)
            {
                return _channel.SetNetworkInterceptionEnabledAsync(false);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task WaitForLoadStateAsync(LifecycleEvent waitUntil, int? timeout = null)
            => MainFrame.WaitForLoadStateAsync(true, waitUntil, timeout);

        /// <inheritdoc />
        public Task SetViewportSizeAsync(int width, int height)
            => SetViewportSizeAsync(new ViewportSize { Width = width, Height = height });

        /// <inheritdoc />
        public Task SetViewportSizeAsync(ViewportSize viewport)
        {
            Viewport = viewport;
            return _channel.SetViewportSizeAsync(viewport);
        }

        /// <inheritdoc />
        public Task CheckAsync(string selector, CheckOptions options = null) => MainFrame.CheckAsync(true, selector, options);

        /// <inheritdoc />
        public Task UncheckAsync(string selector, CheckOptions options = null) => MainFrame.UncheckAsync(true, selector, options);

        /// <inheritdoc />
        public Task DispatchEventAsync(string selector, string type, object eventInit = null, int? timeout = null)
             => MainFrame.DispatchEventAsync(true, selector, type, eventInit, timeout);

        /// <inheritdoc />
        public Task<string> GetAttributeAsync(string selector, string name, int? timeout = null)
             => MainFrame.GetAttributeAsync(true, selector, name, timeout);

        /// <inheritdoc />
        public Task<string> GetInnerHtmlAsync(string name, int? timeout = null)
             => MainFrame.GetInnerHtmlAsync(true, name, timeout);

        /// <inheritdoc />
        public Task<string> GetInnerTextAsync(string name, int? timeout = null)
             => MainFrame.GetInnerTextAsync(true, name, timeout);

        /// <inheritdoc />
        public Task<string> GetTextContentAsync(string name, int? timeout = null)
             => MainFrame.GetTextContentAsync(true, name, timeout);

        private void Channel_Closed(object sender, EventArgs e)
        {
            IsClosed = true;
            BrowserContext?.PagesList.Remove(this);
            RejectPendingOperations(false);
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void Channel_Crashed(object sender, EventArgs e)
        {
            RejectPendingOperations(true);
            Crashed?.Invoke(this, EventArgs.Empty);
        }

        private void Channel_BindingCall(object sender, BindingCallEventArgs e)
        {
            if (Bindings.TryGetValue(e.BidingCall.Name, out var binding))
            {
                _ = e.BidingCall.CallAsync(binding);
            }
        }

        private void Channel_Route(object sender, RouteEventArgs e)
        {
            foreach (var route in _routes)
            {
                if (
                    (route.Url != null && e.Request.Url.UrlMatches(route.Url)) ||
                    (route.Regex?.IsMatch(e.Request.Url) == true))
                {
                    route.Handler(e.Route, e.Request);
                    return;
                }
            }

            BrowserContext.OnRoute(e.Route, e.Request);
        }

        private void Channel_FrameNavigated(object sender, FrameNavigatedEventArgs e)
        {
            e.Frame.Object.Url = e.Url;
            e.Frame.Object.Name = e.Name;
            FrameNavigated?.Invoke(this, new FrameEventArgs(e.Frame.Object));
        }

        private void Channel_FrameDetached(object sender, FrameEventArgs e)
        {
            var frame = e.Frame as Frame;
            _frames.Remove(frame);
            frame.Detached = true;
            frame.ParentFrame?.ChildFramesList?.Remove(frame);
            FrameDetached?.Invoke(this, e);
        }

        private void Channel_FrameAttached(object sender, FrameEventArgs e)
        {
            var frame = e.Frame as Frame;
            frame.Page = this;
            _frames.Add(frame);
            frame.ParentFrame?.ChildFramesList?.Add(frame);
            FrameAttached?.Invoke(this, e);
        }

        private void RejectPendingOperations(bool isCrash)
        {
            foreach (var (_, waitTcs) in _waitForCancellationTcs.Where(e => e.pageEvent != (isCrash ? PageEvent.Crashed : PageEvent.Closed)))
            {
                waitTcs.TrySetException(new TargetClosedException(isCrash ? "Page crashed" : "Page closed"));
            }

            _waitForCancellationTcs.Clear();
        }

        private Task ExposeBindingAsync(string name, Delegate playwrightFunction)
        {
            if (Bindings.ContainsKey(name))
            {
                throw new PlaywrightSharpException($"Function \"{name}\" has been already registered");
            }

            Bindings.Add(name, playwrightFunction);

            return _channel.ExposeBindingAsync(name);
        }
    }
}
