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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class BrowserTypeChannel : Channel<Core.BrowserType>
    {
        public BrowserTypeChannel(string guid, Connection connection, Core.BrowserType owner) : base(guid, connection, owner)
        {
        }

        public Task<BrowserChannel> LaunchAsync(
            bool? headless = default,
            string channel = default,
            string executablePath = default,
            IEnumerable<string> passedArguments = default,
            Proxy proxy = default,
            string downloadsPath = default,
            string tracesDir = default,
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
            args.Add("tracesDir", tracesDir);
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
            string tracesDir = default,
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
            ReducedMotion? reducedMotion = default,
            string recordHarPath = default,
            bool? recordHarOmitContent = default,
            Dictionary<string, object> recordVideo = default,
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
            channelArgs.Add("tracesDir", tracesDir);
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
            channelArgs.Add("reducedMotion", reducedMotion);

            if (!string.IsNullOrEmpty(recordHarPath))
            {
                channelArgs.Add("recordHar", new
                {
                    Path = recordHarPath,
                    OmitContent = recordHarOmitContent.GetValueOrDefault(false),
                });
            }

            if (recordVideo != null)
            {
                channelArgs.Add("recordVideo", recordVideo);
            }

            channelArgs.Add("ignoreDefaultArgs", ignoreDefaultArgs);
            channelArgs.Add("ignoreAllDefaultArgs", ignoreAllDefaultArgs);
            channelArgs.Add("sdkLanguage", "csharp");

            return Connection.SendMessageToServerAsync<BrowserContextChannel>(Guid, "launchPersistentContext", channelArgs);
        }
    }
}
