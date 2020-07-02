using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IPage" />
    public class Page : IChannelOwner, IPage
    {
        internal Page(PlaywrightClient client, Channel channel, PageInitializer initializer)
        {
            throw new NotImplementedException();
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
        public event EventHandler<EventArgs> Close;

        /// <inheritdoc />
        public event EventHandler<ErrorEventArgs> Error;

        /// <inheritdoc />
        public event EventHandler<PageErrorEventArgs> PageError;

        /// <inheritdoc />
        public event EventHandler<WorkerEventArgs> WorkerCreated;

        /// <inheritdoc />
        public event EventHandler<WorkerEventArgs> WorkerDestroyed;

        /// <inheritdoc />
        public event EventHandler<WebsocketEventArgs> Websocket;

        /// <inheritdoc />
        public bool IsClosed { get; }

        /// <inheritdoc />
        public IFrame MainFrame { get; }

        /// <inheritdoc />
        public IBrowserContext BrowserContext { get; }

        /// <inheritdoc />
        public Viewport Viewport { get; }

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

        /// <inheritdoc />
        public int DefaultTimeout { get; set; }

        /// <inheritdoc />
        public int DefaultNavigationTimeout { get; set; }

        /// <inheritdoc />
        public IWorker[] Workers { get; }

        /// <inheritdoc />
        public ICoverage Coverage { get; }

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
        public Task<IResponse> GoToAsync(string url, GoToOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(WaitForNavigationOptions options = null, CancellationToken token = default) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(WaitUntilNavigation waitUntil) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IRequest> WaitForRequestAsync(string url, WaitForOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IRequest> WaitForRequestAsync(Regex regex, WaitForOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string pageFunction, WaitForFunctionOptions options = null, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string pageFunction, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> WaitForEvent<T>(PageEvent e, WaitForEventOptions<T> options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> GoToAsync(string url, int timeout, params WaitUntilNavigation[] waitUntil) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IResponse> GoToAsync(string url, params WaitUntilNavigation[] waitUntil) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task CloseAsync(PageCloseOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string script, params object[] args) => throw new NotImplementedException();

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
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> WaitForSelectorEvaluateAsync(
            string selector,
            string script,
            WaitForFunctionOptions options = null,
            params object[] args) =>
            throw new NotImplementedException();

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string script, params object[] args) => throw new NotImplementedException();

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
        public Task<IJSHandle> EvaluateHandleAsync(string pageFunction, params object[] args) => throw new NotImplementedException();

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
        public Task SetViewportAsync(Viewport viewport) => throw new NotImplementedException();

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
        public Task<IResponse> WaitForResponseAsync(string url, WaitForOptions options = null) => throw new NotImplementedException();

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
    }
}
