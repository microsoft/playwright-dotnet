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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Browser : ChannelOwnerBase, IChannelOwner<Browser>, IBrowser
{
    private readonly BrowserInitializer _initializer;
    private readonly TaskCompletionSource<bool> _closedTcs = new();
    internal readonly List<BrowserContext> _contexts = new();
    internal BrowserType _browserType;
    internal string _closeReason;

    internal Browser(IChannelOwner parent, string guid, BrowserInitializer initializer) : base(parent, guid)
    {
        Channel = new(guid, parent.Connection, this);
        IsConnected = true;
        _initializer = initializer;
    }

    public event EventHandler<IBrowser> Disconnected;

    ChannelBase IChannelOwner.Channel => Channel;

    IChannel<Browser> IChannelOwner<Browser>.Channel => Channel;

    public IReadOnlyList<IBrowserContext> Contexts => _contexts.ToArray();

    public bool IsConnected { get; private set; }

    internal bool ShouldCloseConnectionOnClose { get; set; }

    public string Version => _initializer.Version;

    internal BrowserChannel Channel { get; }

    public IBrowserType BrowserType => _browserType;

    internal override void OnMessage(string method, JsonElement? serverParams)
    {
        switch (method)
        {
            case "close":
                DidClose();
                break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task CloseAsync(BrowserCloseOptions options = default)
    {
        _closeReason = options?.Reason;
        try
        {
            if (ShouldCloseConnectionOnClose)
            {
                Channel.Connection.DoClose();
            }
            else
            {
                await SendMessageToServerAsync<BrowserContextChannel>("close", new Dictionary<string, object>
                {
                    ["reason"] = options?.Reason,
                }).ConfigureAwait(false);
            }
            await _closedTcs.Task.ConfigureAwait(false);
        }
        catch (Exception e) when (DriverMessages.IsTargetClosedError(e))
        {
            // Swallow exception
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions options = default)
    {
        options ??= new();

        var args = new Dictionary<string, object>
        {
            ["bypassCSP"] = options.BypassCSP,
            ["deviceScaleFactor"] = options.DeviceScaleFactor,
            ["serviceWorkers"] = options.ServiceWorkers,
            ["geolocation"] = options.Geolocation,
            ["hasTouch"] = options.HasTouch,
            ["httpCredentials"] = options.HttpCredentials,
            ["ignoreHTTPSErrors"] = options.IgnoreHTTPSErrors,
            ["isMobile"] = options.IsMobile,
            ["javaScriptEnabled"] = options.JavaScriptEnabled,
            ["locale"] = options.Locale,
            ["offline"] = options.Offline,
            ["permissions"] = options.Permissions,
            ["proxy"] = options.Proxy,
            ["strictSelectors"] = options.StrictSelectors,
            ["colorScheme"] = options.ColorScheme == ColorScheme.Null ? "no-override" : options.ColorScheme,
            ["reducedMotion"] = options.ReducedMotion == ReducedMotion.Null ? "no-override" : options.ReducedMotion,
            ["forcedColors"] = options.ForcedColors == ForcedColors.Null ? "no-override" : options.ForcedColors,
            ["extraHTTPHeaders"] = options.ExtraHTTPHeaders?.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray(),
            ["recordHar"] = PrepareHarOptions(
                    recordHarContent: options.RecordHarContent,
                    recordHarMode: options.RecordHarMode,
                    recordHarPath: options.RecordHarPath,
                    recordHarOmitContent: options.RecordHarOmitContent,
                    recordHarUrlFilter: options.RecordHarUrlFilter,
                    recordHarUrlFilterString: options.RecordHarUrlFilterString,
                    recordHarUrlFilterRegex: options.RecordHarUrlFilterRegex),
            ["recordVideo"] = GetVideoArgs(options.RecordVideoDir, options.RecordVideoSize),
            ["timezoneId"] = options.TimezoneId,
            ["userAgent"] = options.UserAgent,
            ["baseURL"] = options.BaseURL,
        };

        if (options.AcceptDownloads.HasValue)
        {
            args.Add("acceptDownloads", options.AcceptDownloads.Value ? "accept" : "deny");
        }

        var storageState = options.StorageState;
        if (!string.IsNullOrEmpty(options.StorageStatePath))
        {
            if (!File.Exists(options.StorageStatePath))
            {
                throw new PlaywrightException($"The specified storage state file does not exist: {options.StorageStatePath}");
            }

            storageState = File.ReadAllText(options.StorageStatePath);
        }

        if (!string.IsNullOrEmpty(storageState))
        {
            args.Add("storageState", JsonSerializer.Deserialize<StorageState>(storageState, Helpers.JsonExtensions.DefaultJsonSerializerOptions));
        }

        if (options.ViewportSize?.Width == -1)
        {
            args.Add("noDefaultViewport", true);
        }
        else
        {
            args.Add("viewport", options.ViewportSize);
            args.Add("screen", options.ScreenSize);
        }

        var context = (await SendMessageToServerAsync<BrowserContextChannel>("newContext", args).ConfigureAwait(false)).Object;

        _browserType.DidCreateContext(context, options, null);
        return context;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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
            RecordHarContent = options.RecordHarContent,
            RecordHarMode = options.RecordHarMode,
            RecordHarOmitContent = options.RecordHarOmitContent,
            RecordHarUrlFilter = options.RecordHarUrlFilter,
            RecordHarUrlFilterString = options.RecordHarUrlFilterString,
            RecordHarUrlFilterRegex = options.RecordHarUrlFilterRegex,
            RecordVideoDir = options.RecordVideoDir,
            RecordVideoSize = options.RecordVideoSize,
            Proxy = options.Proxy,
            StorageState = options.StorageState,
            StorageStatePath = options.StorageStatePath,
            ServiceWorkers = options.ServiceWorkers,
            BaseURL = options.BaseURL,
            StrictSelectors = options.StrictSelectors,
        };

        var context = (BrowserContext)await NewContextAsync(contextOptions).ConfigureAwait(false);

        return await WrapApiCallAsync(async () =>
        {
            var page = (Page)await context.NewPageAsync().ConfigureAwait(false);
            page.OwnedContext = context;
            context.Options = contextOptions;
            context.OwnerPage = page;
            return page;
        }).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ValueTask DisposeAsync() => new ValueTask(CloseAsync());

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
                ["dir"] = System.IO.Path.Combine(Environment.CurrentDirectory, recordVideoDir),
            };

            if (recordVideoSize != null)
            {
                recordVideoArgs["size"] = recordVideoSize;
            }
        }

        return recordVideoArgs;
    }

    internal void DidClose()
    {
        IsConnected = false;
        Disconnected?.Invoke(this, this);
        _closedTcs.TrySetResult(true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<ICDPSession> NewBrowserCDPSessionAsync()
        => (await SendMessageToServerAsync<CDPChannel>(
        "newBrowserCDPSession").ConfigureAwait(false)).Object;

    internal static Dictionary<string, object> PrepareHarOptions(
        HarContentPolicy? recordHarContent,
        HarMode? recordHarMode,
        string recordHarPath,
        bool? recordHarOmitContent,
        string recordHarUrlFilter,
        string recordHarUrlFilterString,
        Regex recordHarUrlFilterRegex)
    {
        if (string.IsNullOrEmpty(recordHarPath))
        {
            return null;
        }
        var recordHarArgs = new Dictionary<string, object>();
        recordHarArgs["path"] = recordHarPath;
        if (recordHarContent.HasValue)
        {
            recordHarArgs["content"] = recordHarContent;
        }
        else if (recordHarOmitContent == true)
        {
            recordHarArgs["content"] = HarContentPolicy.Omit;
        }
        if (!string.IsNullOrEmpty(recordHarUrlFilter))
        {
            recordHarArgs["urlGlob"] = recordHarUrlFilter;
        }
        else if (!string.IsNullOrEmpty(recordHarUrlFilterString))
        {
            recordHarArgs["urlGlob"] = recordHarUrlFilterString;
        }
        else if (recordHarUrlFilterRegex != null)
        {
            recordHarArgs["urlRegexSource"] = recordHarUrlFilterRegex.ToString();
            recordHarArgs["urlRegexFlags"] = recordHarUrlFilterRegex.Options.GetInlineFlags();
        }
        if (recordHarMode.HasValue)
        {
            recordHarArgs["mode"] = recordHarMode;
        }

        if (recordHarArgs.Keys.Count > 0)
        {
            return recordHarArgs;
        }
        return null;
    }
}
