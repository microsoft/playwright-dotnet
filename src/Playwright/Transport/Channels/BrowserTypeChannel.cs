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
            var args = new Dictionary<string, object>();
            args.Add("channel", channel);
            args.Add("executablePath", executablePath);
            args.Add("args", passedArguments);
            args.Add("ignoreAllDefaultArgs", ignoreAllDefaultArgs);
            args.Add("ignoreDefaultArgs", ignoreDefaultArgs);
            args.Add("handleSIGHUP", handleSIGHUP);
            args.Add("handleSIGINT", handleSIGINT);
            args.Add("handleSIGTERM", handleSIGTERM);
            args.Add("headless", headless);
            args.Add("devtools", devtools);
            args.Add("env", env.Remap());
            args.Add("proxy", proxy);
            args.Add("downloadsPath", downloadsPath);
            args.Add("firefoxUserPrefs", firefoxUserPrefs);
            args.Add("chromiumSandbox", chromiumSandbox);
            args.Add("slowMo", slowMo);
            args.Add("timeout", timeout);

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
            var channelArgs = new Dictionary<string, object>();

            channelArgs.Add("userDataDir", userDataDir);
            channelArgs.Add("headless", headless);
            channelArgs.Add("channel", channel);
            channelArgs.Add("executablePath", executablePath);
            channelArgs.Add("args", args);
            channelArgs.Add("downloadsPath", downloadsPath);
            channelArgs.Add("proxy", proxy);
            channelArgs.Add("chromiumSandbox", chromiumSandbox);
            channelArgs.Add("handleSIGINT", handleSIGINT);
            channelArgs.Add("handleSIGTERM", handleSIGTERM);
            channelArgs.Add("handleSIGHUP", handleSIGHUP);
            channelArgs.Add("timeout", timeout);
            channelArgs.Add("env", env.Remap());
            channelArgs.Add("devtools", devtools);
            channelArgs.Add("slowMo", slowMo);
            channelArgs.Add("acceptDownloads", acceptDownloads);
            channelArgs.Add("ignoreHTTPSErrors", ignoreHTTPSErrors);
            channelArgs.Add("bypassCSP", bypassCSP);

            if (viewportSize?.Width == -1)
            {
                channelArgs.Add("noDefaultViewport", true);
            }
            else
            {
                channelArgs.Add("viewport", viewportSize);
            }

            channelArgs.Add("screensize", screenSize);
            channelArgs.Add("userAgent", userAgent);
            channelArgs.Add("deviceScaleFactor", deviceScaleFactor);
            channelArgs.Add("isMobile", isMobile);
            channelArgs.Add("hasTouch", hasTouch);
            channelArgs.Add("javaScriptEnabled", javaScriptEnabled);
            channelArgs.Add("timezoneId", timezoneId);
            channelArgs.Add("geolocation", geolocation);
            channelArgs.Add("locale", locale);
            channelArgs.Add("permissions", permissions);
            channelArgs.Add("extraHTTPHeaders", extraHTTPHeaders.Remap());
            channelArgs.Add("offline", offline);
            channelArgs.Add("httpCredentials", httpCredentials);
            channelArgs.Add("colorScheme", colorScheme);
            if (!string.IsNullOrEmpty(recordHarPath))
            {
                channelArgs.Add("recordHar", new
                {
                    Path = recordHarPath,
                    OmitContent = recordHarOmitContent.GetValueOrDefault(false),
                });
            }

            if (!string.IsNullOrEmpty(recordVideoDir))
            {
                channelArgs.Add("recordVideo", new Dictionary<string, object>()
                {
                    { "dir", recordVideoDir },
                    { "size", recordVideoSize },
                });
            }

            channelArgs.Add("ignoreDefaultArgs", ignoreDefaultArgs);
            channelArgs.Add("ignoreAllDefaultArgs", ignoreAllDefaultArgs);
            channelArgs.Add("sdkLanguage", "csharp");

            return Connection.SendMessageToServerAsync<BrowserContextChannel>(Guid, "launchPersistentContext", channelArgs);
        }
    }
}
