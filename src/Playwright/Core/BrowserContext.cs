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
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    internal class BrowserContext : ChannelOwnerBase, IChannelOwner<BrowserContext>, IBrowserContext
    {
        private readonly TaskCompletionSource<bool> _closeTcs = new();
        private readonly Dictionary<string, Delegate> _bindings = new();
        private readonly BrowserContextInitializer _initializer;
        private readonly ITracing _tracing;
        private List<RouteSetting> _routes = new();

        private float? _defaultNavigationTimeout;
        private float? _defaultTimeout;

        internal BrowserContext(IChannelOwner parent, string guid, BrowserContextInitializer initializer) : base(parent, guid)
        {
            Channel = new(guid, parent.Connection, this);
            Channel.Close += (_, _) => OnClose();
            Channel.Page += Channel_OnPage;
            Channel.BindingCall += Channel_BindingCall;
            Channel.Route += Channel_Route;
            Channel.RequestFailed += (_, e) =>
            {
                e.Request.Failure = e.FailureText;
                e.Request.Timing.ResponseEnd = e.ResponseEndTiming;
                RequestFailed?.Invoke(this, e.Request);
                e.Page?.FireRequestFailed(e.Request);
                e.Response?.ReportFinished(e.FailureText);
            };
            Channel.Request += (_, e) =>
            {
                Request?.Invoke(this, e.Request);
                e.Page?.FireRequest(e.Request);
            };
            Channel.RequestFinished += (_, e) =>
            {
                e.Request.Timing.ResponseEnd = e.ResponseEndTiming;
                e.Request.Sizes = e.RequestSizes;
                RequestFinished?.Invoke(this, e.Request);
                e.Page?.FireRequestFinished(e.Request);
                e.Response?.ReportFinished();
            };
            Channel.Response += (_, e) =>
            {
                Response?.Invoke(this, e.Response);
                e.Page?.FireResponse(e.Response);
            };

            _tracing = new Tracing(this);
            _initializer = initializer;
            Browser = parent as IBrowser;
        }

        public event EventHandler<IBrowserContext> Close;

        public event EventHandler<IPage> Page;

        public event EventHandler<IRequest> Request;

        public event EventHandler<IRequest> RequestFailed;

        public event EventHandler<IRequest> RequestFinished;

        public event EventHandler<IResponse> Response;

        public ITracing Tracing
        {
            get => _tracing;
            set => throw new NotSupportedException();
        }

        ChannelBase IChannelOwner.Channel => Channel;

        IChannel<BrowserContext> IChannelOwner<BrowserContext>.Channel => Channel;

        public IBrowser Browser { get; }

        internal LocalUtils LocalUtils { get; set; }

        public IReadOnlyList<IPage> Pages => PagesList;

        internal float DefaultNavigationTimeout
        {
            get => _defaultNavigationTimeout ?? PlaywrightImpl.DefaultTimeout;
            set
            {
                _defaultNavigationTimeout = value;
                _ = Channel.SetDefaultNavigationTimeoutNoReplyAsync(value);
            }
        }

        internal float DefaultTimeout
        {
            get => _defaultTimeout ?? PlaywrightImpl.DefaultTimeout;
            set
            {
                _defaultTimeout = value;
                _ = Channel.SetDefaultTimeoutNoReplyAsync(value);
            }
        }

        internal BrowserContextChannel Channel { get; }

        internal List<Page> PagesList { get; } = new();

        internal Page OwnerPage { get; set; }

        internal List<Worker> ServiceWorkersList { get; } = new();

        internal bool IsChromium => _initializer.IsChromium;

        internal BrowserNewContextOptions Options { get; set; }

        public Task AddCookiesAsync(IEnumerable<Cookie> cookies) => Channel.AddCookiesAsync(cookies);

        public Task AddInitScriptAsync(string script = null, string scriptPath = null)
        {
            if (string.IsNullOrEmpty(script))
            {
                script = ScriptsHelper.EvaluationScript(script, scriptPath);
            }

            return Channel.AddInitScriptAsync(script);
        }

        public Task ClearCookiesAsync() => Channel.ClearCookiesAsync();

        public Task ClearPermissionsAsync() => Channel.ClearPermissionsAsync();

        public async Task CloseAsync()
        {
            try
            {
                if (Options.RecordHarPath != null)
                {
                    Artifact artifact = await Channel.HarExportAsync().ConfigureAwait(false);
                    await artifact.SaveAsAsync(Options.RecordHarPath).ConfigureAwait(false);
                    await artifact.DeleteAsync().ConfigureAwait(false);
                }
                await Channel.CloseAsync().ConfigureAwait(false);
                await _closeTcs.Task.ConfigureAwait(false);
            }
            catch (Exception e) when (DriverMessages.IsSafeCloseError(e))
            {
                // Swallow exception
            }
        }

        public Task<IReadOnlyList<BrowserContextCookiesResult>> CookiesAsync(IEnumerable<string> urls = null) => Channel.CookiesAsync(urls);

        public Task ExposeBindingAsync(string name, Action callback, BrowserContextExposeBindingOptions options = default)
            => ExposeBindingAsync(name, callback, handle: options?.Handle ?? false);

        public Task ExposeBindingAsync(string name, Action<BindingSource> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, IJSHandle, TResult> callback)
            => ExposeBindingAsync(name, callback, true);

        public Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> callback)
            => ExposeBindingAsync(name, (Delegate)callback);

        public Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> callback)
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

        public Task GrantPermissionsAsync(IEnumerable<string> permissions, BrowserContextGrantPermissionsOptions options = default)
            => Channel.GrantPermissionsAsync(permissions, options?.Origin);

        public async Task<IPage> NewPageAsync()
        {
            if (OwnerPage != null)
            {
                throw new PlaywrightException("Please use Browser.NewContextAsync()");
            }

            return (await Channel.NewPageAsync().ConfigureAwait(false)).Object;
        }

        public Task RouteAsync(string url, Action<IRoute> handler, BrowserContextRouteOptions options = default)
            => RouteAsync(new Regex(CombineUrlWithBase(url).GlobToRegex()), null, handler, options);

        public Task RouteAsync(Regex url, Action<IRoute> handler, BrowserContextRouteOptions options = default)
            => RouteAsync(url, null, handler, options);

        public Task RouteAsync(Func<string, bool> url, Action<IRoute> handler, BrowserContextRouteOptions options = default)
            => RouteAsync(null, url, handler, options);

        public Task SetExtraHTTPHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers)
            => Channel.SetExtraHTTPHeadersAsync(headers);

        public Task SetGeolocationAsync(Geolocation geolocation) => Channel.SetGeolocationAsync(geolocation);

        public Task SetOfflineAsync(bool offline) => Channel.SetOfflineAsync(offline);

        public async Task<string> StorageStateAsync(BrowserContextStorageStateOptions options = default)
        {
            var serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            string state = JsonSerializer.Serialize(
                await Channel.GetStorageStateAsync().ConfigureAwait(false),
                serializerOptions);

            if (!string.IsNullOrEmpty(options?.Path))
            {
                File.WriteAllText(options?.Path, state);
            }

            return state;
        }

        public Task UnrouteAsync(string urlString, Action<IRoute> handler = default)
            => UnrouteAsync(new Regex(CombineUrlWithBase(urlString).GlobToRegex()), null, handler);

        public Task UnrouteAsync(Regex urlRegex, Action<IRoute> handler = default)
            => UnrouteAsync(urlRegex, null, handler);

        public Task UnrouteAsync(Func<string, bool> urlFunc, Action<IRoute> handler = default)
            => UnrouteAsync(null, urlFunc, handler);

        public async Task<T> InnerWaitForEventAsync<T>(PlaywrightEvent<T> playwrightEvent, Func<Task> action = default, Func<T, bool> predicate = default, float? timeout = default)
        {
            if (playwrightEvent == null)
            {
                throw new ArgumentException("Page event is required", nameof(playwrightEvent));
            }

            timeout ??= DefaultTimeout;
            using var waiter = new Waiter(this, $"context.WaitForEventAsync(\"{playwrightEvent.Name}\")");
            waiter.RejectOnTimeout(Convert.ToInt32(timeout), $"Timeout {timeout}ms exceeded while waiting for event \"{playwrightEvent.Name}\"");

            if (playwrightEvent.Name != BrowserContextEvent.Close.Name)
            {
                waiter.RejectOnEvent<IBrowserContext>(this, BrowserContextEvent.Close.Name, new("Context closed"));
            }

            var result = waiter.WaitForEventAsync(this, playwrightEvent.Name, predicate);
            if (action != null)
            {
                await Task.WhenAll(result, action()).ConfigureAwait(false);
            }

            return await result.ConfigureAwait(false);
        }

        public Task<IPage> WaitForPageAsync(BrowserContextWaitForPageOptions options = default)
            => InnerWaitForEventAsync(BrowserContextEvent.Page, null, options?.Predicate, options?.Timeout);

        public Task<IPage> RunAndWaitForPageAsync(Func<Task> action, BrowserContextRunAndWaitForPageOptions options = default)
            => InnerWaitForEventAsync(BrowserContextEvent.Page, action, options?.Predicate, options?.Timeout);

        public ValueTask DisposeAsync() => new ValueTask(CloseAsync());

        public void SetDefaultNavigationTimeout(float timeout) => DefaultNavigationTimeout = timeout;

        public void SetDefaultTimeout(float timeout) => DefaultTimeout = timeout;

        internal void OnRoute(Route route, IRequest request)
        {
            foreach (var routeHandler in _routes)
            {
                if (
                    routeHandler.Regex?.IsMatch(request.Url) == true ||
                    routeHandler.Function?.Invoke(request.Url) == true)
                {
                    try
                    {
                        routeHandler.Handle(route);
                    }
                    finally
                    {
                        if (!routeHandler.IsActive())
                        {
                            _routes.Remove(routeHandler);
                            if (_routes.Count == 0)
                            {
                                DisableInterceptionAsync().ConfigureAwait(false);
                            }
                        }
                    }
                    return;
                }
            }

            _ = route.ContinueInternalAsync(new());
        }

        internal async Task DisableInterceptionAsync()
        {
            await Channel.SetNetworkInterceptionEnabledAsync(false).ConfigureAwait(false);
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

        private Task RouteAsync(Regex urlRegex, Func<string, bool> urlFunc, Action<IRoute> handler, BrowserContextRouteOptions options)
            => RouteAsync(new()
            {
                Regex = urlRegex,
                Function = urlFunc,
                Handler = handler,
                Times = options?.Times,
            });

        private Task RouteAsync(RouteSetting setting)
        {
            _routes.Insert(0, setting);

            if (_routes.Count == 1)
            {
                return Channel.SetNetworkInterceptionEnabledAsync(true);
            }

            return Task.CompletedTask;
        }

        private Task UnrouteAsync(Regex urlRegex, Func<string, bool> urlFunc, Action<IRoute> handler = null)
            => UnrouteAsync(new()
            {
                Function = urlFunc,
                Regex = urlRegex,
                Handler = handler,
            });

        private Task UnrouteAsync(RouteSetting setting)
        {
            var newRoutesList = new List<RouteSetting>();
            newRoutesList.AddRange(_routes.Where(r =>
                (setting.Regex != null && !(r.Regex == setting.Regex || (r.Regex.ToString() == setting.Regex.ToString() && r.Regex.Options == setting.Regex.Options))) ||
                (setting.Function != null && r.Function != setting.Function) ||
                (setting.Handler != null && r.Handler != setting.Handler)));
            _routes = newRoutesList;

            if (_routes.Count == 0)
            {
                return Channel.SetNetworkInterceptionEnabledAsync(false);
            }

            return Task.CompletedTask;
        }

        internal void OnClose()
        {
            if (Browser != null)
            {
                ((Browser)Browser).BrowserContextsList.Remove(this);
            }

            Close?.Invoke(this, this);
            _closeTcs.TrySetResult(true);
        }

        private void Channel_OnPage(object sender, BrowserContextPageEventArgs e)
        {
            var page = e.PageChannel.Object;
            page.Context = this;
            PagesList.Add(page);
            Page?.Invoke(this, page);

            if (page.Opener?.IsClosed == false)
            {
                page.Opener.NotifyPopup(page);
            }
        }

        private void Channel_BindingCall(object sender, BindingCallEventArgs e)
        {
            if (_bindings.TryGetValue(e.BidingCall.Name, out var binding))
            {
                _ = e.BidingCall.CallAsync(binding);
            }
        }

        private void Channel_Route(object sender, RouteEventArgs e) => OnRoute(e.Route, e.Request);

        private Task ExposeBindingAsync(string name, Delegate callback, bool handle = false)
        {
            foreach (var page in PagesList)
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
    }
}
