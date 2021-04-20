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

        private float _defaultNavigationTimeout;
        private float _defaultTimeout;


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
        public event EventHandler<IBrowserContext> Close;

        /// <inheritdoc/>
        public event EventHandler<IPage> Page;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => Channel;

        /// <inheritdoc/>
        IChannel<BrowserContext> IChannelOwner<BrowserContext>.Channel => Channel;

        /// <inheritdoc/>
        public IBrowser Browser { get; }

        /// <inheritdoc/>
        public IReadOnlyCollection<IPage> Pages => PagesList;

        /// <inheritdoc/>
        public float DefaultNavigationTimeout
        {
            get => _defaultNavigationTimeout;
            set
            {
                _defaultNavigationTimeout = value;
                _ = Channel.SetDefaultNavigationTimeoutNoReplyAsync(value);
            }
        }

        /// <inheritdoc/>
        public float DefaultTimeout
        {
            get => _defaultTimeout;
            set
            {
                _defaultTimeout = value;
                _ = Channel.SetDefaultTimeoutNoReplyAsync(value);
            }
        }

        internal BrowserContextChannel Channel { get; }

        internal List<Page> PagesList { get; } = new List<Page>();

        /// <inheritdoc/>
        public Task AddCookiesAsync(IEnumerable<Cookie> cookies) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task AddInitScriptAsync(string script = null, string scriptPath = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task ClearCookiesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task ClearPermissionsAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task CloseAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IReadOnlyCollection<BrowserContextCookiesResult>> GetCookiesAsync(IEnumerable<string> urls = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task ExposeBindingAsync(string name, Action callback, bool? handle = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task ExposeFunctionAsync(string name, Action callback) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task GrantPermissionsAsync(IEnumerable<string> permissions, string origin = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<IPage> NewPageAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task RouteAsync(string urlString, Regex urlRegex, Func<string, bool> urlFunc, Action<IRoute> handler) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task SetExtraHttpHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task SetGeolocationAsync(Geolocation geolocation) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task SetOfflineAsync(bool offline) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<string> StorageStateAsync(string path = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task UnrouteAsync(string urlString, Regex urlRegex, Func<string, bool> urlFunc, Action<IRoute> handler = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<object> WaitForEventAsync(string @event, float? timeout = null) => throw new NotImplementedException();

            timeout ??= TimeoutSettings.Timeout;
            using var waiter = new Waiter();
            waiter.RejectOnTimeout(timeout, $"Timeout while waiting for event \"{typeof(T)}\"");

            if (e.Name != ContextEvent.Close.Name)
            {
                waiter.RejectOnEvent<EventArgs>(this, ContextEvent.Close.Name, new TargetClosedException("Context closed"));
            }

            if (typeof(T) == typeof(EventArgs))
            {
                await waiter.WaitForEventAsync(this, e.Name).ConfigureAwait(false);
                return default;
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
        public Task SetHttpCredentialsAsync(HttpCredentials httpCredentials) => Channel.SetHttpCredentialsAsync(httpCredentials);

        /// <inheritdoc />
        public Task SetOfflineAsync(bool offline) => Channel.SetOfflineAsync(offline);

        /// <inheritdoc />
        public Task RouteAsync(string url, Action<IRoute> handler)
            => RouteAsync(
                new RouteSetting
                {
                    Url = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task RouteAsync(Regex url, Action<IRoute> handler)
            => RouteAsync(
                new RouteSetting
                {
                    Regex = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task RouteAsync(Func<string, bool> url, Action<IRoute> handler)
            => RouteAsync(
                new RouteSetting
                {
                    Function = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task UnrouteAsync(string url, Action<IRoute> handler = null)
            => UnrouteAsync(
                new RouteSetting
                {
                    Url = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task UnrouteAsync(Regex url, Action<IRoute> handler = null)
            => UnrouteAsync(
                new RouteSetting
                {
                    Regex = url,
                    Handler = handler,
                });

        /// <inheritdoc />
        public Task UnrouteAsync(Func<string, bool> url, Action<IRoute> handler = null)
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

        internal void OnRoute(Route route, IRequest request)
        {
            foreach (var item in _routes)
            {
                if (
                    (item.Url != null && request.Url.UrlMatches(item.Url)) ||
                    (item.Regex?.IsMatch(request.Url) == true) ||
                    (item.Function?.Invoke(request.Url) == true))
                {
                    item.Handler(route);
                    return;
                }
            }

            _ = route.ResumeAsync();
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
    }
}
