using System;
using System.Collections.Generic;
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
    /// <inheritdoc cref="IBrowserContext" />
    public class BrowserContext : ChannelOwnerBase, IChannelOwner<BrowserContext>, IBrowserContext
    {
        private readonly TaskCompletionSource<bool> _closeTcs = new();
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

        internal Page OwnerPage { get; set; }

        internal List<Worker> ServiceWorkersList { get; } = new List<Worker>();

        internal bool IsChromium => _initializer.IsChromium;

        internal bool RecordVideo { get; set; }

        /// <inheritdoc/>
        public Task AddCookiesAsync(IEnumerable<Cookie> cookies) => Channel.AddCookiesAsync(cookies);

        /// <inheritdoc/>
        public Task AddInitScriptAsync(string script = null, string scriptPath = null)
        {
            if (string.IsNullOrEmpty(script))
            {
                script = ScriptsHelper.EvaluationScript(script, scriptPath);
            }

            return Channel.AddInitScriptAsync(ScriptsHelper.SerializeScriptCall(script, null));
        }

        /// <inheritdoc/>
        public Task ClearCookiesAsync() => Channel.ClearCookiesAsync();

        /// <inheritdoc/>
        public Task ClearPermissionsAsync() => Channel.ClearPermissionsAsync();

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
            catch (Exception e) when (IsTransient(e))
            {
                // Swallow exception
            }
        }

        /// <inheritdoc/>
        public Task<IReadOnlyCollection<BrowserContextCookiesResult>> GetCookiesAsync(IEnumerable<string> urls = null) => Channel.GetCookiesAsync(urls);

        /// <inheritdoc/>
        public Task ExposeBindingAsync(string name, Action callback, bool? handle = null)
            => ExposeBindingAsync(name, _ => callback());

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
            => ExposeBindingAsync(name, callback, true);

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
        public Task GrantPermissionsAsync(IEnumerable<string> permissions, string origin = null) => Channel.GrantPermissionsAsync(permissions, origin);

        /// <inheritdoc/>
        public async Task<IPage> NewPageAsync()
        {
            if (OwnerPage != null)
            {
                throw new PlaywrightSharpException("Please use Browser.NewContextAsync()");
            }

            return (await Channel.NewPageAsync().ConfigureAwait(false)).Object;
        }

        /// <inheritdoc cref="RouteAsync(string, Regex, Func{string, bool}, Action{IRoute})"/>
        public Task RouteAsync(string urlString, Action<IRoute> handler)
            => RouteAsync(urlString, null, null, handler);

        /// <inheritdoc cref="RouteAsync(string, Regex, Func{string, bool}, Action{IRoute})"/>
        public Task RouteAsync(Regex urlRegex, Action<IRoute> handler)
            => RouteAsync(null, urlRegex, null, handler);

        /// <inheritdoc cref="RouteAsync(string, Regex, Func{string, bool}, Action{IRoute})"/>
        public Task RouteAsync(Func<string, bool> urlFunc, Action<IRoute> handler)
            => RouteAsync(null, null, urlFunc, handler);

        /// <inheritdoc/>
        public Task SetExtraHttpHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers)
            => Channel.SetExtraHTTPHeadersAsync(headers);

        /// <inheritdoc/>
        public Task SetGeolocationAsync(Geolocation geolocation) => Channel.SetGeolocationAsync(geolocation);

        /// <inheritdoc/>
        public Task SetOfflineAsync(bool offline) => Channel.SetOfflineAsync(offline);

        /// <inheritdoc/>
        public async Task<string> StorageStateAsync(string path = null)
        {
            string state = JsonSerializer.Serialize(
                await Channel.GetStorageStateAsync().ConfigureAwait(false),
                Channel.Connection.GetDefaultJsonSerializerOptions());

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, state);
            }

            return state;
        }

        /// <inheritdoc cref="UnrouteAsync(string, Regex, Func{string, bool}, Action{IRoute})"/>
        public Task UnrouteAsync(string urlString, Action<IRoute> handler = default)
            => UnrouteAsync(urlString, null, null, handler);

        /// <inheritdoc cref="UnrouteAsync(string, Regex, Func{string, bool}, Action{IRoute})"/>
        public Task UnrouteAsync(Regex urlRegex, Action<IRoute> handler = default)
            => UnrouteAsync(null, urlRegex, null, handler);

        /// <inheritdoc cref="UnrouteAsync(string, Regex, Func{string, bool}, Action{IRoute})"/>
        public Task UnrouteAsync(Func<string, bool> urlFunc, Action<IRoute> handler = default)
            => UnrouteAsync(null, null, urlFunc, handler);

        /// <inheritdoc/>
        public async Task<object> WaitForEventAsync(string @event, float? timeout = null)
        => @event switch
        {
            ContextEvent.PageEventName => await WaitForEventAsync(ContextEvent.Page, timeout).ConfigureAwait(false),
            ContextEvent.CloseEventName => await WaitForEventAsync(ContextEvent.Close, timeout).ConfigureAwait(false),
            _ => throw new InvalidOperationException(),
        };

        /// <inheritdoc/>
        public async Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> playwrightEvent, float? timeout = null)
        {
            if (playwrightEvent == null)
            {
                throw new ArgumentException("Page event is required", nameof(playwrightEvent));
            }

            timeout ??= DefaultTimeout;
            using var waiter = new Waiter();
            waiter.RejectOnTimeout(Convert.ToInt32(timeout), $"Timeout while waiting for event \"{playwrightEvent.Name}\"");

            if (playwrightEvent.Name != ContextEvent.Close.Name)
            {
                waiter.RejectOnEvent<IBrowserContext>(this, ContextEvent.Close.Name, new TargetClosedException("Context closed"));
            }

            return await waiter.WaitForEventAsync<T>(this, playwrightEvent.Name, null).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<IPage> WaitForPageAsync(Func<IPage, bool> predicate = null, float? timeout = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

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

        private Task RouteAsync(string urlString, Regex urlRegex, Func<string, bool> urlFunc, Action<IRoute> handler)
            => RouteAsync(new RouteSetting()
            {
                Regex = urlRegex,
                Url = urlString,
                Function = urlFunc,
                Handler = handler,
            });

        private Task RouteAsync(RouteSetting setting)
        {
            _routes.Add(setting);

            if (_routes.Count == 1)
            {
                return Channel.SetNetworkInterceptionEnabledAsync(true);
            }

            return Task.CompletedTask;
        }

        private Task UnrouteAsync(string urlString, Regex urlRegex, Func<string, bool> urlFunc, Action<IRoute> handler = null)
            => UnrouteAsync(new RouteSetting()
            {
                Function = urlFunc,
                Url = urlString,
                Regex = urlRegex,
                Handler = handler,
            });

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

        private bool IsTransient(Exception e)
            => e.Message.Contains(DriverMessages.BrowserClosedExceptionMessage) ||
                e.Message.Contains(DriverMessages.BrowserOrContextClosedExceptionMessage);
    }
}
