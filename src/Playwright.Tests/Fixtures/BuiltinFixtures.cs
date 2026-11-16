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

using System.Globalization;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary><c>contextFactory(options)</c>: a context that the harness closes when the test ends.</summary>
public delegate Task<IBrowserContext> ContextFactory(BrowserNewContextOptions options = null);

/// <summary><c>createUserDataDir()</c>: a new directory that the harness removes when the test ends.</summary>
public delegate Task<string> CreateUserDataDir();

/// <summary><c>const { context, page } = await launchPersistent(options)</c>.</summary>
public delegate Task<(IBrowserContext Context, IPage Page)> LaunchPersistent(BrowserTypeLaunchPersistentContextOptions options = null);

/// <summary>The servers of a worker, <c>__servers</c> upstream.</summary>
public sealed class WorkerServers
{
    public Server Server { get; init; }

    public Server HttpsServer { get; init; }
}

/// <summary>
/// The fixtures of every test: the port of <c>tests/config/browserTest.ts</c>, <c>baseTest.ts</c>,
/// <c>platformFixtures.ts</c>, <c>serverFixtures.ts</c> and <c>testModeFixtures.ts</c>, and of the
/// fixtures of Playwright Test that they build on (<c>packages/playwright/src/index.ts</c>).
/// The name of a fixture is the name of the method with a lower-case first letter.
/// </summary>
[FixtureProvider(Order = 0)]
public static class BuiltinFixtures
{
    private static readonly SemaphoreSlim _createPlaywrightLock = new(1, 1);
    private static int _portBlock;

    // Worker fixtures: the project.

    [Fixture(Scope = FixtureScope.Worker)]
    public static string BrowserName() => Env.BrowserName;

    /// <summary>
    /// The options of every launch, <c>playwright._defaultLaunchOptions</c> upstream: the headless mode, the
    /// channel and the executable of the run. Upstream adds them to every <c>browserType.launch(options)</c>;
    /// a port passes <c>new(launchOptions) { ... }</c>.
    /// </summary>
    [Fixture(Scope = FixtureScope.Worker)]
    public static BrowserTypeLaunchOptions LaunchOptions(WorkerInfo workerInfo) => new()
    {
        Headless = !Env.Headed,
        Channel = Env.Channel,
        ExecutablePath = Env.ExecutablePath,
        TracesDir = Path.Combine(Env.OutputDir, Utils.ArtifactsFolderName(workerInfo.WorkerIndex), "traces"),
    };

    [Fixture(Scope = FixtureScope.Worker)]
    public static bool Headless(BrowserTypeLaunchOptions launchOptions) => launchOptions.Headless ?? true;

    [Fixture(Scope = FixtureScope.Worker)]
    public static string Channel(BrowserTypeLaunchOptions launchOptions) => launchOptions.Channel;

    [Fixture(Scope = FixtureScope.Worker)]
    public static string Mode() => "default";

    [Fixture(Scope = FixtureScope.Worker)]
    public static string Video() => "off";

    [Fixture(Scope = FixtureScope.Worker)]
    public static string Trace() => "off";

    // Worker fixtures: the platform.

    [Fixture(Scope = FixtureScope.Worker)]
    public static string Platform() => Env.Platform;

    [Fixture(Scope = FixtureScope.Worker)]
    public static bool IsWindows() => Env.IsWindows;

    [Fixture(Scope = FixtureScope.Worker)]
    public static bool IsMac() => Env.IsMac;

    [Fixture(Scope = FixtureScope.Worker)]
    public static bool IsLinux() => Env.IsLinux;

    [Fixture(Scope = FixtureScope.Worker)]
    public static int MacVersion() => Env.MacVersion;

    /// <summary>
    /// The version of the Node.js that runs the tests upstream. A .NET test does not run in Node.js, so
    /// no test is skipped for an old version.
    /// </summary>
    [Fixture(Scope = FixtureScope.Worker)]
    public static (int Major, int Minor, int Patch) NodeVersion() => (int.MaxValue, 0, 0);

