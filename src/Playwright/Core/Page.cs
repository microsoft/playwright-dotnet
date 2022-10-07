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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Page : ChannelOwnerBase, IChannelOwner<Page>, IPage
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

    internal readonly List<Worker> _workers = new();
    internal readonly TimeoutSettings _timeoutSettings;
    private List<RouteHandler> _routes = new();
    private EventHandler<IFileChooser> _fileChooserEventHandler;
    private bool _fileChooserIntercepted;
    private Video _video;

    internal Page(IChannelOwner parent, string guid, PageInitializer initializer) : base(parent, guid)
    {
        Context = (BrowserContext)parent;
        _timeoutSettings = new(Context._timeoutSettings);

        _channel = new(guid, parent.Connection, this);

        MainFrame = initializer.MainFrame;
        MainFrame.Page = this;
        _frames.Add(MainFrame);
        if (initializer.ViewportSize != null)
        {
            ViewportSize = new() { Width = initializer.ViewportSize.Width, Height = initializer.ViewportSize.Height };
        }

        IsClosed = initializer.IsClosed;
        _accessibility = new Accessibility(_channel);
        _keyboard = new Keyboard(_channel);
        _touchscreen = new Touchscreen(_channel);
        _mouse = new Mouse(_channel);
        APIRequest = Context._request;
        _channel.Closed += (_, _) => OnClose();
        _channel.Crashed += Channel_Crashed;
        _channel.Popup += (_, page) => Popup?.Invoke(this, page);
        _channel.WebSocket += (_, e) => WebSocket?.Invoke(this, e);
        _channel.BindingCall += Channel_BindingCall;
        _channel.Route += (_, route) => OnRouteAsync(route).ConfigureAwait(false);
        _channel.FrameAttached += Channel_FrameAttached;
        _channel.FrameDetached += Channel_FrameDetached;
        _channel.Dialog += (_, e) =>
        {
            if (Dialog == null)
            {
                if ("beforeunload".Equals(e.Type, StringComparison.Ordinal))
                {
                    e.AcceptAsync(null).IgnoreException();
                }
                else
                {
                    e.DismissAsync().IgnoreException();
                }
            }
            else
            {
                Dialog?.Invoke(this, e);
            }
        };
        _channel.Console += (_, e) => Console?.Invoke(this, e);
        _channel.Download += (_, e) => Download?.Invoke(this, new Download(this, e.Url, e.SuggestedFilename, e.Artifact.Object));
        _channel.PageError += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Error.Stack))
            {
                PageError?.Invoke(this, e.Error.Stack);
                return;
            }

            PageError?.Invoke(this, $"{e.Error.Name}: {e.Error.Message}");
        };
        _channel.Video += (_, artifact) => ForceVideo().ArtifactReady(artifact);

        _channel.FileChooser += (_, e) => _fileChooserEventHandler?.Invoke(this, new FileChooser(this, e.Element.Object, e.IsMultiple));
        _channel.Worker += (_, worker) =>
        {
            _workers.Add(worker);
            worker.Page = this;
            Worker?.Invoke(this, worker);
        };

        _initializer = initializer;

        Close += (_, _) => ClosedOrCrashedTcs.TrySetResult(true);
        Crash += (_, _) => ClosedOrCrashedTcs.TrySetResult(true);
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
                _channel.SetFileChooserInterceptedNoReplyAsync(true).IgnoreException();
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
                    _channel.SetFileChooserInterceptedNoReplyAsync(false).IgnoreException();
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

    public IReadOnlyList<IWorker> Workers => _workers;

    public IVideo Video
    {
        get
        {
            if (Context.Options.RecordVideoDir == null)
            {
                return null;
            }

            return ForceVideo();
        }
        set => _video = value as Video;
    }

    internal BrowserContext OwnedContext { get; set; }

    internal Dictionary<string, Delegate> Bindings { get; } = new();

    internal Page Opener => _initializer.Opener;

    internal PageChannel Channel => _channel;

    internal TaskCompletionSource<bool> ClosedOrCrashedTcs { get; } = new();

    public IAPIRequestContext APIRequest { get; }

    public IFrame Frame(string name)
        => Frames.FirstOrDefault(f => f.Name == name);

    public IFrame FrameByUrl(string urlString) => Frames.FirstOrDefault(f => Context.UrlMatches(urlString, f.Url));

    public IFrame FrameByUrl(Regex urlRegex) => Frames.FirstOrDefault(f => urlRegex.IsMatch(f.Url));

    public IFrame FrameByUrl(Func<string, bool> urlFunc) => Frames.FirstOrDefault(f => urlFunc(f.Url));

    IFrameLocator IPage.FrameLocator(string selector) => MainFrame.FrameLocator(selector);

    public Task<string> TitleAsync() => MainFrame.TitleAsync();

    public Task BringToFrontAsync() => _channel.BringToFrontAsync();

    public Task<IPage> OpenerAsync() => Task.FromResult<IPage>(Opener?.IsClosed == false ? Opener : null);

    public Task EmulateMediaAsync(PageEmulateMediaOptions options = default)
    {
        var args = new Dictionary<string, object>
        {
            ["media"] = options?.Media,
            ["colorScheme"] = options?.ColorScheme,
            ["reducedMotion"] = options?.ReducedMotion,
            ["forcedColors"] = options?.ForcedColors,
        };
        return _channel.EmulateMediaAsync(args);
    }

    public Task<IResponse> GotoAsync(string url, PageGotoOptions options = default)
        => MainFrame.GotoAsync(url, new() { WaitUntil = options?.WaitUntil, Timeout = options?.Timeout, Referer = options?.Referer });

    public Task WaitForURLAsync(string url, PageWaitForURLOptions options = default)
        => MainFrame.WaitForURLAsync(url, new() { WaitUntil = options?.WaitUntil, Timeout = options?.Timeout });

    public Task WaitForURLAsync(Regex url, PageWaitForURLOptions options = default)
        => MainFrame.WaitForURLAsync(url, new() { WaitUntil = options?.WaitUntil, Timeout = options?.Timeout });

    public Task WaitForURLAsync(Func<string, bool> url, PageWaitForURLOptions options = default)
        => MainFrame.WaitForURLAsync(url, new() { WaitUntil = options?.WaitUntil, Timeout = options?.Timeout });

    public Task<IConsoleMessage> WaitForConsoleMessageAsync(PageWaitForConsoleMessageOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Console, null, options?.Predicate, options?.Timeout);

    public Task<IFileChooser> WaitForFileChooserAsync(PageWaitForFileChooserOptions options = default)
        => InnerWaitForEventAsync(PageEvent.FileChooser, null, options?.Predicate, options?.Timeout);

    public Task<IPage> WaitForPopupAsync(PageWaitForPopupOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Popup, null, options?.Predicate, options?.Timeout);

    public Task<IWebSocket> WaitForWebSocketAsync(PageWaitForWebSocketOptions options = default)
        => InnerWaitForEventAsync(PageEvent.WebSocket, null, options?.Predicate, options?.Timeout);

    public Task<IWorker> WaitForWorkerAsync(PageWaitForWorkerOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Worker, null, options?.Predicate, options?.Timeout);

    public Task<IResponse> WaitForNavigationAsync(PageWaitForNavigationOptions options = default)
        => MainFrame.WaitForNavigationAsync(new()
        {
            UrlString = options?.UrlString,
            UrlRegex = options?.UrlRegex,
            UrlFunc = options?.UrlFunc,
            WaitUntil = options?.WaitUntil,
            Timeout = options?.Timeout,
        });

    public Task<IResponse> RunAndWaitForNavigationAsync(Func<Task> action, PageRunAndWaitForNavigationOptions options = default)
        => MainFrame.RunAndWaitForNavigationAsync(action, new()
        {
            UrlString = options?.UrlString,
            UrlRegex = options?.UrlRegex,
            UrlFunc = options?.UrlFunc,
            WaitUntil = options?.WaitUntil,
            Timeout = options?.Timeout,
        });

    public Task<IRequest> WaitForRequestAsync(string urlOrPredicate, PageWaitForRequestOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Request, null, e => Context.UrlMatches(e.Url, urlOrPredicate), options?.Timeout);

    public Task<IRequest> WaitForRequestAsync(Regex urlOrPredicate, PageWaitForRequestOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Request, null, e => urlOrPredicate.IsMatch(e.Url), options?.Timeout);

    public Task<IRequest> WaitForRequestAsync(Func<IRequest, bool> urlOrPredicate, PageWaitForRequestOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Request, null, e => urlOrPredicate(e), options?.Timeout);

    public Task<IRequest> WaitForRequestFinishedAsync(PageWaitForRequestFinishedOptions options = default)
        => InnerWaitForEventAsync(PageEvent.RequestFinished, null, options?.Predicate, options?.Timeout);

    public Task<IResponse> WaitForResponseAsync(string urlOrPredicate, PageWaitForResponseOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Response, null, e => Context.UrlMatches(e.Url, urlOrPredicate), options?.Timeout);

    public Task<IResponse> WaitForResponseAsync(Regex urlOrPredicate, PageWaitForResponseOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Response, null, e => urlOrPredicate.IsMatch(e.Url), options?.Timeout);

    public Task<IResponse> WaitForResponseAsync(Func<IResponse, bool> urlOrPredicate, PageWaitForResponseOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Response, null, e => urlOrPredicate(e), options?.Timeout);

    public Task<IConsoleMessage> RunAndWaitForConsoleMessageAsync(Func<Task> action, PageRunAndWaitForConsoleMessageOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Console, action, options?.Predicate, options?.Timeout);

    public Task<IDownload> WaitForDownloadAsync(PageWaitForDownloadOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Download, null, options?.Predicate, options?.Timeout);

    public Task<IDownload> RunAndWaitForDownloadAsync(Func<Task> action, PageRunAndWaitForDownloadOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Download, action, options?.Predicate, options?.Timeout);

    public Task<IFileChooser> RunAndWaitForFileChooserAsync(Func<Task> action, PageRunAndWaitForFileChooserOptions options = default)
        => InnerWaitForEventAsync(PageEvent.FileChooser, action, options?.Predicate, options?.Timeout);

    public Task<IPage> RunAndWaitForPopupAsync(Func<Task> action, PageRunAndWaitForPopupOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Popup, action, options?.Predicate, options?.Timeout);

    public Task<IRequest> RunAndWaitForRequestFinishedAsync(Func<Task> action, PageRunAndWaitForRequestFinishedOptions options = default)
        => InnerWaitForEventAsync(PageEvent.RequestFinished, action, options?.Predicate, options?.Timeout);

    public Task<IWebSocket> RunAndWaitForWebSocketAsync(Func<Task> action, PageRunAndWaitForWebSocketOptions options = default)
        => InnerWaitForEventAsync(PageEvent.WebSocket, action, options?.Predicate, options?.Timeout);

    public Task<IWorker> RunAndWaitForWorkerAsync(Func<Task> action, PageRunAndWaitForWorkerOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Worker, action, options?.Predicate, options?.Timeout);

    public Task<IRequest> RunAndWaitForRequestAsync(Func<Task> action, string urlOrPredicate, PageRunAndWaitForRequestOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Request, action, e => Context.UrlMatches(e.Url, urlOrPredicate), options?.Timeout);

    public Task<IRequest> RunAndWaitForRequestAsync(Func<Task> action, Regex urlOrPredicate, PageRunAndWaitForRequestOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Request, action, e => urlOrPredicate.IsMatch(e.Url), options?.Timeout);

    public Task<IRequest> RunAndWaitForRequestAsync(Func<Task> action, Func<IRequest, bool> urlOrPredicate, PageRunAndWaitForRequestOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Request, action, e => urlOrPredicate(e), options?.Timeout);

    public Task<IResponse> RunAndWaitForResponseAsync(Func<Task> action, string urlOrPredicate, PageRunAndWaitForResponseOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Response, action, e => Context.UrlMatches(e.Url, urlOrPredicate), options?.Timeout);

    public Task<IResponse> RunAndWaitForResponseAsync(Func<Task> action, Regex urlOrPredicate, PageRunAndWaitForResponseOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Response, action, e => urlOrPredicate.IsMatch(e.Url), options?.Timeout);

    public Task<IResponse> RunAndWaitForResponseAsync(Func<Task> action, Func<IResponse, bool> urlOrPredicate, PageRunAndWaitForResponseOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Response, action, e => urlOrPredicate(e), options?.Timeout);

    public Task<IJSHandle> WaitForFunctionAsync(string expression, object arg = default, PageWaitForFunctionOptions options = default)
        => MainFrame.WaitForFunctionAsync(expression, arg, new() { PollingInterval = options?.PollingInterval, Timeout = options?.Timeout });

    public async Task<T> InnerWaitForEventAsync<T>(PlaywrightEvent<T> pageEvent, Func<Task> action = default, Func<T, bool> predicate = default, float? timeout = default)
    {
        if (pageEvent == null)
        {
            throw new ArgumentException("Page event is required", nameof(pageEvent));
        }

        timeout = _timeoutSettings.Timeout(timeout);
        using var waiter = new Waiter(this, $"page.WaitForEventAsync(\"{typeof(T)}\")");
        waiter.RejectOnTimeout(Convert.ToInt32(timeout, CultureInfo.InvariantCulture), $"Timeout {timeout}ms exceeded while waiting for event \"{pageEvent.Name}\"");

        if (pageEvent.Name != PageEvent.Crash.Name)
        {
            waiter.RejectOnEvent<IPage>(this, PageEvent.Crash.Name, new("Page crashed"));
        }

        if (pageEvent.Name != PageEvent.Close.Name)
        {
            waiter.RejectOnEvent<IPage>(this, PageEvent.Close.Name, new("Page closed"));
        }

        var result = waiter.WaitForEventAsync(this, pageEvent.Name, predicate);
        if (action != null)
        {
            await WrapApiBoundaryAsync(() => Task.WhenAll(result, action())).ConfigureAwait(false);
        }

        return await result.ConfigureAwait(false);
    }

    public async Task CloseAsync(PageCloseOptions options = default)
    {
        try
        {
            await _channel.CloseAsync(options?.RunBeforeUnload ?? false).ConfigureAwait(false);
            if (OwnedContext != null)
            {
                await OwnedContext.CloseAsync().ConfigureAwait(false);
            }
        }
        catch (Exception e) when (DriverMessages.IsSafeCloseError(e))
        {
            // Swallow exception
        }
    }

    public Task<T> EvaluateAsync<T>(string expression, object arg) => MainFrame.EvaluateAsync<T>(expression, arg);

    public Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAsync(selector, expression, arg);

    public Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg = null, PageEvalOnSelectorOptions options = null)
        => MainFrame.EvalOnSelectorAsync<T>(selector, expression, arg, new() { Strict = options?.Strict });

    public ILocator Locator(string selector, PageLocatorOptions options = default)
        => MainFrame.Locator(selector, new()
        {
            Has = options?.Has,
            HasTextString = options?.HasTextString,
            HasTextRegex = options?.HasTextRegex,
        });

    public Task<IElementHandle> QuerySelectorAsync(string selector, PageQuerySelectorOptions options = null)
        => MainFrame.QuerySelectorAsync(selector, new() { Strict = options?.Strict });

    public Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAsync<T>(selector, expression, arg);

    public Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAllAsync(selector, expression, arg);

    public Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object arg) => MainFrame.EvalOnSelectorAllAsync<T>(selector, expression, arg);

    public Task FillAsync(string selector, string value, PageFillOptions options = default)
        => MainFrame.FillAsync(selector, value, new() { NoWaitAfter = options?.NoWaitAfter, Timeout = options?.Timeout, Force = options?.Force, Strict = options?.Strict });

    public Task SetInputFilesAsync(string selector, string files, PageSetInputFilesOptions options = default)
        => MainFrame.SetInputFilesAsync(selector, files, Map(options));

    public Task SetInputFilesAsync(string selector, IEnumerable<string> files, PageSetInputFilesOptions options = default)
        => MainFrame.SetInputFilesAsync(selector, files, Map(options));

    public Task SetInputFilesAsync(string selector, FilePayload files, PageSetInputFilesOptions options = default)
        => MainFrame.SetInputFilesAsync(selector, files, Map(options));

    public Task SetInputFilesAsync(string selector, IEnumerable<FilePayload> files, PageSetInputFilesOptions options = default)
        => MainFrame.SetInputFilesAsync(selector, files, Map(options));

    public Task TypeAsync(string selector, string text, PageTypeOptions options = default)
        => MainFrame.TypeAsync(selector, text, new()
        {
            Delay = options?.Delay,
            NoWaitAfter = options?.NoWaitAfter,
            Timeout = options?.Timeout,
            Strict = options?.Strict,
        });

    public Task FocusAsync(string selector, PageFocusOptions options = default)
        => MainFrame.FocusAsync(selector, new()
        {
            Timeout = options?.Timeout,
            Strict = options?.Strict,
        });

    public Task HoverAsync(string selector, PageHoverOptions options = default)
        => MainFrame.HoverAsync(
            selector,
            new()
            {
                Position = options?.Position,
                Modifiers = options?.Modifiers,
                Force = options?.Force,
                Timeout = options?.Timeout,
                Trial = options?.Trial,
                Strict = options?.Strict,
            });

    public Task PressAsync(string selector, string key, PagePressOptions options = default)
        => MainFrame.PressAsync(selector, key, new()
        {
            Delay = options?.Delay,
            NoWaitAfter = options?.NoWaitAfter,
            Timeout = options?.Timeout,
            Strict = options?.Strict,
        });

    public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, string values, PageSelectOptionOptions options = default)
        => SelectOptionAsync(selector, new[] { values }, options);

    public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<string> values, PageSelectOptionOptions options = default)
        => SelectOptionAsync(selector, values.Select(x => new SelectOptionValue() { Value = x }), options);

    public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IElementHandle values, PageSelectOptionOptions options = default)
        => SelectOptionAsync(selector, new[] { values }, options);

    public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, PageSelectOptionOptions options = default)
        => MainFrame.SelectOptionAsync(selector, values, new()
        {
            NoWaitAfter = options?.NoWaitAfter,
            Timeout = options?.Timeout,
            Force = options?.Force,
            Strict = options?.Strict,
        });

    public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, SelectOptionValue values, PageSelectOptionOptions options = default)
        => SelectOptionAsync(selector, new[] { values }, options);

    public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, PageSelectOptionOptions options = default)
        => MainFrame.SelectOptionAsync(selector, values, new()
        {
            NoWaitAfter = options?.NoWaitAfter,
            Timeout = options?.Timeout,
            Force = options?.Force,
            Strict = options?.Strict,
        });

    public Task WaitForTimeoutAsync(float timeout) => MainFrame.WaitForTimeoutAsync(timeout);

    public Task<IElementHandle> WaitForSelectorAsync(string selector, PageWaitForSelectorOptions options = default)
        => MainFrame.WaitForSelectorAsync(selector, new()
        {
            State = options?.State,
            Timeout = options?.Timeout,
            Strict = options?.Strict,
        });

    public Task<JsonElement?> EvaluateAsync(string expression, object arg) => MainFrame.EvaluateAsync(expression, arg);

    public async Task<byte[]> ScreenshotAsync(PageScreenshotOptions options = default)
    {
        options ??= new PageScreenshotOptions();
        if (options.Type == null && !string.IsNullOrEmpty(options.Path))
        {
            options.Type = ElementHandle.DetermineScreenshotType(options.Path);
        }

        byte[] result = await _channel.ScreenshotAsync(
            path: options.Path,
            fullPage: options.FullPage,
            clip: options.Clip,
            omitBackground: options.OmitBackground,
            type: options.Type,
            quality: options.Quality,
            mask: options.Mask,
            animations: options.Animations,
            caret: options.Caret,
            scale: options.Scale,
            timeout: options.Timeout).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(options.Path))
        {
            Directory.CreateDirectory(new FileInfo(options.Path).Directory.FullName);
            File.WriteAllBytes(options.Path, result);
        }

        return result;
    }

    public Task SetContentAsync(string html, PageSetContentOptions options = default)
        => MainFrame.SetContentAsync(html, new() { WaitUntil = options?.WaitUntil, Timeout = options?.Timeout });

    public Task<string> ContentAsync() => MainFrame.ContentAsync();

    public Task SetExtraHTTPHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers)
        => _channel.SetExtraHTTPHeadersAsync(headers);

    public Task<IElementHandle> QuerySelectorAsync(string selector) => MainFrame.QuerySelectorAsync(selector);

    public Task<IReadOnlyList<IElementHandle>> QuerySelectorAllAsync(string selector)
        => MainFrame.QuerySelectorAllAsync(selector);

    public Task<IJSHandle> EvaluateHandleAsync(string expression, object arg) => MainFrame.EvaluateHandleAsync(expression, arg);

    public Task<IElementHandle> AddScriptTagAsync(PageAddScriptTagOptions options = default)
        => MainFrame.AddScriptTagAsync(new()
        {
            Url = options?.Url,
            Path = options?.Path,
            Content = options?.Content,
            Type = options?.Type,
        });

    public Task<IElementHandle> AddStyleTagAsync(PageAddStyleTagOptions options = default)
        => MainFrame.AddStyleTagAsync(new()
        {
            Url = options?.Url,
            Path = options?.Path,
            Content = options?.Content,
        });

    public Task ClickAsync(string selector, PageClickOptions options = default)
        => MainFrame.ClickAsync(
            selector,
            new()
            {
                Button = options?.Button,
                ClickCount = options?.ClickCount,
                Delay = options?.Delay,
                Position = options?.Position,
                Modifiers = options?.Modifiers,
                Force = options?.Force,
                NoWaitAfter = options?.NoWaitAfter,
                Timeout = options?.Timeout,
                Trial = options?.Trial,
                Strict = options?.Strict,
            });

    public Task DblClickAsync(string selector, PageDblClickOptions options = default)
        => MainFrame.DblClickAsync(selector, new()
        {
            Delay = options?.Delay,
            Button = options?.Button,
            Position = options?.Position,
            Modifiers = options?.Modifiers,
            Timeout = options?.Timeout,
            Force = options?.Force,
            NoWaitAfter = options?.NoWaitAfter,
            Trial = options?.Trial,
            Strict = options?.Strict,
        });

    public async Task<IResponse> GoBackAsync(PageGoBackOptions options = default)
        => (await _channel.GoBackAsync(options?.Timeout, options?.WaitUntil).ConfigureAwait(false))?.Object;

    public async Task<IResponse> GoForwardAsync(PageGoForwardOptions options = default)
        => (await _channel.GoForwardAsync(options?.Timeout, options?.WaitUntil).ConfigureAwait(false))?.Object;

    public async Task<IResponse> ReloadAsync(PageReloadOptions options = default)
        => (await _channel.ReloadAsync(options?.Timeout, options?.WaitUntil).ConfigureAwait(false))?.Object;

    public Task ExposeBindingAsync(string name, Action callback, PageExposeBindingOptions options = default)
        => InnerExposeBindingAsync(name, (Delegate)callback, options?.Handle ?? false);

    public Task ExposeBindingAsync(string name, Action<BindingSource> callback)
        => InnerExposeBindingAsync(name, (Delegate)callback);

    public Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> callback)
        => InnerExposeBindingAsync(name, (Delegate)callback);

    public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> callback)
        => InnerExposeBindingAsync(name, (Delegate)callback);

    public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, IJSHandle, TResult> callback)
        => InnerExposeBindingAsync(name, (Delegate)callback, true);

    public Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> callback)
        => InnerExposeBindingAsync(name, (Delegate)callback);

    public Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> callback)
        => InnerExposeBindingAsync(name, (Delegate)callback);

    public Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> callback)
        => InnerExposeBindingAsync(name, (Delegate)callback);

    public Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> callback)
        => InnerExposeBindingAsync(name, (Delegate)callback);

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

    public async Task<byte[]> PdfAsync(PagePdfOptions options = default)
    {
        if (!Context.IsChromium)
        {
            throw new NotSupportedException("This browser doesn't support this action.");
        }

        byte[] result = await _channel.PdfAsync(
            scale: options?.Scale,
            displayHeaderFooter: options?.DisplayHeaderFooter,
            headerTemplate: options?.HeaderTemplate,
            footerTemplate: options?.FooterTemplate,
            printBackground: options?.PrintBackground,
            landscape: options?.Landscape,
            pageRanges: options?.PageRanges,
            format: options?.Format,
            width: options?.Width,
            height: options?.Height,
            margin: options?.Margin,
            preferCSSPageSize: options?.PreferCSSPageSize).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(options?.Path))
        {
            Directory.CreateDirectory(new FileInfo(options.Path).Directory.FullName);
            File.WriteAllBytes(options.Path, result);
        }

        return result;
    }

    public Task AddInitScriptAsync(string script, string scriptPath)
        => _channel.AddInitScriptAsync(ScriptsHelper.EvaluationScript(script, scriptPath));

    public Task RouteAsync(string url, Func<IRoute, Task> handler, PageRouteOptions options = null)
        => RouteAsync(new Regex(Context.CombineUrlWithBase(url).GlobToRegex()), null, handler, options);

    public Task RouteAsync(string url, Action<IRoute> handler, PageRouteOptions options = null)
        => RouteAsync(new Regex(Context.CombineUrlWithBase(url).GlobToRegex()), null, handler, options);

    public Task RouteAsync(Regex url, Action<IRoute> handler, PageRouteOptions options = null)
         => RouteAsync(url, null, handler, options);

    public Task RouteAsync(Regex url, Func<IRoute, Task> handler, PageRouteOptions options = null)
         => RouteAsync(url, null, handler, options);

    public Task RouteAsync(Func<string, bool> url, Action<IRoute> handler, PageRouteOptions options = null)
        => RouteAsync(null, url, handler, options);

    public Task RouteAsync(Func<string, bool> url, Func<IRoute, Task> handler, PageRouteOptions options = null)
        => RouteAsync(null, url, handler, options);

    public Task UnrouteAsync(string urlString, Action<IRoute> handler)
        => UnrouteAsync(new Regex(Context.CombineUrlWithBase(urlString).GlobToRegex()), null, handler);

    public Task UnrouteAsync(string urlString, Func<IRoute, Task> handler)
        => UnrouteAsync(new Regex(Context.CombineUrlWithBase(urlString).GlobToRegex()), null, handler);

    public Task UnrouteAsync(Regex urlString, Action<IRoute> handler)
        => UnrouteAsync(urlString, null, handler);

    public Task UnrouteAsync(Regex urlString, Func<IRoute, Task> handler)
        => UnrouteAsync(urlString, null, handler);

    public Task UnrouteAsync(Func<string, bool> urlFunc, Action<IRoute> handler)
        => UnrouteAsync(null, urlFunc, handler);

    public Task UnrouteAsync(Func<string, bool> urlFunc, Func<IRoute, Task> handler)
        => UnrouteAsync(null, urlFunc, handler);

    public Task WaitForLoadStateAsync(LoadState? state = default, PageWaitForLoadStateOptions options = default)
        => MainFrame.WaitForLoadStateAsync(state, new() { Timeout = options?.Timeout });

    public Task SetViewportSizeAsync(int width, int height)
    {
        ViewportSize = new() { Width = width, Height = height };
        return _channel.SetViewportSizeAsync(ViewportSize);
    }

    public Task SetCheckedAsync(string selector, bool checkedState, PageSetCheckedOptions options = null)
        => checkedState ?
        MainFrame.CheckAsync(selector, new()
        {
            Position = options?.Position,
            Force = options?.Force,
            NoWaitAfter = options?.NoWaitAfter,
            Strict = options?.Strict,
            Timeout = options?.Timeout,
            Trial = options?.Trial,
        })
        : MainFrame.UncheckAsync(selector, new()
        {
            Position = options?.Position,
            Force = options?.Force,
            NoWaitAfter = options?.NoWaitAfter,
            Timeout = options?.Timeout,
            Trial = options?.Trial,
            Strict = options?.Strict,
        });

    public Task CheckAsync(string selector, PageCheckOptions options = default)
        => MainFrame.CheckAsync(selector, new()
        {
            Position = options?.Position,
            Force = options?.Force,
            NoWaitAfter = options?.NoWaitAfter,
            Strict = options?.Strict,
            Timeout = options?.Timeout,
            Trial = options?.Trial,
        });

    public Task UncheckAsync(string selector, PageUncheckOptions options = default)
        => MainFrame.UncheckAsync(selector, new()
        {
            Position = options?.Position,
            Force = options?.Force,
            NoWaitAfter = options?.NoWaitAfter,
            Timeout = options?.Timeout,
            Trial = options?.Trial,
            Strict = options?.Strict,
        });

    public Task DispatchEventAsync(string selector, string type, object eventInit = default, PageDispatchEventOptions options = default)
         => MainFrame.DispatchEventAsync(selector, type, eventInit, new() { Timeout = options?.Timeout, Strict = options?.Strict });

    public Task<string> GetAttributeAsync(string selector, string name, PageGetAttributeOptions options = default)
         => MainFrame.GetAttributeAsync(selector, name, new()
         {
             Timeout = options?.Timeout,
             Strict = options?.Strict,
         });

    public Task<string> InnerHTMLAsync(string selector, PageInnerHTMLOptions options = default)
         => MainFrame.InnerHTMLAsync(selector, new()
         {
             Timeout = options?.Timeout,
             Strict = options?.Strict,
         });

    public Task<string> InnerTextAsync(string selector, PageInnerTextOptions options = default)
         => MainFrame.InnerTextAsync(selector, new()
         {
             Timeout = options?.Timeout,
             Strict = options?.Strict,
         });

    public Task<string> TextContentAsync(string selector, PageTextContentOptions options = default)
         => MainFrame.TextContentAsync(selector, new()
         {
             Timeout = options?.Timeout,
             Strict = options?.Strict,
         });

    public Task TapAsync(string selector, PageTapOptions options = default)
        => MainFrame.TapAsync(
            selector,
            new()
            {
                Modifiers = options?.Modifiers,
                Position = options?.Position,
                Force = options?.Force,
                NoWaitAfter = options?.NoWaitAfter,
                Timeout = options?.Timeout,
                Trial = options?.Trial,
                Strict = options?.Strict,
            });

    public Task<bool> IsCheckedAsync(string selector, PageIsCheckedOptions options = default)
        => MainFrame.IsCheckedAsync(selector, new()
        {
            Timeout = options?.Timeout,
            Strict = options?.Strict,
        });

    public Task<bool> IsDisabledAsync(string selector, PageIsDisabledOptions options = default)
        => MainFrame.IsDisabledAsync(selector, new()
        {
            Timeout = options?.Timeout,
            Strict = options?.Strict,
        });

    public Task<bool> IsEditableAsync(string selector, PageIsEditableOptions options = default)
        => MainFrame.IsEditableAsync(selector, new()
        {
            Timeout = options?.Timeout,
            Strict = options?.Strict,
        });

    public Task<bool> IsEnabledAsync(string selector, PageIsEnabledOptions options = default)
        => MainFrame.IsEnabledAsync(selector, new()
        {
            Timeout = options?.Timeout,
            Strict = options?.Strict,
        });

