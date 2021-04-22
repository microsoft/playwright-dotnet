using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Transport.Channels
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
            ColorScheme colorScheme = ColorScheme.Undefined,
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
            var args = new Dictionary<string, object>();

            if (acceptDownloads.HasValue)
            {
                args.Add("acceptDownloads", acceptDownloads.Value);
            }

            if (bypassCSP.HasValue)
            {
                args.Add("bypassCSP", bypassCSP.Value);
            }

            if (colorScheme != ColorScheme.Undefined)
            {
                args.Add("colorScheme", colorScheme);
            }

            if (deviceScaleFactor.HasValue)
            {
                args.Add("deviceScaleFactor", deviceScaleFactor.Value);
            }

            if (extraHTTPHeaders != null)
            {
                args["extraHTTPHeaders"] = extraHTTPHeaders.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray();
            }

            if (geolocation != null)
            {
                args.Add("geolocation", geolocation);
            }

            if (hasTouch.HasValue)
            {
                args.Add("hasTouch", hasTouch.Value);
            }

            if (httpCredentials != null)
            {
                args.Add("httpCredentials", httpCredentials);
            }

            if (ignoreHTTPSErrors.HasValue)
            {
                args.Add("ignoreHTTPSErrors", ignoreHTTPSErrors.Value);
            }

            if (isMobile.HasValue)
            {
                args.Add("isMobile", isMobile.Value);
            }

            if (javaScriptEnabled.HasValue)
            {
                args.Add("javaScriptEnabled", javaScriptEnabled.Value);
            }

            if (!string.IsNullOrEmpty(locale))
            {
                args.Add("locale", locale);
            }

            if (offline.HasValue)
            {
                args.Add("offline", offline.Value);
            }

            if (permissions != null)
            {
                args.Add("permissions", permissions);
            }

            if (proxy != null)
            {
                args.Add("proxy", proxy);
            }

            if (!string.IsNullOrEmpty(recordHarPath))
            {
                args.Add("recordHar", new
                {
                    Path = recordHarPath,
                    OmitContent = recordHarOmitContent,
                });
            }

            if (!string.IsNullOrEmpty(recordVideoDir)
                && recordVideoDir != null)
            {
                args.Add("recordVideo", new Dictionary<string, object>()
                {
                    { "dir", recordVideoDir },
                    { "size", recordVideoSize },
                });
            }

            if (!string.IsNullOrEmpty(storageState))
            {
                args.Add("storageState", storageState);
            }

            if (!string.IsNullOrEmpty(storageStatePath))
            {
                args.Add("storageStatePath", storageStatePath);
            }

            if (!string.IsNullOrEmpty(timezoneId))
            {
                args.Add("timezoneId", timezoneId);
            }

            if (!string.IsNullOrEmpty(userAgent))
            {
                args.Add("userAgent", userAgent);
            }

            if (ViewportSize.NoViewport.Equals(viewportSize))
            {
                args.Add("noDefaultViewport", true);
            }
            else if (viewportSize != null && !ViewportSize.Default.Equals(viewportSize))
            {
                args.Add("viewport", viewportSize);
            }

            args["sdkLanguage"] = "csharp";

            return Connection.SendMessageToServerAsync<BrowserContextChannel>(
                Guid,
                "newContext",
                args,
                true);
        }

        internal Task CloseAsync() => Connection.SendMessageToServerAsync<BrowserContextChannel>(Guid, "close", null);

        internal Task<CDPSessionChannel> NewBrowserCDPSessionAsync()
            => Connection.SendMessageToServerAsync<CDPSessionChannel>(Guid, "crNewBrowserCDPSession", null);

        internal Task StartTracingAsync(IPage page, bool screenshots, string path, IEnumerable<string> categories)
        {
            var args = new Dictionary<string, object>
            {
                ["screenshots"] = screenshots,
            };

            if (path != null)
            {
                args["path"] = path;
            }

            if (page != null)
            {
                args["page"] = page;
            }

            if (categories != null)
            {
                args["categories"] = categories;
            }

            return Connection.SendMessageToServerAsync(Guid, "crStartTracing", args);
        }

        internal async Task<string> StopTracingAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "crStopTracing", null).ConfigureAwait(false))?.GetProperty("binary").ToString();
    }
}
