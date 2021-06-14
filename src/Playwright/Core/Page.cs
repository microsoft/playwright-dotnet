/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Copyright (c) 2020 Meir Blachman
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    internal partial class Page : ChannelOwnerBase, IChannelOwner<Page>, IPage
    {
        private readonly PageChannel _channel;
        private readonly List<Frame> _frames = new();
        private readonly List<(IEvent PageEvent, TaskCompletionSource<bool> WaitTcs)> _waitForCancellationTcs = new();
        private readonly object _fileChooserEventLock = new();
        private readonly IAccessibility _accessibility;
        private readonly IMouse _mouse;
        private readonly IKeyboard _keyboard;
        private readonly ITouchscreen _touchscreen;
        private readonly PageInitializer _initializer;

        private List<RouteSetting> _routes = new();
        private EventHandler<IFileChooser> _fileChooserEventHandler;
        private bool _fileChooserIntercepted;
        private Video _video;
        private float _defaultNavigationTimeout;
        private float _defaultTimeout;

        internal Page(IChannelOwner parent, string guid, PageInitializer initializer) : base(parent, guid)
        {
            Context = (BrowserContext)parent;

            _channel = new PageChannel(guid, parent.Connection, this);

            MainFrame = initializer.MainFrame;
            MainFrame.Page = this;
            _frames.Add(MainFrame);
            if (initializer.ViewportSize != null)
            {
                ViewportSize = new PageViewportSizeResult { Width = initializer.ViewportSize.Width, Height = initializer.ViewportSize.Height };
            }

            IsClosed = initializer.IsClosed;
            _accessibility = new Accessibility(_channel);
            _keyboard = new Keyboard(_channel);
            _touchscreen = new Touchscreen(_channel);
            _mouse = new Mouse(_channel);
            _channel.Closed += Channel_Closed;
            _channel.Crashed += Channel_Crashed;
            _channel.Popup += (_, e) => Popup?.Invoke(this, e.Page);
            _channel.WebSocket += (_, e) => WebSocket?.Invoke(this, e);
            _channel.BindingCall += Channel_BindingCall;
            _channel.Route += Channel_Route;
            _channel.FrameAttached += Channel_FrameAttached;
            _channel.FrameDetached += Channel_FrameDetached;
            _channel.Dialog += (_, e) =>
            {
                if (Dialog == null)
                {
                    _ = e.DismissAsync();
                }
                else
                {
                    Dialog?.Invoke(this, e);
                }
            };
            _channel.Console += (_, e) => Console?.Invoke(this, e);
            _channel.DOMContentLoaded += (_, e) => DOMContentLoaded?.Invoke(this, this);
            _channel.Download += (_, e) => Download?.Invoke(this, new Download(this, e.Url, e.SuggestedFilename, e.Artifact.Object));
            _channel.PageError += (_, e) => PageError?.Invoke(this, e.ToString());
            _channel.Load += (_, e) => Load?.Invoke(this, this);
            _channel.Video += (_, e) => ForceVideo().ArtifactReady(e.Artifact);

            _channel.FileChooser += (_, e) => _fileChooserEventHandler?.Invoke(this, new FileChooser(this, e.Element.Object, e.IsMultiple));
            _channel.Worker += (_, e) =>
            {
                WorkersList.Add(e.WorkerChannel.Object);
                e.WorkerChannel.Object.Page = this;
                Worker?.Invoke(this, e.WorkerChannel.Object);
            };

            _defaultNavigationTimeout = Context.DefaultNavigationTimeout;
            _defaultTimeout = Context.DefaultTimeout;
            _initializer = initializer;
        }

        public event EventHandler<IConsoleMessage> Console;

        public event EventHandler<IPage> Popup;

        public event EventHandler<IRequest> Request;

        public event EventHandler<IWebSocket> WebSocket;

        public event EventHandler<IResponse> Response;

        public event EventHandler<IRequest> RequestFinished;

        public event EventHandler<IRequest> RequestFailed;

        public event EventHandler<IDialog> Dialog;

        public event EventHandler<IFrame> FrameAttached;

        public event EventHandler<IFrame> FrameDetached;

        public event EventHandler<IFrame> FrameNavigated;

        public event EventHandler<IFileChooser> FileChooser
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

        public event EventHandler<IPage> Load;

        public event EventHandler<IPage> DOMContentLoaded;

        public event EventHandler<IPage> Close;

        public event EventHandler<IPage> Crash;

        public event EventHandler<string> PageError;

        public event EventHandler<IWorker> Worker;

        public event EventHandler<IDownload> Download;

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<Page> IChannelOwner<Page>.Channel => _channel;

        public bool IsClosed { get; private set; }

        IFrame IPage.MainFrame => MainFrame;

        public Frame MainFrame { get; }

        IBrowserContext IPage.Context => Context;

        public BrowserContext Context { get; set; }

        public PageViewportSizeResult ViewportSize { get; private set; }

        public IAccessibility Accessibility
        {
            get => _accessibility;
            set => throw new NotSupportedException();
        }

        public IMouse Mouse
        {
            get => _mouse;
            set => throw new NotSupportedException();
        }

        public string Url => MainFrame.Url;

        public IReadOnlyList<IFrame> Frames => _frames.AsReadOnly();

        public IKeyboard Keyboard
        {
            get => _keyboard;
        }

        public ITouchscreen Touchscreen
        {
            get => _touchscreen;
        }

        public IReadOnlyList<IWorker> Workers => WorkersList;

        public IVideo Video
        {
            get
            {
                if (!Context.RecordVideo)
                {
                    return null;
                }

                return ForceVideo();
            }
            set => _video = value as Video;
        }

        internal BrowserContext OwnedContext { get; set; }

        internal Dictionary<string, Delegate> Bindings { get; } = new Dictionary<string, Delegate>();

        internal List<Worker> WorkersList { get; } = new List<Worker>();

        internal Page Opener => _initializer.Opener;

        internal float DefaultTimeout
        {
            get => _defaultTimeout;
            set
            {
                _defaultTimeout = value;
                _ = _channel.SetDefaultTimeoutNoReplyAsync(value);
            }
        }

        internal float DefaultNavigationTimeout
        {
            get => _defaultNavigationTimeout;
            set
            {
                _defaultNavigationTimeout = value;
                _ = _channel.SetDefaultNavigationTimeoutNoReplyAsync(value);
            }
        }

        public IFrame Frame(string name)
            => Frames.FirstOrDefault(f => f.Name == name);

        public IFrame FrameByUrl(string urlString) => Frames.FirstOrDefault(f => urlString.UrlMatches(f.Url));

        public IFrame FrameByUrl(Regex urlRegex) => Frames.FirstOrDefault(f => urlRegex.IsMatch(f.Url));

        public IFrame FrameByUrl(Func<string, bool> urlFunc) => Frames.FirstOrDefault(f => urlFunc(f.Url));

        public Task<string> TitleAsync() => MainFrame.TitleAsync();

        public Task BringToFrontAsync() => _channel.BringToFrontAsync();

        public Task<IPage> OpenerAsync() => Task.FromResult<IPage>(Opener?.IsClosed == false ? Opener : null);

        public Task EmulateMediaAsync(Media? media, ColorScheme? colorScheme, ReducedMotion? reducedMotion)
        {
            var args = new Dictionary<string, object>();
            args["media"] = media;
            args["colorScheme"] = colorScheme;
            args["reducedMotion"] = reducedMotion;
            return _channel.EmulateMediaAsync(args);
        }

        public Task<IResponse> GotoAsync(string url, WaitUntilState? waitUntil, float? timeout, string referer)
            => MainFrame.GotoAsync(url, waitUntil, referer, timeout);

        public Task WaitForURLAsync(string urlString, float? timeout = null, WaitUntilState? waitUntil = default)
            => MainFrame.WaitForURLAsync(urlString, timeout, waitUntil);

        public Task WaitForURLAsync(Regex urlRegex, float? timeout = null, WaitUntilState? waitUntil = default)
            => MainFrame.WaitForURLAsync(urlRegex, timeout, waitUntil);

        public Task WaitForURLAsync(Func<string, bool> urlFunc, float? timeout = null, WaitUntilState? waitUntil = default)
            => MainFrame.WaitForURLAsync(urlFunc, timeout, waitUntil);

        public Task<IConsoleMessage> WaitForConsoleMessageAsync(Func<IConsoleMessage, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Console, null, predicate, timeout);

        public Task<IDownload> WaitForDownloadAsync(Func<IDownload, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Download, null, predicate, timeout);

        public Task<IFileChooser> WaitForFileChooserAsync(Func<IFileChooser, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.FileChooser, null, predicate, timeout);

        public Task<IPage> WaitForPopupAsync(Func<IPage, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Popup, null, predicate, timeout);

        public Task<IRequest> WaitForRequestFinishedAsync(Func<IRequest, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.RequestFinished, null, predicate, timeout);

        public Task<IWebSocket> WaitForWebSocketAsync(Func<IWebSocket, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.WebSocket, null, predicate, timeout);

        public Task<IWorker> WaitForWorkerAsync(Func<IWorker, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Worker, null, predicate, timeout);

        public Task<IResponse> WaitForNavigationAsync(string urlString = default, Regex urlRegex = default, Func<string, bool> urlFunc = default, WaitUntilState? waitUntil = default, float? timeout = default)
            => MainFrame.WaitForNavigationAsync(urlString, urlRegex, urlFunc, waitUntil, timeout);

        public Task<IRequest> WaitForRequestAsync(string urlOrPredicateString, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Request, null, e => e.Url.UrlMatches(urlOrPredicateString), timeout);

        public Task<IRequest> WaitForRequestAsync(Regex urlOrPredicateRegex, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Request, null, e => urlOrPredicateRegex.IsMatch(e.Url), timeout);

        public Task<IRequest> WaitForRequestAsync(Func<IRequest, bool> urlOrPredicateFunc, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Request, null, e => urlOrPredicateFunc(e), timeout);

        public Task<IResponse> WaitForResponseAsync(string urlOrPredicateString, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Response, null, e => e.Url.UrlMatches(urlOrPredicateString), timeout);

        public Task<IResponse> WaitForResponseAsync(Regex urlOrPredicateRegex, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Response, null, e => urlOrPredicateRegex.IsMatch(e.Url), timeout);

        public Task<IResponse> WaitForResponseAsync(Func<IResponse, bool> urlOrPredicateFunc, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Response, null, e => urlOrPredicateFunc(e), timeout);

        public Task<IConsoleMessage> RunAndWaitForConsoleMessageAsync(Func<Task> action, Func<IConsoleMessage, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Console, action, predicate, timeout);

        public Task<IDownload> RunAndWaitForDownloadAsync(Func<Task> action, Func<IDownload, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Download, action, predicate, timeout);

        public Task<IFileChooser> RunAndWaitForFileChooserAsync(Func<Task> action, Func<IFileChooser, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.FileChooser, action, predicate, timeout);

        public Task<IPage> RunAndWaitForPopupAsync(Func<Task> action, Func<IPage, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Popup, action, predicate, timeout);

        public Task<IRequest> RunAndWaitForRequestFinishedAsync(Func<Task> action, Func<IRequest, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.RequestFinished, action, predicate, timeout);

        public Task<IWebSocket> RunAndWaitForWebSocketAsync(Func<Task> action, Func<IWebSocket, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.WebSocket, action, predicate, timeout);

        public Task<IWorker> RunAndWaitForWorkerAsync(Func<Task> action, Func<IWorker, bool> predicate, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Worker, action, predicate, timeout);

        public Task<IResponse> RunAndWaitForNavigationAsync(Func<Task> action, string urlString = default, Regex urlRegex = default, Func<string, bool> urlFunc = default, WaitUntilState? waitUntil = default, float? timeout = default)
            => MainFrame.RunAndWaitForNavigationAsync(action, urlString, urlRegex, urlFunc, waitUntil, timeout);

        public Task<IRequest> RunAndWaitForRequestAsync(Func<Task> action, string urlOrPredicateString, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Request, action, e => e.Url.UrlMatches(urlOrPredicateString), timeout);

        public Task<IRequest> RunAndWaitForRequestAsync(Func<Task> action, Regex urlOrPredicateRegex, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Request, action, e => urlOrPredicateRegex.IsMatch(e.Url), timeout);

        public Task<IRequest> RunAndWaitForRequestAsync(Func<Task> action, Func<IRequest, bool> urlOrPredicateFunc, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Request, action, e => urlOrPredicateFunc(e), timeout);

        public Task<IResponse> RunAndWaitForResponseAsync(Func<Task> action, string urlOrPredicateString, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Response, action, e => e.Url.UrlMatches(urlOrPredicateString), timeout);

        public Task<IResponse> RunAndWaitForResponseAsync(Func<Task> action, Regex urlOrPredicateRegex, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Response, action, e => urlOrPredicateRegex.IsMatch(e.Url), timeout);

        public Task<IResponse> RunAndWaitForResponseAsync(Func<Task> action, Func<IResponse, bool> urlOrPredicateFunc, float? timeout = default)
            => InnerWaitForEventAsync(PageEvent.Response, action, e => urlOrPredicateFunc(e), timeout);

        public Task<IJSHandle> WaitForFunctionAsync(
            string expression,
            object arg,
            float? pollingInterval,
            float? timeout)
            => MainFrame.WaitForFunctionAsync(expression, arg, pollingInterval, timeout);

        public async Task<T> InnerWaitForEventAsync<T>(PlaywrightEvent<T> pageEvent, Func<Task> action = default, Func<T, bool> predicate = default, float? timeout = default)
        {
            if (pageEvent == null)
            {
                throw new ArgumentException("Page event is required", nameof(pageEvent));
            }

            timeout ??= _defaultTimeout;
            using var waiter = new Waiter(_channel, $"page.WaitForEventAsync(\"{typeof(T)}\")");
            waiter.RejectOnTimeout(Convert.ToInt32(timeout), $"Timeout while waiting for event \"{pageEvent.Name}\"");

            if (pageEvent.Name != PageEvent.Crash.Name)
            {
                waiter.RejectOnEvent<IPage>(this, PageEvent.Crash.Name, new PlaywrightException("Page crashed"));
            }

            if (pageEvent.Name != PageEvent.Close.Name)
            {
                waiter.RejectOnEvent<IPage>(this, PageEvent.Close.Name, new PlaywrightException("Page closed"));
            }

            var result = waiter.WaitForEventAsync(this, pageEvent.Name, predicate);
            if (action != null)
            {
                await Task.WhenAll(result, action()).ConfigureAwait(false);
            }

            return await result.ConfigureAwait(false);
        }

        public async Task CloseAsync(bool? runBeforeUnload)
        {
            try
            {
                await _channel.CloseAsync(runBeforeUnload ?? false).ConfigureAwait(false);
                if (OwnedContext != null)
                {
                    await OwnedContext.CloseAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e) when (IsSafeCloseException(e))
            {
                // Swallow exception
            }
        }

        public Task<T> EvaluateAsync<T>(string expression, object arg) => MainFrame.EvaluateAsync<T>(expression, arg);

        public Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAsync(selector, expression, arg);

        public Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAsync<T>(selector, expression, arg);

        public Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAllAsync(selector, expression, arg);

        public Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAllAsync<T>(selector, expression, arg);

        public Task FillAsync(string selector, string value, bool? noWaitAfter, float? timeout)
            => MainFrame.FillAsync(selector, value, noWaitAfter, timeout);

        public Task SetInputFilesAsync(string selector, string files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(selector, new[] { files }, noWaitAfter, timeout);

        public Task SetInputFilesAsync(string selector, IEnumerable<string> files, bool? noWaitAfter, float? timeout)
            => MainFrame.SetInputFilesAsync(selector, files, noWaitAfter, timeout);

        public Task SetInputFilesAsync(string selector, FilePayload files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(selector, new[] { files }, noWaitAfter, timeout);

        public Task SetInputFilesAsync(string selector, IEnumerable<FilePayload> files, bool? noWaitAfter, float? timeout)
            => MainFrame.SetInputFilesAsync(selector, files, noWaitAfter, timeout);

        public Task TypeAsync(string selector, string text, float? delay, bool? noWaitAfter, float? timeout)
            => MainFrame.TypeAsync(selector, text, delay, noWaitAfter, timeout);

        public Task FocusAsync(string selector, float? timeout) => MainFrame.FocusAsync(selector, timeout);

        public Task HoverAsync(
            string selector,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            float? timeout,
            bool? trial)
            => MainFrame.HoverAsync(selector, position, modifiers, force, timeout, trial);

        public Task PressAsync(string selector, string key, float? delay, bool? noWaitAfter, float? timeout)
            => MainFrame.PressAsync(selector, key, delay, noWaitAfter, timeout);

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, string value, bool? noWaitAfter, float? timeout)
            => MainFrame.SelectOptionAsync(selector, value, noWaitAfter, timeout);

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<string> values, bool? noWaitAfter, float? timeout)
            => MainFrame.SelectOptionAsync(selector, values, noWaitAfter, timeout);

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, SelectOptionValue value, bool? noWaitAfter, float? timeout)
            => MainFrame.SelectOptionAsync(selector, value, noWaitAfter, timeout);

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, bool? noWaitAfter, float? timeout)
            => MainFrame.SelectOptionAsync(selector, values, noWaitAfter, timeout);

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IElementHandle value, bool? noWaitAfter, float? timeout)
            => MainFrame.SelectOptionAsync(selector, value, noWaitAfter, timeout);

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, bool? noWaitAfter, float? timeout)
            => MainFrame.SelectOptionAsync(selector, values, noWaitAfter, timeout);

        public Task WaitForTimeoutAsync(float timeout) => Task.Delay(Convert.ToInt32(timeout));

        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorState? state, float? timeout)
            => MainFrame.WaitForSelectorAsync(selector, state, timeout);

        public Task<JsonElement?> EvaluateAsync(string expression, object arg) => MainFrame.EvaluateAsync(expression, arg);

        public async Task<byte[]> ScreenshotAsync(
            string path,
            ScreenshotType? type,
            int? quality,
            bool? fullPage,
            Clip clip,
            bool? omitBackground,
            float? timeout)
        {
            if (type == null && !string.IsNullOrEmpty(path))
            {
                type = ElementHandle.DetermineScreenshotType(path);
            }

            byte[] result = Convert.FromBase64String(await _channel.ScreenshotAsync(
                path,
                fullPage,
                clip,
                omitBackground,
                type,
                quality,
                Convert.ToInt32(timeout)).ConfigureAwait(false));

            if (!string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory(new FileInfo(path).Directory.FullName);
                File.WriteAllBytes(path, result);
            }

            return result;
        }

        public Task SetContentAsync(string html, float? timeout, WaitUntilState? waitUntil)
            => MainFrame.SetContentAsync(html, waitUntil, timeout);

        public Task<string> ContentAsync() => MainFrame.ContentAsync();

        public Task SetExtraHTTPHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers)
            => _channel.SetExtraHTTPHeadersAsync(headers);

        public Task<IElementHandle> QuerySelectorAsync(string selector) => MainFrame.QuerySelectorAsync(selector);

        public Task<IReadOnlyList<IElementHandle>> QuerySelectorAllAsync(string selector)
            => MainFrame.QuerySelectorAllAsync(selector);

        public Task<IJSHandle> EvaluateHandleAsync(string expression, object arg) => MainFrame.EvaluateHandleAsync(expression, arg);

        public Task<IElementHandle> AddScriptTagAsync(string url, string path, string content, string type)
            => MainFrame.AddScriptTagAsync(url, path, content, type);

        public Task<IElementHandle> AddStyleTagAsync(string url, string path, string content)
            => MainFrame.AddStyleTagAsync(url, path, content);

        public Task ClickAsync(
            string selector,
            MouseButton? button,
            int? clickCount,
            float? delay,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            bool? noWaitAfter,
            float? timeout,
            bool? trial)
            => MainFrame.ClickAsync(selector, delay, button, clickCount, modifiers, position, timeout, force, noWaitAfter, trial);

        public Task DblClickAsync(
            string selector,
            MouseButton? button,
            float? delay,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            bool? noWaitAfter,
            float? timeout,
            bool? trial)
            => MainFrame.DblClickAsync(selector, delay, button, position, modifiers, timeout, force, noWaitAfter, trial);

        public async Task<IResponse> GoBackAsync(WaitUntilState? waitUntil, float? timeout)
            => (await _channel.GoBackAsync(timeout, waitUntil).ConfigureAwait(false))?.Object;

        public async Task<IResponse> GoForwardAsync(WaitUntilState? waitUntil, float? timeout)
            => (await _channel.GoForwardAsync(timeout, waitUntil).ConfigureAwait(false))?.Object;

        public async Task<IResponse> ReloadAsync(WaitUntilState? waitUntil, float? timeout)
            => (await _channel.ReloadAsync(timeout, waitUntil).ConfigureAwait(false))?.Object;

        public Task ExposeBindingAsync(string name, Action<BindingSource> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, IJSHandle, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback, true);

        public Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync(string name, Action callback, bool? handle = default)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeFunctionAsync(string name, Action callback)
            => ExposeBindingAsync(name, (BindingSource _) => callback());

        public Task ExposeFunctionAsync<T>(string name, Action<T> callback)
            => ExposeBindingAsync(name, (BindingSource _, T t) => callback(t));

        public Task ExposeFunctionAsync<TResult>(string name, Func<TResult> callback)
            => ExposeBindingAsync(name, (BindingSource _) => callback());

        public Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> callback)
            => ExposeBindingAsync(name, (BindingSource _, T t) => callback(t));

        public Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> callback)
            => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2) => callback(t1, t2));

        public Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> callback)
            => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2, T3 t3) => callback(t1, t2, t3));

        public Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> callback)
            => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2, T3 t3, T4 t4) => callback(t1, t2, t3, t4));

        public async Task<byte[]> PdfAsync(
            string path,
            float? scale,
            bool? displayHeaderFooter,
            string headerTemplate,
            string footerTemplate,
            bool? printBackground,
            bool? landscape,
            string pageRanges,
            string format,
            string width,
            string height,
            Margin margin,
            bool? preferCSSPageSize)
        {
            if (!Context.IsChromium)
            {
                throw new NotSupportedException("This browser doesn't support this action.");
            }

            byte[] result = Convert.FromBase64String(await _channel.PdfAsync(
                scale,
                displayHeaderFooter,
                headerTemplate,
                footerTemplate,
                printBackground,
                landscape,
                pageRanges,
                format,
                width,
                height,
                margin,
                preferCSSPageSize).ConfigureAwait(false));

            if (!string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory(new FileInfo(path).Directory.FullName);
                File.WriteAllBytes(path, result);
            }

            return result;
        }

        public Task AddInitScriptAsync(string script, string scriptPath)
            => _channel.AddInitScriptAsync(ScriptsHelper.EvaluationScript(script, scriptPath));

        public Task RouteAsync(string urlString, Action<IRoute> handler)
            => RouteAsync(
                new RouteSetting
                {
                    Url = urlString,
                    Handler = handler,
                });

        public Task RouteAsync(Regex urlRegex, Action<IRoute> handler)
            => RouteAsync(
                new RouteSetting
                {
                    Regex = urlRegex,
                    Handler = handler,
                });

        public Task RouteAsync(Func<string, bool> urlFunc, Action<IRoute> handler)
            => RouteAsync(
                new RouteSetting
                {
                    Function = urlFunc,
                    Handler = handler,
                });

        public Task UnrouteAsync(string urlString, Action<IRoute> handler)
            => UnrouteAsync(
                new RouteSetting
                {
                    Url = urlString,
                    Handler = handler,
                });

        public Task UnrouteAsync(Regex urlString, Action<IRoute> handler)
            => UnrouteAsync(
                new RouteSetting
                {
                    Regex = urlString,
                    Handler = handler,
                });

        public Task UnrouteAsync(Func<string, bool> urlFunc, Action<IRoute> handler)
            => UnrouteAsync(
                new RouteSetting
                {
                    Function = urlFunc,
                    Handler = handler,
                });

        public Task WaitForLoadStateAsync(LoadState? state, float? timeout)
            => MainFrame.WaitForLoadStateAsync(state, timeout);

        public Task SetViewportSizeAsync(int width, int height)
        {
            ViewportSize = new PageViewportSizeResult { Width = width, Height = height };
            return _channel.SetViewportSizeAsync(ViewportSize);
        }

        public Task CheckAsync(string selector, Position position, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => MainFrame.CheckAsync(selector, position, force, noWaitAfter, timeout, trial);

        public Task UncheckAsync(string selector, Position position, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => MainFrame.UncheckAsync(selector, position, force, noWaitAfter, timeout, trial);

        public Task DispatchEventAsync(string selector, string type, object eventInit, float? timeout)
             => MainFrame.DispatchEventAsync(selector, type, eventInit, timeout);

        public Task<string> GetAttributeAsync(string selector, string name, float? timeout)
             => MainFrame.GetAttributeAsync(selector, name, timeout);

        public Task<string> InnerHTMLAsync(string selector, float? timeout)
             => MainFrame.InnerHTMLAsync(selector, timeout);

        public Task<string> InnerTextAsync(string selector, float? timeout)
             => MainFrame.InnerTextAsync(selector, timeout);

        public Task<string> TextContentAsync(string selector, float? timeout)
             => MainFrame.TextContentAsync(selector, timeout);

        public Task TapAsync(string selector, Position position, IEnumerable<KeyboardModifier> modifiers, bool? noWaitAfter, bool? force, float? timeout, bool? trial)
            => MainFrame.TapAsync(selector, modifiers, position, force, noWaitAfter, timeout, trial);

        public Task<bool> IsCheckedAsync(string selector, float? timeout) => MainFrame.IsCheckedAsync(selector, timeout);

        public Task<bool> IsDisabledAsync(string selector, float? timeout) => MainFrame.IsDisabledAsync(selector, timeout);

        public Task<bool> IsEditableAsync(string selector, float? timeout) => MainFrame.IsEditableAsync(selector, timeout);

        public Task<bool> IsEnabledAsync(string selector, float? timeout) => MainFrame.IsEnabledAsync(selector, timeout);

        public Task<bool> IsHiddenAsync(string selector, float? timeout) => MainFrame.IsHiddenAsync(selector, timeout);

        public Task<bool> IsVisibleAsync(string selector, float? timeout) => MainFrame.IsVisibleAsync(selector, timeout);

        public Task PauseAsync() => Context.Channel.PauseAsync();

        public void SetDefaultNavigationTimeout(float timeout) => DefaultNavigationTimeout = timeout;

        public void SetDefaultTimeout(float timeout) => DefaultTimeout = timeout;

        internal void NotifyPopup(Page page) => Popup?.Invoke(this, page);

        internal void OnFrameNavigated(Frame frame)
            => FrameNavigated?.Invoke(this, frame);

        internal void FireRequest(IRequest request) => Request?.Invoke(this, request);

        internal void FireRequestFailed(IRequest request) => RequestFailed?.Invoke(this, request);

        internal void FireRequestFinished(IRequest request) => RequestFinished?.Invoke(this, request);

        internal void FireResponse(IResponse response) => Response?.Invoke(this, response);

        private Task RouteAsync(RouteSetting setting)
        {
            _routes.Add(setting);

            if (_routes.Count == 1)
            {
                return _channel.SetNetworkInterceptionEnabledAsync(true);
            }

            return Task.CompletedTask;
        }

        private Task UnrouteAsync(RouteSetting setting)
        {
            var newRoutesList = new List<RouteSetting>();
            newRoutesList.AddRange(_routes.Where(r =>
                (setting.Url != null && r.Url != setting.Url) ||
                (setting.Regex != null && r.Regex != setting.Regex) ||
                (setting.Function != null && r.Function != setting.Function) ||
                (setting.Handler != null && r.Handler != setting.Handler)));
            _routes = newRoutesList;

            if (_routes.Count == 0)
            {
                return _channel.SetNetworkInterceptionEnabledAsync(false);
            }

            return Task.CompletedTask;
        }

        private bool IsSafeCloseException(Exception e)
            => e.Message.Contains(DriverMessages.BrowserClosedExceptionMessage) ||
               e.Message.Contains(DriverMessages.BrowserOrContextClosedExceptionMessage);

        private void Channel_Closed(object sender, EventArgs e)
        {
            IsClosed = true;
            Context?.PagesList.Remove(this);
            RejectPendingOperations(false);
            Close?.Invoke(this, this);
        }

        private void Channel_Crashed(object sender, EventArgs e)
        {
            RejectPendingOperations(true);
            Crash?.Invoke(this, this);
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
                    (route.Regex?.IsMatch(e.Request.Url) == true) ||
                    (route.Function?.Invoke(e.Request.Url) == true))
                {
                    route.Handler(e.Route);
                    return;
                }
            }

            Context.OnRoute(e.Route, e.Request);
        }

        private void Channel_FrameDetached(object sender, IFrame args)
        {
            var frame = (Frame)args;
            _frames.Remove(frame);
            frame.IsDetached = true;
            frame.ParentFrame?.ChildFramesList?.Remove(frame);
            FrameDetached?.Invoke(this, args);
        }

        private void Channel_FrameAttached(object sender, IFrame args)
        {
            var frame = (Frame)args;
            frame.Page = this;
            _frames.Add(frame);
            frame.ParentFrame?.ChildFramesList?.Add(frame);
            FrameAttached?.Invoke(this, args);
        }

        private void RejectPendingOperations(bool isCrash)
        {
            foreach (var (_, waitTcs) in _waitForCancellationTcs.Where(e => e.PageEvent != (isCrash ? PageEvent.Crash : PageEvent.Close)))
            {
                waitTcs.TrySetException(new PlaywrightException(isCrash ? "Page crashed" : "Page closed"));
            }

            _waitForCancellationTcs.Clear();
        }

        private Task ExposeBindingAsync(string name, Delegate callback, bool handle = false)
        {
            if (Bindings.ContainsKey(name))
            {
                throw new PlaywrightException($"Function \"{name}\" has been already registered");
            }

            Bindings.Add(name, callback);

            return _channel.ExposeBindingAsync(name, handle);
        }

        private Video ForceVideo() => _video ??= new Video(this);
    }
}
