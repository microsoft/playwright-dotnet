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

namespace Microsoft.Playwright
{
    /// <inheritdoc cref="IPage" />
    public class Page : ChannelOwnerBase, IChannelOwner<Page>, IPage
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

            MainFrame = initializer.MainFrame.Object;
            MainFrame.Page = this;
            _frames.Add(MainFrame);
            ViewportSize = initializer.ViewportSize;
            IsClosed = initializer.IsClosed;
            _accessibility = new Accesibility(_channel);
            _keyboard = new Keyboard(_channel);
            _touchscreen = new Touchscreen(_channel);
            _mouse = new Mouse(_channel);
            _channel.Closed += Channel_Closed;
            _channel.Crashed += Channel_Crashed;
            _channel.Popup += (_, e) => Popup?.Invoke(this, e.Page);
            _channel.RequestFailed += (_, e) =>
            {
                e.Request.Object.Failure = e.FailureText;
                e.Request.Object.Timing.ResponseEnd = e.ResponseEndTiming;
                RequestFailed?.Invoke(this, e.Request.Object);
            };

            _channel.Request += (_, e) => Request?.Invoke(this, e);
            _channel.RequestFinished += (_, e) =>
            {
                e.Request.Object.Timing.ResponseEnd = e.ResponseEndTiming;
                RequestFinished?.Invoke(this, e.Request.Object);
            };
            _channel.Response += (_, e) => Response?.Invoke(this, e);
            _channel.WebSocket += (_, e) => WebSocket?.Invoke(this, e);
            _channel.BindingCall += Channel_BindingCall;
            _channel.Route += Channel_Route;
            _channel.FrameAttached += Channel_FrameAttached;
            _channel.FrameDetached += Channel_FrameDetached;
            _channel.Dialog += (_, e) => Dialog?.Invoke(this, e);
            _channel.Console += (_, e) => Console?.Invoke(this, e);
            _channel.DOMContentLoaded += (_, e) => DOMContentLoaded?.Invoke(this, this);
            _channel.Download += (_, e) => Download?.Invoke(this, new Download(e.Url, e.SuggestedFilename, e.Artifact.Object));
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

        /// <inheritdoc />
        public event EventHandler<IConsoleMessage> Console;

        /// <inheritdoc />
        public event EventHandler<IPage> Popup;

        /// <inheritdoc />
        public event EventHandler<IRequest> Request;

        /// <inheritdoc />
        public event EventHandler<IWebSocket> WebSocket;

        /// <inheritdoc />
        public event EventHandler<IResponse> Response;

        /// <inheritdoc />
        public event EventHandler<IRequest> RequestFinished;

        /// <inheritdoc />
        public event EventHandler<IRequest> RequestFailed;

        /// <inheritdoc />
        public event EventHandler<IDialog> Dialog;

        /// <inheritdoc />
        public event EventHandler<IFrame> FrameAttached;

        /// <inheritdoc />
        public event EventHandler<IFrame> FrameDetached;

        /// <inheritdoc />
        public event EventHandler<IFrame> FrameNavigated;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public event EventHandler<IPage> Load;

        /// <inheritdoc />
        public event EventHandler<IPage> DOMContentLoaded;

        /// <inheritdoc />
        public event EventHandler<IPage> Close;

        /// <inheritdoc />
        public event EventHandler<IPage> Crash;

        /// <inheritdoc />
        public event EventHandler<string> PageError;

        /// <inheritdoc />
        public event EventHandler<IWorker> Worker;

        /// <inheritdoc />
        public event EventHandler<IDownload> Download;

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
        IBrowserContext IPage.Context => Context;

        /// <inheritdoc cref="IPage.Context" />
        public BrowserContext Context { get; set; }

        /// <inheritdoc />
        public PageViewportSizeResult ViewportSize { get; private set; }

