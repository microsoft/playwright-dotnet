using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Accessibility;
using PlaywrightSharp.Chromium.Messaging.Page;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IPage"/>
    internal class ChromiumPage : PageBase
    {
        private readonly ChromiumSession _client;
        private readonly ChromiumBrowser _browser;
        private readonly ChromiumBrowserContext _browserContext;

        public ChromiumPage(ChromiumSession client, ChromiumBrowser browser, ChromiumBrowserContext browserContext)
        {
            _client = client;
            _browser = browser;
            _browserContext = browserContext;
        }

        public ChromiumTarget Target { get; set; }

        private async Task InitializeAsync(bool ignoreHTTPSErrors)
        {
            var getFrameTreeTask = _client.SendAsync<PageGetFrameTreeResponse>("Page.getFrameTree");

            await Task.WhenAll(
                _client.SendAsync("Page.enable"),
                getFrameTreeTask).ConfigureAwait(false);

            HandleFrameTree(getFrameTreeTask.Result);

            FrameManager = await FrameManager.CreateFrameManagerAsync(Client, this, ignoreHTTPSErrors, _timeoutSettings).ConfigureAwait(false);
            var networkManager = FrameManager.NetworkManager;

            Client.MessageReceived += Client_MessageReceived;
            FrameManager.FrameAttached += (sender, e) => FrameAttached?.Invoke(this, e);
            FrameManager.FrameDetached += (sender, e) => FrameDetached?.Invoke(this, e);
            FrameManager.FrameNavigated += (sender, e) => FrameNavigated?.Invoke(this, e);

            networkManager.Request += (sender, e) => Request?.Invoke(this, e);
            networkManager.RequestFailed += (sender, e) => RequestFailed?.Invoke(this, e);
            networkManager.Response += (sender, e) => Response?.Invoke(this, e);
            networkManager.RequestFinished += (sender, e) => RequestFinished?.Invoke(this, e);

            await Task.WhenAll(
               Client.SendAsync("Target.setAutoAttach", new TargetSetAutoAttachRequest
               {
                   AutoAttach = true,
                   WaitForDebuggerOnStart = false,
                   Flatten = true
               }),
               Client.SendAsync("Performance.enable", null),
               Client.SendAsync("Log.enable", null)
           ).ConfigureAwait(false);

            try
            {
                await Client.SendAsync("Page.setInterceptFileChooserDialog", new PageSetInterceptFileChooserDialog
                {
                    Enabled = true
                }).ConfigureAwait(false);
            }
            catch
            {
                _fileChooserInterceptionIsDisabled = true;
            }
        }

        private async void HandleFrameTree(PageGetFrameTreeItem frameTree)
        {
            OnFrameAttached(frameTree.Frame.Id, frameTree.Frame.ParentId);
            OnFrameNavigated(frameTree.Frame, true);

            if (frameTree.Childs != null)
            {
                foreach (var child in frameTree.Childs)
                {
                    await HandleFrameTreeAsync(child);
                }
            }
        }

        private void OnFrameNavigated(PageGetFrameTreeItemInfo frame, bool initial)
            => FrameManager.FrameCommittedNewDocumentNavigation(frame.Id, frame.Url, frame.Name ?? string.Empty, frame.LoaderId, initial);

        private void OnFrameAttached(string frameId, string parentFrameId) => FrameManager.FrameAttached(frameId, parentFrameId);

    }
}