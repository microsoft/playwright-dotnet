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
            bool? acceptDownloads,
            bool? bypassCSP,
            ColorScheme? colorScheme,
            float? deviceScaleFactor,
            IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders,
            Geolocation geolocation,
            bool? hasTouch,
            HttpCredentials httpCredentials,
            bool? ignoreHTTPSErrors,
            bool? isMobile,
            bool? javaScriptEnabled,
            string locale,
            bool? offline,
            IEnumerable<string> permissions,
            Proxy proxy,
            bool? recordHarOmitContent,
            string recordHarPath,
            Dictionary<string, object> recordVideo,
            string storageState,
            string storageStatePath,
            string timezoneId,
            string userAgent,
            ViewportSize viewportSize)
        {
            var args = new Dictionary<string, object>();
            args.Add("acceptDownloads", acceptDownloads);
            args.Add("bypassCSP", bypassCSP);
            args.Add("colorScheme", colorScheme);
            args.Add("deviceScaleFactor", deviceScaleFactor);

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

            if (!string.IsNullOrEmpty(recordHarPath))
            {
                args.Add("recordHar", new
                {
                    Path = recordHarPath,
                    OmitContent = recordHarOmitContent,
                });
            }

            if (recordVideo != null)
            {
                args.Add("recordVideo", recordVideo);
            }

            args.Add("storageState", storageState);
            args.Add("storageStatePath", storageStatePath);
            args.Add("timezoneId", timezoneId);
            args.Add("userAgent", userAgent);

            if (viewportSize?.Width == -1)
            {
                args.Add("noDefaultViewport", true);
            }
            else
            {
                args.Add("viewport", viewportSize);
            }

            args["sdkLanguage"] = "csharp";

            return Connection.SendMessageToServerAsync<BrowserContextChannel>(
                Guid,
                "newContext",
                args);
        }

        internal Task CloseAsync() => Connection.SendMessageToServerAsync<BrowserContextChannel>(Guid, "close", null);

        internal Task StartTracingAsync(IPage page, bool screenshots, string path, IEnumerable<string> categories)
        {
            var args = new Dictionary<string, object>();
            args["screenshots"] = screenshots;
            args["path"] = path;
            args["page"] = page;
            args["categories"] = categories;

            return Connection.SendMessageToServerAsync(Guid, "crStartTracing", args);
        }

        internal async Task<string> StopTracingAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "crStopTracing", null).ConfigureAwait(false))?.GetProperty("binary").ToString();
    }
}
