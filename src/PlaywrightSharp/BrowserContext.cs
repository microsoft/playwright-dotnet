using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserContext" />
    public class BrowserContext : IChannelOwner<BrowserContext>, IBrowserContext
    {
        private readonly ConnectionScope _scope;
        private readonly BrowserContextChannel _channel;
        private readonly List<Page> _crBackgroundPages = new List<Page>();
        private readonly TaskCompletionSource<bool> _closeTcs = new TaskCompletionSource<bool>();
        private bool _isClosedOrClosing;

        internal BrowserContext(ConnectionScope scope, string guid, BrowserContextInitializer initializer)
        {
            _scope = scope.CreateChild(guid);
            _channel = new BrowserContextChannel(guid, scope, this);
            _channel.Closed += Channel_Closed;
            _channel.OnPage += Channel_OnPage;

            if (initializer.Pages != null)
            {
                foreach (var pageChannel in initializer.Pages)
                {
                    var page = pageChannel.Object;
                    PagesList.Add(page);
                    page.BrowserContext = this;
                }
            }

            if (initializer.CrBackgroundPages != null)
            {
                foreach (var pageChannel in initializer.CrBackgroundPages)
                {
                    var page = pageChannel.Object;
                    _crBackgroundPages.Add(page);
                    page.BrowserContext = this;
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<EventArgs> Closed;

        /// <inheritdoc/>
        public event EventHandler<PageEventArgs> PageCreated;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        Channel<BrowserContext> IChannelOwner<BrowserContext>.Channel => _channel;

        /// <inheritdoc />
        public BrowserContextOptions Options { get; }

        /// <inheritdoc />
        public IPage[] Pages => PagesList.ToArray();

        /// <inheritdoc />
        public Browser Browser { get; internal set; }

        internal Page OwnerPage { get; set; }

        internal List<Page> PagesList { get; } = new List<Page>();

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
                return _channel.CloseAsync();
            }

            return _closeTcs.Task;
        }

        /// <inheritdoc />
        public Task<IEnumerable<NetworkCookie>> GetCookiesAsync(params string[] urls) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetCookiesAsync(params SetNetworkCookieParam[] cookies) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ClearCookiesAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetPermissionsAsync(string origin, params ContextPermission[] permissions) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetGeolocationAsync(GeolocationOption geolocation) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ClearPermissionsAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public IEnumerable<IPage> GetExistingPages() => throw new NotImplementedException();

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

        /// <inheritdoc/>
        public Task<T> WaitForEvent<T>(ContextEvent e, WaitForEventOptions<T> options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeBindingAsync(string name, Action<BindingSource> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeFunctionAsync(string name, Action playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T>(string name, Action<T> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<TResult>(string name, Func<TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> playwrightFunction)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task AddInitScriptAsync(string script, object args = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task RouteAsync(string url, Action<Route, IRequest> handler)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UnrouteAsync(string url, Action<Route, IRequest> handler = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task SetHttpCredentials(Credentials credentials)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task SetOfflineModeAsync(bool enabled)
        {
            throw new NotImplementedException();
        }

        private void Channel_Closed(object sender, EventArgs e)
        {
            _isClosedOrClosing = true;
            if (Browser != null)
            {
                Browser.BrowserContextsList.Remove(this);
            }

            _closeTcs.TrySetResult(true);
            Closed?.Invoke(this, EventArgs.Empty);
            _scope.Dispose();
        }

        private void Channel_OnPage(object sender, BrowserContextOnPageEventArgs e)
        {
            var page = e.PageChannel.Object;
            page.BrowserContext = this;
            PagesList.Add(page);
            PageCreated?.Invoke(this, new PageEventArgs { Page = page });
        }
    }
}
