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

        internal Page OwnerPage { get; set; }

        internal List<Worker> ServiceWorkersList { get; } = new List<Worker>();

        internal bool IsChromium => _initializer.IsChromium;

        internal string VideoPath { get; set; }

        /// <inheritdoc/>
        public Task AddCookiesAsync(IEnumerable<Cookie> cookies) => Channel.AddCookiesAsync(cookies);

        /// <inheritdoc/>
        public Task AddInitScriptAsync(string script = null, string scriptPath = null, object arg = null)
        {
            if (string.IsNullOrEmpty(script))
            {
                script = ScriptsHelper.EvaluationScript(script, scriptPath);
            }

            return Channel.AddInitScriptAsync(ScriptsHelper.SerializeScriptCall(script, new[] { arg }));
        }

        /// <inheritdoc/>
        public Task ClearCookiesAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task ClearPermissionsAsync() => throw new NotImplementedException();

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
        public Task<IReadOnlyCollection<BrowserContextCookiesResult>> GetCookiesAsync(IEnumerable<string> urls = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task ExposeBindingAsync(string name, Action callback, bool? handle = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task ExposeFunctionAsync(string name, Action callback) => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task GrantPermissionsAsync(IEnumerable<string> permissions, string origin = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public async Task<IPage> NewPageAsync() => (await Channel.NewPageAsync().ConfigureAwait(false)).Object;

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

        /// <inheritdoc/>
        public Task<IPage> WaitForPageAsync(Func<IPage, bool> predicate = null, float? timeout = null) => throw new NotImplementedException();

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

            Close?.Invoke(this, this);
            _closeTcs.TrySetResult(true);
            RejectPendingOperations();
        }

        private void Channel_OnPage(object sender, BrowserContextPageEventArgs e)
        {
            var page = e.PageChannel.Object;
            page.Context = this;
            PagesList.Add(page);
            Page?.Invoke(this, page);
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

        private bool IsTransient(Exception e)
            => e.Message.Contains(DriverMessages.BrowserClosedExceptionMessage) ||
                e.Message.Contains(DriverMessages.BrowserOrContextClosedExceptionMessage);
    }
}
