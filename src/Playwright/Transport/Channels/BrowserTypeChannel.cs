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
            global::Microsoft.Playwright.BrowserChannel channel = default,
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

            if (channel != global::Microsoft.Playwright.BrowserChannel.Undefined)
            {
                args.Add("channel", channel);
            }

            if (!string.IsNullOrEmpty(executablePath))
            {
                args.Add("executablePath", executablePath);
            }

            if (passedArguments != null)
            {
                args.Add("args", passedArguments);
            }

            if (ignoreAllDefaultArgs.HasValue)
            {
                args.Add("ignoreAllDefaultArgs", ignoreAllDefaultArgs.Value);
            }

            if (ignoreDefaultArgs != null)
            {
                args.Add("ignoreDefaultArgs", ignoreDefaultArgs);
            }

            if (handleSIGHUP.HasValue)
            {
                args.Add("handleSIGHUP", handleSIGHUP.Value);
            }

            if (handleSIGINT.HasValue)
            {
                args.Add("handleSIGINT", handleSIGINT.Value);
            }

            if (handleSIGTERM.HasValue)
            {
                args.Add("handleSIGTERM", handleSIGTERM.Value);
            }

            if (headless.HasValue)
            {
                args.Add("headless", headless.Value);
            }

            if (devtools.HasValue)
            {
                args.Add("devtools", devtools.Value);
            }

            if (env != null)
            {
                args.Add("env", env.Remap());
            }

            if (proxy != null)
            {
                args.Add("proxy", proxy);
            }

            if (!string.IsNullOrEmpty(downloadsPath))
            {
                args.Add("downloadsPath", downloadsPath);
            }

            if (firefoxUserPrefs != null)
            {
                args.Add("firefoxUserPrefs", firefoxUserPrefs);
            }

            if (chromiumSandbox.HasValue)
            {
                args.Add("chromiumSandbox", chromiumSandbox.Value);
            }

            if (slowMo.HasValue)
            {
                args.Add("slowMo", slowMo.Value);
            }

            if (timeout.HasValue)
            {
                args.Add("timeout", timeout.Value);
            }

            return Connection.SendMessageToServerAsync<BrowserChannel>(
                Guid,
                "launch",
                args);
        }

        internal Task<BrowserContextChannel> LaunchPersistentContextAsync(
            string userDataDir,
            bool? headless = default,
            Microsoft.Playwright.BrowserChannel channel = default,
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
            ColorScheme colorScheme = default,
            string recordHarPath = default,
            bool? recordHarOmitContent = default,
            string recordVideoDir = default,
            RecordVideoSize recordVideoSize = default,
            IEnumerable<string> ignoreDefaultArgs = default,
            bool? ignoreAllDefaultArgs = default)
        {
            var channelArgs = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(userDataDir))
            {
                channelArgs.Add("userDataDir", userDataDir);
            }

            if (headless.HasValue)
            {
                channelArgs.Add("headless", headless);
            }

            if (channel != Microsoft.Playwright.BrowserChannel.Undefined)
            {
                channelArgs.Add("channel", channel);
            }

            if (!string.IsNullOrEmpty(executablePath))
            {
                channelArgs.Add("executablePath", executablePath);
            }

            if (args?.Any() == true)
            {
                channelArgs.Add("args", args);
            }

            if (!string.IsNullOrEmpty(downloadsPath))
            {
                channelArgs.Add("downloadsPath", downloadsPath);
            }

            if (proxy != null)
            {
                channelArgs.Add("proxy", proxy);
            }

            if (chromiumSandbox.HasValue)
            {
                channelArgs.Add("chromiumSandbox", chromiumSandbox);
            }

            if (handleSIGINT.HasValue)
            {
                channelArgs.Add("handleSIGINT", handleSIGINT);
            }

            if (handleSIGTERM.HasValue)
            {
                channelArgs.Add("handleSIGTERM", handleSIGTERM);
            }

            if (handleSIGHUP.HasValue)
            {
                channelArgs.Add("handleSIGHUP", handleSIGHUP);
            }

            if (timeout.HasValue)
            {
                channelArgs.Add("timeout", timeout);
            }

            if (env?.Any() == true)
            {
                channelArgs.Add("env", env.Remap());
            }

            if (devtools.HasValue)
            {
                channelArgs.Add("devtools", devtools);
            }

            if (slowMo.HasValue)
            {
                channelArgs.Add("slowMo", slowMo);
            }

            if (acceptDownloads.HasValue)
            {
                channelArgs.Add("acceptDownloads", acceptDownloads);
            }

            if (ignoreHTTPSErrors.HasValue)
            {
                channelArgs.Add("ignoreHTTPSErrors", ignoreHTTPSErrors);
            }

            if (bypassCSP.HasValue)
            {
                channelArgs.Add("bypassCSP", bypassCSP);
            }

            if (ViewportSize.NoViewport.Equals(viewportSize))
            {
                channelArgs.Add("noDefaultViewport", true);
            }
            else if (viewportSize != null && !ViewportSize.Default.Equals(viewportSize))
            {
                channelArgs.Add("viewport", viewportSize);
            }

            if (screenSize != default)
            {
                channelArgs.Add("screensize", screenSize);
            }

            if (!string.IsNullOrEmpty(userAgent))
            {
                channelArgs.Add("userAgent", userAgent);
            }

            if (deviceScaleFactor.HasValue)
            {
                channelArgs.Add("deviceScaleFactor", deviceScaleFactor);
            }

            if (isMobile.HasValue)
            {
                channelArgs.Add("isMobile", isMobile);
            }

            if (hasTouch.HasValue)
            {
                channelArgs.Add("hasTouch", hasTouch);
            }

            if (javaScriptEnabled.HasValue)
            {
                channelArgs.Add("javaScriptEnabled", javaScriptEnabled);
            }

            if (!string.IsNullOrEmpty(timezoneId))
            {
                channelArgs.Add("timezoneId", timezoneId);
            }

            if (geolocation != default)
            {
                channelArgs.Add("geolocation", geolocation);
            }

            if (!string.IsNullOrEmpty(locale))
            {
                channelArgs.Add("locale", locale);
            }

            if (permissions != null && permissions.Any())
            {
                channelArgs.Add("permissions", permissions);
            }

            if (extraHTTPHeaders != null && extraHTTPHeaders.Any())
            {
                channelArgs.Add("extraHTTPHeaders", extraHTTPHeaders.Remap());
            }

            if (offline.HasValue)
            {
                channelArgs.Add("offline", offline);
            }

            if (httpCredentials != default)
            {
                channelArgs.Add("httpCredentials", httpCredentials);
            }

            if (colorScheme != ColorScheme.Undefined)
            {
                channelArgs.Add("colorScheme", colorScheme);
            }

            if (!string.IsNullOrEmpty(recordHarPath))
            {
                channelArgs.Add("recordHar", new
                {
                    Path = recordHarPath,
                    OmitContent = recordHarOmitContent.GetValueOrDefault(false),
                });
            }

            if (!string.IsNullOrEmpty(recordVideoDir)
                 && recordVideoDir != null)
            {
                channelArgs.Add("recordVideo", new Dictionary<string, object>()
                {
                    { "dir", recordVideoDir },
                    { "size", recordVideoSize },
                });
            }

            if (ignoreDefaultArgs != null && ignoreDefaultArgs.Any())
            {
                channelArgs.Add("ignoreDefaultArgs", ignoreDefaultArgs);
            }

            if (ignoreAllDefaultArgs.HasValue)
            {
                channelArgs.Add("ignoreAllDefaultArgs", ignoreAllDefaultArgs);
            }

            channelArgs.Add("sdkLanguage", "csharp");

            return Connection.SendMessageToServerAsync<BrowserContextChannel>(Guid, "launchPersistentContext", channelArgs);
        }
    }
}
