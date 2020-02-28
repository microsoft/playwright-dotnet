using System;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Chromium.Messaging.Target;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserContextDelegate"/>
    public class ChromiumBrowserContext : IBrowserContextDelegate
    {
        private readonly ChromiumSession _client;
        private readonly string _contextId;

        internal ChromiumBrowserContext(ChromiumSession client, ChromiumBrowser chromiumBrowser) : this(client, chromiumBrowser, null, null)
        {
        }

        internal ChromiumBrowserContext(
            ChromiumSession client,
            ChromiumBrowser chromiumBrowser,
            string contextId,
            BrowserContextOptions options)
        {
            _client = client;
            Browser = chromiumBrowser;
            _contextId = contextId;
            Options = options;
        }

        /// <inheritdoc cref="IBrowserContext"/>
        public BrowserContextOptions Options { get; }

        /// <inheritdoc cref="IBrowserContext"/>
        public BrowserContext BrowserContext { get; set; }

        internal ChromiumBrowser Browser { get; }

        /// <inheritdoc cref="IBrowserContext"/>
        public async Task<IPage[]> GetPagesAsync()
        {
            var pageTasks =
                Browser.GetAllTargets()
                .Where(target => target.BrowserContext == BrowserContext && target.Type == TargetType.Page)
                .Select(t => t.PageAsync());

            var pages = await Task.WhenAll(pageTasks).ConfigureAwait(false);

            return pages.Where(p => p != null).ToArray();
        }

        /// <inheritdoc cref="IBrowserContext"/>
        public async Task<IPage> NewPage()
        {
            var createTargetRequest = new TargetCreateTargetRequest
            {
                Url = "about:blank",
            };

            if (_contextId != null)
            {
                createTargetRequest.BrowserContextId = _contextId;
            }

            string targetId = (await _client.SendAsync<TargetCreateTargetResponse>("Target.createTarget", createTargetRequest)
                .ConfigureAwait(false)).TargetId;
            var target = Browser.TargetsMap[targetId];
            await target.InitializedTask.ConfigureAwait(false);
            return await target.PageAsync().ConfigureAwait(false);
        }
    }
}
