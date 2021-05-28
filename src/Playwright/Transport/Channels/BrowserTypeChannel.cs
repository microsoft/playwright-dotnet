using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class BrowserTypeChannel : Channel<BrowserType>
    {
        public BrowserTypeChannel(string guid, Connection connection, BrowserType owner) : base(guid, connection, owner)
        {
        }

        public Task<BrowserChannel> LaunchAsync(
            bool? headless = default,
            string channel = default,
            string executablePath = default,
            IEnumerable<string> passedArguments = default,
            Proxy proxy = default,
            string downloadsPath = default,
            bool? chromiumSandbox = default,
            IEnumerable<KeyValuePair<string, object>> firefoxUserPrefs = default,
            bool? handleSIGINT = default,
            bool? handleSIGTERM = default,
            bool? handleSIGHUP = default,
            float? timeout = default,
            IEnumerable<KeyValuePair<string, string>> env = default,
            bool? devtools = default,
            float? slowMo = default,
            IEnumerable<string> ignoreDefaultArgs = default,
            bool? ignoreAllDefaultArgs = default)
        {
            var args = new Dictionary<string, object>
            {
                { "channel", channel },
                { "executablePath", executablePath },
                { "args", passedArguments },
                { "ignoreAllDefaultArgs", ignoreAllDefaultArgs },
                { "ignoreDefaultArgs", ignoreDefaultArgs },
                { "handleSIGHUP", handleSIGHUP },
                { "handleSIGINT", handleSIGINT },
                { "handleSIGTERM", handleSIGTERM },
                { "headless", headless },
                { "devtools", devtools },
                { "env", env.Remap() },
                { "proxy", proxy },
                { "downloadsPath", downloadsPath },
                { "firefoxUserPrefs", firefoxUserPrefs },
                { "chromiumSandbox", chromiumSandbox },
                { "slowMo", slowMo },
                { "timeout", timeout },
            };

            return Connection.SendMessageToServerAsync<BrowserChannel>(
                Guid,
                "launch",
                args);
        }

        internal Task<BrowserContextChannel> LaunchPersistentContextAsync(
            string userDataDir,
            bool? headless = default,
            string channel = default,
            string executablePath = default,
            IEnumerable<string> args = default,
            Proxy proxy = default,
            string downloadsPath = default,
            bool? chromiumSandbox = default,
            bool? handleSIGINT = default,
            bool? handleSIGTERM = default,
            bool? handleSIGHUP = default,
            float? timeout = default,
            IEnumerable<KeyValuePair<string, string>> env = default,
            bool? devtools = default,
            float? slowMo = default,
            bool? acceptDownloads = default,
            bool? ignoreHTTPSErrors = default,
            bool? bypassCSP = default,
            ViewportSize viewportSize = default,
            ScreenSize screenSize = default,
            string userAgent = default,
            float? deviceScaleFactor = default,
            bool? isMobile = default,
            bool? hasTouch = default,
            bool? javaScriptEnabled = default,
            string timezoneId = default,
            Geolocation geolocation = default,
            string locale = default,
            IEnumerable<string> permissions = default,
            IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders = default,
            bool? offline = default,
            HttpCredentials httpCredentials = default,
            ColorScheme? colorScheme = default,
            string recordHarPath = default,
            bool? recordHarOmitContent = default,
            string recordVideoDir = default,
            RecordVideoSize recordVideoSize = default,
            IEnumerable<string> ignoreDefaultArgs = default,
            bool? ignoreAllDefaultArgs = default)
        {
            if (recordVideoSize != null && string.IsNullOrEmpty(recordVideoDir))
            {
                throw new PlaywrightException("\"RecordVideoSize\" option requires \"RecordVideoDir\" to be specified");
            }

            var channelArgs = new Dictionary<string, object>
            {
                { "userDataDir", userDataDir },
                { "headless", headless },
                { "channel", channel },
                { "executablePath", executablePath },
                { "args", args },
                { "downloadsPath", downloadsPath },
                { "proxy", proxy },
                { "chromiumSandbox", chromiumSandbox },
                { "handleSIGINT", handleSIGINT },
                { "handleSIGTERM", handleSIGTERM },
                { "handleSIGHUP", handleSIGHUP },
                { "timeout", timeout },
                { "env", env.Remap() },
                { "devtools", devtools },
                { "slowMo", slowMo },
                { "acceptDownloads", acceptDownloads },
                { "ignoreHTTPSErrors", ignoreHTTPSErrors },
                { "bypassCSP", bypassCSP },
                { "screensize", screenSize },
                { "userAgent", userAgent },
                { "deviceScaleFactor", deviceScaleFactor },
                { "isMobile", isMobile },
                { "hasTouch", hasTouch },
                { "javaScriptEnabled", javaScriptEnabled },
                { "timezoneId", timezoneId },
                { "geolocation", geolocation },
                { "locale", locale },
                { "permissions", permissions },
                { "extraHTTPHeaders", extraHTTPHeaders.Remap() },
                { "offline", offline },
                { "httpCredentials", httpCredentials },
                { "colorScheme", colorScheme },
                { "ignoreDefaultArgs", ignoreDefaultArgs },
                { "ignoreAllDefaultArgs", ignoreAllDefaultArgs },
                { "sdkLanguage", "csharp" },
            };

            if (viewportSize?.Width == -1)
            {
                channelArgs.Add("noDefaultViewport", true);
            }
            else
            {
                channelArgs.Add("viewport", viewportSize);
            }

            if (!string.IsNullOrEmpty(recordHarPath))
            {
                channelArgs.Add("recordHar", new
                {
                    Path = recordHarPath,
                    OmitContent = recordHarOmitContent ?? false,
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

                channelArgs.Add("recordVideo", recordVideoArgs);
            }

            return Connection.SendMessageToServerAsync<BrowserContextChannel>(Guid, "launchPersistentContext", channelArgs);
        }
    }
}
