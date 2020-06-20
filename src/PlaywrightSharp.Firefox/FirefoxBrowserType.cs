using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.BrowserLocation;
using PlaywrightSharp.Firefox.Messaging;
using PlaywrightSharp.Firefox.Protocol.Browser;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Server;

namespace PlaywrightSharp.Firefox
{
    /// <inheritdoc cref="IBrowserType"/>
    public class FirefoxBrowserType : BrowserTypeBase
    {
        /// <summary>
        /// Preferred revision.
        /// </summary>
        public const int PreferredRevision = 1021;

        private const string DummyUmaServer = "dummy.test";
        private static readonly string[] DefaultArgs = { "-no-remote" };
        private static readonly IDictionary<string, object> DefaultPreferences = new Dictionary<string, object>
        {
            // Make sure Shield doesn't hit the network.
            ["app.normandy.api_url"] = string.Empty,

            // Disable Firefox old build background check
            ["app.update.checkInstallTime"] = false,

            // Disable automatically upgrading Firefox
            ["app.update.disabledForTesting"] = true,

            // Increase the APZ content response timeout to 1 minute
            ["apz.content_response_timeout"] = 60000,

            // Prevent various error message on the console
            ["browser.contentblocking.features.standard"] = "-tp,tpPrivate,cookieBehavior0,-cm,-fp",

            // Enable the dump function: which sends messages to the system
            // console
            // https://bugzilla.mozilla.org/show_bug.cgi?id=1543115
            ["browser.dom.window.dump.enabled"] = true,

            // Disable topstories
            ["browser.newtabpage.activity-stream.feeds.section.topstories"] = false,

            // Always display a blank page
            ["browser.newtabpage.enabled"] = false,

            // Background thumbnails in particular cause grief: and disabling
            // thumbnails in general cannot hurt
            ["browser.pagethumbnails.capturing_disabled"] = true,

            // Disable safebrowsing components.
            ["browser.safebrowsing.blockedURIs.enabled"] = false,
            ["browser.safebrowsing.downloads.enabled"] = false,
            ["browser.safebrowsing.malware.enabled"] = false,
            ["browser.safebrowsing.passwords.enabled"] = false,
            ["browser.safebrowsing.phishing.enabled"] = false,

            // Disable updates to search engines.
            ["browser.search.update"] = false,

            // Do not restore the last open set of tabs if the browser has crashed
            ["browser.sessionstore.resume_from_crash"] = false,

            // Skip check for default browser on startup
            ["browser.shell.checkDefaultBrowser"] = false,

            // Disable newtabpage
            ["browser.startup.homepage"] = "about:blank",

            // Do not redirect user when a milstone upgrade of Firefox is detected
            ["browser.startup.homepage_override.mstone"] = "ignore",

            // Start with a blank page about:blank
            ["browser.startup.page"] = 0,

            // Do not allow background tabs to be zombified on Android: otherwise for
            // tests that open additional tabs: the test harness tab itself might get
            // unloaded
            ["browser.tabs.disableBackgroundZombification"] = false,

            // Do not warn when closing all other open tabs
            ["browser.tabs.warnOnCloseOtherTabs"] = false,

            // Do not warn when multiple tabs will be opened
            ["browser.tabs.warnOnOpen"] = false,

            // Disable the UI tour.
            ["browser.uitour.enabled"] = false,

            // Turn off search suggestions in the location bar so as not to trigger
            // network connections.
            ["browser.urlbar.suggest.searches"] = false,

            // Disable first run splash page on Windows 10
            ["browser.usedOnWindows10.introURL"] = string.Empty,

            // Do not warn on quitting Firefox
            ["browser.warnOnQuit"] = false,

            // Do not show datareporting policy notifications which can
            // interfere with tests
            ["datareporting.healthreport.about.reportUrl"] = $"http://${DummyUmaServer}/dummy/abouthealthreport/",
            ["datareporting.healthreport.documentServerURI"] = $"http://${DummyUmaServer}/dummy/healthreport/",
            ["datareporting.healthreport.logging.consoleEnabled"] = false,
            ["datareporting.healthreport.service.enabled"] = false,
            ["datareporting.healthreport.service.firstRun"] = false,
            ["datareporting.healthreport.uploadEnabled"] = false,
            ["datareporting.policy.dataSubmissionEnabled"] = false,
            ["datareporting.policy.dataSubmissionPolicyAccepted"] = false,
            ["datareporting.policy.dataSubmissionPolicyBypassNotification"] = true,

            // DevTools JSONViewer sometimes fails to load dependencies with its require.js.
            ["devtools.jsonview.enabled"] = false,

            // Disable popup-blocker
            ["dom.disable_open_during_load"] = false,

            // Enable the support for File object creation in the content process
            // Required for |Page.setFileInputFiles| protocol method.
            ["dom.file.createInChild"] = true,

            // Disable the ProcessHangMonitor
            ["dom.ipc.reportProcessHangs"] = false,

            // Disable slow script dialogues
            ["dom.max_chrome_script_run_time"] = 0,
            ["dom.max_script_run_time"] = 0,

            // Only load extensions from the application and user profile
            // AddonManager.SCOPE_PROFILE + AddonManager.SCOPE_APPLICATION
            ["extensions.autoDisableScopes"] = 0,
            ["extensions.enabledScopes"] = 5,

            // Disable metadata caching for installed add-ons by default
            ["extensions.getAddons.cache.enabled"] = false,

            // Disable installing any distribution extensions or add-ons.
            ["extensions.installDistroAddons"] = false,

            // Disabled screenshots extension
            ["extensions.screenshots.disabled"] = true,

            // Turn off extension updates so they do not bother tests
            ["extensions.update.enabled"] = false,

            // Turn off extension updates so they do not bother tests
            ["extensions.update.notifyUser"] = false,

            // Make sure opening about:addons will not hit the network
            ["extensions.webservice.discoverURL"] = $"http://${DummyUmaServer}/dummy/discoveryURL",

            // Allow the application to have focus even it runs in the background
            ["focusmanager.testmode"] = true,

            // Disable useragent updates
            ["general.useragent.updates.enabled"] = false,

            // Always use network provider for geolocation tests so we bypass the
            // macOS dialog raised by the corelocation provider
            ["geo.provider.testing"] = true,

            // Do not scan Wifi
            ["geo.wifi.scan"] = false,

            // No ICC color correction. See
            // https://developer.mozilla.org/en/docs/Mozilla/Firefox/Releases/3.5/ICC_color_correction_in_Firefox.
            ["gfx.color_management.mode"] = 0,
            ["gfx.color_management.rendering_intent"] = 3,

            // No hang monitor
            ["hangmonitor.timeout"] = 0,

            // Show chrome errors and warnings in the error console
            ["javascript.options.showInConsole"] = true,

            // Disable download and usage of OpenH264: and Widevine plugins
            ["media.gmp-manager.updateEnabled"] = false,

            // Prevent various error message on the console
            ["network.cookie.cookieBehavior"] = 0,

            // Do not prompt for temporary redirects
            ["network.http.prompt-temp-redirect"] = false,

            // Disable speculative connections so they are not reported as leaking
            // when they are hanging around
            ["network.http.speculative-parallel-limit"] = 0,

            // Do not automatically switch between offline and online
            ["network.manage-offline-status"] = false,

            // Make sure SNTP requests do not hit the network
            ["network.sntp.pools"] = DummyUmaServer,

            // Disable Flash.
            ["plugin.state.flash"] = 0,

            ["privacy.trackingprotection.enabled"] = false,

            // Enable Remote Agent
            // https://bugzilla.mozilla.org/show_bug.cgi?id=1544393
            ["remote.enabled"] = true,

            // Don't do network connections for mitm priming
            ["security.certerrors.mitm.priming.enabled"] = false,

            // Local documents have access to all other local documents,
            // including directory listings
            ["security.fileuri.strict_origin_policy"] = false,

            // Do not wait for the notification button security delay
            ["security.notification_enable_delay"] = 0,

            // Ensure blocklist updates do not hit the network
            ["services.settings.server"] = $"http://${DummyUmaServer}/dummy/blocklist/",

            ["browser.tabs.documentchannel"] = false,

            // Do not automatically fill sign-in forms with known usernames and
            // passwords
            ["signon.autofillForms"] = false,

            // Disable password capture, so that tests that include forms are not
            // influenced by the presence of the persistent doorhanger notification
            ["signon.rememberSignons"] = false,

            // Disable first-run welcome page
            ["startup.homepage_welcome_url"] = "about:blank",

            // Disable first-run welcome page
            ["startup.homepage_welcome_url.additional"] = string.Empty,

            // Disable browser animations (tabs, fullscreen, sliding alerts)
            ["toolkit.cosmeticAnimations.enabled"] = false,

            // We want to collect telemetry, but we don't want to send in the results
            ["toolkit.telemetry.server"] = $"https://${DummyUmaServer}/dummy/telemetry/",

            // Prevent starting into safe mode after application crashes
            ["toolkit.startup.max_resumed_crashes"] = -1,
        };

