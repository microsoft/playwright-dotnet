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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class BrowserType : ChannelOwnerBase, IChannelOwner<BrowserType>, IBrowserType
{
    private readonly BrowserTypeInitializer _initializer;
    private readonly BrowserTypeChannel _channel;

    internal BrowserType(IChannelOwner parent, string guid, BrowserTypeInitializer initializer) : base(parent, guid)
    {
        _initializer = initializer;
        _channel = new(guid, parent.Connection, this);
    }

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<BrowserType> IChannelOwner<BrowserType>.Channel => _channel;

    internal PlaywrightImpl Playwright { get; set; }

    public string ExecutablePath => _initializer.ExecutablePath;

    public string Name => _initializer.Name;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IBrowser> LaunchAsync(BrowserTypeLaunchOptions options = default)
    {
        options ??= new BrowserTypeLaunchOptions();
        Browser browser = (await _channel.LaunchAsync(
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
        DidLaunchBrowser(browser);
        return browser;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, BrowserTypeLaunchPersistentContextOptions options = default)
    {
        options ??= new BrowserTypeLaunchPersistentContextOptions();
        var context = (await _channel.LaunchPersistentContextAsync(
            userDataDir,
            headless: options.Headless,
            channel: options.Channel,
            executablePath: options.ExecutablePath,
            args: options.Args,
            proxy: options.Proxy,
            downloadsPath: options.DownloadsPath,
            tracesDir: options.TracesDir,
            chromiumSandbox: options.ChromiumSandbox,
            handleSIGINT: options.HandleSIGINT,
            handleSIGTERM: options.HandleSIGTERM,
            handleSIGHUP: options.HandleSIGHUP,
            timeout: options.Timeout,
            env: options.Env,
            devtools: options.Devtools,
            slowMo: options.SlowMo,
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
            recordHarContent: options.RecordHarContent,
            recordHarMode: options.RecordHarMode,
            recordHarPath: options.RecordHarPath,
            recordHarOmitContent: options.RecordHarOmitContent,
            recordHarUrlFilter: options.RecordHarUrlFilter,
            recordHarUrlFilterString: options.RecordHarUrlFilterString,
            recordHarUrlFilterRegex: options.RecordHarUrlFilterRegex,
            recordVideo: Browser.GetVideoArgs(options.RecordVideoDir, options.RecordVideoSize),
            serviceWorkers: options.ServiceWorkers,
            ignoreDefaultArgs: options.IgnoreDefaultArgs,
            ignoreAllDefaultArgs: options.IgnoreAllDefaultArgs,
            baseUrl: options.BaseURL,
            forcedColors: options.ForcedColors).ConfigureAwait(false)).Object;

        // TODO: unite with a single browser context options type which is derived from channels
        DidCreateContext(
            context,
            new()
            {
                RecordVideoDir = options.RecordVideoDir,
                RecordVideoSize = options.RecordVideoSize,
                RecordHarContent = options.RecordHarContent,
                RecordHarMode = options.RecordHarMode,
                RecordHarOmitContent = options.RecordHarOmitContent,
                RecordHarPath = options.RecordHarPath,
                RecordHarUrlFilter = options.RecordHarUrlFilter,
                RecordHarUrlFilterString = options.RecordHarUrlFilterString,
                RecordHarUrlFilterRegex = options.RecordHarUrlFilterRegex,
            },
            options?.TracesDir);

        return context;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IBrowser> ConnectAsync(string wsEndpoint, BrowserTypeConnectOptions options = null)
    {
        options ??= new BrowserTypeConnectOptions();
        var headers = new List<KeyValuePair<string, string>>(options.Headers ?? new Dictionary<string, string>())
            {
                new KeyValuePair<string, string>("x-playwright-browser", Name),
            }.ToDictionary(pair => pair.Key, pair => pair.Value);
        var localUtils = _channel.Connection.LocalUtils;
        var pipe = await localUtils.ConnectAsync(wsEndpoint: wsEndpoint, headers: headers, slowMo: options.SlowMo, timeout: options.Timeout, exposeNetwork: options.ExposeNetwork).ConfigureAwait(false);

        void ClosePipe()
        {
            pipe.CloseAsync().IgnoreException();
        }
#pragma warning disable CA2000 // Dispose objects before losing scope
        var connection = new Connection(_channel.Connection.LocalUtils);
#pragma warning restore CA2000
        connection.MarkAsRemote();
        connection.Close += (_, _) => ClosePipe();

        Exception closeError = null;
        Browser browser = null;
        void OnPipeClosed()
        {
            // Emulate all pages, contexts and the browser closing upon disconnect.
            foreach (BrowserContext context in browser?._contexts.ToArray() ?? Array.Empty<BrowserContext>())
            {
                foreach (Page page in context._pages.ToArray())
                {
                    page.OnClose();
                }
                context.OnClose();
            }
            browser?.DidClose();
            connection.DoClose(closeError);
        }
        pipe.Closed += (_, _) => OnPipeClosed();
        connection.OnMessage = async (object message, bool _) =>
        {
            try
            {
                await pipe.SendAsync(message).ConfigureAwait(false);
            }
            catch (Exception e) when (DriverMessages.IsTargetClosedError(e))
            {
                // swallow exception
            }
            catch
            {
                OnPipeClosed();
            }
        };

        pipe.Message += (_, message) =>
        {
            try
            {
                connection.Dispatch(message);
            }
            catch (Exception ex)
            {
                closeError = ex;
                _channel.Connection.TraceMessage("pw:dotnet", $"Dispatching error: {ex.Message}\n{ex.StackTrace}");
                ClosePipe();
            }
        };

        async Task<IBrowser> CreateBrowserAsync()
        {
            var playwright = await connection.InitializePlaywrightAsync().ConfigureAwait(false);
            if (playwright.PreLaunchedBrowser == null)
            {
                ClosePipe();
                throw new ArgumentException("Malformed endpoint. Did you use launchServer method?");
            }
            playwright.SetSelectors(this.Playwright._selectors);
            browser = playwright.PreLaunchedBrowser;
            browser.ShouldCloseConnectionOnClose = true;
            browser.Disconnected += (_, _) => ClosePipe();
            DidLaunchBrowser(browser);
            return playwright.PreLaunchedBrowser;
        }
        var task = CreateBrowserAsync();
        var timeout = options?.Timeout != null ? (int)options.Timeout : 30_000;
        return await task.WithTimeout(timeout, _ => throw new TimeoutException($"BrowserType.ConnectAsync: Timeout {options.Timeout}ms exceeded")).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IBrowser> ConnectOverCDPAsync(string endpointURL, BrowserTypeConnectOverCDPOptions options = null)
    {
        if (Name != "chromium")
        {
            throw new ArgumentException("Connecting over CDP is only supported in Chromium.");
        }
        options ??= new BrowserTypeConnectOverCDPOptions();
        JsonElement result = await _channel.ConnectOverCDPAsync(endpointURL, headers: options.Headers, slowMo: options.SlowMo, timeout: options.Timeout).ConfigureAwait(false);
        Browser browser = result.GetProperty("browser").ToObject<Browser>(_channel.Connection.DefaultJsonSerializerOptions);
        DidLaunchBrowser(browser);
        if (result.TryGetProperty("defaultContext", out JsonElement defaultContextValue))
        {
            var defaultContext = defaultContextValue.ToObject<BrowserContext>(_channel.Connection.DefaultJsonSerializerOptions);
            DidCreateContext(defaultContext, new(), null);
        }
        return browser;
    }

    internal void DidLaunchBrowser(Browser browser)
    {
        browser._browserType = this;
    }

    internal void DidCreateContext(BrowserContext context, BrowserNewContextOptions contextOptions, string tracesDir)
    {
        context.SetOptions(contextOptions, tracesDir);
    }
}
