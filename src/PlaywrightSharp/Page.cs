using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Accessibility;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IPage"/>
    public class Page : IPage
    {
        /// <inheritdoc cref="IPage"/>
        internal Page(IPageDelegate pageDelegate, IBrowserContext browserContext)
        {
            FrameManager = new FrameManager(this);
            Delegate = pageDelegate;
            BrowserContext = browserContext;
        }

        /// <inheritdoc cref="IPage"/>
        public event EventHandler<ConsoleEventArgs> Console;

        /// <inheritdoc cref="IPage"/>
        public event EventHandler<PopupEventArgs> Popup;

        /// <inheritdoc cref="IPage"/>
        public event EventHandler<RequestEventArgs> Request;

        /// <inheritdoc cref="IPage"/>
        public event EventHandler<RequestEventArgs> RequestFinished;

        /// <inheritdoc cref="IPage"/>
        public event EventHandler<RequestEventArgs> RequestFailed;

        /// <inheritdoc cref="IPage"/>
        public event EventHandler<DialogEventArgs> Dialog;

        /// <inheritdoc cref="IPage"/>
        public event EventHandler<FrameEventArgs> FrameAttached;

        /// <inheritdoc cref="IPage"/>
        public event EventHandler<FrameEventArgs> FrameDetached;

        /// <inheritdoc cref="IPage"/>
        public event EventHandler<FrameEventArgs> FrameNavigated;

        /// <inheritdoc cref="IPage"/>
        public event EventHandler Load;

        /// <inheritdoc cref="IPage"/>
        public event EventHandler DOMContentLoaded;

        /// <inheritdoc cref="IPage"/>
        public IFrame MainFrame => FrameManager.MainFrame;

        /// <inheritdoc cref="IPage"/>
        public virtual IBrowserContext BrowserContext { get; internal set; }

        /// <inheritdoc cref="IPage"/>
        public Viewport Viewport => null;

        /// <inheritdoc cref="IPage"/>
        public IAccessibility Accessibility => null;

        /// <inheritdoc cref="IPage"/>
        public IMouse Mouse => null;

        /// <inheritdoc cref="IPage"/>
        public string Url => MainFrame.Url;

        /// <inheritdoc cref="IPage"/>
        public IFrame[] Frames => null;

        /// <inheritdoc cref="IPage"/>
        public IKeyboard Keyboard => null;

        /// <inheritdoc cref="IPage"/>
        public int DefaultTimeout { get; set; }

        /// <inheritdoc cref="IPage"/>
        public int DefaultNavigationTimeout { get; set; }

        internal FrameManager FrameManager { get; }

        internal bool HasPopupEventListeners => Popup?.GetInvocationList().Any() == true;

        internal PageState PageState { get; } = new PageState();

        internal IPageDelegate Delegate { get; }

        /// <inheritdoc cref="IPage"/>
        public Task<IElementHandle> AddScriptTagAsync(AddTagOptions options)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task AuthenticateAsync(Credentials credentials)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task ClickAsync(string selector, ClickOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task CloseAsync(PageCloseOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task EmulateMediaAsync(EmulateMedia options)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<T> EvaluateAsync<T>(string script, params object[] args)
            => Task.FromResult<T>((T)Convert.ChangeType(56, typeof(T), System.Globalization.CultureInfo.InvariantCulture));

        /// <inheritdoc cref="IPage"/>
        public Task<JsonElement?> EvaluateAsync(string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<IJSHandle> EvaluateHandleAsync(string expression)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<IJSHandle> EvaluateHandleAsync(string pageFunction, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task EvaluateOnNewDocumentAsync(string pageFunction, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task ExposeFunctionAsync(string name, Action playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task ExposeFunctionAsync<TResult>(string name, Func<TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task FillAsync(string selector, string text, WaitForSelectorOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task FocusAsync(string selector)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<IResponse> GoBackAsync(NavigationOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<IResponse> GoForwardAsync(NavigationOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<IResponse> GoToAsync(string url, GoToOptions options = null) => MainFrame.GoToAsync(url, options);

        /// <inheritdoc cref="IPage"/>
        public Task<IResponse> GoToAsync(string url, int timeout, params WaitUntilNavigation[] waitUntil)
             => MainFrame.GoToAsync(url, new GoToOptions { Timeout = timeout, WaitUntil = waitUntil });

        /// <inheritdoc cref="IPage"/>
        public Task<IResponse> GoToAsync(string url, params WaitUntilNavigation[] waitUntil)
             => MainFrame.GoToAsync(url, new GoToOptions { WaitUntil = waitUntil });

        /// <inheritdoc cref="IPage"/>
        public Task HoverAsync(string selector)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<IElementHandle> QuerySelectorAsync(string selector)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task QuerySelectorEvaluateAsync(string selector, string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<IResponse> ReloadAsync(NavigationOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<byte[]> ScreenshotAsync(ScreenshotOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task SetCacheEnabledAsync(bool enabled = true)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task SetContentAsync(string html, NavigationOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task SetContentAsync(string html, WaitUntilNavigation waitUntil)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task SetExtraHttpHeadersAsync(IReadOnlyDictionary<string, string> headers)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task SetOfflineModeAsync(bool value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task SetRequestInterceptionAsync(bool value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task SetUserAgentAsync(string userAgent)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task SetViewportAsync(Viewport viewport)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task TripleClickAsync(string selector, ClickOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task TypeAsync(string selector, string text, TypeOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<T> WaitForEvent<T>(PageEvent e, WaitForEventOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task WaitForLoadStateAsync(NavigationOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<IResponse> WaitForNavigationAsync(WaitForNavigationOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<IResponse> WaitForNavigationAsync(WaitUntilNavigation waitUntil)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<IRequest> WaitForRequestAsync(string url, WaitForOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<IResponse> WaitForResponseAsync(string url, WaitForOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IPage"/>
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null)
        {
            throw new NotImplementedException();
        }

        internal void OnPopup(object popupPage)
        {
            throw new NotImplementedException();
        }

        internal void DidDisconnected()
        {
        }

        internal void OnDOMContentLoaded() => DOMContentLoaded?.Invoke(this, new EventArgs());

        internal void OnFrameAttached(IFrame frame) => FrameAttached?.Invoke(this, new FrameEventArgs(frame));

        internal void OnFrameNavigated(Frame frame) => FrameNavigated?.Invoke(this, new FrameEventArgs(frame));

        internal void OnLoad() => Load?.Invoke(this, new EventArgs());
    }
}
