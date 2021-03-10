using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly TaskCompletionSource<bool> _closeTcs = new();
        private readonly List<(IEvent ContextEvent, TaskCompletionSource<bool> WaitTcs)> _waitForCancellationTcs = new();
        private readonly Dictionary<string, Delegate> _bindings = new();
        private readonly BrowserContextInitializer _initializer;
        private List<RouteSetting> _routes = new();

        private bool _isClosedOrClosing;

        internal BrowserContext(IChannelOwner parent, string guid, BrowserContextInitializer initializer) : base(parent, guid)
        {
            Channel = new BrowserContextChannel(guid, parent.Connection, this);
            Channel.Close += Channel_Closed;
            Channel.Page += Channel_OnPage;
            Channel.BindingCall += Channel_BindingCall;
            Channel.Route += Channel_Route;
            _initializer = initializer;
            Browser = parent as IBrowser;

            if (initializer.Pages != null)
            {
                foreach (var pageChannel in initializer.Pages)
                {
                    var page = ((PageChannel)pageChannel).Object;
                    PagesList.Add(page);
                    page.Context = this;
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler Close;

        /// <inheritdoc/>
        public event EventHandler<PageEventArgs> Page;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => Channel;

        /// <inheritdoc/>
        IChannel<BrowserContext> IChannelOwner<BrowserContext>.Channel => Channel;

        /// <inheritdoc />
        public IBrowser Browser { get; }

        /// <inheritdoc />
        public IPage[] Pages => PagesList.ToArray();

        /// <inheritdoc />
        public int DefaultTimeout
        {
            get => TimeoutSettings.Timeout;
            set
            {
                TimeoutSettings.SetDefaultTimeout(value);
                _ = Channel.SetDefaultTimeoutNoReplyAsync(value);
            }
        }

        /// <inheritdoc />
        public int DefaultNavigationTimeout
        {
            get => TimeoutSettings.NavigationTimeout;
            set
            {
                TimeoutSettings.SetDefaultNavigationTimeout(value);
                _ = Channel.SetDefaultNavigationTimeoutNoReplyAsync(value);
            }
        }

        internal BrowserContextChannel Channel { get; }

        internal Page OwnerPage { get; set; }

        internal List<Page> PagesList { get; } = new List<Page>();

        internal List<Worker> ServiceWorkersList { get; } = new List<Worker>();

        internal bool IsChromium => _initializer.IsChromium;

        internal BrowserContextOptions Options { get; set; }

        internal TimeoutSettings TimeoutSettings { get; } = new();

        /// <inheritdoc />
        public async Task<IPage> NewPageAsync(string url = null)
        {
            if (OwnerPage != null)
            {
                throw new PlaywrightSharpException("Please use Browser.NewContextAsync()");
            }

            return (await Channel.NewPageAsync(url).ConfigureAwait(false)).Object;
        }

        /// <inheritdoc />
        public async Task CloseAsync()
        {
            try
            {
                if (!_isClosedOrClosing)
                {
                    _isClosedOrClosing = true;
                    await Channel.CloseAsync().ConfigureAwait(false);
                    await _closeTcs.Task.ConfigureAwait(false);
                }

                await _closeTcs.Task.ConfigureAwait(false);
            }
            catch (Exception e) when (IsSafeCloseException(e))
            {
                // Swallow exception
            }
        }

        /// <inheritdoc />
        public Task<IEnumerable<NetworkCookie>> GetCookiesAsync(params string[] urls) => Channel.GetCookiesAsync(urls);

        /// <inheritdoc />
        public Task AddCookiesAsync(IEnumerable<SetNetworkCookieParam> cookies) => AddCookiesAsync(cookies.ToArray());

        /// <inheritdoc />
        public Task AddCookiesAsync(params SetNetworkCookieParam[] cookies) => Channel.AddCookiesAsync(cookies);

        /// <inheritdoc />
        public Task ClearCookiesAsync() => Channel.ClearCookiesAsync();

        /// <inheritdoc />
        public Task GrantPermissionsAsync(ContextPermission[] permissions, string origin = null) => Channel.GrantPermissionsAsync(permissions, origin);

        /// <inheritdoc />
        public Task GrantPermissionsAsync(ContextPermission permission, string origin = null) => GrantPermissionsAsync(new[] { permission }, origin);

        /// <inheritdoc />
        public Task SetGeolocationAsync(decimal latitude, decimal longitude, decimal accuracy = 0)
            => SetGeolocationAsync(new Geolocation { Latitude = latitude, Longitude = longitude, Accuracy = accuracy });

        /// <inheritdoc />
        public Task SetGeolocationAsync(Geolocation geolocation) => Channel.SetGeolocationAsync(geolocation);

        /// <inheritdoc />
        public Task ClearPermissionsAsync() => Channel.ClearPermissionsAsync();

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

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

        /// <inheritdoc/>
        public async Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> e, Func<T, bool> predicate = null, int? timeout = null)
            where T : EventArgs
        {
            if (e == null)
            {
                throw new ArgumentException("Page event is required", nameof(e));
            }

            timeout ??= TimeoutSettings.Timeout;
            using var waiter = new Waiter();
            waiter.RejectOnTimeout(timeout, $"Timeout while waiting for event \"{typeof(T)}\"");

            if (e.Name != ContextEvent.Close.Name)
            {
                waiter.RejectOnEvent<EventArgs>(this, ContextEvent.Close.Name, new TargetClosedException("Context closed"));
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

            return Channel.AddInitScriptAsync(ScriptsHelper.SerializeScriptCall(script, arg));
        }

        /// <inheritdoc />
        public Task SetHttpCredentialsAsync(Credentials httpCredentials) => Channel.SetHttpCredentialsAsync(httpCredentials);

        /// <inheritdoc />
        public Task SetOfflineAsync(bool offline) => Channel.SetOfflineAsync(offline);

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
        public Task SetExtraHTTPHeadersAsync(Dictionary<string, string> headers) => Channel.SetExtraHTTPHeadersAsync(headers);

        /// <inheritdoc />
        public async Task<StorageState> GetStorageStateAsync(string path = null)
        {
            var state = await Channel.GetStorageStateAsync().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(
                    path,
                    JsonSerializer.Serialize(state, Channel.Connection.GetDefaultJsonSerializerOptions()));
            }

            return state;
        }

        internal Task PauseAsync() => Channel.PauseAsync();

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
                return Channel.SetNetworkInterceptionEnabledAsync(true);
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
                return Channel.SetNetworkInterceptionEnabledAsync(false);
            }

            return Task.CompletedTask;
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            if (Browser != null)
            {
                ((Browser)Browser).BrowserContextsList.Remove(this);
            }

            Close?.Invoke(this, EventArgs.Empty);
            _closeTcs.TrySetResult(true);
            RejectPendingOperations();
        }

        private void Channel_OnPage(object sender, BrowserContextPageEventArgs e)
        {
            var page = e.PageChannel.Object;
            page.Context = this;
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
            foreach (var (_, waitTcs) in _waitForCancellationTcs.Where(e => e.ContextEvent != ContextEvent.Close))
            {
                waitTcs.TrySetException(new TargetClosedException("Context closed"));
            }

            _waitForCancellationTcs.Clear();
        }

        private Task ExposeBindingAsync(string name, Delegate callback, bool handle = false)
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

            _bindings.Add(name, callback);

            return Channel.ExposeBindingAsync(name, handle);
        }

        private bool IsSafeCloseException(Exception e)
            => e.Message.Contains(DriverMessages.BrowserClosedExceptionMessage) ||
               e.Message.Contains(DriverMessages.BrowserOrContextClosedExceptionMessage);
    }
}
