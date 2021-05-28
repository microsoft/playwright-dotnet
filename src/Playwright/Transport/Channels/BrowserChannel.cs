using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class BrowserChannel : Channel<Browser>
    {
        public BrowserChannel(string guid, Connection connection, Browser owner) : base(guid, connection, owner)
        {
        }

        internal event EventHandler Closed;

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
            bool? recordHarOmitContent = null,
            string recordHarPath = null,
            string recordVideoDir = null,
            RecordVideoSize recordVideoSize = null,
            string storageState = null,
            string storageStatePath = null,
            string timezoneId = null,
            string userAgent = null,
            ViewportSize viewportSize = default)
        {
            if (recordVideoSize != null && string.IsNullOrEmpty(recordVideoDir))
            {
                throw new PlaywrightException("\"RecordVideoSize\" option requires \"RecordVideoDir\" to be specified");
            }

            var args = new Dictionary<string, object>
            {
                { "acceptDownloads", acceptDownloads },
                { "bypassCSP", bypassCSP },
                { "colorScheme", colorScheme },
                { "deviceScaleFactor", deviceScaleFactor },
                { "geolocation", geolocation },
                { "hasTouch", hasTouch },
                { "httpCredentials", httpCredentials },
                { "ignoreHTTPSErrors", ignoreHTTPSErrors },
                { "isMobile", isMobile },
                { "javaScriptEnabled", javaScriptEnabled },
                { "locale", locale },
                { "offline", offline },
                { "permissions", permissions },
                { "proxy", proxy },
                { "storageState", storageState },
                { "storageStatePath", storageStatePath },
                { "timezoneId", timezoneId },
                { "userAgent", userAgent },
            };
            args["sdkLanguage"] = "csharp";

            if (extraHTTPHeaders != null)
            {
                args["extraHTTPHeaders"] = extraHTTPHeaders.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray();
            }

            if (!string.IsNullOrEmpty(recordHarPath))
            {
                args.Add("recordHar", new
                {
                    Path = recordHarPath,
                    OmitContent = recordHarOmitContent,
                });
            }

            if (!string.IsNullOrEmpty(recordVideoDir))
            {
                var recordVideoArgs = new Dictionary<string, object>()
                {
                    { "dir", recordVideoDir },
                };

                if (recordVideoSize != null)
                {
                    recordVideoArgs["size"] = recordVideoSize;
                }

                args.Add("recordVideo", recordVideoArgs);
            }

            if (viewportSize?.Width == -1)
            {
                args.Add("noDefaultViewport", true);
            }
            else
            {
                args.Add("viewport", viewportSize);
            }

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
}
