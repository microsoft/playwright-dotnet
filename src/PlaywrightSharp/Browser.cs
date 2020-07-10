using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    internal class Browser : IChannelOwner<Browser>, IBrowser
    {
        private readonly ConnectionScope _scope;
        private readonly BrowserChannel _channel;

        public Browser(ConnectionScope scope, string guid, BrowserInitializer initializer)
        {
            _scope = scope;
            _channel = new BrowserChannel(guid, scope, this);
        }

        public event EventHandler<TargetChangedArgs> TargetChanged;

        public event EventHandler<TargetChangedArgs> TargetCreated;

        public event EventHandler<TargetChangedArgs> TargetDestroyed;

        public event EventHandler Disconnected;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        Channel<Browser> IChannelOwner<Browser>.Channel => _channel;

        public IEnumerable<IBrowserContext> BrowserContexts { get; }

        public IBrowserContext DefaultContext { get; }

        public bool IsConnected { get; }

        public Task StartTracingAsync(IPage page = null, TracingOptions options = null) => throw new NotImplementedException();

        public Task<string> StopTracingAsync() => throw new NotImplementedException();

        public Task CloseAsync() => throw new NotImplementedException();

        public Task DisconnectAsync() => throw new NotImplementedException();

        public ITarget GetPageTarget(IPage page) => throw new NotImplementedException();

        public async Task<IBrowserContext> NewContextAsync(BrowserContextOptions options = null)
            => (await _channel.NewContextAsync(options).ConfigureAwait(false)).Object;

        public async Task<IPage> NewPageAsync(BrowserContextOptions options = null)
        {
            var context = await NewContextAsync(options).ConfigureAwait(false) as BrowserContext;
            var page = await context.NewPageAsync().ConfigureAwait(false) as Page;
            page.BrowserContext = context;
            context.OwnerPage = page;
            return page;
        }

        public Task<ITarget> WaitForTargetAsync(Func<ITarget, bool> predicate, WaitForOptions options = null) => throw new NotImplementedException();

        public IEnumerable<ITarget> GetTargets(IBrowserContext context = null) => throw new NotImplementedException();

        public Task<IWorker> GetServiceWorkerAsync(ITarget target) => throw new NotImplementedException();

        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);
    }
}
