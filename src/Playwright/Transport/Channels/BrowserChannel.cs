using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
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
            ReducedMotion? reducedMotion = null,
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
            Dictionary<string, object> recordVideo = null,
            string storageState = null,
            string storageStatePath = null,
            string timezoneId = null,
            string userAgent = null,
            ViewportSize viewportSize = default,
            ScreenSize screenSize = default)
        {
            var args = new Dictionary<string, object>();
            args.Add("acceptDownloads", acceptDownloads);
            args.Add("bypassCSP", bypassCSP);
            args.Add("colorScheme", colorScheme);
            args.Add("reducedMotion", reducedMotion);
            args.Add("deviceScaleFactor", deviceScaleFactor);
            args.Add("screensize", screenSize);

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