    /// <summary>The <c>loopback</c> option of <c>serverFixtures.ts</c>: not set.</summary>
    [Fixture(Scope = FixtureScope.Worker)]
    public static string Loopback() => null;

    // Worker fixtures: the browser.

    /// <summary>
    /// The directory where the driver of the worker keeps the descriptors of the servers of
    /// <c>browser.bind()</c>, <c>PWTEST_SERVER_REGISTRY</c>. Upstream sets the variable in a hook, for a
    /// driver in the same process. Here the driver is another process that reads it when it starts.
    /// </summary>
    [Fixture(Scope = FixtureScope.Worker)]
    public static string ServerRegistry(WorkerInfo workerInfo) => Path.Combine(Env.OutputDir, $"registry-{Environment.ProcessId}-{workerInfo.WorkerIndex}");

    [Fixture(Scope = FixtureScope.Worker)]
    public static async Task Playwright(string serverRegistry, Func<IPlaywright, Task> use)
    {
        IPlaywright playwright;
        await _createPlaywrightLock.WaitAsync().ConfigureAwait(false);
        var underTest = Environment.GetEnvironmentVariable("PWTEST_UNDER_TEST");
        var registry = Environment.GetEnvironmentVariable("PWTEST_SERVER_REGISTRY");
        try
        {
            Environment.SetEnvironmentVariable("PWTEST_SERVER_REGISTRY", serverRegistry);
            // The driver of the upstream tests runs with PWTEST_UNDER_TEST, which gives the pages
            // window.builtins, among other things. The driver of the old tests does not.
            Environment.SetEnvironmentVariable("PWTEST_UNDER_TEST", "1");
            playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PWTEST_UNDER_TEST", underTest);
            Environment.SetEnvironmentVariable("PWTEST_SERVER_REGISTRY", registry);
            _createPlaywrightLock.Release();
        }
        await use(playwright).ConfigureAwait(false);
        playwright.Dispose();
    }

    [Fixture(Scope = FixtureScope.Worker)]
    public static IBrowserType BrowserType(IPlaywright playwright, string browserName) => playwright[browserName];

    [Fixture(Scope = FixtureScope.Worker)]
    public static async Task Browser(IBrowserType browserType, BrowserTypeLaunchOptions launchOptions, Func<IBrowser, Task> use)
    {
        var browser = await browserType.LaunchAsync(launchOptions).ConfigureAwait(false);
        // actionTimeout and navigationTimeout are 0 in Playwright Test, for every context of a test:
        // the timeout of the test is the limit.
        browser.Context += (_, context) =>
        {
            context.SetDefaultTimeout(0);
            context.SetDefaultNavigationTimeout(0);
        };
        await use(browser).ConfigureAwait(false);
        await browser.CloseAsync(new() { Reason = "Test ended." }).ConfigureAwait(false);
    }

    [Fixture(Scope = FixtureScope.Worker)]
    public static string BrowserVersion(IBrowser browser) => browser.Version;

    [Fixture(Scope = FixtureScope.Worker)]
    public static int BrowserMajorVersion(string browserVersion) => int.Parse(browserVersion.Split('.')[0], CultureInfo.InvariantCulture);

    [Fixture(Scope = FixtureScope.Worker)]
    public static bool AllowsThirdParty(string browserName) => browserName == "firefox";

    [Fixture(Scope = FixtureScope.Worker)]
    public static string DefaultSameSiteCookieValue(string browserName, string platform, string channel, bool isBidi)
    {
        if (browserName == "chromium" || isBidi)
        {
            return "Lax";
        }
        if (browserName == "webkit" && (platform == "linux" || channel == "webkit-wsl"))
        {
            return "Lax";
        }
        if (browserName == "webkit")
        {
            return "None"; // Windows + older macOS
        }
        if (browserName == "firefox")
        {
            return "None";
        }
        throw new InvalidOperationException("unknown browser - " + browserName);
    }