        /// <inheritdoc />
        public IAccessibility Accessibility
        {
            get => _accessibility;
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        /// <inheritdoc />
        public IMouse Mouse
        {
            get => _mouse;
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public string Url => MainFrame.Url;

        /// <inheritdoc />
        public IReadOnlyCollection<IFrame> Frames => _frames.AsReadOnly();

        /// <inheritdoc />
        public IKeyboard Keyboard
        {
            get => _keyboard;
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public ITouchscreen Touchscreen
        {
            get => _touchscreen;
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public float DefaultTimeout
        {
            get
            {
                return _defaultTimeout;
            }

            set
            {
                _defaultTimeout = value;
                _ = _channel.SetDefaultTimeoutNoReplyAsync(value);
            }
        }

        /// <inheritdoc/>
        public float DefaultNavigationTimeout
        {
            get
            {
                return _defaultNavigationTimeout;
            }

            set
            {
                _defaultNavigationTimeout = value;
                _ = _channel.SetDefaultNavigationTimeoutNoReplyAsync(value);
            }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IWorker> Workers => WorkersList;

        /// <inheritdoc />
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

        internal Page Opener => _initializer.Opener?.Object;

        /// <inheritdoc />
        public IFrame Frame(string name)
            => Frames.FirstOrDefault(f => f.Name == name);

        /// <inheritdoc />
        public IFrame FrameByUrl(string urlString) => Frames.FirstOrDefault(f => urlString.UrlMatches(f.Url));

        /// <inheritdoc />
        public IFrame FrameByUrl(Regex urlRegex) => Frames.FirstOrDefault(f => urlRegex.IsMatch(f.Url));

        /// <inheritdoc />
        public IFrame FrameByUrl(Func<string, bool> urlFunc) => Frames.FirstOrDefault(f => urlFunc(f.Url));

        /// <inheritdoc />
        public Task<string> TitleAsync() => MainFrame.TitleAsync();

        /// <inheritdoc />
        public Task BringToFrontAsync() => _channel.BringToFrontAsync();

        /// <inheritdoc />
        public Task<IPage> OpenerAsync() => Task.FromResult<IPage>(Opener?.IsClosed == false ? Opener : null);

        /// <inheritdoc />
        public Task EmulateMediaAsync(ColorScheme? colorScheme) => EmulateMediaAsync(null, colorScheme);

        /// <inheritdoc />
        public Task EmulateMediaAsync(Media? media, ColorScheme? colorScheme)
        {
            var args = new Dictionary<string, object>();

            if (media != null)
            {
                args["media"] = media == Media.Undefined ? "null" : media;
            }

            if (colorScheme != null)
            {
                args["colorScheme"] = colorScheme == ColorScheme.Undefined ? "null" : colorScheme;
            }

            return _channel.EmulateMediaAsync(args);
        }

        /// <inheritdoc />
        public Task<IResponse> GoToAsync(string url, WaitUntilState waitUntil, float? timeout, string referer)
            => MainFrame.GoToAsync(true, url, waitUntil.EnsureDefaultValue(WaitUntilState.Load), referer, timeout);

        /// <inheritdoc />
        public Task WaitForURLAsync(string urlString, float? timeout = null, WaitUntilState waitUntil = WaitUntilState.Undefined)
            => MainFrame.WaitForURLAsync(urlString, timeout, waitUntil);

        /// <inheritdoc />
        public Task WaitForURLAsync(Regex urlRegex, float? timeout = null, WaitUntilState waitUntil = WaitUntilState.Undefined)
            => MainFrame.WaitForURLAsync(urlRegex, timeout, waitUntil);

        /// <inheritdoc />
        public Task WaitForURLAsync(Func<string, bool> urlFunc, float? timeout = null, WaitUntilState waitUntil = WaitUntilState.Undefined)
            => MainFrame.WaitForURLAsync(urlFunc, timeout, waitUntil);

        /// <inheritdoc/>
        public Task<IResponse> WaitForNavigationAsync(string urlString, WaitUntilState waitUntil = default, float? timeout = default)
            => MainFrame.WaitForNavigationAsync(urlString, null, null, waitUntil, timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(WaitUntilState waitUntil, float? timeout)
             => MainFrame.WaitForNavigationAsync(urlString: null, urlRegex: null, urlFunc: null, waitUntil: waitUntil, timeout: timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(Regex urlRegex, WaitUntilState waitUntil, float? timeout)
            => MainFrame.WaitForNavigationAsync(urlString: null, urlRegex: urlRegex, urlFunc: null, waitUntil: default, timeout: timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(Func<string, bool> urlFunc, WaitUntilState waitUntil, float? timeout)
            => MainFrame.WaitForNavigationAsync(urlString: null, urlRegex: null, urlFunc: urlFunc, waitUntil: default, timeout: timeout);

        /// <inheritdoc />
        public Task<IRequest> WaitForRequestAsync(string urlOrPredicateString, float? timeout)
            => WaitForEventAsync(PageEvent.Request, e => e.Url.Equals(urlOrPredicateString, StringComparison.Ordinal), timeout);

        /// <inheritdoc />
        public Task<IRequest> WaitForRequestAsync(Regex urlOrPredicateRegex, float? timeout)
            => WaitForEventAsync(PageEvent.Request, e => urlOrPredicateRegex.IsMatch(e.Url), timeout);

        /// <inheritdoc />
        public Task<IRequest> WaitForRequestAsync(Func<IRequest, bool> urlOrPredicateFunc, float? timeout)
            => WaitForEventAsync(PageEvent.Request, e => urlOrPredicateFunc(e), timeout);

        /// <inheritdoc />
        public Task<IPage> WaitForCloseAsync(float? timeout)
            => WaitForEventAsync(PageEvent.Close, null, timeout);

        /// <inheritdoc />
        public Task<IConsoleMessage> WaitForConsoleMessageAsync(Func<IConsoleMessage, bool> predicate, float? timeout)
            => WaitForEventAsync(PageEvent.Console, predicate, timeout);

        /// <inheritdoc />
        public Task<IFileChooser> WaitForFileChooserAsync(Func<IFileChooser, bool> predicate, float? timeout)
            => WaitForEventAsync(PageEvent.FileChooser, predicate, timeout);

        /// <inheritdoc />
        public Task<IPage> WaitForPopupAsync(Func<IPage, bool> predicate, float? timeout)
            => WaitForEventAsync(PageEvent.Popup, predicate, timeout);

        /// <inheritdoc />
        public Task<IDownload> WaitForDownloadAsync(Func<IDownload, bool> predicate, float? timeout)
            => WaitForEventAsync(PageEvent.Download, predicate, timeout);

        /// <inheritdoc />
        public Task<IWebSocket> WaitForWebSocketAsync(Func<IWebSocket, bool> predicate, float? timeout)
            => WaitForEventAsync(PageEvent.WebSocket, predicate, timeout);

        /// <inheritdoc />
        public Task<IWorker> WaitForWorkerAsync(Func<IWorker, bool> predicate, float? timeout)
            => WaitForEventAsync(PageEvent.Worker, predicate, timeout);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(
            string expression,
            object arg,
            float? pollingInterval,
            float? timeout)
            => MainFrame.WaitForFunctionAsync(true, expression, arg, pollingInterval, timeout);

        /// <inheritdoc />
        public async Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> pageEvent, Func<T, bool> predicate, float? timeout)
        {
            if (pageEvent == null)
            {
                throw new ArgumentException("Page event is required", nameof(pageEvent));
            }

            timeout ??= _defaultTimeout;
            using var waiter = new Waiter();
            waiter.RejectOnTimeout(Convert.ToInt32(timeout), $"Timeout while waiting for event \"{typeof(T)}\"");

            if (pageEvent.Name != PageEvent.Crash.Name)
            {
                waiter.RejectOnEvent<IPage>(this, PageEvent.Crash.Name, new TargetClosedException("Page crashed"));
            }

            if (pageEvent.Name != PageEvent.Close.Name)
            {
                waiter.RejectOnEvent<IPage>(this, PageEvent.Close.Name, new TargetClosedException("Page closed"));
            }

            return await waiter.WaitForEventAsync(this, pageEvent.Name, predicate).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<object> WaitForEventAsync(string @event, float? timeout)
        {
            if (string.IsNullOrEmpty(@event))
            {
                throw new ArgumentException("Page event is required", nameof(@event));
            }

            timeout ??= _defaultTimeout;
            using var waiter = new Waiter();
            waiter.RejectOnTimeout(Convert.ToInt32(timeout), $"Timeout while waiting for event \"{@event}\"");

            if (@event != PageEvent.Crash.Name)
            {
                waiter.RejectOnEvent<EventArgs>(this, PageEvent.Crash.Name, new TargetClosedException("Page crashed"));
            }

            if (@event != PageEvent.Close.Name)
            {
                waiter.RejectOnEvent<EventArgs>(this, PageEvent.Close.Name, new TargetClosedException("Page closed"));
            }

            return await waiter.WaitForEventAsync(this, @event).ConfigureAwait(false);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string expression, object arg) => MainFrame.EvaluateAsync<T>(true, expression, arg);

        /// <inheritdoc />
        public Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAsync(true, selector, expression, arg);

        /// <inheritdoc />
        public Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAsync<T>(true, selector, expression, arg);

        /// <inheritdoc />
        public Task EvalOnSelectorAllAsync(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAllAsync(true, selector, expression, arg);

        /// <inheritdoc />
        public Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAllAsync<T>(true, selector, expression, arg);

        /// <inheritdoc />
        public Task FillAsync(string selector, string value, bool? noWaitAfter, float? timeout)
            => MainFrame.FillAsync(true, selector, value, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, string files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(selector, new[] { files }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, IEnumerable<string> files, bool? noWaitAfter, float? timeout)
            => MainFrame.SetInputFilesAsync(true, selector, files, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, FilePayload files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(selector, new[] { files }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, IEnumerable<FilePayload> files, bool? noWaitAfter, float? timeout)
            => MainFrame.SetInputFilesAsync(true, selector, files, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task TypeAsync(string selector, string text, float? delay, bool? noWaitAfter, float? timeout)
            => MainFrame.TypeAsync(true, selector, text, delay, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task FocusAsync(string selector, float? timeout) => MainFrame.FocusAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task HoverAsync(
            string selector,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            float? timeout,
            bool? trial)
            => MainFrame.HoverAsync(true, selector, position, modifiers, force ?? false, timeout, trial);

        /// <inheritdoc />
        public Task PressAsync(string selector, string key, float? delay, bool? noWaitAfter, float? timeout)
            => MainFrame.PressAsync(true, selector, key, delay, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, bool? noWaitAfter, float? timeout)
            => MainFrame.SelectOptionAsync(true, selector, null, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, string values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(selector, new[] { values }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, SelectOptionValue values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(selector, new[] { values }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IElementHandle values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(selector, new[] { values }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<string> values, bool? noWaitAfter, float? timeout)
            => MainFrame.SelectOptionAsync(true, selector, values.Cast<object>().Select(v => v == null ? v : new { value = v }).ToArray(), noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, bool? noWaitAfter, float? timeout)
        {
            if (values == null)
            {
                throw new ArgumentException("values should not be null", nameof(values));
            }

            return MainFrame.SelectOptionAsync(true, selector, values.ToArray(), noWaitAfter, timeout);
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, bool? noWaitAfter, float? timeout)
            => MainFrame.SelectOptionAsync(true, selector, values.Cast<ElementHandle>().ToArray(), noWaitAfter, timeout);

        /// <inheritdoc />
        public Task WaitForTimeoutAsync(float timeout) => Task.Delay(Convert.ToInt32(timeout));

        /// <inheritdoc />
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorState state, float? timeout)
            => MainFrame.WaitForSelectorAsync(true, selector, state.EnsureDefaultValue(WaitForSelectorState.Visible), timeout);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string expression, object arg) => MainFrame.EvaluateAsync(true, expression, arg);

        /// <inheritdoc />
        public async Task<byte[]> ScreenshotAsync(
            string path,
            ScreenshotType type,
            int? quality,
            bool? fullPage,
            Clip clip,
            bool? omitBackground,
            float? timeout)
        {
            type = !string.IsNullOrEmpty(path) ? ElementHandle.DetermineScreenshotType(path) : type.EnsureDefaultValue(ScreenshotType.Png);
            byte[] result = Convert.FromBase64String(await _channel.ScreenshotAsync(
                path,
                fullPage ?? false,
                clip,
                omitBackground ?? false,
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

        /// <inheritdoc />
        public Task SetContentAsync(string html, float? timeout, WaitUntilState waitUntil)
            => MainFrame.SetContentAsync(true, html, waitUntil.EnsureDefaultValue(WaitUntilState.Load), timeout);

        /// <inheritdoc />
        public Task<string> ContentAsync() => MainFrame.ContentAsync(true);

        /// <inheritdoc />
        public Task SetExtraHttpHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers)
            => _channel.SetExtraHttpHeadersAsync(headers);

        /// <inheritdoc />
        public Task<IElementHandle> QuerySelectorAsync(string selector) => MainFrame.QuerySelectorAsync(true, selector);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<IElementHandle>> QuerySelectorAllAsync(string selector)
            => MainFrame.QuerySelectorAllAsync(true, selector);

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string expression, object arg) => MainFrame.EvaluateHandleAsync(expression, arg);

        /// <inheritdoc />
        public Task<IElementHandle> AddScriptTagAsync(string url, string path, string content, string type)
            => MainFrame.AddScriptTagAsync(true, url, path, content, type);

        /// <inheritdoc />
        public Task<IElementHandle> AddStyleTagAsync(string url, string path, string content)
            => MainFrame.AddStyleTagAsync(true, url, path, content);

        /// <inheritdoc />
        public Task ClickAsync(
            string selector,
            MouseButton button,
            int? clickCount,
            float? delay,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            bool? noWaitAfter,
            float? timeout,
            bool? trial)
            => MainFrame.ClickAsync(true, selector, delay ?? 0, button.EnsureDefaultValue(MouseButton.Left), clickCount ?? 1, modifiers, position, timeout, force ?? false, noWaitAfter, trial);

        /// <inheritdoc />
        public Task DblClickAsync(
            string selector,
            MouseButton button,
            float? delay,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            bool? noWaitAfter,
            float? timeout,
            bool? trial)
            => MainFrame.DblClickAsync(true, selector, delay ?? 0, button.EnsureDefaultValue(MouseButton.Left), position, modifiers, timeout, force ?? false, noWaitAfter, trial);

        /// <inheritdoc />
        public async Task<IResponse> GoBackAsync(WaitUntilState waitUntil, float? timeout)
            => (await _channel.GoBackAsync(timeout, waitUntil.EnsureDefaultValue(WaitUntilState.Load)).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IResponse> GoForwardAsync(WaitUntilState waitUntil, float? timeout)
            => (await _channel.GoForwardAsync(timeout, waitUntil.EnsureDefaultValue(WaitUntilState.Load)).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IResponse> ReloadAsync(WaitUntilState waitUntil, float? timeout)
            => (await _channel.ReloadAsync(timeout, waitUntil.EnsureDefaultValue(WaitUntilState.Load)).ConfigureAwait(false))?.Object;

        /// <inheritdoc/>
        public Task ExposeBindingAsync(string name, Action<BindingSource> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, IJSHandle, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback, true);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        /// <inheritdoc/>
        public Task ExposeBindingAsync(string name, Action callback, bool? handle = default)
            => ExposeBindingAsync(name, (Delegate)callback);

        /// <inheritdoc/>
        public Task ExposeFunctionAsync(string name, Action callback)
            => ExposeBindingAsync(name, (BindingSource _) => callback());

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T>(string name, Action<T> callback)
            => ExposeBindingAsync(name, (BindingSource _, T t) => callback(t));

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<TResult>(string name, Func<TResult> callback)
            => ExposeBindingAsync(name, (BindingSource _) => callback());

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> callback)
            => ExposeBindingAsync(name, (BindingSource _, T t) => callback(t));

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> callback)
            => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2) => callback(t1, t2));

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> callback)
            => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2, T3 t3) => callback(t1, t2, t3));

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> callback)
            => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2, T3 t3, T4 t4) => callback(t1, t2, t3, t4));

        /// <inheritdoc />
        public Task<IResponse> WaitForResponseAsync(string urlOrPredicateString, float? timeout)
            => WaitForEventAsync(PageEvent.Response, e => e.Url.Equals(urlOrPredicateString, StringComparison.Ordinal), timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForResponseAsync(Regex urlOrPredicateRegex, float? timeout)
            => WaitForEventAsync(PageEvent.Response, e => urlOrPredicateRegex.IsMatch(e.Url), timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForResponseAsync(Func<IResponse, bool> urlOrPredicateFunc, float? timeout)
            => WaitForEventAsync(PageEvent.Response, e => urlOrPredicateFunc(e), timeout);

        /// <inheritdoc />
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
                scale ?? 1,
                displayHeaderFooter ?? false,
                headerTemplate,
                footerTemplate,
                printBackground ?? false,
                landscape ?? false,
                pageRanges,
                format,
                width,
                height,
                margin,
                preferCSSPageSize ?? false).ConfigureAwait(false));

            if (!string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory(new FileInfo(path).Directory.FullName);
                File.WriteAllBytes(path, result);
            }

            return result;
        }

        /// <inheritdoc />
        public Task AddInitScriptAsync(string script, string scriptPath)
            => _channel.AddInitScriptAsync(ScriptsHelper.SerializeScriptCall(ScriptsHelper.EvaluationScript(script, scriptPath)));

        /// <inheritdoc />
        public Task RouteAsync(string urlString, Action<IRoute> handler)
            => RouteAsync(
                new RouteSetting
                {
                    Url = urlString,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task RouteAsync(Regex urlRegex, Action<IRoute> handler)
            => RouteAsync(
                new RouteSetting
                {
                    Regex = urlRegex,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task RouteAsync(Func<string, bool> urlFunc, Action<IRoute> handler)
            => RouteAsync(
                new RouteSetting
                {
                    Function = urlFunc,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task UnrouteAsync(string urlString, Action<IRoute> handler)
            => UnrouteAsync(
                new RouteSetting
                {
                    Url = urlString,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task UnrouteAsync(Regex urlString, Action<IRoute> handler)
            => UnrouteAsync(
                new RouteSetting
                {
                    Regex = urlString,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task UnrouteAsync(Func<string, bool> urlFunc, Action<IRoute> handler)
            => UnrouteAsync(
                new RouteSetting
                {
                    Function = urlFunc,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task WaitForLoadStateAsync(LoadState state, float? timeout)
            => MainFrame.WaitForLoadStateAsync(state, timeout);

        /// <inheritdoc />
        public Task SetViewportSizeAsync(int width, int height)
        {
            ViewportSize = new PageViewportSizeResult { Width = width, Height = height };
            return _channel.SetViewportSizeAsync(ViewportSize);
        }

        /// <inheritdoc />
        public Task CheckAsync(string selector, Position position, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => MainFrame.CheckAsync(true, selector, position, force, noWaitAfter, timeout, trial);

        /// <inheritdoc />
        public Task UncheckAsync(string selector, Position position, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => MainFrame.UncheckAsync(true, selector, position, force, noWaitAfter, timeout, trial);

        /// <inheritdoc />
        public Task DispatchEventAsync(string selector, string type, object eventInit, float? timeout)
             => MainFrame.DispatchEventAsync(true, selector, type, eventInit, timeout);

        /// <inheritdoc />
        public Task<string> GetAttributeAsync(string selector, string name, float? timeout)
             => MainFrame.GetAttributeAsync(true, selector, name, timeout);

        /// <inheritdoc />
        public Task<string> InnerHTMLAsync(string selector, float? timeout)
             => MainFrame.InnerHTMLAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task<string> InnerTextAsync(string selector, float? timeout)
             => MainFrame.InnerTextAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task<string> TextContentAsync(string selector, float? timeout)
             => MainFrame.GetTextContentAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task TapAsync(string selector, Position position, IEnumerable<KeyboardModifier> modifiers, bool? noWaitAfter, bool? force, float? timeout, bool? trial)
            => MainFrame.TapAsync(true, selector, modifiers, position, force, noWaitAfter, timeout, trial);

        /// <inheritdoc />
        public Task<bool> IsCheckedAsync(string selector, float? timeout) => MainFrame.IsCheckedAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task<bool> IsDisabledAsync(string selector, float? timeout) => MainFrame.IsDisabledAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task<bool> IsEditableAsync(string selector, float? timeout) => MainFrame.IsEditableAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task<bool> IsEnabledAsync(string selector, float? timeout) => MainFrame.IsEnabledAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task<bool> IsHiddenAsync(string selector, float? timeout) => MainFrame.IsHiddenAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task<bool> IsVisibleAsync(string selector, float? timeout) => MainFrame.IsVisibleAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task PauseAsync() => _channel.PauseAsync();

        internal void NotifyPopup(Page page) => Popup?.Invoke(this, page);

        internal void OnFrameNavigated(Frame frame)
            => FrameNavigated?.Invoke(this, frame);

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
                waitTcs.TrySetException(new TargetClosedException(isCrash ? "Page crashed" : "Page closed"));
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
