using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Accessibility;
using PlaywrightSharp.Chromium.Messaging.Emulation;
using PlaywrightSharp.Chromium.Messaging.Page;
using PlaywrightSharp.Chromium.Messaging.Security;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IPageDelegate"/>
    internal class ChromiumPage : IPageDelegate
    {
        private readonly ChromiumSession _client;
        private readonly ChromiumBrowser _browser;
        private readonly IBrowserContext _browserContext;
        private readonly ChromiumNetworkManager _networkManager;

        public ChromiumPage(ChromiumSession client, ChromiumBrowser browser, IBrowserContext browserContext)
        {
            _client = client;
            _browser = browser;
            _browserContext = browserContext;
            _networkManager = new ChromiumNetworkManager(_client, this);
            Page = new Page(this, browserContext);
        }

        public ChromiumTarget Target { get; set; }

        internal Page Page { get; }

        public Task<GotoResult> NavigateFrameAsync(IFrame frame, string url, string referrer)
        {
            throw new NotImplementedException();
        }

        public Task SetViewportAsync(Viewport viewport)
        {
            throw new NotImplementedException();
        }

        internal async Task InitializeAsync()
        {
            var getFrameTreeTask = _client.SendAsync<PageGetFrameTreeResponse>("Page.getFrameTree");

            await Task.WhenAll(
                _client.SendAsync("Page.enable"),
                getFrameTreeTask).ConfigureAwait(false);

            HandleFrameTree(getFrameTreeTask.Result.FrameTree);

            var tasks = new List<Task>
            {
               _client.SendAsync("Log.enable", null),
               _client.SendAsync("Page.setLifecycleEventsEnabled", new PageSetLifecycleEventsEnabledRequest { Enabled = true }),
               _client.SendAsync("Runtime.enable").ContinueWith(t => EnsureIsolatedWorldAsync(), TaskScheduler.Default),
               _networkManager.InitializeAsync(),
            };

            if (_browserContext.Options != null)
            {
                var options = _browserContext.Options;

                if (options.BypassCSP)
                {
                    tasks.Add(_client.SendAsync("Page.setBypassCSP", new PageSetBypassCSPRequest { Enabled = true }));
                }

                if (options.IgnoreHTTPSErrors)
                {
                    tasks.Add(_client.SendAsync("Security.setIgnoreCertificateErrors", new SecuritySetIgnoreCertificateErrorsRequest { Ignore = true }));
                }

                if (options.Viewport != null)
                {
                    tasks.Add(SetViewportAsync(options.Viewport));
                }

                if (!options.JavaScriptEnabled)
                {
                    tasks.Add(_client.SendAsync("Emulation.setScriptExecutionDisabled", new EmulationSetScriptExecutionDisabledRequest { Value = true }));
                }

                if (options.UserAgent != null)
                {
                    tasks.Add(_networkManager.SetUserAgentAsync(options.UserAgent));
                }

                if (options.TimezoneId != null)
                {
                    tasks.Add(EmulateTimezoneAsync(options.TimezoneId));
                }

                if (options.Geolocation != null)
                {
                    tasks.Add(_client.SendAsync("Emulation.setGeolocationOverride", options.Geolocation));
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private Task EnsureIsolatedWorldAsync() => Task.CompletedTask;

        private Task EmulateTimezoneAsync(string timezoneId)
        {
            throw new NotImplementedException();
        }

        private void HandleFrameTree(PageGetFrameTreeItem frameTree)
        {
            OnFrameAttached(frameTree.Frame.Id, frameTree.Frame.ParentId ?? string.Empty);
            OnFrameNavigated(frameTree.Frame, true);

            if (frameTree.ChildFrames != null)
            {
                foreach (var child in frameTree.ChildFrames)
                {
                    HandleFrameTree(child);
                }
            }
        }

        private void OnFrameNavigated(PageGetFrameTreeItemInfo frame, bool initial)
            => Page.FrameManager.FrameCommittedNewDocumentNavigation(frame.Id, frame.Url, frame.Name ?? string.Empty, frame.LoaderId, initial);

        private void OnFrameAttached(string frameId, string parentFrameId) => Page.FrameManager.FrameAttached(frameId, parentFrameId);
    }
}