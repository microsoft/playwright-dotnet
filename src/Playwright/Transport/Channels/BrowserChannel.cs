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

            if (!string.IsNullOrEmpty(recordVideoDir))
            {
                args.Add("recordVideo", new Dictionary<string, object>()
                {
                    { "dir", recordVideoDir },
                    { "size", recordVideoSize },
                });
            }

            args.Add("storageState", storageState);
            args.Add("storageStatePath", storageStatePath);
            args.Add("timezoneId", timezoneId);
            args.Add("userAgent", userAgent);

            if (ViewportSize.NoViewport.Equals(viewportSize))
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
