using System;
using System.Diagnostics;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;

namespace PlaywrightSharp
{
    internal abstract class BrowserBase : IBrowser
    {
        public abstract event EventHandler<TargetChangedArgs> TargetChanged;

        public abstract event EventHandler<TargetChangedArgs> TargetCreated;

        public abstract event EventHandler<TargetChangedArgs> TargetDestroyed;

        public abstract event EventHandler Disconnected;

        public abstract IBrowserContext[] BrowserContexts { get; }

        public abstract IBrowserContext DefaultContext { get; }

        public abstract Process Process { get; }

        public abstract bool IsConnected { get; }

        public abstract Task CloseAsync();

        public abstract Task DisconnectAsync();

        public abstract void Dispose();

        public abstract Task<IBrowserContext> NewContextAsync(BrowserContextOptions options = null);

        protected async Task<ITransport> CreateTransportAsync(ConnectOptions options)
        {
            if (!string.IsNullOrEmpty(options.BrowserWSEndpoint) && options.Transport != null)
            {
                throw new ArgumentException("Exactly one of BrowserWSEndpoint or Transport must be passed to connect");
            }

            ITransport transport = null;

            if (options.Transport != null)
            {
                transport = options.Transport;
            }
            else if (!string.IsNullOrEmpty(options.BrowserWSEndpoint))
            {
                transport = await WebsocketTransport.CreateAsync(options.BrowserWSEndpoint).ConfigureAwait(false);
            }

            return SlowMoTransport.Wrap(transport, options.SlowMo);
        }
    }
}
