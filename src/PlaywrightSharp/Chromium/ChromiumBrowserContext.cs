using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IChromiumBrowserContext"/>
    public class ChromiumBrowserContext : BrowserContext, IChromiumBrowserContext
    {
        private readonly List<Page> _crBackgroundPages = new List<Page>();

        internal ChromiumBrowserContext(IChannelOwner parent, string guid, BrowserContextInitializer initializer) : base(parent, guid, initializer)
        {
            if (initializer.CrBackgroundPages != null)
            {
                foreach (var pageChannel in initializer.CrBackgroundPages)
                {
                    var page = ((PageChannel)pageChannel).Object;
                    _crBackgroundPages.Add(page);
                    page.BrowserContext = this;
                    BackgroundPage?.Invoke(this, new PageEventArgs { Page = page });
                }
            }

            Channel.BackgroundPage += (sender, e) =>
            {
                var page = e.PageChannel.Object;
                page.BrowserContext = this;
                _crBackgroundPages.Add(page);
                BackgroundPage?.Invoke(this, new PageEventArgs { Page = page });
            };

            if (initializer.CrServiceWorkers != null)
            {
                foreach (var workerChannel in initializer.CrServiceWorkers)
                {
                    var worker = ((WorkerChannel)workerChannel).Object;
                    ServiceWorkersList.Add(worker);
                    worker.BrowserContext = this;
                    ServiceWorker?.Invoke(this, new WorkerEventArgs(worker));
                }
            }

            Channel.ServiceWorker += (sender, e) =>
            {
                var worker = e.WorkerChannel.Object;
                ServiceWorkersList.Add(worker);
                worker.BrowserContext = this;
                ServiceWorker?.Invoke(this, new WorkerEventArgs(worker));
            };
        }

        /// <inheritdoc/>
        public event EventHandler<PageEventArgs> BackgroundPage;

        /// <inheritdoc/>
        public event EventHandler<WorkerEventArgs> ServiceWorker;

        /// <inheritdoc />
        public IPage[] BackgroundPages => _crBackgroundPages.ToArray();

        /// <inheritdoc />
        public IWorker[] ServiceWorkers => ServiceWorkersList.ToArray();

        /// <inheritdoc />
        public async Task<ICDPSession> NewCDPSessionAsync(IPage page) => (await Channel.NewCDPSessionAsync(page).ConfigureAwait(false))?.Object;
    }
}