#pragma warning disable CS0612 // Type or member is obsolete
    public Task<bool> IsHiddenAsync(string selector, PageIsHiddenOptions options = default)
        => MainFrame.IsHiddenAsync(selector, new()
        {
            Timeout = options?.Timeout,
            Strict = options?.Strict,
        });

    public Task<bool> IsVisibleAsync(string selector, PageIsVisibleOptions options = default)
        => MainFrame.IsVisibleAsync(selector, new()
        {
            Timeout = options?.Timeout,
            Strict = options?.Strict,
        });
#pragma warning restore CS0612 // Type or member is obsolete

    public Task PauseAsync() => Context.Channel.PauseAsync();

    public void SetDefaultNavigationTimeout(float timeout)
    {
        _timeoutSettings.SetDefaultNavigationTimeout(timeout);
        WrapApiCallAsync(() => Channel.SetDefaultNavigationTimeoutNoReplyAsync(timeout), true).IgnoreException();
    }

    public void SetDefaultTimeout(float timeout)
    {
        _timeoutSettings.SetDefaultTimeout(timeout);
        WrapApiCallAsync(() => Channel.SetDefaultTimeoutNoReplyAsync(timeout), true).IgnoreException();
    }

    public Task<string> InputValueAsync(string selector, PageInputValueOptions options = null)
        => MainFrame.InputValueAsync(selector, new()
        {
            Timeout = options?.Timeout,
            Strict = options?.Strict,
        });

    public Task DragAndDropAsync(string source, string target, PageDragAndDropOptions options = null)
        => MainFrame.DragAndDropAsync(source, target, new()
        {
            Force = options?.Force,
            NoWaitAfter = options?.NoWaitAfter,
            Timeout = options?.Timeout,
            Trial = options?.Trial,
            Strict = options?.Strict,
            SourcePosition = options?.SourcePosition,
            TargetPosition = options?.TargetPosition,
        });

    internal void NotifyPopup(Page page) => Popup?.Invoke(this, page);

    internal void OnFrameNavigated(Frame frame)
        => FrameNavigated?.Invoke(this, frame);

    internal void FireRequest(IRequest request) => Request?.Invoke(this, request);

    internal void FireRequestFailed(IRequest request) => RequestFailed?.Invoke(this, request);

    internal void FireRequestFinished(IRequest request) => RequestFinished?.Invoke(this, request);

    internal void FireResponse(IResponse response) => Response?.Invoke(this, response);

    internal void FireLoad() => Load?.Invoke(this, this);

    internal void FireDOMContentLoaded() => DOMContentLoaded?.Invoke(this, this);

    private Task RouteAsync(Regex urlRegex, Func<string, bool> urlFunc, Delegate handler, PageRouteOptions options)
        => RouteAsync(new()
        {
            Regex = urlRegex,
            Function = urlFunc,
            Handler = handler,
            Times = options?.Times,
        });

    private Task RouteAsync(RouteHandler setting)
    {
        _routes.Insert(0, setting);

        if (_routes.Count == 1)
        {
            return _channel.SetNetworkInterceptionEnabledAsync(true);
        }

        return Task.CompletedTask;
    }

    private Task UnrouteAsync(Regex urlRegex, Func<string, bool> urlFunc, Delegate handler = null)
        => UnrouteAsync(new()
        {
            Function = urlFunc,
            Regex = urlRegex,
            Handler = handler,
        });

    private Task UnrouteAsync(RouteHandler setting)
    {
        var newRoutes = new List<RouteHandler>();
        newRoutes.AddRange(_routes.Where(r =>
            (setting.Regex != null && !(r.Regex == setting.Regex || (r.Regex.ToString() == setting.Regex.ToString() && r.Regex.Options == setting.Regex.Options))) ||
            (setting.Function != null && r.Function != setting.Function) ||
            (setting.Handler != null && r.Handler != setting.Handler)));
        _routes = newRoutes;

        if (_routes.Count == 0)
        {
            return DisableInterceptionAsync();
        }

        return Task.CompletedTask;
    }

    internal void OnClose()
    {
        IsClosed = true;
        Context?._pages.Remove(this);
        RejectPendingOperations(false);
        Close?.Invoke(this, this);
    }

    private void Channel_Crashed(object sender, EventArgs e)
    {
        RejectPendingOperations(true);
        Crash?.Invoke(this, this);
    }

    private void Channel_BindingCall(object sender, BindingCall bindingCall)
    {
        if (Bindings.TryGetValue(bindingCall.Name, out var binding))
        {
            _ = bindingCall.CallAsync(binding);
        }
    }

    private async Task OnRouteAsync(Route route)
    {
        var routeHandlers = _routes.ToArray();
        foreach (var routeHandler in routeHandlers)
        {
            var matches = (routeHandler.Regex?.IsMatch(route.Request.Url) == true) ||
                (routeHandler.Function?.Invoke(route.Request.Url) == true);
            if (!matches)
            {
                continue;
            }
            if (routeHandler.WillExpire())
            {
                _routes.Remove(routeHandler);
            }
            var handled = await routeHandler.HandleAsync(route).ConfigureAwait(false);
            if (_routes.Count == 0)
            {
                await DisableInterceptionAsync().ConfigureAwait(false);
            }
            if (handled)
            {
                return;
            }
        }

        await Context.OnRouteAsync(route).ConfigureAwait(false);
    }

    internal async Task DisableInterceptionAsync()
    {
        await Channel.SetNetworkInterceptionEnabledAsync(false).ConfigureAwait(false);
    }

    private void Channel_FrameDetached(object sender, IFrame args)
    {
        var frame = (Frame)args;
        _frames.Remove(frame);
        frame.IsDetached = true;
        frame.ParentFrame?._childFrames?.Remove(frame);
        FrameDetached?.Invoke(this, args);
    }

    private void Channel_FrameAttached(object sender, IFrame args)
    {
        var frame = (Frame)args;
        frame.Page = this;
        _frames.Add(frame);
        frame.ParentFrame?._childFrames?.Add(frame);
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

    private Task InnerExposeBindingAsync(string name, Delegate callback, bool handle = false)
    {
        if (Bindings.ContainsKey(name))
        {
            throw new PlaywrightException($"Function \"{name}\" has been already registered");
        }

        Bindings.Add(name, callback);

        return _channel.ExposeBindingAsync(name, handle);
    }

    private Video ForceVideo() => _video ??= new(this, _channel.Connection);

    private FrameSetInputFilesOptions Map(PageSetInputFilesOptions options)
    {
        if (options == null)
        {
            return null;
        }

        return new()
        {
            NoWaitAfter = options.NoWaitAfter,
            Timeout = options.Timeout,
            Strict = options.Strict,
        };
    }

    public async Task RouteFromHARAsync(string har, PageRouteFromHAROptions options = null)
    {
        if (options?.Update == true)
        {
            await Context.RecordIntoHarAsync(har, this, new()
            {
                NotFound = options?.NotFound,
                UrlString = options?.UrlString,
                UrlRegex = options?.UrlRegex,
            }).ConfigureAwait(false);
            return;
        }
        var harRouter = await HarRouter.CreateAsync(Channel.Connection.LocalUtils, har, options?.NotFound ?? HarNotFound.Abort, new()
        {
            UrlRegex = options?.UrlRegex,
            UrlString = options?.UrlString,
        }).ConfigureAwait(false);
        await harRouter.AddPageRouteAsync(this).ConfigureAwait(false);
    }

    public ILocator GetByAltText(string text, PageGetByAltTextOptions options = null)
        => MainFrame.GetByAltText(text, new() { Exact = options?.Exact });

    public ILocator GetByAltText(Regex text, PageGetByAltTextOptions options = null)
        => MainFrame.GetByAltText(text, new() { Exact = options?.Exact });

    public ILocator GetByLabel(string text, PageGetByLabelOptions options = null)
        => MainFrame.GetByLabel(text, new() { Exact = options?.Exact });

    public ILocator GetByLabel(Regex text, PageGetByLabelOptions options = null)
        => MainFrame.GetByLabel(text, new() { Exact = options?.Exact });

    public ILocator GetByPlaceholder(string text, PageGetByPlaceholderOptions options = null)
        => MainFrame.GetByPlaceholder(text, new() { Exact = options?.Exact });

    public ILocator GetByPlaceholder(Regex text, PageGetByPlaceholderOptions options = null)
        => MainFrame.GetByPlaceholder(text, new() { Exact = options?.Exact });

    public ILocator GetByRole(AriaRole role, PageGetByRoleOptions options = null)
        => Locator(Core.Locator.GetByRoleSelector(role, new(options)));

    public ILocator GetByTestId(string testId)
        => MainFrame.GetByTestId(testId);

    public ILocator GetByText(string text, PageGetByTextOptions options = null)
        => MainFrame.GetByText(text, new() { Exact = options?.Exact });

    public ILocator GetByText(Regex text, PageGetByTextOptions options = null)
        => MainFrame.GetByText(text, new() { Exact = options?.Exact });

    public ILocator GetByTitle(string text, PageGetByTitleOptions options = null)
        => MainFrame.GetByTitle(text, new() { Exact = options?.Exact });

    public ILocator GetByTitle(Regex text, PageGetByTitleOptions options = null)
        => MainFrame.GetByTitle(text, new() { Exact = options?.Exact });
}