    [Fixture(Scope = FixtureScope.Worker)]
    public static bool IsBidi(string channel) => Utils.IsBidiChannel(channel);

    [Fixture(Scope = FixtureScope.Worker)]
    public static bool IsAndroid() => false;

    [Fixture(Scope = FixtureScope.Worker)]
    public static bool IsElectron() => false;

    [Fixture(Scope = FixtureScope.Worker)]
    public static int ElectronMajorVersion() => 0;

    [Fixture(Scope = FixtureScope.Worker)]
    public static bool IsHeadlessShell(string browserName, string channel, bool headless)
    {
        var isShell = channel == "chromium-headless-shell" || (channel == null && headless);
        return browserName == "chromium" && isShell;
    }

    [Fixture(Scope = FixtureScope.Worker)]
    public static bool IsFrozenWebkit(string browserName, bool isMac, int macVersion) =>
        browserName == "webkit" && (Utils.HostPlatform.StartsWith("debian11", StringComparison.Ordinal) || Utils.HostPlatform.StartsWith("ubuntu20.04", StringComparison.Ordinal) || (isMac && macVersion < 15));

    // Worker fixtures: the servers.

    [Fixture(Scope = FixtureScope.Worker)]
    public static async Task WorkerServers(WorkerInfo workerInfo, Func<WorkerServers, Task> use)
    {
        var cachedPath = Path.Combine(Env.AssetsDir, "cached");

        Server server = null;
        Server httpsServer = null;
        while (server == null)
        {
            // Upstream uses 8907 + workerIndex * 4; the old tests of this repository use those ports.
            // Another test run on this machine takes the next block of ports.
            var port = 10907 + (_portBlock * 200) + (workerInfo.ParallelIndex * 4);
            try
            {
                // The SOCKS server and the proxy of the worker take the next two ports.
                foreach (var probe in new[] { port + 2, port + 3 })
                {
                    var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, probe);
                    listener.Start();
                    listener.Stop();
                }
                server = Fixtures.Server.Create(Env.AssetsDir, port);
                httpsServer = Fixtures.Server.CreateHttps(Env.AssetsDir, port + 1);
            }
            catch (System.Net.Sockets.SocketException e) when (e.SocketErrorCode == System.Net.Sockets.SocketError.AddressAlreadyInUse && _portBlock < 100)
            {
                if (server != null)
                {
                    await server.StopAsync().ConfigureAwait(false);
                }
                server = null;
                Interlocked.Increment(ref _portBlock);
            }
        }
        server.EnableHTTPCache(cachedPath);
        httpsServer.EnableHTTPCache(cachedPath);

        await use(new WorkerServers { Server = server, HttpsServer = httpsServer }).ConfigureAwait(false);

