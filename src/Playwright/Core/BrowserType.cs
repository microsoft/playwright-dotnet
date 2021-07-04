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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright.API.Generated.Options;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    internal class BrowserType : ChannelOwnerBase, IChannelOwner<BrowserType>, IBrowserType
    {
        private readonly IChannelOwner _parent;
        private readonly BrowserTypeInitializer _initializer;
        private readonly BrowserTypeChannel _channel;

        internal BrowserType(IChannelOwner parent, string guid, BrowserTypeInitializer initializer) : base(parent, guid)
        {
            _initializer = initializer;
            _parent = parent;
            _channel = new(guid, parent.Connection, this);
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<BrowserType> IChannelOwner<BrowserType>.Channel => _channel;

        public string ExecutablePath => _initializer.ExecutablePath;

        public string Name => _initializer.Name;

        public async Task<IBrowser> LaunchAsync(BrowserTypeLaunchOptions options = default)
        {
            options ??= new BrowserTypeLaunchOptions();
            return (await _channel.LaunchAsync(
                headless: options.Headless,
                channel: options.Channel,
                executablePath: options.ExecutablePath,
                passedArguments: options.Args,
                proxy: options.Proxy,
                downloadsPath: options.DownloadsPath,
                tracesDir: options.TracesDir,
                chromiumSandbox: options.ChromiumSandbox,
                firefoxUserPrefs: options.FirefoxUserPrefs,
                handleSIGINT: options.HandleSIGINT,
                handleSIGTERM: options.HandleSIGTERM,
                handleSIGHUP: options.HandleSIGHUP,
                timeout: options.Timeout,
                env: options.Env,
                devtools: options.Devtools,
                slowMo: options.SlowMo,
                ignoreDefaultArgs: options.IgnoreDefaultArgs,
                ignoreAllDefaultArgs: options.IgnoreAllDefaultArgs).ConfigureAwait(false)).Object;
        }

        public async Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, BrowserTypeLaunchPersistentContextOptions options = default)
        {
            options ??= new BrowserTypeLaunchPersistentContextOptions();
            return (await _channel.LaunchPersistentContextAsync(
                userDataDir,
                args: options.Args,
                channel: options.Channel,
                chromiumSandbox: options.ChromiumSandbox,
                devtools: options.Devtools,
                downloadsPath: options.DownloadsPath,
                env: options.Env,
                executablePath: options.ExecutablePath,
                handleSIGINT: options.HandleSIGINT,
                handleSIGTERM: options.HandleSIGTERM,
                handleSIGHUP: options.HandleSIGHUP,
                headless: options.Headless,
                proxy: options.Proxy,
                timeout: options.Timeout,
                tracesDir: options.TracesDir,
                slowMo: options.SlowMo,
                ignoreDefaultArgs: options.IgnoreDefaultArgs,
                ignoreAllDefaultArgs: options.IgnoreAllDefaultArgs,
                acceptDownloads: options.AcceptDownloads,
                ignoreHTTPSErrors: options.IgnoreHTTPSErrors,
                bypassCSP: options.BypassCSP,
                viewportSize: options.ViewportSize,
                screenSize: options.ScreenSize,
                userAgent: options.UserAgent,
                deviceScaleFactor: options.DeviceScaleFactor,
                isMobile: options.IsMobile,
                hasTouch: options.HasTouch,
                javaScriptEnabled: options.JavaScriptEnabled,
                timezoneId: options.TimezoneId,
                geolocation: options.Geolocation,
                locale: options.Locale,
                permissions: options.Permissions,
                extraHTTPHeaders: options.ExtraHTTPHeaders,
                offline: options.Offline,
                httpCredentials: options.HttpCredentials,
                colorScheme: options.ColorScheme,
                reducedMotion: options.ReducedMotion,
                recordHarPath: options.RecordHarPath,
                recordHarOmitContent: options.RecordHarOmitContent,
                recordVideo: Browser.GetVideoArgs(options.RecordVideoDir, options.RecordVideoSize)).ConfigureAwait(false)).Object;
        }

        public async Task<IBrowser> ConnectAsync(string wsEndpoint, BrowserTypeConnectOptions options = null)
        {
            options ??= new BrowserTypeConnectOptions();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddFilter((f, _) => f == "PlaywrightSharp.Playwright");
            });
            WebSocketTransport webSocketTransport = new WebSocketTransport(wsEndpoint, options, loggerFactory);
            await webSocketTransport.ConnectAsync().ConfigureAwait(false);
            _parent.Connection.Transport = webSocketTransport;

            Connection connection = new Connection(webSocketTransport, loggerFactory);
            var playwright = await connection.WaitForObjectWithKnownNameAsync<PlaywrightImpl>("Playwright").ConfigureAwait(false);
            playwright.Connection = connection;

            if (playwright.Intitializer.PreLaunchedBrowser == null)
            {
                _parent.Connection.Close("Disconnected");
                throw new PlaywrightException("Malformed endpoint. Did you use launchServer method?");
            }

            Browser browser = playwright.Intitializer.PreLaunchedBrowser;
            browser.IsRemote = true;
            browser.Disconnected += Browser_Disconnected;
            return browser;
        }

        private void Browser_Disconnected(object sender, IBrowser e)
        {
            _parent.Connection.Close("Browser Disconnected");
        }
    }
}
