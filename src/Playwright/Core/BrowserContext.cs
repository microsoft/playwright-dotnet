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

internal class BrowserContext : ChannelOwnerBase, IChannelOwner<BrowserContext>, IBrowserContext
{
    private readonly TaskCompletionSource<bool> _closeTcs = new();
    private readonly Dictionary<string, Delegate> _bindings = new();
    private readonly BrowserContextInitializer _initializer;
    internal readonly Tracing _tracing;
    internal readonly IAPIRequestContext _request;
    private readonly IDictionary<string, HarRecorder> _harRecorders = new Dictionary<string, HarRecorder>();
    internal readonly List<IWorker> _serviceWorkers = new();
    private List<RouteHandler> _routes = new();
    internal readonly List<Page> _pages = new();
    private readonly Browser _browser;
    private bool _closeWasCalled;

    internal TimeoutSettings _timeoutSettings = new();

    internal BrowserContext(IChannelOwner parent, string guid, BrowserContextInitializer initializer) : base(parent, guid)
    {
        _browser = parent as Browser;
        _browser?._contexts.Add(this);
        Channel = new(guid, parent.Connection, this);
        Channel.Close += (_, _) => OnClose();
        Channel.Console += (_, consoleMessage) =>
        {
            _consoleImpl?.Invoke(this, consoleMessage);
            if (consoleMessage.Page != null)
            {
                (consoleMessage.Page as Page).FireConsole(consoleMessage);
            }
        };
        Channel.Dialog += (_, dialog) =>
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
        };
        Channel.Page += Channel_OnPage;
        Channel.BindingCall += Channel_BindingCall;
        Channel.Route += Channel_Route;
        Channel.RequestFailed += (_, e) =>
        {
            e.Request.Failure = e.FailureText;
            e.Request.SetResponseEndTiming(e.ResponseEndTiming);
            _requestFailedImpl?.Invoke(this, e.Request);
            e.Page?.FireRequestFailed(e.Request);
            e.Response?.ReportFinished(e.FailureText);
        };
        Channel.Request += (_, e) =>
        {
            _requestImpl?.Invoke(this, e.Request);
            e.Page?.FireRequest(e.Request);
        };
        Channel.RequestFinished += (_, e) =>
        {
            e.Request.SetResponseEndTiming(e.ResponseEndTiming);
            e.Request.Sizes = e.RequestSizes;
            _requestFinishedImpl?.Invoke(this, e.Request);
            e.Page?.FireRequestFinished(e.Request);
            e.Response?.ReportFinished();
        };
        Channel.Response += (_, e) =>
        {
            _responseImpl?.Invoke(this, e.Response);
            e.Page?.FireResponse(e.Response);
        };

        Channel.ServiceWorker += (_, serviceWorker) =>
        {
            ((Worker)serviceWorker).Context = this;
            _serviceWorkers.Add(serviceWorker);
            ServiceWorker?.Invoke(this, serviceWorker);
        };

        _tracing = initializer.Tracing;
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

    ChannelBase IChannelOwner.Channel => Channel;

    IChannel<BrowserContext> IChannelOwner<BrowserContext>.Channel => Channel;

    public IBrowser Browser => _browser;

    public IReadOnlyList<IPage> Pages => _pages;

    internal BrowserContextChannel Channel { get; }

    internal Page OwnerPage { get; set; }

    internal bool IsChromium => _initializer.IsChromium;

    internal BrowserNewContextOptions Options { get; set; }

    public IAPIRequestContext APIRequest => _request;

