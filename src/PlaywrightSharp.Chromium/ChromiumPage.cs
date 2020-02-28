using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Helpers;
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
            client.MessageReceived += Client_MessageReceived;
        }

        public ChromiumTarget Target { get; set; }

        internal Page Page { get; }

        public async Task<GotoResult> NavigateFrameAsync(IFrame frame, string url, string referrer)
        {
            var response = await _client.SendAsync(new PageNavigateRequest
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

        internal async Task InitializeAsync()
        {
            var getFrameTreeTask = _client.SendAsync(new PageGetFrameTreeRequest());

            await Task.WhenAll(
                _client.SendAsync(new PageEnableRequest()),
                getFrameTreeTask).ConfigureAwait(false);

            HandleFrameTree(getFrameTreeTask.Result.FrameTree);

            var tasks = new List<Task>
            {
               _client.SendAsync(new LogEnableRequest()),
               _client.SendAsync(new PageSetLifecycleEventsEnabledRequest { Enabled = true }),
               _client.SendAsync(new RuntimeEnableRequest()).ContinueWith(t => EnsureIsolatedWorldAsync(), TaskScheduler.Default),
               _networkManager.InitializeAsync(),
            };

            if (_browserContext.Options != null)
            {
                var options = _browserContext.Options;

                if (options.BypassCSP)
                {
                    tasks.Add(_client.SendAsync(new PageSetBypassCSPRequest { Enabled = true }));
                }

                if (options.IgnoreHTTPSErrors)
                {
                    tasks.Add(_client.SendAsync(new SecuritySetIgnoreCertificateErrorsRequest { Ignore = true }));
                }

                if (options.Viewport != null)
                {
                    tasks.Add(SetViewportAsync(options.Viewport));
                }

                if (!options.JavaScriptEnabled)
                {
                    tasks.Add(_client.SendAsync(new EmulationSetScriptExecutionDisabledRequest { Value = true }));
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
                    tasks.Add(_client.SendAsync(new EmulationSetGeolocationOverrideRequest
                    {
                        Accuracy = (double)options.Geolocation.Accuracy,
                        Latitude = (double)options.Geolocation.Latitude,
                        Longitude = (double)options.Geolocation.Longitude,
                    }));
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private void Client_MessageReceived(object sender, MessageEventArgs e)
        {
            try
            {
                switch (e.MessageID)
                {
                    case "Page.frameAttached":
                        OnFrameAttached(e.MessageData?.ToObject<PageFrameAttachedEventArgs>());
                        break;
                    case "Page.frameNavigated":
                        OnFrameNavigated(e.MessageData?.ToObject<PageFrameNavigatedEventArgs>()?.Frame, false);
                        break;
                    case "Page.lifecycleEvent":
                        OnLifecycleEvent(e.MessageData?.ToObject<PageLifecycleEventEventArgs>());
                        break;
                }
            }

            // We need to silence exceptions on async void events.
#pragma warning disable CA1031 // Do not catch general exception types.
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types.
            {
                // TODO Add Logger
                /*
                var message = $"Page failed to process {e.MessageID}. {ex.Message}. {ex.StackTrace}";
                _logger.LogError(ex, message);
                */
                _client.Close(ex.Message);
            }
        }

        private void OnLifecycleEvent(PageLifecycleEventEventArgs e)
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

        private Task EnsureIsolatedWorldAsync() => Task.CompletedTask;

        private Task EmulateTimezoneAsync(string timezoneId)
        {
            throw new NotImplementedException();
        }

        private void HandleFrameTree(Protocol.Page.FrameTree frameTree)
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

        private void OnFrameAttached(PageFrameAttachedEventArgs e) => OnFrameAttached(e.FrameId, e.ParentFrameId);

        private void OnFrameAttached(string frameId, string parentFrameId) => Page.FrameManager.FrameAttached(frameId, parentFrameId);
    }
}
