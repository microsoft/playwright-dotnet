using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
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

        internal Page(ConnectionScope scope, string guid, PageInitializer initializer)
        {
            _scope = scope;
            _channel = new PageChannel(guid, scope, this);

            MainFrame = initializer.MainFrame.Object;
            MainFrame.Page = this;
            _frames.Add(MainFrame);
            Viewport = initializer.ViewportSize;

            _channel.Closed += Channel_Closed;
            _channel.Crashed += Channel_Crashed;
            _channel.Popup += (sender, e) => Popup?.Invoke(this, new PopupEventArgs(e.Page));
            _channel.Request += (sender, e) => Request?.Invoke(this, new RequestEventArgs(e.RequestChannel.Object));
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
        public event EventHandler<RequestEventArgs> RequestFailed;

        /// <inheritdoc />
        public event EventHandler<DialogEventArgs> Dialog;

        /// <inheritdoc />
        public event EventHandler<FrameEventArgs> FrameAttached;

        /// <inheritdoc />
        public event EventHandler<FrameEventArgs> FrameDetached;

        /// <inheritdoc />
        public event EventHandler<FrameEventArgs> FrameNavigated;

        /// <inheritdoc />
        public event EventHandler<FileChooserEventArgs> FileChooser;

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
        public event EventHandler<WorkerEventArgs> WorkerCreated;

        /// <inheritdoc />
        public event EventHandler<WorkerEventArgs> WorkerDestroyed;

        /// <inheritdoc />
        public event EventHandler<WebsocketEventArgs> Websocket;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        Channel<Page> IChannelOwner<Page>.Channel => _channel;

        /// <inheritdoc />
        public bool IsClosed { get; }

        /// <inheritdoc />
        IFrame IPage.MainFrame => MainFrame;

        /// <inheritdoc cref="IPage.MainFrame" />
        public Frame MainFrame { get; }

        /// <inheritdoc />
        IBrowserContext IPage.BrowserContext => BrowserContext;

        /// <inheritdoc cref="IPage.BrowserContext" />
        public BrowserContext BrowserContext { get; internal set; }

        /// <inheritdoc />
        public ViewportSize Viewport { get; private set; }

        /// <inheritdoc />
        public IAccessibility Accessibility { get; }

        /// <inheritdoc />
        public IMouse Mouse { get; }

        /// <inheritdoc />
        public string Url { get; }

        /// <inheritdoc />
        public IFrame[] Frames { get; }

        /// <inheritdoc />
        public IKeyboard Keyboard { get; }

        /// <inheritdoc/>
        public int DefaultTimeout
        {
            get => _timeoutSettings.Timeout;
            set => _timeoutSettings.SetDefaultTimeout(value);
        }

        /// <inheritdoc/>
        public int DefaultNavigationTimeout
        {
            get => _timeoutSettings.NavigationTimeout;
            set => _timeoutSettings.SetDefaultNavigationTimeout(value);
        }

        /// <inheritdoc />
        public IWorker[] Workers { get; }

        /// <inheritdoc />
        public ICoverage Coverage { get; }

        internal BrowserContext OwnedContext { get; set; }

        /// <inheritdoc />
        public Task<string> GetTitleAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IPage> GetOpenerAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task WaitForLoadStateAsync(NavigationOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetCacheEnabledAsync(bool enabled = true) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task EmulateMediaAsync(EmulateMedia options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> GoToAsync(string url, GoToOptions options = null) => MainFrame.GoToAsync(url, options);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(WaitForNavigationOptions options = null, CancellationToken token = default) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(WaitUntilNavigation waitUntil) => throw new NotImplementedException();

        /// <inheritdoc />
        public async Task<IRequest> WaitForRequestAsync(string url, WaitForOptions options = null)
        {
            var result = await WaitForEvent(PageEvent.Request, new WaitForEventOptions<RequestEventArgs>
            {
                Predicate = e => e.Request.Url.Equals(url),
                Timeout = options?.Timeout,
            }).ConfigureAwait(false);
            return result.Request;
        }

        /// <inheritdoc />
        public async Task<IRequest> WaitForRequestAsync(Regex regex, WaitForOptions options = null)
        {
            var result = await WaitForEvent(PageEvent.Request, new WaitForEventOptions<RequestEventArgs>
            {
                Predicate = e => regex.IsMatch(e.Request.Url),
                Timeout = options?.Timeout,
            }).ConfigureAwait(false);
            return result.Request;
        }

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string pageFunction, WaitForFunctionOptions options = null, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string pageFunction, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        /// <inheritdoc cref="IPage.WaitForEvent{T}(PageEvent, WaitForEventOptions{T})"/>
        public async Task<T> WaitForEvent<T>(PageEvent e, WaitForEventOptions<T> options = null)
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
            var disconnectedTcs = new TaskCompletionSource<bool>();
            _waitForCancellationTcs.Add((e, disconnectedTcs));
            await Task.WhenAny(eventTsc.Task, disconnectedTcs.Task).WithTimeout(options?.Timeout ?? DefaultTimeout).ConfigureAwait(false);
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
        public Task<IResponse> GoToAsync(string url, int timeout, params WaitUntilNavigation[] waitUntil) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> GoToAsync(string url, params WaitUntilNavigation[] waitUntil) => throw new NotImplementedException();

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
        public Task<T> EvaluateAsync<T>(string script) => MainFrame.EvaluateAsync<T>(script);

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string script, object args) => MainFrame.EvaluateAsync<T>(script, args);

        /// <inheritdoc />
        public Task EvaluateOnNewDocumentAsync(string pageFunction, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task QuerySelectorEvaluateAsync(string selector, string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task QuerySelectorAllEvaluateAsync(string selector, string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task FillAsync(string selector, string text, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task TypeAsync(string selector, string text, TypeOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task FocusAsync(string selector, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task HoverAsync(string selector, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, string value, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, SelectOption value, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, IElementHandle value, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, string[] values, WaitForSelectorOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, SelectOption[] values, WaitForSelectorOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, IElementHandle[] values, WaitForSelectorOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, params string[] values) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, params SelectOption[] values) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, params IElementHandle[] values) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task WaitForTimeoutAsync(int timeout) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null) => MainFrame.WaitForSelectorAsync(true, selector, options);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForSelectorEvaluateAsync(
            string selector,
            string script,
            WaitForFunctionOptions options = null,
            params object[] args) =>
            throw new NotImplementedException();

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string script) => MainFrame.EvaluateAsync(script);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string script, object args) => MainFrame.EvaluateAsync(script, args);

        /// <inheritdoc />
        public Task<byte[]> ScreenshotAsync(ScreenshotOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string> ScreenshotBase64Async(ScreenshotOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetContentAsync(string html, NavigationOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetContentAsync(string html, WaitUntilNavigation waitUntil) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string> GetContentAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetExtraHttpHeadersAsync(IDictionary<string, string> headers) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task AuthenticateAsync(Credentials credentials) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> QuerySelectorAsync(string selector) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle[]> QuerySelectorAllAsync(string selector) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string pageFunction) => MainFrame.EvaluateHandleAsync(pageFunction);

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string pageFunction, object args) => MainFrame.EvaluateHandleAsync(pageFunction, args);

        /// <inheritdoc />
        public Task<IElementHandle> AddScriptTagAsync(AddTagOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> AddStyleTagAsync(AddTagOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ClickAsync(string selector, ClickOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task DoubleClickAsync(string selector, ClickOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task TripleClickAsync(string selector, ClickOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> GoBackAsync(NavigationOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> GoForwardAsync(NavigationOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> ReloadAsync(NavigationOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetRequestInterceptionAsync(bool enabled) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetOfflineModeAsync(bool enabled) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ExposeFunctionAsync(string name, Action playwrightFunction) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ExposeFunctionAsync<TResult>(string name, Func<TResult> playwrightFunction) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> playwrightFunction) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> playwrightFunction) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> playwrightFunction) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> playwrightFunction) => throw new NotImplementedException();

        /// <inheritdoc />
        public async Task<IResponse> WaitForResponseAsync(string url, WaitForOptions options = null)
        {
            var result = await WaitForEvent(PageEvent.Response, new WaitForEventOptions<ResponseEventArgs>
            {
                Predicate = e => e.Response.Url.Equals(url),
                Timeout = options?.Timeout,
            }).ConfigureAwait(false);
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

        private void Channel_Closed(object sender, EventArgs e)
        {
            BrowserContext?.PagesList.Remove(this);
            RejectPendingOperations(false);
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void Channel_Crashed(object sender, EventArgs e)
        {
            RejectPendingOperations(true);
            Crashed?.Invoke(this, EventArgs.Empty);
        }

        private void RejectPendingOperations(bool isCrash)
        {
            foreach (var (_, waitTcs) in _waitForCancellationTcs.Where(e => e.pageEvent != (isCrash ? PageEvent.Crashed : PageEvent.Closed)))
            {
                waitTcs.TrySetException(new TargetClosedException(isCrash ? "Page crashed" : "Page closed"));
            }

            _waitForCancellationTcs.Clear();
        }
    }
}
