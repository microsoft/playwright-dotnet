using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserTypeChannel : Channel<BrowserType>
    {
        public BrowserTypeChannel(string guid, Connection connection, BrowserType owner) : base(guid, connection, owner)
        {
        }

        public Task<BrowserChannel> LaunchAsync(
            IEnumerable<string> passedArguments = null,
            string channel = null,
            bool? chromiumSandbox = null,
            bool? devtools = null,
            string downloadsPath = null,
            IEnumerable<KeyValuePair<string, string>> env = null,
            string executablePath = null,
            IEnumerable<KeyValuePair<string, object>> firefoxUserPrefs = null,
            bool? handleSIGHUP = null,
            bool? handleSIGINT = null,
            bool? handleSIGTERM = null,
            bool? headless = null,
            bool? ignoreAllDefaultArgs = null,
            IEnumerable<string> ignoreDefaultArgs = null,
            Proxy proxy = null,
            float? slowMo = null,
            float? timeout = null)
        {
            var args = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(channel))
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
                args.Add("env", env);
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
                args,
                false);
        }

        internal Task<BrowserContextChannel> LaunchPersistentContextAsync(
            string userDataDir,
            bool? acceptDownloads = null,
            IEnumerable<string> args = null,
            bool? bypassCSP = null,
            string channel = null,
            bool? chromiumSandbox = null,
            ColorScheme colorScheme = ColorScheme.Undefined,
            float? deviceScaleFactor = null,
            bool? devtools = null,
            string downloadsPath = null,
            IEnumerable<KeyValuePair<string, string>> env = null,
            string executablePath = null,
            IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders = null,
            Geolocation geolocation = null,
            bool? handleSIGHUP = null,
            bool? handleSIGINT = null,
            bool? handleSIGTERM = null,
            bool? hasTouch = null,
            bool? headless = null,
            HttpCredentials httpCredentials = null,
            bool? ignoreAllDefaultArgs = null,
            IEnumerable<string> ignoreDefaultArgs = null,
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
            float? slowMo = null,
            float? timeout = null,
            string timezoneId = null,
            string userAgent = null)
        {
            /*
            var args = options.ToChannelDictionary();
            args["userDataDir"] = userDataDir;*/

            return Connection.SendMessageToServerAsync<BrowserContextChannel>(Guid, "launchPersistentContext", args, false);
        }
    }
}