    public IReadOnlyList<IWorker> ServiceWorkers => _serviceWorkers;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task AddCookiesAsync(IEnumerable<Cookie> cookies) => Channel.AddCookiesAsync(cookies);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task AddInitScriptAsync(string script = null, string scriptPath = null)
    {
        if (string.IsNullOrEmpty(script))
        {
            script = ScriptsHelper.EvaluationScript(script, scriptPath);
        }

        return Channel.AddInitScriptAsync(script);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ClearCookiesAsync() => Channel.ClearCookiesAsync();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ClearPermissionsAsync() => Channel.ClearPermissionsAsync();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task CloseAsync()
    {
        if (_closeWasCalled)
        {
            return;
        }
        _closeWasCalled = true;
        await WrapApiCallAsync(
            async () =>
        {
            foreach (var harRecorder in _harRecorders)
            {
                Artifact artifact = await Channel.HarExportAsync(harRecorder.Key).ConfigureAwait(false);
                // Server side will compress artifact if content is attach or if file is .zip.
                var isCompressed = harRecorder.Value.Content == HarContentPolicy.Attach || harRecorder.Value.Path.EndsWith(".zip", StringComparison.Ordinal);
                var needCompressed = harRecorder.Value.Path.EndsWith(".zip", StringComparison.Ordinal);
                if (isCompressed && !needCompressed)
                {
                    await artifact.SaveAsAsync(harRecorder.Value.Path + ".tmp").ConfigureAwait(false);
                    await Channel.Connection.LocalUtils.HarUnzipAsync(harRecorder.Value.Path + ".tmp", harRecorder.Value.Path).ConfigureAwait(false);
                }
                else
                {
                    await artifact.SaveAsAsync(harRecorder.Value.Path).ConfigureAwait(false);
                }
                await artifact.DeleteAsync().ConfigureAwait(false);
            }
        },
            true).ConfigureAwait(false);
        await Channel.CloseAsync().ConfigureAwait(false);
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
    public Task<IReadOnlyList<BrowserContextCookiesResult>> CookiesAsync(IEnumerable<string> urls = null) => Channel.CookiesAsync(urls);

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
        => Channel.GrantPermissionsAsync(permissions, options?.Origin);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<ICDPSession> NewCDPSessionAsync(IPage page)
        => (await Channel.NewCDPSessionAsync(page as Page).ConfigureAwait(false)).Object;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<ICDPSession> NewCDPSessionAsync(IFrame frame)
        => (await Channel.NewCDPSessionAsync(frame as Frame).ConfigureAwait(false)).Object;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IPage> NewPageAsync()
    {
        if (OwnerPage != null)
        {
            throw new PlaywrightException("Please use Browser.NewContextAsync()");
        }

        return (await Channel.NewPageAsync().ConfigureAwait(false)).Object;
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
        => Channel.SetExtraHTTPHeadersAsync(headers);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task SetGeolocationAsync(Geolocation geolocation) => Channel.SetGeolocationAsync(geolocation);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task SetOfflineAsync(bool offline) => Channel.SetOfflineAsync(offline);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<string> StorageStateAsync(BrowserContextStorageStateOptions options = default)
    {
        string state = JsonSerializer.Serialize(
            await Channel.GetStorageStateAsync().ConfigureAwait(false),
            JsonExtensions.DefaultJsonSerializerOptions);

        if (!string.IsNullOrEmpty(options?.Path))
        {
            File.WriteAllText(options?.Path, state);
        }

        return state;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> InnerWaitForEventAsync<T>(PlaywrightEvent<T> playwrightEvent, Func<Task> action = default, Func<T, bool> predicate = default, float? timeout = default)
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
            waiter.RejectOnEvent<IBrowserContext>(this, BrowserContextEvent.Close.Name, new("Context closed"));
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

    public ValueTask DisposeAsync() => new(CloseAsync());

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetDefaultNavigationTimeout(float timeout) => SetDefaultNavigationTimeoutImpl(timeout);

    internal void SetDefaultNavigationTimeoutImpl(float? timeout)
    {
        _timeoutSettings.SetDefaultNavigationTimeout(timeout);
        WrapApiCallAsync(() => Channel.SetDefaultNavigationTimeoutNoReplyAsync(timeout), true).IgnoreException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetDefaultTimeout(float timeout) => SetDefaultTimeoutImpl(timeout);

    internal void SetDefaultTimeoutImpl(float? timeout)
    {
        _timeoutSettings.SetDefaultTimeout(timeout);
        WrapApiCallAsync(() => Channel.SetDefaultTimeoutNoReplyAsync(timeout), true).IgnoreException();
    }

    internal async Task OnRouteAsync(Route route)
    {
        var routeHandlers = _routes.ToArray();
        foreach (var routeHandler in routeHandlers)
        {
            var matches = routeHandler.Regex?.IsMatch(route.Request.Url) == true ||
                routeHandler.Function?.Invoke(route.Request.Url) == true;
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
                await UpdateInterceptionAsync().ConfigureAwait(false);
            }
            if (handled)
            {
                return;
            }
        }

        await route.InnerContinueAsync(true).ConfigureAwait(false);
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

    private Task UpdateInterceptionAsync()
    {
        var patterns = RouteHandler.PrepareInterceptionPatterns(_routes);
        return Channel.SetNetworkInterceptionPatternsAsync(patterns);
    }

    internal void OnClose()
    {
        if (Browser != null)
        {
            ((Browser)Browser)._contexts.Remove(this);
        }

        Close?.Invoke(this, this);
        _closeTcs.TrySetResult(true);
    }

    private void Channel_OnPage(object sender, BrowserContextPageEventArgs e)
    {
        var page = e.PageChannel.Object;
        page.Context = this;
        _pages.Add(page);
        Page?.Invoke(this, page);

        if (page.Opener?.IsClosed == false)
        {
            page.Opener.NotifyPopup(page);
        }
    }

    private void Channel_BindingCall(object sender, BindingCall bindingCall)
    {
        if (_bindings.TryGetValue(bindingCall.Name, out var binding))
        {
            _ = bindingCall.CallAsync(binding);
        }
    }

    private void Channel_Route(object sender, Route route) => OnRouteAsync(route).ConfigureAwait(false);

    private Task ExposeBindingAsync(string name, Delegate callback, bool handle = false)
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

        return Channel.ExposeBindingAsync(name, handle);
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
        var harId = await Channel.HarStartAsync(
            page,
            har,
            options?.Url,
            options?.UrlString,
            options?.UrlRegex,
            contentPolicy,
            options?.UpdateMode).ConfigureAwait(false);
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
        var harRouter = await HarRouter.CreateAsync(Channel.Connection.LocalUtils, har, options?.NotFound ?? HarNotFound.Abort, new()
        {
            Url = options?.Url,
            UrlRegex = options?.UrlRegex,
            UrlString = options?.UrlString,
        }).ConfigureAwait(false);
        await harRouter.AddContextRouteAsync(this).ConfigureAwait(false);
    }
}

internal class HarRecorder
{
    internal string Path { get; set; }

    internal HarContentPolicy? Content { get; set; }
}
