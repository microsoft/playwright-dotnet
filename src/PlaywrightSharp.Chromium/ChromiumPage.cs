using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Emulation;
using PlaywrightSharp.Chromium.Protocol.Log;
using PlaywrightSharp.Chromium.Protocol.Page;
using PlaywrightSharp.Chromium.Protocol.Runtime;
using PlaywrightSharp.Chromium.Protocol.Security;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IPageDelegate"/>
    internal class ChromiumPage : IPageDelegate
    {
        private const string UtilityWorldName = "__playwright_utility_world__";
        private const string EvaluationScriptUrl = "__playwright_evaluation_script__";

        private readonly ChromiumBrowser _browser;
        private readonly IBrowserContext _browserContext;
        private readonly ChromiumNetworkManager _networkManager;
        private readonly ISet<string> _isolatedWorlds = new HashSet<string>();

        public ChromiumPage(ChromiumSession client, ChromiumBrowser browser, IBrowserContext browserContext)
        {
            Client = client;
            _browser = browser;
            _browserContext = browserContext;
            _networkManager = new ChromiumNetworkManager(Client, this);
            Page = new Page(this, browserContext);
            client.MessageReceived += Client_MessageReceived;
        }

        public ChromiumTarget Target { get; set; }

        internal Page Page { get; }

        internal ChromiumSession Client { get; }

        public async Task<GotoResult> NavigateFrameAsync(IFrame frame, string url, string referrer)
        {
            var response = await Client.SendAsync(new PageNavigateRequest
            {
                Url = url,
                Referrer = referrer ?? string.Empty,
                FrameId = frame.Id,
            }).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(response.ErrorText))
            {
                throw new NavigationException(response.ErrorText, url);
            }

            return new GotoResult
            {
                NewDocumentId = response.LoaderId,
                IsSameDocument = string.IsNullOrEmpty(response.LoaderId),
            };
        }

        public Task SetViewportAsync(Viewport viewport)
        {
            throw new NotImplementedException();
        }

        public Task ClosePageAsync(bool runBeforeUnload)
        {
            if (runBeforeUnload)
            {
                return Client.SendAsync(new PageCloseRequest());
            }
            else
            {
                return _browser.ClosePageAsync(this);
            }
        }

        internal async Task InitializeAsync()
        {
            var getFrameTreeTask = Client.SendAsync(new PageGetFrameTreeRequest());

            await Task.WhenAll(
                Client.SendAsync(new PageEnableRequest()),
                getFrameTreeTask).ConfigureAwait(false);

            HandleFrameTree(getFrameTreeTask.Result.FrameTree);

            var tasks = new List<Task>
            {
               Client.SendAsync(new LogEnableRequest()),
               Client.SendAsync(new PageSetLifecycleEventsEnabledRequest { Enabled = true }),
               Client.SendAsync(new RuntimeEnableRequest()).ContinueWith(t => EnsureIsolatedWorldAsync(UtilityWorldName), TaskScheduler.Default),
               _networkManager.InitializeAsync(),
            };

            if (_browserContext.Options != null)
            {
                var options = _browserContext.Options;

                if (options.BypassCSP)
                {
                    tasks.Add(Client.SendAsync(new PageSetBypassCSPRequest { Enabled = true }));
                }

                if (options.IgnoreHTTPSErrors)
                {
                    tasks.Add(Client.SendAsync(new SecuritySetIgnoreCertificateErrorsRequest { Ignore = true }));
                }

                if (options.Viewport != null)
                {
                    tasks.Add(SetViewportAsync(options.Viewport));
                }

                if (!options.JavaScriptEnabled)
                {
                    tasks.Add(Client.SendAsync(new EmulationSetScriptExecutionDisabledRequest { Value = true }));
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
                    tasks.Add(Client.SendAsync(new EmulationSetGeolocationOverrideRequest
                    {
                        Accuracy = options.Geolocation.Accuracy,
                        Latitude = options.Geolocation.Latitude,
                        Longitude = options.Geolocation.Longitude,
                    }));
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        internal void DidClose() => Page.DidClose();

        private void Client_MessageReceived(object sender, IChromiumEvent e)
        {
            try
            {
                switch (e)
                {
                    case PageFrameAttachedChromiumEvent pageFrameAttached:
                        OnFrameAttached(pageFrameAttached);
                        break;
                    case PageFrameNavigatedChromiumEvent pageFrameNavigated:
                        OnFrameNavigated(pageFrameNavigated.Frame, false);
                        break;
                    case PageLifecycleEventChromiumEvent pageLifecycleEvent:
                        OnLifecycleEvent(pageLifecycleEvent);
                        break;
                }
            }

            // We need to silence exceptions on async void events.
#pragma warning disable CA1031 // Do not catch general exception types.
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types.
            {
                // TODO Add Logger
                /*
                var message = $"Page failed to process {e.MessageID}. {ex.Message}. {ex.StackTrace}";
                _logger.LogError(ex, message);
                */
                Client.OnClosed();
            }
        }

        private void OnLifecycleEvent(PageLifecycleEventChromiumEvent e)
        {
            if (e.Name == "load")
            {
                Page.FrameManager.FrameLifecycleEvent(e.FrameId, "load");
            }
            else if (e.Name == "DOMContentLoaded")
            {
                Page.FrameManager.FrameLifecycleEvent(e.FrameId, "domcontentloaded");
            }
        }

        private void OnFrameNavigated(Protocol.Page.Frame frame, bool initial)
            => Page.FrameManager.FrameCommittedNewDocumentNavigation(frame.Id, frame.Url, frame.Name ?? string.Empty, frame.LoaderId, initial);

        private async Task EnsureIsolatedWorldAsync(string name)
        {
            if (!_isolatedWorlds.Add(name))
            {
                return;
            }

            await Client.SendAsync(new PageAddScriptToEvaluateOnNewDocumentRequest
            {
                Source = $"//# sourceURL={EvaluationScriptUrl}",
                WorldName = name,
            }).ConfigureAwait(false);

            await Task.WhenAll(Page.Frames.Select(frame => Client.SendAsync(new PageCreateIsolatedWorldRequest
            {
                FrameId = frame.Id,
                GrantUniveralAccess = true,
                WorldName = name,
            })).ToArray()).ConfigureAwait(false);
        }

        private Task EmulateTimezoneAsync(string timezoneId)
        {
            throw new NotImplementedException();
        }

        private void HandleFrameTree(FrameTree frameTree)
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

        private void OnFrameAttached(PageFrameAttachedChromiumEvent e) => OnFrameAttached(e.FrameId, e.ParentFrameId);

        private void OnFrameAttached(string frameId, string parentFrameId) => Page.FrameManager.FrameAttached(frameId, parentFrameId);
    }
}
