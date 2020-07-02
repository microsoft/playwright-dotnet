using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp
{
    internal class Browser : ChannelOwnerBase, IBrowser
    {
        public Browser(PlaywrightClient client, Channel channel, BrowserInitializer initializer) : base(channel)
        {
            throw new NotImplementedException();
        }

        public void Dispose() => throw new NotImplementedException();

        public ValueTask DisposeAsync() => throw new NotImplementedException();

        public event EventHandler<TargetChangedArgs> TargetChanged;
        public event EventHandler<TargetChangedArgs> TargetCreated;
        public event EventHandler<TargetChangedArgs> TargetDestroyed;
        public event EventHandler Disconnected;
        public IEnumerable<IBrowserContext> BrowserContexts { get; }
        public IBrowserContext DefaultContext { get; }
        public bool IsConnected { get; }
        public Task StartTracingAsync(IPage page = null, TracingOptions options = null) => throw new NotImplementedException();

        public Task<string> StopTracingAsync() => throw new NotImplementedException();

        public Task CloseAsync() => throw new NotImplementedException();

        public Task DisconnectAsync() => throw new NotImplementedException();

        public ITarget GetPageTarget(IPage page) => throw new NotImplementedException();

        public Task<IBrowserContext> NewContextAsync(BrowserContextOptions options = null) => throw new NotImplementedException();

        public Task<ITarget> WaitForTargetAsync(Func<ITarget, bool> predicate, WaitForOptions options = null) => throw new NotImplementedException();

        public IEnumerable<ITarget> GetTargets(IBrowserContext context = null) => throw new NotImplementedException();

        public Task<IWorker> GetServiceWorkerAsync(ITarget target) => throw new NotImplementedException();
    }
}
