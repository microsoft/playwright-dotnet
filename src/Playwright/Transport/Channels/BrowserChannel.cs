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
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Transport.Channels;

internal class BrowserChannel : Channel<Browser>
{
    public BrowserChannel(string guid, Connection connection, Browser owner) : base(guid, connection, owner)
    {
    }

    internal event EventHandler Closed;

    internal static Dictionary<string, object> PrepareHarOptions(
        HarContentPolicy? recordHarContent,
        HarMode? recordHarMode,
        string recordHarPath,
        bool? recordHarOmitContent,
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
        if (!string.IsNullOrEmpty(recordHarUrlFilterString))
        {
            recordHarArgs["urlGlob"] = recordHarUrlFilterString;
        }
        else if (recordHarUrlFilterRegex != null)
        {
            recordHarArgs["urlRegex"] = recordHarUrlFilterRegex;
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

    internal override void OnMessage(string method, JsonElement? serverParams)
    {
        switch (method)
        {
            case "close":
                Closed?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    internal Task<BrowserContextChannel> NewContextAsync(
        bool? acceptDownloads = null,
        bool? bypassCSP = null,
        ColorScheme? colorScheme = null,
        ReducedMotion? reducedMotion = null,
        ForcedColors? forcedColors = null,
        float? deviceScaleFactor = null,
        IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders = null,
        Geolocation geolocation = null,
        bool? hasTouch = null,
        HttpCredentials httpCredentials = null,
        bool? ignoreHTTPSErrors = null,
        bool? isMobile = null,
        bool? javaScriptEnabled = null,
        string locale = null,
        bool? offline = null,
        IEnumerable<string> permissions = null,
        Proxy proxy = null,
        HarContentPolicy? recordHarContent = default,
        HarMode? recordHarMode = default,
        string recordHarPath = default,
        bool? recordHarOmitContent = default,
        string recordHarUrlFilterString = default,
        Regex recordHarUrlFilterRegex = default,
        Dictionary<string, object> recordVideo = null,
        string storageState = null,
        string storageStatePath = null,
        ServiceWorkerPolicy? serviceWorkers = default,
        string timezoneId = null,
        string userAgent = null,
        ViewportSize viewportSize = default,
        ScreenSize screenSize = default,
        string baseUrl = default,
        bool? strictSelectors = default)
    {
        var args = new Dictionary<string, object>
            {
                { "acceptDownloads", acceptDownloads },
                { "bypassCSP", bypassCSP },
                { "colorScheme", colorScheme },
                { "reducedMotion", reducedMotion },
                { "deviceScaleFactor", deviceScaleFactor },
                { "serviceWorkers", serviceWorkers },
            };

        if (extraHTTPHeaders != null)
        {
            args["extraHTTPHeaders"] = extraHTTPHeaders.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray();
        }

        args.Add("geolocation", geolocation);
        args.Add("hasTouch", hasTouch);
        args.Add("httpCredentials", httpCredentials);
        args.Add("ignoreHTTPSErrors", ignoreHTTPSErrors);
        args.Add("isMobile", isMobile);
        args.Add("javaScriptEnabled", javaScriptEnabled);
        args.Add("locale", locale);
        args.Add("offline", offline);
        args.Add("permissions", permissions);
        args.Add("proxy", proxy);
        args.Add("strictSelectors", strictSelectors);
        args.Add("forcedColors", forcedColors);

        var recordHarOptions = PrepareHarOptions(
            recordHarContent: recordHarContent,
            recordHarMode: recordHarMode,
            recordHarPath: recordHarPath,
            recordHarOmitContent: recordHarOmitContent,
            recordHarUrlFilterString: recordHarUrlFilterString,
            recordHarUrlFilterRegex: recordHarUrlFilterRegex);
        if (recordHarOptions != null)
        {
            args["recordHar"] = recordHarOptions;
        }

        if (recordVideo != null)
        {
            args.Add("recordVideo", recordVideo);
        }

        if (!string.IsNullOrEmpty(storageStatePath))
        {
            if (!File.Exists(storageStatePath))
            {
                throw new PlaywrightException($"The specified storage state file does not exist: {storageStatePath}");
            }

            storageState = File.ReadAllText(storageStatePath);
        }

        if (!string.IsNullOrEmpty(storageState))
        {
            args.Add("storageState", JsonSerializer.Deserialize<StorageState>(storageState, Helpers.JsonExtensions.DefaultJsonSerializerOptions));
        }

        args.Add("timezoneId", timezoneId);
        args.Add("userAgent", userAgent);

        if (viewportSize?.Width == -1)
        {
            args.Add("noDefaultViewport", true);
        }
        else
        {
            args.Add("viewport", viewportSize);
            args.Add("screen", screenSize);
        }

        args.Add("baseURL", baseUrl);

        return Connection.SendMessageToServerAsync<BrowserContextChannel>(
            Guid,
            "newContext",
            args);
    }

    internal Task CloseAsync() => Connection.SendMessageToServerAsync<BrowserContextChannel>(Guid, "close", null);

    internal Task StartTracingAsync(IPage page, bool screenshots, string path, IEnumerable<string> categories)
    {
        var args = new Dictionary<string, object>
        {
            ["screenshots"] = screenshots,
            ["path"] = path,
            ["page"] = page,
            ["categories"] = categories,
        };

        return Connection.SendMessageToServerAsync(Guid, "crStartTracing", args);
    }

    internal async Task<string> StopTracingAsync()
        => (await Connection.SendMessageToServerAsync(Guid, "crStopTracing", null).ConfigureAwait(false))?.GetProperty("binary").ToString();
}
