using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowser"/>
    public class Browser : IChannelOwner<Browser>, IBrowser
    {
        private readonly ConnectionScope _scope;
        private readonly BrowserChannel _channel;
        private readonly TaskCompletionSource<bool> _closedTcs = new TaskCompletionSource<bool>();
        private bool _isClosedOrClosing;

        internal Browser(ConnectionScope scope, string guid, BrowserInitializer initializer)
        {
            _scope = scope.CreateChild(guid);
            _channel = new BrowserChannel(guid, scope, this);
            IsConnected = true;
            _channel.Closed += (sender, e) =>
            {
                IsConnected = false;
                _isClosedOrClosing = true;
                Disconnected?.Invoke(this, EventArgs.Empty);
                _scope.Dispose();
                _closedTcs.TrySetResult(true);
            };
        }

        /// <inheritdoc/>
        public event EventHandler Disconnected;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Browser> IChannelOwner<Browser>.Channel => _channel;

        /// <inheritdoc/>
        public IEnumerable<IBrowserContext> BrowserContexts => BrowserContextsList.ToArray();

        /// <inheritdoc/>
        public bool IsConnected { get; private set; }

        /// <inheritdoc/>
        public IBrowserContext[] Contexts => BrowserContextsList.ToArray();

        internal List<BrowserContext> BrowserContextsList { get; } = new List<BrowserContext>();

        /// <inheritdoc/>
        public Task StartTracingAsync(IPage page = null, bool screenshots = false, string path = null, IEnumerable<string> categories = null)
            => _channel.StartTracingAsync(page, screenshots, path, categories);

        /// <inheritdoc/>
        public async Task<string> StopTracingAsync()
        {
            string result = await _channel.StopTracingAsync().ConfigureAwait(false);
            return Encoding.UTF8.GetString(Convert.FromBase64String(result));
        }

        /// <inheritdoc/>
        public async Task CloseAsync()
        {
            if (!_isClosedOrClosing)
            {
                _isClosedOrClosing = true;
                await _channel.CloseAsync().ConfigureAwait(false);
            }

            await _closedTcs.Task.ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IBrowserContext> NewContextAsync(BrowserContextOptions options = null)
        {
            var context = (await _channel.NewContextAsync(options).ConfigureAwait(false)).Object;
            BrowserContextsList.Add(context);
            context.Browser = this;
            return context;
        }

        /// <inheritdoc/>
        public async Task<ICDPSession> NewBrowserCDPSessionAsync() => (await _channel.NewBrowserCDPSessionAsync().ConfigureAwait(false)).Object;

        /// <inheritdoc/>
        public async Task<IPage> NewPageAsync(BrowserContextOptions options = null)
        {
            var context = await NewContextAsync(options).ConfigureAwait(false) as BrowserContext;
            var page = await context.NewPageAsync().ConfigureAwait(false) as Page;
            page.OwnedContext = context;
            context.OwnerPage = page;
            return page;
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);
    }
}