        await Task.WhenAll(server.StopAsync(), httpsServer.StopAsync()).ConfigureAwait(false);
    }

    // Test fixtures.

    [Fixture]
    public static Server Server(WorkerServers workerServers)
    {
        workerServers.Server.Reset();
        return workerServers.Server;
    }

    [Fixture]
    public static Server HttpsServer(WorkerServers workerServers)
    {
        workerServers.HttpsServer.Reset();
        return workerServers.HttpsServer;
    }

    /// <summary><c>asset('file')</c>: the path of a file of the upstream <c>tests/assets</c>.</summary>
    [Fixture]
    public static Func<string, string> Asset() => Utils.Asset;

    /// <summary><c>context configuration</c> upstream: runs for every test.</summary>
    [Fixture(Auto = true)]
    public static void SetupContextOptions(IPlaywright playwright) => playwright.Selectors.SetTestIdAttribute("data-testid");

    /// <summary>
    /// The options of the contexts of the test, <c>_combinedContextOptions</c> upstream: what
    /// <c>test.use({ hasTouch: true })</c> and the defaults of Playwright Test set. Upstream adds them to
    /// every context of the test. Here the <c>context</c>, <c>contextFactory</c> and <c>launchPersistent</c>
    /// fixtures add them; a test that calls <c>browser.newContext()</c> itself passes
    /// <c>new(contextOptions) { ... }</c>.
    /// </summary>
    [Fixture]
    public static BrowserNewContextOptions ContextOptions() => new()
    {
        Locale = "en-US",
    };

    /// <summary>The same for <c>browser.newPage(options)</c>: <c>new(newPageOptions) { ... }</c>.</summary>
    [Fixture]
    public static BrowserNewPageOptions NewPageOptions(BrowserNewContextOptions contextOptions) => Utils.MergeOptions(new BrowserNewPageOptions(), contextOptions);

    /// <summary>The same for <c>browserType.launchPersistentContext(dir, options)</c>: <c>new(launchPersistentContextOptions) { ... }</c>.</summary>
    [Fixture]
    public static BrowserTypeLaunchPersistentContextOptions LaunchPersistentContextOptions(BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions) =>
        Utils.MergeOptions(Utils.MergeOptions(new BrowserTypeLaunchPersistentContextOptions(), launchOptions), contextOptions);

    [Fixture]
    public static async Task ContextFactory(IBrowser browser, BrowserNewContextOptions contextOptions, TestInfo testInfo, Func<ContextFactory, Task> use)
    {
        var contexts = new List<IBrowserContext>();
        await use(async options =>
        {
            var context = await browser.NewContextAsync(Utils.MergeOptions(options == null ? new BrowserNewContextOptions() : new BrowserNewContextOptions(options), contextOptions)).ConfigureAwait(false);
            contexts.Add(context);
            return context;
        }).ConfigureAwait(false);
        var closeReason = testInfo.Status == "timedOut" ? $"Test timeout of {testInfo.Timeout}ms exceeded." : "Test ended.";
        await Task.WhenAll(contexts.Select(context => context.CloseAsync(new() { Reason = closeReason }))).ConfigureAwait(false);
    }

    [Fixture]
    public static Task<IBrowserContext> Context(ContextFactory contextFactory) => contextFactory();

    [Fixture]
    public static Task<IPage> Page(IBrowserContext context) => context.NewPageAsync();

    [Fixture]
    public static async Task Request(IPlaywright playwright, Func<IAPIRequestContext, Task> use)
    {
        var request = await playwright.APIRequest.NewContextAsync().ConfigureAwait(false);
        await use(request).ConfigureAwait(false);
        await request.DisposeAsync().ConfigureAwait(false);
    }

    [Fixture]
    public static async Task CreateUserDataDir(Func<CreateUserDataDir, Task> use)
    {
        var dirs = new List<string>();
        // We do not put user data dir in testOutputPath,
        // because we do not want to upload them as test result artifacts.
        await use(() =>
        {
            var dir = Directory.CreateTempSubdirectory("playwright-test-").FullName;
            dirs.Add(dir);
            return Task.FromResult(dir);
        }).ConfigureAwait(false);
        await Utils.RemoveFoldersAsync(dirs).ConfigureAwait(false);
    }

    [Fixture]
    public static async Task LaunchPersistent(CreateUserDataDir createUserDataDir, IBrowserType browserType, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions, Func<LaunchPersistent, Task> use)
    {
        IBrowserContext persistentContext = null;
        await use(async options =>
        {
            if (persistentContext != null)
            {
                throw new InvalidOperationException("can only launch one persistent context");
            }
            var userDataDir = await createUserDataDir().ConfigureAwait(false);
            persistentContext = await browserType.LaunchPersistentContextAsync(userDataDir, Utils.MergeOptions(options == null ? new BrowserTypeLaunchPersistentContextOptions() : new BrowserTypeLaunchPersistentContextOptions(options), launchPersistentContextOptions)).ConfigureAwait(false);
            persistentContext.SetDefaultTimeout(0);
            persistentContext.SetDefaultNavigationTimeout(0);
            var page = persistentContext.Pages[0];
            return (persistentContext, page);
        }).ConfigureAwait(false);
        if (persistentContext != null)
        {
            await persistentContext.CloseAsync().ConfigureAwait(false);
        }
    }
}
