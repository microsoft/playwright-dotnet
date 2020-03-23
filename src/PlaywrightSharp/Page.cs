using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Accessibility;
using PlaywrightSharp.Input;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IPage"/>
    public sealed class Page : IPage
    {
        private readonly TaskCompletionSource<bool> _closeTsc = new TaskCompletionSource<bool>();
        private bool _disconnected;

        /// <inheritdoc cref="IPage"/>
        internal Page(IPageDelegate pageDelegate, IBrowserContext browserContext)
        {
            FrameManager = new FrameManager(this);
            Delegate = pageDelegate;
            BrowserContext = browserContext;
            Keyboard = new Keyboard(Delegate.RawKeyboard);
            Mouse = new Mouse(Delegate.RawMouse, Keyboard);
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
        public Viewport Viewport => null;

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

        internal PageState PageState { get; } = new PageState();

        internal IPageDelegate Delegate { get; }

        /// <inheritdoc cref="IPage.AddScriptTagAsync(AddTagOptions)"/>
        public Task<IElementHandle> AddScriptTagAsync(AddTagOptions options)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.AuthenticateAsync(Credentials)"/>
        public Task AuthenticateAsync(Credentials credentials)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.ClickAsync(string, ClickOptions)"/>
        public Task ClickAsync(string selector, ClickOptions options = null) => MainFrame.ClickAsync(selector, options);

        /// <inheritdoc cref="IPage.CloseAsync(PageCloseOptions)"/>
        public async Task CloseAsync(PageCloseOptions options = null)
        {
            if (IsClosed)
            {
                return;
            }

            if (_disconnected)
            {
                throw new PlaywrightSharpException("Protocol error: Connection closed. Most likely the page has been closed.");
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
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.ExposeFunctionAsync(string, Action)"/>
        public Task ExposeFunctionAsync(string name, Action playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.ExposeFunctionAsync{TResult}(string, Func{TResult})"/>
        public Task ExposeFunctionAsync<TResult>(string name, Func<TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.ExposeFunctionAsync{T, TResult}(string, Func{T, TResult})"/>
        public Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.ExposeFunctionAsync{T1, T2, TResult}(string, Func{T1, T2, TResult})"/>
        public Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.ExposeFunctionAsync{T1, T2, T3, TResult}(string, Func{T1, T2, T3, TResult})"/>
        public Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.ExposeFunctionAsync{T1, T2, T3, T4, TResult}(string, Func{T1, T2, T3, T4, TResult})"/>
        public Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.FillAsync(string, string, FillOptions)"/>
        public Task FillAsync(string selector, string text, WaitForSelectorOptions options = null)
        {
            throw new NotImplementedException();
        }

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
        public Task<IElementHandle> QuerySelectorAsync(string selector)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.QuerySelectorEvaluateAsync(string, string, object[])"/>
        public Task QuerySelectorEvaluateAsync(string selector, string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.QuerySelectorEvaluateAsync{T}(string, string, object[])"/>
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.ReloadAsync(NavigationOptions)"/>
        public Task<IResponse> ReloadAsync(NavigationOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.ScreenshotAsync(ScreenshotOptions)"/>
        public Task<byte[]> ScreenshotAsync(ScreenshotOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.SetCacheEnabledAsync(bool)"/>
        public Task SetCacheEnabledAsync(bool enabled = true)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.SetContentAsync(string, NavigationOptions)"/>
        public Task SetContentAsync(string html, NavigationOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.SetContentAsync(string, WaitUntilNavigation)"/>
        public Task SetContentAsync(string html, WaitUntilNavigation waitUntil)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.SetExtraHttpHeadersAsync(IReadOnlyDictionary{string, string})"/>
        public Task SetExtraHttpHeadersAsync(IReadOnlyDictionary<string, string> headers)
        {
            throw new NotImplementedException();
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

        /// <inheritdoc cref="IPage.SetUserAgentAsync(string)"/>
        public Task SetUserAgentAsync(string userAgent)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.SetViewportAsync(PlaywrightSharp.Viewport)"/>
        public Task SetViewportAsync(Viewport viewport)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.TripleClickAsync(string, ClickOptions)"/>
        public Task TripleClickAsync(string selector, ClickOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.TypeAsync(string, string, TypeOptions)"/>
        public Task TypeAsync(string selector, string text, TypeOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.GetTitleAsync"/>
        public Task<string> GetTitleAsync()
        {
            throw new NotImplementedException();
        }

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
        public Task<IResponse> WaitForNavigationAsync(WaitForNavigationOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.WaitForNavigationAsync(WaitUntilNavigation)"/>
        public Task<IResponse> WaitForNavigationAsync(WaitUntilNavigation waitUntil)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.WaitForRequestAsync(string, WaitForOptions)"/>
        public Task<IRequest> WaitForRequestAsync(string url, WaitForOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.WaitForResponseAsync(string, WaitForOptions)"/>
        public Task<IResponse> WaitForResponseAsync(string url, WaitForOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.GetPdfAsync(string)"/>
        public Task GetPdfAsync(string file) => throw new NotImplementedException();

        /// <inheritdoc cref="IPage.WaitForRequestAsync(Regex, WaitForOptions)"/>
        public Task<IRequest> WaitForRequestAsync(Regex regex, WaitForOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.WaitForFunctionAsync(string, object[])"/>
        public Task<IJSHandle> WaitForFunctionAsync(string script, params object[] args)
        {
            throw new NotImplementedException();
        }

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

        /// <inheritdoc cref="IPage.FillAsync(string, string, FillOptions)"/>
        public Task FillAsync(string selector, string text, FillOptions options = null)
        {
            throw new NotImplementedException();
        }

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
        public Task<string> GetContentAsync()
        {
            throw new NotImplementedException();
        }

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

        /// <inheritdoc cref="IPage.WaitForFunctionAsync(string, WaitForFunctionOptions, object[])"/>
        public Task<IJSHandle> WaitForFunctionAsync(string script, WaitForFunctionOptions options = null, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.WaitForTimeoutAsync(int)"/>
        public Task WaitForTimeoutAsync(int timeout)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage.WaitForSelectorAsync(string, WaitForSelectorOptions)"/>
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null)
        {
            throw new NotImplementedException();
        }

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

        internal void OnPopup(object parent) => Popup?.Invoke(parent, new PopupEventArgs(this));

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

        internal void OnFrameNavigated(Frame frame) => FrameNavigated?.Invoke(this, new FrameEventArgs(frame));

        internal void OnLoad() => Load?.Invoke(this, new EventArgs());
    }
}