        /// <inheritdoc cref="IBrowserType.ConnectAsync(ConnectOptions)"/>
        public override Task<IBrowser> ConnectAsync(ConnectOptions options = null)
        {
            if (options?.BrowserURL != null)
            {
                throw new PlaywrightSharpException("Option \"BrowserURL\" is not supported by Firefox");
            }

            return FirefoxBrowser.ConnectAsync(options);
        }

        /// <inheritdoc cref="IBrowserType.CreateBrowserFetcher(BrowserFetcherOptions)"/>
        public override IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options = null)
        {
            var downloadUrls = new Dictionary<Platform, string>
            {
                [Platform.Linux] = "{0}/builds/firefox/{1}/firefox-linux.zip",
                [Platform.MacOS] = "{0}/builds/firefox/{1}/firefox-mac.zip",
                [Platform.Win32] = "{0}/builds/firefox/{1}/firefox-win32.zip",
                [Platform.Win64] = "{0}/builds/firefox/{1}/firefox-win64.zip",
            };

            string path = options?.Path ?? Path.Combine(Directory.GetCurrentDirectory(), ".local-firefox");
            string host = options?.Host ?? "https://playwright.azureedge.net";
            var platform = options?.Platform ?? GetPlatform();

            Func<Platform, string, BrowserFetcherConfig> paramsGetter = (platform, revision) =>
            {
                string executablePath = string.Empty;
                switch (platform)
                {
                    case Platform.Linux:
                        executablePath = Path.Combine("firefox", "firefox");
                        break;
                    case Platform.MacOS:
                        executablePath = Path.Combine("firefox", "Nightly.app", "Contents", "MacOS", "firefox");
                        break;
                    case Platform.Win32:
                    case Platform.Win64:
                        executablePath = Path.Combine("firefox", "firefox.exe");
                        break;
                }

                return new BrowserFetcherConfig
                {
                    DownloadURL = string.Format(CultureInfo.InvariantCulture, downloadUrls[platform], host, revision),
                    ExecutablePath = executablePath,
                };
            };

            return new BrowserFetcher(path, platform, PreferredRevision.ToString(CultureInfo.InvariantCulture.NumberFormat), paramsGetter);
        }

