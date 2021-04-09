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
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IPage" />
    public class Page : ChannelOwnerBase, IChannelOwner<Page>, IPage
    {
        private readonly PageChannel _channel;
        private readonly List<Frame> _frames = new();
        private readonly List<(IEvent PageEvent, TaskCompletionSource<bool> WaitTcs)> _waitForCancellationTcs = new();
        private readonly object _fileChooserEventLock = new();
        private readonly IAccessibility _accessibility;

        private List<RouteSetting> _routes = new();
        private EventHandler<IFileChooser> _fileChooserEventHandler;
        private bool _fileChooserIntercepted;
        private IVideo _video;

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
            Keyboard = new Keyboard(_channel);
            Touchscreen = new Touchscreen(_channel);
            Mouse = new Mouse(_channel);
            TimeoutSettings = new TimeoutSettings(Context.TimeoutSettings);

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
            _channel.DOMContentLoaded += (_, e) => DOMContentLoaded?.Invoke(this, e);
            _channel.Download += (_, e) => Download?.Invoke(this, e);
            _channel.PageError += (_, e) => PageError?.Invoke(this, e);
            _channel.Load += (_, e) => Load?.Invoke(this, e);
            _channel.Video += (_, e) =>
            {
                if (Video != null)
                {
                    ((Video)Video).SetRelativePath(e.RelativePath);
                }
            };

            _channel.FileChooser += (_, e) => _fileChooserEventHandler?.Invoke(this, new FileChooser(this, e.Element.Object, e.IsMultiple));
            _channel.Worker += (_, e) =>
            {
                WorkersList.Add(e.WorkerChannel.Object);
                e.WorkerChannel.Object.Page = this;
                Worker?.Invoke(this, e.WorkerChannel.Object);
            };
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
        public IMouse Mouse { get; }

        /// <inheritdoc />
        public string Url => MainFrame.Url;

        /// <inheritdoc />
        public IReadOnlyCollection<IFrame> Frames => _frames.AsReadOnly();

        /// <inheritdoc />
        public IKeyboard Keyboard { get; }

        /// <inheritdoc />
        public ITouchscreen Touchscreen { get; }

        /// <inheritdoc/>
        public float DefaultTimeout
        {
            get
            {
                return TimeoutSettings.Timeout;
            }

            set
            {
                TimeoutSettings.SetDefaultTimeout(Convert.ToInt32(value));
                _ = _channel.SetDefaultTimeoutNoReplyAsync(Convert.ToInt32(value));
            }
        }

        /// <inheritdoc/>
        public float DefaultNavigationTimeout
        {
            get
            {
                return TimeoutSettings.NavigationTimeout;
            }

            set
            {
                TimeoutSettings.SetDefaultNavigationTimeout(Convert.ToInt32(value));
                _ = _channel.SetDefaultNavigationTimeoutNoReplyAsync(Convert.ToInt32(value));
            }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IWorker> Workers => WorkersList;

        /// <inheritdoc />
        public IVideo Video
        {
            get
            {
                if (_video != null)
                {
                    return _video;
                }

                if (string.IsNullOrEmpty(Context.Options?.RecordVideo?.Dir))
                {
                    return null;
                }

                _video = new Video(this);
                return _video;
            }
        }

        internal BrowserContext OwnedContext { get; set; }

        internal Dictionary<string, Delegate> Bindings { get; } = new Dictionary<string, Delegate>();

        internal List<Worker> WorkersList { get; } = new List<Worker>();

        internal TimeoutSettings TimeoutSettings { get; set; }

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
        public IFrame FrameByUrl(string urlString, Regex urlRegex, Func<string, bool> urlFunc)
        {
            if (string.IsNullOrEmpty(urlString))
            {
                return FrameByUrl(urlString);
            }

            if (urlRegex != null)
            {
                return FrameByUrl(urlRegex);
            }

            return FrameByUrl(urlFunc);
        }

        /// <inheritdoc />
        public Task<string> TitleAsync() => MainFrame.TitleAsync();

        /// <inheritdoc />
        public Task BringToFrontAsync() => _channel.BringToFrontAsync();

        /// <inheritdoc />
        public async Task<IPage> OpenerAsync() => (await _channel.GetOpenerAsync().ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public Task EmulateMediaAsync()
            => _channel.EmulateMediaAsync(new Dictionary<string, object>());

        /// <inheritdoc />
        public Task EmulateMediaAsync(ColorScheme colorScheme)
            => _channel.EmulateMediaAsync(new Dictionary<string, object>
            {
                ["colorScheme"] = colorScheme == ColorScheme.Undefined ? colorScheme : "null",
            });

        /// <inheritdoc />
        public Task EmulateMediaAsync(Media media, ColorScheme colorScheme)
            => _channel.EmulateMediaAsync(new Dictionary<string, object>
            {
                ["media"] = media == Media.Undefined ? media : "null",
                ["colorScheme"] = colorScheme == ColorScheme.Undefined ? colorScheme : "null",
            });

        /// <inheritdoc />
        public Task<IResponse> GoToAsync(string url, WaitUntilState waitUntil, float? timeout, string referer)
            => MainFrame.GoToAsync(true, url, waitUntil.EnsureDefaultValue(WaitUntilState.Load), referer, timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(WaitUntilState waitUntil, float? timeout)
             => MainFrame.WaitForNavigationAsync(waitUntil: waitUntil, urlString: null, urlRegex: null, urlFunc: null, timeout: timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(string url, WaitUntilState waitUntil, float? timeout)
            => MainFrame.WaitForNavigationAsync(waitUntil: default, urlString: url, urlRegex: null, urlFunc: null, timeout: timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(Regex url, WaitUntilState waitUntil, float? timeout)
            => MainFrame.WaitForNavigationAsync(waitUntil: default, urlString: null, urlRegex: url, urlFunc: null, timeout: timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(Func<string, bool> url, WaitUntilState waitUntil, float? timeout)
            => MainFrame.WaitForNavigationAsync(waitUntil: default, urlString: null, urlRegex: null, urlFunc: url, timeout: timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(
            string urlString,
            Regex urlRegex,
            Func<string, bool> urlFunc,
            WaitUntilState waitUntil,
            float? timeout)
            => MainFrame.WaitForNavigationAsync(urlString, urlRegex, urlFunc, waitUntil, timeout);

        /// <inheritdoc />
        public async Task<IRequest> WaitForRequestAsync(string url, float? timeout = default)
        {
            var result = await WaitForEventAsync(PageEvent.Request, e => e.Url.Equals(url, StringComparison.Ordinal), timeout).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc />
        public async Task<IRequest> WaitForRequestAsync(Regex url, float? timeout = default)
        {
            var result = await WaitForEventAsync(PageEvent.Request, e => url.IsMatch(e.Url), timeout).ConfigureAwait(false);
            return result;
        }

        /// <inheritdoc />
        public async Task<IRequest> WaitForRequestAsync(Func<IRequest, bool> predicate, float? timeout = default)
        {
            var result = await WaitForEventAsync(PageEvent.Request, e => predicate(e), timeout).ConfigureAwait(false);
            return result.Request;
        }

        /// <inheritdoc />
        public Task<IRequest> WaitForRequestAsync(string urlOrPredicateString, Regex urlOrPredicateRegex, Func<IRequest, bool> urlOrPredicateFunc, float? timeout = default)
        {
            if (string.IsNullOrEmpty(urlOrPredicateString))
            {
                return WaitForRequestAsync(urlOrPredicateString);
            }

            if (urlOrPredicateRegex != null)
            {
                return WaitForRequestAsync(urlOrPredicateRegex);
            }

            return WaitForRequestAsync(urlOrPredicateFunc);
        }

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(
            string expression,
            object arg,
            float? polling,
            float? timeout)
            => MainFrame.WaitForFunctionAsync(true, expression, arg, polling, timeout);

        /// <inheritdoc />
        public async Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> pageEvent, Func<T, bool> predicate, float? timeout)
        {
            if (pageEvent == null)
            {
                throw new ArgumentException("Page event is required", nameof(pageEvent));
            }

            timeout ??= TimeoutSettings.Timeout;
            using var waiter = new Waiter();
            waiter.RejectOnTimeout(Convert.ToInt32(timeout), $"Timeout while waiting for event \"{typeof(T)}\"");

            if (pageEvent.Name != PageEvent.Crash.Name)
            {
                waiter.RejectOnEvent<EventArgs>(this, PageEvent.Crash.Name, new TargetClosedException("Page crashed"));
            }

            if (pageEvent.Name != PageEvent.Close.Name)
            {
                waiter.RejectOnEvent<EventArgs>(this, PageEvent.Close.Name, new TargetClosedException("Page closed"));
            }

            return await waiter.WaitForEventAsync(this, pageEvent.Name, predicate).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<object> WaitForEventAsync(string @event, float? timeout = default)
        {
            if (@event == null || PageEvent.Events.TryGetValue(@event, out var pageEvent))
            {
                throw new ArgumentException("Page event is required", nameof(@event));
            }

            return await WaitForEventAsync(pageEvent, timeout: timeout).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task CloseAsync(bool runBeforeUnload = false)
        {
            try
            {
                await _channel.CloseAsync(runBeforeUnload).ConfigureAwait(false);
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
        public Task<T> EvaluateAsync<T>(string expression) => MainFrame.EvaluateAsync<T>(true, expression);

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string expression, object arg) => MainFrame.EvaluateAsync<T>(true, expression, arg);

        /// <inheritdoc />
        public Task EvalOnSelectorAsync(string selector, string expression) => MainFrame.EvalOnSelectorAsync(true, selector, expression);

        /// <inheritdoc />
        public Task EvalOnSelectorAsync(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAsync(true, selector, expression, arg);

        /// <inheritdoc />
        public Task<T> EvalOnSelectorAsync<T>(string selector, string expression) => MainFrame.EvalOnSelectorAsync<T>(true, selector, expression);

        /// <inheritdoc />
        public Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAsync<T>(true, selector, expression, arg);

        /// <inheritdoc />
        public Task EvalOnSelectorAllAsync(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAllAsync(true, selector, expression, arg);

        /// <inheritdoc />
        public Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAllAsync<T>(true, selector, expression, arg);

        /// <inheritdoc />
        public Task EvalOnSelectorAllAsync(string selector, string expression) => MainFrame.EvalOnSelectorAllAsync(true, selector, expression);

        /// <inheritdoc />
        public Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression) => MainFrame.EvalOnSelectorAllAsync<T>(true, selector, expression);

        /// <inheritdoc />
        public Task FillAsync(string selector, string value, float? timeout, bool? noWaitAfter)
            => MainFrame.FillAsync(true, selector, value, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, string files, float? timeout, bool? noWaitAfter)
            => SetInputFilesAsync(selector, new[] { files }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, string[] files, float? timeout, bool? noWaitAfter)
            => MainFrame.SetInputFilesAsync(true, selector, files, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, FilePayload files, float? timeout, bool? noWaitAfter)
            => SetInputFilesAsync(selector, new[] { files }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, FilePayload[] files, float? timeout, bool? noWaitAfter)
            => MainFrame.SetInputFilesAsync(true, selector, files, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task TypeAsync(string selector, string text, int delay, float? timeout, bool? noWaitAfter)
            => MainFrame.TypeAsync(true, selector, text, delay, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task FocusAsync(string selector, float? timeout) => MainFrame.FocusAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task HoverAsync(
            string selector,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool force,
            float? timeout) => MainFrame.HoverAsync(true, selector, position, modifiers, force, timeout);

        /// <inheritdoc />
        public Task PressAsync(string selector, string key, int delay, float? timeout, bool? noWaitAfter)
            => MainFrame.PressAsync(true, selector, key, delay, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, float? timeout, bool? noWaitAfter)
            => MainFrame.SelectOptionAsync(true, selector, null, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, string values, float? timeout, bool? noWaitAfter)
            => SelectOptionAsync(selector, new[] { values }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, SelectOptionValue values, float? timeout, bool? noWaitAfter)
            => SelectOptionAsync(selector, new[] { values }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, IElementHandle values, float? timeout, bool? noWaitAfter)
            => SelectOptionAsync(selector, new[] { values }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, string[] values, float? timeout, bool? noWaitAfter)
            => MainFrame.SelectOptionAsync(true, selector, values.Cast<object>().Select(v => v == null ? v : new { value = v }).ToArray(), noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, SelectOptionValue[] values, float? timeout, bool? noWaitAfter)
        {
            if (values == null)
            {
                throw new ArgumentException("values should not be null", nameof(values));
            }

            return MainFrame.SelectOptionAsync(true, selector, values, noWaitAfter, timeout);
        }

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, IElementHandle[] values, float? timeout, bool? noWaitAfter)
            => MainFrame.SelectOptionAsync(true, selector, values.Cast<ElementHandle>().ToArray(), noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, params string[] values) => SelectOptionAsync(selector, values, null, null);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, params SelectOptionValue[] values) => SelectOptionAsync(selector, values, null, null);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, params IElementHandle[] values) => SelectOptionAsync(selector, values, null, null);

        /// <inheritdoc />
        public Task WaitForTimeoutAsync(int timeout) => Task.Delay(timeout);

        /// <inheritdoc />
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorState? state, float? timeout) => MainFrame.WaitForSelectorAsync(true, selector, state, timeout);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string expression) => MainFrame.EvaluateAsync(true, expression);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string expression, object arg) => MainFrame.EvaluateAsync(true, expression, arg);

        /// <inheritdoc />
        public Task<byte[]> ScreenshotAsync(bool fullPage) => ScreenshotAsync(null, fullPage);

        /// <inheritdoc />
        public Task<byte[]> ScreenshotAsync(Rect clip) => ScreenshotAsync(null, false, clip);

        /// <inheritdoc />
        public async Task<byte[]> ScreenshotAsync(
            string path,
            bool fullPage,
            Rect clip,
            bool omitBackground,
            ScreenshotType? type,
            int? quality,
            float? timeout)
        {
            type = !string.IsNullOrEmpty(path) ? ElementHandle.DetermineScreenshotType(path) : type;
            byte[] result = Convert.FromBase64String(await _channel.ScreenshotAsync(path, fullPage, clip, omitBackground, type, quality, timeout).ConfigureAwait(false));

            if (!string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory(new FileInfo(path).Directory.FullName);
                File.WriteAllBytes(path, result);
            }

            return result;
        }

        /// <inheritdoc />
        public Task SetContentAsync(string html, WaitUntilState? waitUntil, float? timeout) => MainFrame.SetContentAsync(true, html, waitUntil, timeout);

        /// <inheritdoc />
        public Task<string> GetContentAsync() => MainFrame.GetContentAsync(true);

        /// <inheritdoc />
        public Task SetExtraHTTPHeadersAsync(Dictionary<string, string> headers) => _channel.SetExtraHTTPHeadersAsync(headers);

        /// <inheritdoc />
        public Task<IElementHandle> QuerySelectorAsync(string selector) => MainFrame.QuerySelectorAsync(true, selector);

        /// <inheritdoc />
        public Task<IEnumerable<IElementHandle>> QuerySelectorAllAsync(string selector) => MainFrame.QuerySelectorAllAsync(true, selector);

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string expression) => MainFrame.EvaluateHandleAsync(expression);

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
            float? timeout)
            => MainFrame.ClickAsync(true, selector, delay ?? 0, button.EnsureDefaultValue(MouseButton.Left), clickCount ?? 1, modifiers, position, timeout, force ?? false, noWaitAfter);

        /// <inheritdoc />
        public Task DblClickAsync(
            string selector,
            MouseButton button,
            float? delay,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            bool? noWaitAfter,
            float? timeout)
            => MainFrame.DblClickAsync(true, selector, delay ?? 0, button.EnsureDefaultValue(MouseButton.Left), position, modifiers, timeout, force ?? false, noWaitAfter);

        /// <inheritdoc />
        public async Task<IResponse> GoBackAsync(float? timeout, WaitUntilState? waitUntil)
            => (await _channel.GoBackAsync(timeout, waitUntil).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IResponse> GoForwardAsync(float? timeout, WaitUntilState? waitUntil)
            => (await _channel.GoForwardAsync(timeout, waitUntil).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IResponse> ReloadAsync(float? timeout, WaitUntilState? waitUntil)
            => (await _channel.ReloadAsync(timeout, waitUntil).ConfigureAwait(false))?.Object;

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
        public async Task<IResponse> WaitForResponseAsync(string url, float? timeout)
        {
            var result = await WaitForEventAsync(PageEvent.Response, e => e.Response.Url.Equals(url, StringComparison.Ordinal), timeout).ConfigureAwait(false);
            return result.Response;
        }

        /// <inheritdoc />
        public async Task<IResponse> WaitForResponseAsync(Regex url, float? timeout)
        {
            var result = await WaitForEventAsync(PageEvent.Response, e => url.IsMatch(e.Response.Url), timeout).ConfigureAwait(false);
            return result.Response;
        }

        /// <inheritdoc />
        public async Task<IResponse> WaitForResponseAsync(Func<IResponse, bool> predicate, float? timeout)
        {
            var result = await WaitForEventAsync(PageEvent.Response, e => predicate(e.Response), timeout).ConfigureAwait(false);
            return result.Response;
        }

        /// <inheritdoc />
        public async Task<byte[]> PdfAsync(
            string path,
            decimal scale,
            bool displayHeaderFooter,
            string headerTemplate,
            string footerTemplate,
            bool printBackground,
            bool landscape,
            string pageRanges,
            PaperFormat? format,
            string width,
            string height,
            Margin margin,
            bool preferCSSPageSize = false)
        {
            if (!Context.IsChromium)
            {
                throw new NotSupportedException($"This browser doesn't support this action.");
            }

            byte[] result = Convert.FromBase64String(await _channel.GetPdfAsync(
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

        /// <inheritdoc />
        public Task AddInitScriptAsync(string script, object[] arg, string path, string content)
        {
            if (string.IsNullOrEmpty(script))
            {
                script = ScriptsHelper.EvaluationScript(content, path);
            }

            return _channel.AddInitScriptAsync(ScriptsHelper.SerializeScriptCall(script, arg));
        }

        /// <inheritdoc />
        public Task RouteAsync(string url, Action<Route, IRequest> handler)
            => RouteAsync(
                new RouteSetting
                {
                    Url = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task RouteAsync(Regex url, Action<Route, IRequest> handler)
            => RouteAsync(
                new RouteSetting
                {
                    Regex = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task RouteAsync(Func<string, bool> url, Action<Route, IRequest> handler)
            => RouteAsync(
                new RouteSetting
                {
                    Function = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task UnrouteAsync(string url, Action<Route, IRequest> handler)
            => UnrouteAsync(
                new RouteSetting
                {
                    Url = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task UnrouteAsync(Regex url, Action<Route, IRequest> handler)
            => UnrouteAsync(
                new RouteSetting
                {
                    Regex = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task UnrouteAsync(Func<string, bool> url, Action<Route, IRequest> handler)
            => UnrouteAsync(
                new RouteSetting
                {
                    Function = url,
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
        public Task CheckAsync(string selector, bool? force, bool? noWaitAfter, float? timeout)
            => MainFrame.CheckAsync(true, selector, force, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task UncheckAsync(string selector, bool? force, bool? noWaitAfter, float? timeout)
            => MainFrame.UncheckAsync(true, selector, force, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task DispatchEventAsync(string selector, string type, object eventInit, float? timeout)
             => MainFrame.DispatchEventAsync(true, selector, type, eventInit, timeout);

        /// <inheritdoc />
        public Task<string> GetAttributeAsync(string selector, string name, float? timeout)
             => MainFrame.GetAttributeAsync(true, selector, name, timeout);

        /// <inheritdoc />
        public Task<string> GetInnerHTMLAsync(string selector, float? timeout)
             => MainFrame.GetInnerHTMLAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task<string> GetInnerTextAsync(string selector, float? timeout)
             => MainFrame.GetInnerTextAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task<string> GetTextContentAsync(string selector, float? timeout)
             => MainFrame.GetTextContentAsync(true, selector, timeout);

        /// <inheritdoc />
        public Task TapAsync(string selector, IEnumerable<KeyboardModifier> modifiers, Position position, bool? force, bool? noWaitAfter, float? timeout)
            => MainFrame.TapAsync(true, selector, modifiers, position, force, noWaitAfter, timeout);

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
        public Task PauseAsync() => Context.PauseAsync();

        internal void OnFrameNavigated(Frame frame)
            => FrameNavigated?.Invoke(this, new FrameEventArgs(frame));

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
            Close?.Invoke(this, EventArgs.Empty);
        }

        private void Channel_Crashed(object sender, EventArgs e)
        {
            RejectPendingOperations(true);
            Crash?.Invoke(this, EventArgs.Empty);
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
                    route.Handler(e.Route, e.Request);
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
                throw new PlaywrightSharpException($"Function \"{name}\" has been already registered");
            }

            Bindings.Add(name, callback);

            return _channel.ExposeBindingAsync(name, handle);
        }
    }
}
