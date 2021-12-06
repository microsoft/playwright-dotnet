/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    internal class Browser : ChannelOwnerBase, IChannelOwner<Browser>, IBrowser
    {
        private readonly BrowserInitializer _initializer;
        private readonly TaskCompletionSource<bool> _closedTcs = new();
        private bool _isClosedOrClosing;

        internal Browser(IChannelOwner parent, string guid, BrowserInitializer initializer) : base(parent, guid)
        {
            Channel = new(guid, parent.Connection, this);
            IsConnected = true;
            Channel.Closed += (_, _) => DidClose();
            _initializer = initializer;
        }

        public event EventHandler<IBrowser> Disconnected;

        ChannelBase IChannelOwner.Channel => Channel;

        IChannel<Browser> IChannelOwner<Browser>.Channel => Channel;

        public IReadOnlyList<IBrowserContext> Contexts => BrowserContextsList.ToArray();

        public bool IsConnected { get; private set; }

        public bool IsRemote { get; set; }

        public bool IsConnectedOverWebSocket { get; set; }

        public string Version => _initializer.Version;

        internal BrowserChannel Channel { get; }

        internal List<BrowserContext> BrowserContextsList { get; } = new();

        public async Task CloseAsync()
        {
            if (!_isClosedOrClosing)
            {
                _isClosedOrClosing = true;
                await Channel.CloseAsync().ConfigureAwait(false);
            }
            if (IsConnectedOverWebSocket)
            {
                await NotifyBrowserClosedAsync().ConfigureAwait(false);
                return;
            }
            await _closedTcs.Task.ConfigureAwait(false);
        }

        public async Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions options = default)
        {
            options ??= new();
            var context = (await Channel.NewContextAsync(
               acceptDownloads: options.AcceptDownloads,
               bypassCSP: options.BypassCSP,
               colorScheme: options.ColorScheme,
               reducedMotion: options.ReducedMotion,
               deviceScaleFactor: options.DeviceScaleFactor,
               extraHTTPHeaders: options.ExtraHTTPHeaders,
               geolocation: options.Geolocation,
               hasTouch: options.HasTouch,
               httpCredentials: options.HttpCredentials,
               ignoreHTTPSErrors: options.IgnoreHTTPSErrors,
               isMobile: options.IsMobile,
               javaScriptEnabled: options.JavaScriptEnabled,
               locale: options.Locale,
               offline: options.Offline,
               permissions: options.Permissions,
               proxy: options.Proxy,
               recordHarOmitContent: options.RecordHarOmitContent,
               recordHarPath: options.RecordHarPath,
               recordVideo: GetVideoArgs(options.RecordVideoDir, options.RecordVideoSize),
               storageState: options.StorageState,
               storageStatePath: options.StorageStatePath,
               timezoneId: options.TimezoneId,
               userAgent: options.UserAgent,
               viewportSize: options.ViewportSize,
               screenSize: options.ScreenSize,
               baseUrl: options.BaseURL,
               strictSelectors: options.StrictSelectors,
               forcedColors: options.ForcedColors).ConfigureAwait(false)).Object;

            context.RecordVideo = !string.IsNullOrEmpty(options.RecordVideoDir);

            BrowserContextsList.Add(context);
            return context;
        }

        public async Task<IPage> NewPageAsync(BrowserNewPageOptions options = default)
        {
            options ??= new();

            var contextOptions = new BrowserNewContextOptions()
            {
                AcceptDownloads = options.AcceptDownloads,
                IgnoreHTTPSErrors = options.IgnoreHTTPSErrors,
                BypassCSP = options.BypassCSP,
                ViewportSize = options.ViewportSize,
                ScreenSize = options.ScreenSize,
                UserAgent = options.UserAgent,
                DeviceScaleFactor = options.DeviceScaleFactor,
                IsMobile = options.IsMobile,
                HasTouch = options.HasTouch,
                JavaScriptEnabled = options.JavaScriptEnabled,
                TimezoneId = options.TimezoneId,
                Geolocation = options.Geolocation,
                Locale = options.Locale,
                Permissions = options.Permissions,
                ExtraHTTPHeaders = options.ExtraHTTPHeaders,
                Offline = options.Offline,
                HttpCredentials = options.HttpCredentials,
                ColorScheme = options.ColorScheme,
                ReducedMotion = options.ReducedMotion,
                ForcedColors = options.ForcedColors,
                RecordHarPath = options.RecordHarPath,
                RecordHarOmitContent = options.RecordHarOmitContent,
                RecordVideoDir = options.RecordVideoDir,
                RecordVideoSize = options.RecordVideoSize,
                Proxy = options.Proxy,
                StorageState = options.StorageState,
                StorageStatePath = options.StorageStatePath,
                BaseURL = options.BaseURL,
                StrictSelectors = options.StrictSelectors,
            };

            var context = (BrowserContext)await NewContextAsync(contextOptions).ConfigureAwait(false);

            var page = (Page)await context.NewPageAsync().ConfigureAwait(false);
            page.OwnedContext = context;
            context.Options = contextOptions;
            context.OwnerPage = page;
            return page;
        }

        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

        internal static Dictionary<string, object> GetVideoArgs(string recordVideoDir, RecordVideoSize recordVideoSize)
        {
            Dictionary<string, object> recordVideoArgs = null;

            if (recordVideoSize != null && string.IsNullOrEmpty(recordVideoDir))
            {
                throw new PlaywrightException("\"RecordVideoSize\" option requires \"RecordVideoDir\" to be specified");
            }

            if (!string.IsNullOrEmpty(recordVideoDir))
            {
                recordVideoArgs = new()
                {
                    { "dir", recordVideoDir },
                };

                if (recordVideoSize != null)
                {
                    recordVideoArgs["size"] = recordVideoSize;
                }
            }

            return recordVideoArgs;
        }

        public async Task NotifyBrowserClosedAsync()
        {
            foreach (BrowserContext context in BrowserContextsList.ToArray())
            {
                foreach (Page page in context.PagesList.ToArray())
                {
                    await page.CloseAsync().ConfigureAwait(false);
                }
                await context.CloseAsync().ConfigureAwait(false);
            }
            DidClose();
        }

        private void DidClose()
        {
            IsConnected = false;
            _isClosedOrClosing = true;
            Disconnected?.Invoke(this, this);
            _closedTcs.TrySetResult(true);
        }
    }
}
