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

        public event EventHandler<IBrowserContext> Close;

        public event EventHandler<IPage> Page;

        ChannelBase IChannelOwner.Channel => Channel;

        IChannel<BrowserContext> IChannelOwner<BrowserContext>.Channel => Channel;

        public IBrowser Browser { get; }

        public IReadOnlyCollection<IPage> Pages => PagesList;

        internal float DefaultNavigationTimeout
        {
            get => _defaultNavigationTimeout;
            set
            {
                _defaultNavigationTimeout = value;
                _ = Channel.SetDefaultNavigationTimeoutNoReplyAsync(value);
            }
        }

        internal float DefaultTimeout
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

        public Task AddCookiesAsync(IEnumerable<Cookie> cookies) => Channel.AddCookiesAsync(cookies);

        public Task AddInitScriptAsync(string script = null, string scriptPath = null)
        {
            if (string.IsNullOrEmpty(script))
            {
                script = ScriptsHelper.EvaluationScript(script, scriptPath);
            }

            return Channel.AddInitScriptAsync(ScriptsHelper.SerializeScriptCall(script, null));
        }

        public Task ClearCookiesAsync() => Channel.ClearCookiesAsync();

        public Task ClearPermissionsAsync() => Channel.ClearPermissionsAsync();

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

        public Task<IReadOnlyCollection<BrowserContextCookiesResult>> CookiesAsync(IEnumerable<string> urls = null) => Channel.CookiesAsync(urls);

        public Task ExposeBindingAsync(string name, Action callback, bool? handle = null)
            => ExposeBindingAsync(name, _ => callback());

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

        public Task GrantPermissionsAsync(IEnumerable<string> permissions, string origin = null) => Channel.GrantPermissionsAsync(permissions, origin);

        public async Task<IPage> NewPageAsync()
        {
            if (OwnerPage != null)
            {
                throw new PlaywrightException("Please use Browser.NewContextAsync()");
            }

            return (await Channel.NewPageAsync().ConfigureAwait(false)).Object;
        }

        public Task RouteAsync(string urlString, Action<IRoute> handler)
            => RouteAsync(urlString, null, null, handler);

        public Task RouteAsync(Regex urlRegex, Action<IRoute> handler)
            => RouteAsync(null, urlRegex, null, handler);

        public Task RouteAsync(Func<string, bool> urlFunc, Action<IRoute> handler)
            => RouteAsync(null, null, urlFunc, handler);

        public Task SetExtraHttpHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers)
            => Channel.SetExtraHTTPHeadersAsync(headers);

        public Task SetGeolocationAsync(Geolocation geolocation) => Channel.SetGeolocationAsync(geolocation);

        public Task SetOfflineAsync(bool offline) => Channel.SetOfflineAsync(offline);

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

        public Task UnrouteAsync(string urlString, Action<IRoute> handler = default)
            => UnrouteAsync(urlString, null, null, handler);

        public Task UnrouteAsync(Regex urlRegex, Action<IRoute> handler = default)
            => UnrouteAsync(null, urlRegex, null, handler);

        public Task UnrouteAsync(Func<string, bool> urlFunc, Action<IRoute> handler = default)
            => UnrouteAsync(null, null, urlFunc, handler);

        public async Task<object> WaitForEventAsync(string @event, Func<Task> action = default, float? timeout = null)
        => @event switch
        {
            ContextEvent.PageEventName => await WaitForEventAsync(ContextEvent.Page, action, null, timeout).ConfigureAwait(false),
            ContextEvent.CloseEventName => await WaitForEventAsync(ContextEvent.Close, action, null, timeout).ConfigureAwait(false),
            _ => throw new InvalidOperationException(),
        };

        public async Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> playwrightEvent, Func<Task> action = default, Func<T, bool> predicate = default, float? timeout = default)
        {
            if (playwrightEvent == null)
            {
                throw new ArgumentException("Page event is required", nameof(playwrightEvent));
            }

            timeout ??= DefaultTimeout;
            using var waiter = new Waiter(Channel, $"context.WaitForEventAsync(\"{playwrightEvent.Name}\")");
            waiter.RejectOnTimeout(Convert.ToInt32(timeout), $"Timeout while waiting for event \"{playwrightEvent.Name}\"");

            if (playwrightEvent.Name != ContextEvent.Close.Name)
            {
                waiter.RejectOnEvent<IBrowserContext>(this, ContextEvent.Close.Name, new TargetClosedException("Context closed"));
            }

            var result = waiter.WaitForEventAsync<T>(this, playwrightEvent.Name, null);
            if (action != null)
            {
                await Task.WhenAll(result, action()).ConfigureAwait(false);
            }

            return await result.ConfigureAwait(false);
        }

        public Task<IPage> WaitForPageAsync(Func<Task> action = default, Func<IPage, bool> predicate = default, float? timeout = default)
            => WaitForEventAsync(ContextEvent.Page, action, predicate, timeout);

        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

        public void SetDefaultNavigationTimeout(float timeout) => DefaultNavigationTimeout = timeout;

        public void SetDefaultTimeout(float timeout) => DefaultTimeout = timeout;

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

            _ = route.ContinueAsync();
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

        private bool IsTransient(Exception e)
            => e.Message.Contains(DriverMessages.BrowserClosedExceptionMessage) ||
                e.Message.Contains(DriverMessages.BrowserOrContextClosedExceptionMessage);
    }
}
