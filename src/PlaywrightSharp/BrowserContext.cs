using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserContext" />
    public class BrowserContext : ChannelOwnerBase, IChannelOwner<BrowserContext>, IBrowserContext
    {
        private readonly BrowserContextChannel _channel;
        private readonly List<Page> _crBackgroundPages = new List<Page>();
        private readonly TaskCompletionSource<bool> _closeTcs = new TaskCompletionSource<bool>();
        private readonly List<(IEvent contextEvent, TaskCompletionSource<bool> waitTcs)> _waitForCancellationTcs = new List<(IEvent contextEvent, TaskCompletionSource<bool> waitTcs)>();
        private readonly TimeoutSettings _timeoutSettings = new TimeoutSettings();
        private readonly Dictionary<string, Delegate> _bindings = new Dictionary<string, Delegate>();
        private readonly BrowserContextInitializer _initializer;
        private List<RouteSetting> _routes = new List<RouteSetting>();

        private bool _isClosedOrClosing;

        internal BrowserContext(IChannelOwner parent, string guid, BrowserContextInitializer initializer) : base(parent, guid)
        {
            _channel = new BrowserContextChannel(guid, parent.Connection, this);
            _channel.Close += Channel_Closed;
            _channel.Page += Channel_OnPage;
            _channel.BindingCall += Channel_BindingCall;
            _channel.Route += Channel_Route;
            _initializer = initializer;

            if (initializer.Pages != null)
            {
                foreach (var pageChannel in initializer.Pages)
                {
                    var page = ((PageChannel)pageChannel).Object;
                    PagesList.Add(page);
                    page.BrowserContext = this;
                }
            }

            if (initializer.CrBackgroundPages != null)
            {
                foreach (var pageChannel in initializer.CrBackgroundPages)
                {
                    var page = ((PageChannel)pageChannel).Object;
                    _crBackgroundPages.Add(page);
                    page.BrowserContext = this;
                    BackgroundPage?.Invoke(this, new PageEventArgs { Page = page });
                }
            }

            _channel.BackgroundPage += (sender, e) =>
            {
                var page = e.PageChannel.Object;
                page.BrowserContext = this;
                _crBackgroundPages.Add(page);
                BackgroundPage?.Invoke(this, new PageEventArgs { Page = page });
            };

            if (initializer.CrServiceWorkers != null)
            {
                foreach (var workerChannel in initializer.CrServiceWorkers)
                {
                    var worker = ((WorkerChannel)workerChannel).Object;
                    ServiceWorkersList.Add(worker);
                    worker.BrowserContext = this;
                    ServiceWorker?.Invoke(this, new WorkerEventArgs(worker));
                }
            }

            _channel.ServiceWorker += (sender, e) =>
            {
                var worker = e.WorkerChannel.Object;
                ServiceWorkersList.Add(worker);
                worker.BrowserContext = this;
                ServiceWorker?.Invoke(this, new WorkerEventArgs(worker));
            };
        }

        /// <inheritdoc/>
        public event EventHandler<EventArgs> Closed;

        /// <inheritdoc/>
        public event EventHandler<PageEventArgs> Page;

        /// <inheritdoc/>
        public event EventHandler<PageEventArgs> BackgroundPage;

        /// <inheritdoc/>
        public event EventHandler<WorkerEventArgs> ServiceWorker;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<BrowserContext> IChannelOwner<BrowserContext>.Channel => _channel;

        /// <inheritdoc />
        public IPage[] Pages => PagesList.ToArray();

        /// <inheritdoc />
        public IPage[] BackgroundPages => _crBackgroundPages.ToArray();

        /// <inheritdoc />
        public IWorker[] ServiceWorkers => ServiceWorkersList.ToArray();

        /// <inheritdoc />
        public int DefaultTimeout
        {
            get => _timeoutSettings.Timeout;
            set
            {
                _timeoutSettings.SetDefaultTimeout(value);
                _ = _channel.SetDefaultTimeoutNoReplyAsync(value);
            }
        }

        /// <inheritdoc />
        public int DefaultNavigationTimeout
        {
            get => _timeoutSettings.NavigationTimeout;
            set
            {
                _timeoutSettings.SetDefaultNavigationTimeout(value);
                _ = _channel.SetDefaultNavigationTimeoutNoReplyAsync(value);
            }
        }

        internal Browser Browser { get; set; }

        internal Page OwnerPage { get; set; }

        internal List<Page> PagesList { get; } = new List<Page>();

        internal List<Worker> ServiceWorkersList { get; } = new List<Worker>();

        internal string BrowserName => _initializer.BrowserName;

        /// <inheritdoc />
        public async Task<IPage> NewPageAsync(string url = null)
        {
            if (OwnerPage != null)
            {
                throw new PlaywrightSharpException("Please use Browser.NewContextAsync()");
            }

            return (await _channel.NewPageAsync(url).ConfigureAwait(false)).Object;
        }

        /// <inheritdoc />
        public Task CloseAsync()
        {
            if (!_isClosedOrClosing)
            {
                _isClosedOrClosing = true;
                return Task.WhenAny(_closeTcs.Task, _channel.CloseAsync());
            }

            return _closeTcs.Task;
        }

        /// <inheritdoc />
        public Task<IEnumerable<NetworkCookie>> GetCookiesAsync(params string[] urls) => _channel.GetCookiesAsync(urls);

        /// <inheritdoc />
        public Task AddCookiesAsync(IEnumerable<SetNetworkCookieParam> cookies) => AddCookiesAsync(cookies.ToArray());

        /// <inheritdoc />
        public Task AddCookiesAsync(params SetNetworkCookieParam[] cookies) => _channel.AddCookiesAsync(cookies);

        /// <inheritdoc />
        public Task ClearCookiesAsync() => _channel.ClearCookiesAsync();

        /// <inheritdoc />
        public Task GrantPermissionsAsync(ContextPermission[] permissions, string origin = null) => _channel.GrantPermissionsAsync(permissions, origin);

        /// <inheritdoc />
        public Task GrantPermissionsAsync(ContextPermission permission, string origin = null) => GrantPermissionsAsync(new[] { permission }, origin);

        /// <inheritdoc />
        public Task SetGeolocationAsync(decimal latitude, decimal longitude, decimal accuracy = 0)
            => SetGeolocationAsync(new Geolocation { Latitude = latitude, Longitude = longitude, Accuracy = accuracy });

        /// <inheritdoc />
        public Task SetGeolocationAsync(Geolocation geolocation) => _channel.SetGeolocationAsync(geolocation);

        /// <inheritdoc />
        public Task ClearPermissionsAsync() => _channel.ClearPermissionsAsync();

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

        /// <inheritdoc/>
        public Task ExposeBindingAsync(string name, Action<BindingSource> playwrightBinding)
            => ExposeBindingAsync(name, (Delegate)playwrightBinding);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> playwrightBinding)
            => ExposeBindingAsync(name, (Delegate)playwrightBinding);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> playwrightBinding)
            => ExposeBindingAsync(name, (Delegate)playwrightBinding);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> playwrightBinding)
            => ExposeBindingAsync(name, (Delegate)playwrightBinding);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> playwrightBinding)
            => ExposeBindingAsync(name, (Delegate)playwrightBinding);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> playwrightBinding)
            => ExposeBindingAsync(name, (Delegate)playwrightBinding);

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> playwrightBinding)
            => ExposeBindingAsync(name, (Delegate)playwrightBinding);

        /// <inheritdoc/>
        public Task ExposeFunctionAsync(string name, Action playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _) => playwrightFunction());

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T>(string name, Action<T> playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _, T t) => playwrightFunction(t));

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<TResult>(string name, Func<TResult> playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _) => playwrightFunction());

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _, T t) => playwrightFunction(t));

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2) => playwrightFunction(t1, t2));

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2, T3 t3) => playwrightFunction(t1, t2, t3));

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> playwrightFunction)
            => ExposeBindingAsync(name, (BindingSource _, T1 t1, T2 t2, T3 t3, T4 t4) => playwrightFunction(t1, t2, t3, t4));

        /// <inheritdoc/>
        public async Task<T> WaitForEvent<T>(PlaywrightEvent<T> e, Func<T, bool> predicate = null, int? timeout = null)
            where T : EventArgs
        {
            if (e == null)
            {
                throw new ArgumentException("Page event is required", nameof(e));
            }

            timeout ??= _timeoutSettings.Timeout;
            using var waiter = new Waiter();
            waiter.RejectOnTimeout(timeout, $"Timeout while waiting for event \"{typeof(T)}\"");

            if (e.Name != ContextEvent.Closed.Name)
            {
                waiter.RejectOnEvent<EventArgs>(this, "Closed", new TargetClosedException("Context closed"));
            }

            return await waiter.WaitForEventAsync(this, e.Name, predicate).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task AddInitScriptAsync(string script = null, object[] arg = null, string path = null, string content = null)
        {
            if (string.IsNullOrEmpty(script))
            {
                script = ScriptsHelper.EvaluationScript(content, path);
            }

            return _channel.AddInitScriptAsync(ScriptsHelper.SerializeScriptCall(script, arg));
        }

        /// <inheritdoc />
        public Task SetHttpCredentialsAsync(Credentials httpCredentials) => _channel.SetHttpCredentialsAsync(httpCredentials);

        /// <inheritdoc />
        public Task SetOfflineAsync(bool offline) => _channel.SetOfflineAsync(offline);

        /// <inheritdoc />
        public async Task<ICDPSession> NewCDPSessionAsync(IPage page) => (await _channel.NewCDPSessionAsync(page).ConfigureAwait(false))?.Object;

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
        public Task UnrouteAsync(string url, Action<Route, IRequest> handler = null)
            => UnrouteAsync(
                new RouteSetting
                {
                    Url = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task UnrouteAsync(Regex url, Action<Route, IRequest> handler = null)
            => UnrouteAsync(
                new RouteSetting
                {
                    Regex = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task UnrouteAsync(Func<string, bool> url, Action<Route, IRequest> handler = null)
            => UnrouteAsync(
                new RouteSetting
                {
                    Function = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task SetExtraHttpHeadersAsync(Dictionary<string, string> headers) => _channel.SetExtraHttpHeadersAsync(headers);

        internal void OnRoute(Route route, Request request)
        {
            foreach (var item in _routes)
            {
                if (
                    (item.Url != null && request.Url.UrlMatches(item.Url)) ||
                    (item.Regex?.IsMatch(request.Url) == true) ||
                    (item.Function?.Invoke(request.Url) == true))
                {
                    item.Handler(route, request);
                    return;
                }
            }

            _ = route.ContinueAsync();
        }

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

        private void Channel_Closed(object sender, EventArgs e)
        {
            _isClosedOrClosing = true;
            if (Browser != null)
            {
                Browser.BrowserContextsList.Remove(this);
            }

            Closed?.Invoke(this, EventArgs.Empty);
            _closeTcs.TrySetResult(true);
            RejectPendingOperations();
        }

        private void Channel_OnPage(object sender, BrowserContextPageEventArgs e)
        {
            var page = e.PageChannel.Object;
            page.BrowserContext = this;
            PagesList.Add(page);
            Page?.Invoke(this, new PageEventArgs { Page = page });
        }

        private void Channel_BindingCall(object sender, BindingCallEventArgs e)
        {
            if (_bindings.TryGetValue(e.BidingCall.Name, out var binding))
            {
                _ = e.BidingCall.CallAsync(binding);
            }
        }

        private void Channel_Route(object sender, RouteEventArgs e) => OnRoute(e.Route, e.Request);

        private void RejectPendingOperations()
        {
            foreach (var (_, waitTcs) in _waitForCancellationTcs.Where(e => e.contextEvent != ContextEvent.Closed))
            {
                waitTcs.TrySetException(new TargetClosedException("Context closed"));
            }

            _waitForCancellationTcs.Clear();
        }

        private Task ExposeBindingAsync(string name, Delegate playwrightFunction)
        {
            foreach (var page in PagesList)
            {
                if (page.Bindings.ContainsKey(name))
                {
                    throw new PlaywrightSharpException($"Function \"{name}\" has been already registered in one of the pages");
                }
            }

            if (_bindings.ContainsKey(name))
            {
                throw new PlaywrightSharpException($"Function \"{name}\" has been already registered");
            }

            _bindings.Add(name, playwrightFunction);

            return _channel.ExposeBindingAsync(name);
        }
    }
}