        /// <inheritdoc cref="IBrowserType.GetDefaultArgs(BrowserArgOptions)"/>
        public override string[] GetDefaultArgs(BrowserArgOptions options = null)
        {
            bool devtools = options?.Devtools ?? false;
            bool headless = options?.Headless ?? !devtools;
            string userDataDir = options?.UserDataDir;
            string[] args = options?.Args ?? Array.Empty<string>();

            if (devtools)
            {
                throw new PlaywrightSharpException("Option \"devtools\" is not supported by Firefox");
            }

            var firefoxArguments = new List<string>(DefaultArgs);
            if (userDataDir != null)
            {
                firefoxArguments.Add("-profile");
                firefoxArguments.Add(userDataDir);
            }

            if (headless)
            {
                firefoxArguments.Add("-headless");
            }
            else
            {
                firefoxArguments.Add("-wait-for-browser");
            }

            firefoxArguments.AddRange(args);

            if (firefoxArguments.TrueForAll(arg => arg.StartsWith("-")))
            {
                firefoxArguments.Add("about:blank");
            }

            return firefoxArguments.ToArray();
        }

        /// <inheritdoc cref="IBrowserType.LaunchAsync(LaunchOptions)"/>
        public override async Task<IBrowser> LaunchAsync(LaunchOptions options = null)
        {
            var app = await LaunchBrowserAppAsync(options).ConfigureAwait(false);
            var connectOptions = app.ConnectOptions;
            return await FirefoxBrowser.ConnectAsync(app, connectOptions).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IBrowserType.LaunchBrowserAppAsync(LaunchOptions)"/>
        public override async Task<IBrowserApp> LaunchBrowserAppAsync(LaunchOptions options = null)
        {
            options ??= new LaunchOptions();

            var (firefoxArguments, tempProfileDir) = PrepareFirefoxArgs(options);
            string firefoxExecutable = GetBrowserExecutablePath(options);
            BrowserApp browserApp = null;

            var process = new FirefoxProcessManager(
                firefoxExecutable,
                firefoxArguments,
                tempProfileDir,
                options.Timeout,
                async () =>
                {
                    if (browserApp == null)
                    {
                        return;
                    }

                    var transport = await BrowserHelper.CreateTransportAsync(browserApp.ConnectOptions).ConfigureAwait(false);
                    await transport.SendAsync(new ConnectionRequest
                    {
                        Id = FirefoxConnection.BrowserCloseMessageId,
                        Method = new BrowserCloseRequest().Command,
                    }.ToJson()).ConfigureAwait(false);
                },
                exitCode => browserApp?.ProcessKilled(exitCode));

            try
            {
                SetEnvVariables(process.Process.StartInfo.Environment, options.Env, Environment.GetEnvironmentVariables());

                if (options.DumpIO)
                {
                    process.Process.ErrorDataReceived += (sender, e) => Console.Error.WriteLine(e.Data);
                }

                await process.StartAsync().ConfigureAwait(false);
                var connectOptions = new ConnectOptions()
                {
                    BrowserWSEndpoint = process.Endpoint,
                    SlowMo = options.SlowMo,
                };

                browserApp = new BrowserApp(process, () => process.GracefullyClose(), connectOptions);
                return browserApp;
            }
            catch
            {
                await process.KillAsync().ConfigureAwait(false);
                throw;
            }
        }

        private (List<string> firefoxArguments, TempDirectory temporaryProfileDir) PrepareFirefoxArgs(LaunchOptions options)
        {
            var firefoxArguments = new List<string>();
            if (options?.IgnoreDefaultArgs != true)
            {
                firefoxArguments.AddRange(GetDefaultArgs(options));
            }
            else if (options?.IgnoredDefaultArgs?.Length > 0)
            {
                firefoxArguments.AddRange(GetDefaultArgs(options).Except(options.IgnoredDefaultArgs));
            }
            else if (options?.Args?.Length > 0)
            {
                firefoxArguments.AddRange(options.Args);
            }

            if (!firefoxArguments.Contains("-juggler"))
            {
                firefoxArguments.InsertRange(0, new[] { "-juggler", "0" });
            }

            TempDirectory temporaryProfileDir = null;
            if (!firefoxArguments.Contains("-profile") && !firefoxArguments.Contains("--profile"))
            {
                temporaryProfileDir = CreateProfile();
                firefoxArguments.InsertRange(0, new[] { "-profile", temporaryProfileDir.Path.Quote() });
            }

            return (firefoxArguments, temporaryProfileDir);
        }

        private TempDirectory CreateProfile(IDictionary<string, object> extraPrefs = null)
        {
            var tempDir = new TempDirectory();
            var prefsJS = new List<string>();
            var userJS = new List<string>();

            var prefs = new Dictionary<string, object>(DefaultPreferences);
            if (extraPrefs != null)
            {
                foreach (var pair in extraPrefs)
                {
                    prefs[pair.Key] = pair.Value;
                }
            }

            foreach (var pair in prefs)
            {
                userJS.Add($"user_pref(\"{pair.Key}\", {JsonSerializer.Serialize(pair.Value, pair.Value.GetType())});");
            }

            File.WriteAllText(Path.Combine(tempDir.Path, "user.js"), string.Join("\n", userJS));
            File.WriteAllText(Path.Combine(tempDir.Path, "prefs.js"), string.Join("\n", prefsJS));

            return tempDir;
        }
    }
}
