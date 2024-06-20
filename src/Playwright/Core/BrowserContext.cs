/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class BrowserContext : ChannelOwner, IBrowserContext
{
    private readonly TaskCompletionSource<bool> _closeTcs = new();
    private readonly Dictionary<string, Delegate> _bindings = new();
    private readonly BrowserContextInitializer _initializer;
    private readonly Tracing _tracing;
    private readonly Clock _clock;
    internal readonly HashSet<IPage> _backgroundPages = new();
    internal readonly IAPIRequestContext _request;
    private readonly Dictionary<string, HarRecorder> _harRecorders = new();
    internal readonly List<IWorker> _serviceWorkers = new();
    private List<RouteHandler> _routes = new();
    internal readonly List<Page> _pages = new();
    private readonly Browser _browser;
    private readonly List<HarRouter> _harRouters = new();
    private string _closeReason;

    internal TimeoutSettings _timeoutSettings = new();

    internal BrowserContext(ChannelOwner parent, string guid, BrowserContextInitializer initializer) : base(parent, guid)
    {
        _browser = parent as Browser;
        _browser?._contexts.Add(this);

        _tracing = initializer.Tracing;
        _clock = new Clock(this);
        _request = initializer.RequestContext;
        _initializer = initializer;
    }

    private event EventHandler<IRequest> _requestImpl;

    private event EventHandler<IResponse> _responseImpl;

    private event EventHandler<IRequest> _requestFinishedImpl;

    private event EventHandler<IRequest> _requestFailedImpl;

    private event EventHandler<IConsoleMessage> _consoleImpl;

    private event EventHandler<IDialog> _dialogImpl;

    public event EventHandler<IBrowserContext> Close;

    public event EventHandler<IConsoleMessage> Console
    {
        add => this._consoleImpl = UpdateEventHandler("console", this._consoleImpl, value, true);
        remove => this._consoleImpl = UpdateEventHandler("console", this._consoleImpl, value, false);
    }

    public event EventHandler<IDialog> Dialog
    {
        add => this._dialogImpl = UpdateEventHandler("dialog", this._dialogImpl, value, true);
        remove => this._dialogImpl = UpdateEventHandler("dialog", this._dialogImpl, value, false);
    }

    public event EventHandler<IPage> Page;

    public event EventHandler<IPage> BackgroundPage;

    public event EventHandler<IWebError> WebError;

    public event EventHandler<IRequest> Request
    {
        add => this._requestImpl = UpdateEventHandler("request", this._requestImpl, value, true);
        remove => this._requestImpl = UpdateEventHandler("request", this._requestImpl, value, false);
    }

    public event EventHandler<IResponse> Response
    {
        add => this._responseImpl = UpdateEventHandler("response", this._responseImpl, value, true);
        remove => this._responseImpl = UpdateEventHandler("response", this._responseImpl, value, false);
    }

    public event EventHandler<IRequest> RequestFinished
    {
        add => this._requestFinishedImpl = UpdateEventHandler("requestFinished", this._requestFinishedImpl, value, true);
        remove => this._requestFinishedImpl = UpdateEventHandler("requestFinished", this._requestFinishedImpl, value, false);
    }

    public event EventHandler<IRequest> RequestFailed
    {
        add => this._requestFailedImpl = UpdateEventHandler("requestFailed", this._requestFailedImpl, value, true);
        remove => this._requestFailedImpl = UpdateEventHandler("requestFailed", this._requestFailedImpl, value, false);
    }

    public event EventHandler<IWorker> ServiceWorker;

    public ITracing Tracing
    {
        get => _tracing;
        set => throw new NotSupportedException();
    }

    public IClock Clock
    {
        get => _clock;
        set => throw new NotSupportedException();
    }

    public IBrowser Browser => _browser;

    public IReadOnlyList<IPage> Pages => _pages;

    internal Page OwnerPage { get; set; }

    internal bool IsChromium => _initializer.IsChromium;

    internal BrowserNewContextOptions Options { get; set; }

    internal bool CloseWasCalled { get; private set; }

    public IAPIRequestContext APIRequest => _request;

    public IReadOnlyList<IWorker> ServiceWorkers => _serviceWorkers;

    public IReadOnlyList<IPage> BackgroundPages => _backgroundPages.ToList();

    internal override void OnMessage(string method, JsonElement? serverParams)
    {
        switch (method)
        {
            case "close":
                OnClose();
                break;
            case "backgroundPage":
                {
                    var page = serverParams?.GetProperty("page").ToObject<Page>(_connection.DefaultJsonSerializerOptions);
                    _backgroundPages.Add(page);
                    BackgroundPage?.Invoke(this, page);
                    break;
                }
            case "bindingCall":
                Channel_BindingCall(
                    serverParams?.GetProperty("binding").ToObject<BindingCall>(_connection.DefaultJsonSerializerOptions));
                break;
            case "dialog":
                OnDialog(serverParams?.GetProperty("dialog").ToObject<Dialog>(_connection.DefaultJsonSerializerOptions));
                break;
            case "console":
                var consoleMessage = new ConsoleMessage(serverParams?.ToObject<BrowserContextConsoleEvent>(_connection.DefaultJsonSerializerOptions));
                _consoleImpl?.Invoke(this, consoleMessage);
                if (consoleMessage.Page != null)
                {
                    (consoleMessage.Page as Page).FireConsole(consoleMessage);
                }
                break;
            case "route":
                var route = serverParams?.GetProperty("route").ToObject<Route>(_connection.DefaultJsonSerializerOptions);
                Channel_Route(this, route);
                break;
            case "page":
                Channel_OnPage(
                    this,
                    serverParams?.GetProperty("page").ToObject<Page>(_connection.DefaultJsonSerializerOptions));
                break;
            case "pageError":
                {
                    var error = serverParams?.GetProperty("error").ToObject<SerializedError>(_connection.DefaultJsonSerializerOptions);
                    var pageObject = serverParams?.GetProperty("page").ToObject<Page>(_connection.DefaultJsonSerializerOptions);
                    var parsedError = string.IsNullOrEmpty(error.Error.Stack) ? $"{error.Error.Name}: {error.Error.Message}" : error.Error.Stack;
                    WebError?.Invoke(this, new WebError(pageObject, parsedError));
                    pageObject?.FirePageError(parsedError);
                    break;
                }
            case "serviceWorker":
                {
                    var serviceWorker = serverParams?.GetProperty("worker").ToObject<Worker>(_connection.DefaultJsonSerializerOptions);
                    ((Worker)serviceWorker).Context = this;
                    _serviceWorkers.Add(serviceWorker);
                    ServiceWorker?.Invoke(this, serviceWorker);
                    break;
                }
            case "request":
                {
                    var e = serverParams?.ToObject<BrowserContextChannelRequestEventArgs>(_connection.DefaultJsonSerializerOptions);
                    _requestImpl?.Invoke(this, e.Request);
                    e.Page?.FireRequest(e.Request);
                    break;
                }
            case "requestFinished":
                {
                    var e = serverParams?.ToObject<BrowserContextChannelRequestEventArgs>(_connection.DefaultJsonSerializerOptions);
                    e.Request.SetResponseEndTiming(e.ResponseEndTiming);
                    e.Request.Sizes = e.RequestSizes;
                    _requestFinishedImpl?.Invoke(this, e.Request);
                    e.Page?.FireRequestFinished(e.Request);
                    e.Response?.ReportFinished();
                    break;
                }
            case "requestFailed":
                {
                    var e = serverParams?.ToObject<BrowserContextChannelRequestEventArgs>(_connection.DefaultJsonSerializerOptions);
                    e.Request.Failure = e.FailureText;
                    e.Request.SetResponseEndTiming(e.ResponseEndTiming);
                    _requestFailedImpl?.Invoke(this, e.Request);
                    e.Page?.FireRequestFailed(e.Request);
                    e.Response?.ReportFinished(e.FailureText);
                }
                break;
            case "response":
                {
                    var e = serverParams?.ToObject<BrowserContextChannelResponseEventArgs>(_connection.DefaultJsonSerializerOptions);
                    _responseImpl?.Invoke(this, e.Response);
                    e.Page?.FireResponse(e.Response);
                }
                break;
        }
    }

    internal void OnDialog(IDialog dialog)
    {
        bool hasListeners = _dialogImpl?.GetInvocationList().Length > 0 || ((dialog?.Page as Page)?.HasDialogListenersAttached() ?? false);
        if (!hasListeners)
        {
            // Although we do similar handling on the server side, we still need this logic
            // on the client side due to a possible race condition between two async calls:
            // a) removing "dialog" listener subscription (client->server)
            // b) actual "dialog" event (server->client)
            if ("beforeunload".Equals(dialog.Type, StringComparison.Ordinal))
            {
                dialog.AcceptAsync().IgnoreException();
            }
            else
            {
                dialog.DismissAsync().IgnoreException();
            }
        }
        else
        {
            _dialogImpl?.Invoke(this, dialog);
            (dialog.Page as Page)?.FireDialog(dialog);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task AddCookiesAsync(IEnumerable<Cookie> cookies) => SendMessageToServerAsync(
            "addCookies",
            new Dictionary<string, object>
            {
                ["cookies"] = cookies,
            });

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task AddInitScriptAsync(string script = null, string scriptPath = null)
    {
        if (string.IsNullOrEmpty(script))
        {
            script = ScriptsHelper.EvaluationScript(script, scriptPath, true);
        }

        return SendMessageToServerAsync(
            "addInitScript",
            new Dictionary<string, object>
            {
                ["source"] = script,
            });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task ClearCookiesAsync(BrowserContextClearCookiesOptions options = default)
    {
        var @params = new Dictionary<string, object>
        {
            ["name"] = options?.Name ?? options?.NameString,
            ["nameRegexSource"] = options?.NameRegex?.ToString(),
            ["nameRegexFlags"] = options?.NameRegex?.Options.GetInlineFlags(),
            ["domain"] = options?.Domain ?? options?.DomainString,
            ["domainRegexSource"] = options?.DomainRegex?.ToString(),
            ["domainRegexFlags"] = options?.DomainRegex?.Options.GetInlineFlags(),
            ["path"] = options?.Path ?? options?.PathString,
            ["pathRegexSource"] = options?.PathRegex?.ToString(),
            ["pathRegexFlags"] = options?.PathRegex?.Options.GetInlineFlags(),
        };

        await SendMessageToServerAsync("clearCookies", @params).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ClearPermissionsAsync() => SendMessageToServerAsync("clearPermissions");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task CloseAsync(BrowserContextCloseOptions options = default)
    {
        if (CloseWasCalled)
        {
            return;
        }
        _closeReason = options?.Reason;
        CloseWasCalled = true;
        await WrapApiCallAsync(
            async () =>
        {
            foreach (var harRecorder in _harRecorders)
            {
                Artifact artifact = (await SendMessageToServerAsync(
        "harExport",
        new Dictionary<string, object>
        {
            ["harId"] = harRecorder.Key,
        }).ConfigureAwait(false)).GetObject<Artifact>("artifact", _connection);
                // Server side will compress artifact if content is attach or if file is .zip.
                var isCompressed = harRecorder.Value.Content == HarContentPolicy.Attach || harRecorder.Value.Path.EndsWith(".zip", StringComparison.Ordinal);
                var needCompressed = harRecorder.Value.Path.EndsWith(".zip", StringComparison.Ordinal);
                if (isCompressed && !needCompressed)
                {
                    await artifact.SaveAsAsync(harRecorder.Value.Path + ".tmp").ConfigureAwait(false);
                    await _connection.LocalUtils.HarUnzipAsync(harRecorder.Value.Path + ".tmp", harRecorder.Value.Path).ConfigureAwait(false);
                }
                else
                {
                    await artifact.SaveAsAsync(harRecorder.Value.Path).ConfigureAwait(false);
                }
                await artifact.DeleteAsync().ConfigureAwait(false);
            }
        },
            true).ConfigureAwait(false);
        await SendMessageToServerAsync("close", new Dictionary<string, object>
        {
            ["reason"] = options?.Reason,
        }).ConfigureAwait(false);
        await _closeTcs.Task.ConfigureAwait(false);
    }

    internal void SetOptions(BrowserNewContextOptions contextOptions, string tracesDir)
    {
        Options = contextOptions;
        if (!string.IsNullOrEmpty(Options?.RecordHarPath))
        {
            _harRecorders.Add(string.Empty, new() { Path = Options.RecordHarPath, Content = Options.RecordHarContent });
        }
        _tracing._tracesDir = tracesDir;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IReadOnlyList<BrowserContextCookiesResult>> CookiesAsync(IEnumerable<string> urls = null) => (await SendMessageToServerAsync(
            "cookies",
            new Dictionary<string, object>
            {
                ["urls"] = urls?.ToArray() ?? Array.Empty<string>(),
            }).ConfigureAwait(false))?.GetProperty("cookies").ToObject<IReadOnlyList<BrowserContextCookiesResult>>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeBindingAsync(string name, Action callback, BrowserContextExposeBindingOptions options = default)
        => ExposeBindingAsync(name, callback, handle: options?.Handle ?? false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeBindingAsync(string name, Action<BindingSource> callback)
        => ExposeBindingAsync(name, (Delegate)callback);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> callback)
        => ExposeBindingAsync(name, (Delegate)callback);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> callback)
        => ExposeBindingAsync(name, (Delegate)callback);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, IJSHandle, TResult> callback)
        => ExposeBindingAsync(name, callback, true);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> callback)
        => ExposeBindingAsync(name, (Delegate)callback);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> callback)
        => ExposeBindingAsync(name, (Delegate)callback);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> callback)
        => ExposeBindingAsync(name, (Delegate)callback);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> callback)
        => ExposeBindingAsync(name, (Delegate)callback);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeFunctionAsync(string name, Action callback)
        => ExposeBindingAsync(name, (BindingSource _) => callback());

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeFunctionAsync<T>(string name, Action<T> callback)
        => ExposeBindingAsync(name, (BindingSource _, T t) => callback(t));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeFunctionAsync<TResult>(string name, Func<TResult> callback)
        => ExposeBindingAsync(name, (BindingSource _) => callback());

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> callback)
        => ExposeBindingAsync(name, (BindingSource _, T t) => callback(t));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> callback)
        => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2) => callback(t1, t2));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> callback)
        => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2, T3 t3) => callback(t1, t2, t3));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> callback)
        => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2, T3 t3, T4 t4) => callback(t1, t2, t3, t4));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task GrantPermissionsAsync(IEnumerable<string> permissions, BrowserContextGrantPermissionsOptions options = default)
        => SendMessageToServerAsync("grantPermissions", new Dictionary<string, object>
        {
            ["permissions"] = permissions?.ToArray(),
            ["origin"] = options?.Origin,
        });

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<ICDPSession> NewCDPSessionAsync(IPage page)
        => await SendMessageToServerAsync<CDPSession>(
        "newCDPSession",
        new Dictionary<string, object>
        {
            ["page"] = new { guid = (page as Page).Guid },
        }).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<ICDPSession> NewCDPSessionAsync(IFrame frame)
        => await SendMessageToServerAsync<CDPSession>(
        "newCDPSession",
        new Dictionary<string, object>
        {
            ["frame"] = new { guid = (frame as Frame).Guid },
        }).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IPage> NewPageAsync()
    {
        if (OwnerPage != null)
        {
            throw new PlaywrightException("Please use Browser.NewContextAsync()");
        }

        return await SendMessageToServerAsync<Page>("newPage").ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task RouteAsync(string url, Action<IRoute> handler, BrowserContextRouteOptions options = default)
        => RouteAsync(new Regex(CombineUrlWithBase(url).GlobToRegex()), null, handler, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task RouteAsync(string url, Func<IRoute, Task> handler, BrowserContextRouteOptions options = null)
        => RouteAsync(new Regex(CombineUrlWithBase(url).GlobToRegex()), null, handler, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task RouteAsync(Regex url, Action<IRoute> handler, BrowserContextRouteOptions options = default)
        => RouteAsync(url, null, handler, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task RouteAsync(Regex url, Func<IRoute, Task> handler, BrowserContextRouteOptions options = default)
        => RouteAsync(url, null, handler, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task RouteAsync(Func<string, bool> url, Action<IRoute> handler, BrowserContextRouteOptions options = default)
        => RouteAsync(null, url, handler, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task RouteAsync(Func<string, bool> url, Func<IRoute, Task> handler, BrowserContextRouteOptions options = default)
        => RouteAsync(null, url, handler, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task SetExtraHTTPHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers)
        => SendMessageToServerAsync(
            "setExtraHTTPHeaders",
            new Dictionary<string, object>
            {
                ["headers"] = headers.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }),
            });

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task SetGeolocationAsync(Geolocation geolocation) => SendMessageToServerAsync(
            "setGeolocation",
            new Dictionary<string, object>
            {
                ["geolocation"] = geolocation,
            });

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task SetOfflineAsync(bool offline) => SendMessageToServerAsync(
            "setOffline",
            new Dictionary<string, object>
            {
                ["offline"] = offline,
            });

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<string> StorageStateAsync(BrowserContextStorageStateOptions options = default)
    {
        string state = JsonSerializer.Serialize(
            await SendMessageToServerAsync<StorageState>("storageState").ConfigureAwait(false),
            JsonExtensions.DefaultJsonSerializerOptions);

        if (!string.IsNullOrEmpty(options?.Path))
        {
            File.WriteAllText(options?.Path, state);
        }

        return state;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task UnrouteAllAsync(BrowserContextUnrouteAllOptions options = default)
    {
        await UnrouteInternalAsync(_routes, new(), options?.Behavior).ConfigureAwait(false);
        DisposeHarRouters();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task UnrouteAsync(string urlString, Action<IRoute> handler = default)
        => UnrouteAsync(new Regex(CombineUrlWithBase(urlString).GlobToRegex()), null, handler);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task UnrouteAsync(string urlString, Func<IRoute, Task> handler = null)
        => UnrouteAsync(new Regex(CombineUrlWithBase(urlString).GlobToRegex()), null, handler);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task UnrouteAsync(Regex urlRegex, Action<IRoute> handler = default)
        => UnrouteAsync(urlRegex, null, handler);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task UnrouteAsync(Regex urlRegex, Func<IRoute, Task> handler = default)
        => UnrouteAsync(urlRegex, null, handler);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task UnrouteAsync(Func<string, bool> urlFunc, Action<IRoute> handler = default)
        => UnrouteAsync(null, urlFunc, handler);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task UnrouteAsync(Func<string, bool> urlFunc, Func<IRoute, Task> handler = default)
        => UnrouteAsync(null, urlFunc, handler);

    internal string _effectiveCloseReason()
    {
        return _closeReason ?? _browser._closeReason;
    }

    internal async Task<T> InnerWaitForEventAsync<T>(PlaywrightEvent<T> playwrightEvent, Func<Task> action = default, Func<T, bool> predicate = default, float? timeout = default)
    {
        if (playwrightEvent == null)
        {
            throw new ArgumentException("Page event is required", nameof(playwrightEvent));
        }

        timeout = _timeoutSettings.Timeout(timeout);
        using var waiter = new Waiter(this, $"context.WaitForEventAsync(\"{playwrightEvent.Name}\")");
        waiter.RejectOnTimeout(Convert.ToInt32(timeout, CultureInfo.InvariantCulture), $"Timeout {timeout}ms exceeded while waiting for event \"{playwrightEvent.Name}\"");

        if (playwrightEvent.Name != BrowserContextEvent.Close.Name)
        {
            waiter.RejectOnEvent<IBrowserContext>(this, BrowserContextEvent.Close.Name, () => new TargetClosedException(_effectiveCloseReason()));
        }

        var result = waiter.WaitForEventAsync(this, playwrightEvent.Name, predicate);
        if (action != null)
        {
            await WrapApiBoundaryAsync(() => waiter.CancelWaitOnExceptionAsync(result, action)).ConfigureAwait(false);
        }

        return await result.ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IPage> WaitForPageAsync(BrowserContextWaitForPageOptions options = default)
        => InnerWaitForEventAsync(BrowserContextEvent.Page, null, options?.Predicate, options?.Timeout);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IPage> RunAndWaitForPageAsync(Func<Task> action, BrowserContextRunAndWaitForPageOptions options = default)
        => InnerWaitForEventAsync(BrowserContextEvent.Page, action, options?.Predicate, options?.Timeout);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IConsoleMessage> WaitForConsoleMessageAsync(BrowserContextWaitForConsoleMessageOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Console, null, options?.Predicate, options?.Timeout);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IConsoleMessage> RunAndWaitForConsoleMessageAsync(Func<Task> action, BrowserContextRunAndWaitForConsoleMessageOptions options = default)
        => InnerWaitForEventAsync(PageEvent.Console, action, options?.Predicate, options?.Timeout);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ValueTask DisposeAsync() => new(CloseAsync());

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetDefaultNavigationTimeout(float timeout) => SetDefaultNavigationTimeoutImpl(timeout);

    internal void SetDefaultNavigationTimeoutImpl(float? timeout)
    {
        _timeoutSettings.SetDefaultNavigationTimeout(timeout);
        WrapApiCallAsync(
            () => SendMessageToServerAsync(
            "setDefaultNavigationTimeoutNoReply",
            new Dictionary<string, object>
            {
                ["timeout"] = timeout,
            }),
            true).IgnoreException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetDefaultTimeout(float timeout) => SetDefaultTimeoutImpl(timeout);

    internal void SetDefaultTimeoutImpl(float? timeout)
    {
        _timeoutSettings.SetDefaultTimeout(timeout);
        WrapApiCallAsync(
            () => SendMessageToServerAsync(
            "setDefaultTimeoutNoReply",
            new Dictionary<string, object>
            {
                ["timeout"] = timeout,
            }),
            true).IgnoreException();
    }

    internal async Task OnRouteAsync(Route route)
    {
        route._context = this;
        var page = route._request.SafePage;
        var routeHandlers = _routes.ToArray();
        foreach (var routeHandler in routeHandlers)
        {
            // If the page or the context was closed we stall all requests right away.
            if (page?.CloseWasCalled == true || CloseWasCalled)
            {
                return;
            }
            var matches = routeHandler.Regex?.IsMatch(route.Request.Url) == true ||
                routeHandler.Function?.Invoke(route.Request.Url) == true;
            if (!matches)
            {
                continue;
            }
            if (!_routes.Contains(routeHandler))
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
                await UpdateInterceptionAsync().ConfigureAwait(false);
            }
            if (handled)
            {
                return;
            }
        }

        try
        {
            await route.InnerContinueAsync(true).ConfigureAwait(false);
        }
        catch
        {
            // If the page is closed or UnrouteAll() was called without waiting and interception disabled,
            // the method will throw an error - silence it.
        }
    }

    internal bool UrlMatches(string url, string glob)
        => new Regex(CombineUrlWithBase(glob).GlobToRegex()).Match(url).Success;

    internal string CombineUrlWithBase(string url)
    {
        var baseUrl = Options?.BaseURL;
        if (string.IsNullOrEmpty(baseUrl)
            || (url?.StartsWith("*", StringComparison.InvariantCultureIgnoreCase) ?? false)
            || !Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
        {
            return url;
        }

        var mUri = new Uri(url, UriKind.RelativeOrAbsolute);
        if (!mUri.IsAbsoluteUri)
        {
            return new Uri(new Uri(baseUrl), mUri).ToString();
        }

        return url;
    }

    private Task RouteAsync(Regex urlRegex, Func<string, bool> urlFunc, Delegate handler, BrowserContextRouteOptions options)
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
        return UpdateInterceptionAsync();
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
        return UpdateInterceptionAsync();
    }

    private async Task UnrouteInternalAsync(List<RouteHandler> removed, List<RouteHandler> remaining, UnrouteBehavior? behavior)
    {
        _routes = remaining;
        await UpdateInterceptionAsync().ConfigureAwait(false);
        if (behavior == null || behavior == UnrouteBehavior.Default)
        {
            return;
        }
        var tasks = removed.Select(routeHandler => routeHandler.StopAsync((UnrouteBehavior)behavior));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task UpdateInterceptionAsync()
    {
        var patterns = RouteHandler.PrepareInterceptionPatterns(_routes);
        await SendMessageToServerAsync(
            "setNetworkInterceptionPatterns",
            patterns).ConfigureAwait(false);
    }

    internal void OnClose()
    {
        if (Browser != null)
        {
            ((Browser)Browser)._contexts.Remove(this);
        }

        DisposeHarRouters();
        _tracing.ResetStackCounter();
        Close?.Invoke(this, this);
        _closeTcs.TrySetResult(true);
    }

    private void Channel_OnPage(object sender, Page page)
    {
        page.Context = this;
        _pages.Add(page);
        Page?.Invoke(this, page);

        if (page.Opener?.IsClosed == false)
        {
            page.Opener.NotifyPopup(page);
        }
    }

    private void Channel_BindingCall(BindingCall bindingCall)
    {
        if (_bindings.TryGetValue(bindingCall.Name, out var binding))
        {
            _ = bindingCall.CallAsync(binding);
        }
    }

    private void Channel_Route(object sender, Route route) => _ = OnRouteAsync(route).ConfigureAwait(false);

    private async Task ExposeBindingAsync(string name, Delegate callback, bool handle = false)
    {
        foreach (var page in _pages)
        {
            if (page.Bindings.ContainsKey(name))
            {
                throw new PlaywrightException($"Function \"{name}\" has been already registered in one of the pages");
            }
        }

        if (_bindings.ContainsKey(name))
        {
            throw new PlaywrightException($"Function \"{name}\" has been already registered");
        }

        _bindings.Add(name, callback);

        await SendMessageToServerAsync(
            "exposeBinding",
            new Dictionary<string, object>
            {
                ["name"] = name,
                ["needsHandle"] = handle,
            }).ConfigureAwait(false);
    }

    private HarContentPolicy? RouteFromHarUpdateContentPolicyToHarContentPolicy(RouteFromHarUpdateContentPolicy? policy)
    {
        switch (policy)
        {
            case RouteFromHarUpdateContentPolicy.Attach:
                return HarContentPolicy.Attach;
            case RouteFromHarUpdateContentPolicy.Embed:
                return HarContentPolicy.Embed;
            default:
                return null;
        }
    }

    internal async Task RecordIntoHarAsync(string har, Page page, BrowserContextRouteFromHAROptions options)
    {
        var contentPolicy = RouteFromHarUpdateContentPolicyToHarContentPolicy(options?.UpdateContent);
        var harId = (await SendMessageToServerAsync("harStart", new Dictionary<string, object>
            {
                { "page", page },
                { "options", Core.Browser.PrepareHarOptions(contentPolicy ?? HarContentPolicy.Attach, options.UpdateMode ?? HarMode.Minimal, har, null, options.Url, options.UrlString, options.UrlRegex) },
            }).ConfigureAwait(false)).GetString("harId", false);
        _harRecorders.Add(harId, new() { Path = har, Content = contentPolicy ?? HarContentPolicy.Attach });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task RouteFromHARAsync(string har, BrowserContextRouteFromHAROptions options = null)
    {
        if (options?.Update == true)
        {
            await RecordIntoHarAsync(har, null, options).ConfigureAwait(false);
            return;
        }
        var harRouter = await HarRouter.CreateAsync(_connection.LocalUtils, har, options?.NotFound ?? HarNotFound.Abort, new()
        {
            Url = options?.Url,
            UrlRegex = options?.UrlRegex,
            UrlString = options?.UrlString,
        }).ConfigureAwait(false);
        _harRouters.Add(harRouter);
        await harRouter.AddContextRouteAsync(this).ConfigureAwait(false);
    }

    private void DisposeHarRouters()
    {
        foreach (var router in _harRouters)
        {
            router.Dispose();
        }
        _harRouters.Clear();
    }
}

internal class HarRecorder
{
    internal string Path { get; set; }

    internal HarContentPolicy? Content { get; set; }
}
