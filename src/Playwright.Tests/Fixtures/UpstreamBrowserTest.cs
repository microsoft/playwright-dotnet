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
using System.Text.Json;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// <c>browserTest</c> of <c>tests/config/browserTest.ts</c>: the browser of the worker, and the options
/// that Playwright Test adds to the launches and the contexts of a test.
/// </summary>
public class UpstreamBrowserTest : UpstreamPlaywrightTest
{
    private readonly List<string> _userDataDirs = [];
    private IBrowserContext _persistentContext;
    private PlaywrightServer _remoteServer;
    private Task<IBrowserContext> _context;
    private Task<IPage> _page;

    public IBrowser Browser { get; private set; }

    public string BrowserVersion => Browser.Version;

    public int BrowserMajorVersion => int.Parse(BrowserVersion.Split('.')[0], CultureInfo.InvariantCulture);

    /// <summary>
    /// The options of every launch, <c>playwright._defaultLaunchOptions</c> upstream: the headless mode, the
    /// channel and the executable of the run. Upstream adds them to every <c>browserType.launch(options)</c>;
    /// a port passes <c>new(LaunchOptions()) { ... }</c>. Override it for <c>test.use({ launchOptions })</c>.
    /// </summary>
    public virtual BrowserTypeLaunchOptions LaunchOptions() => new()
    {
        Headless = Headless,
        Channel = Channel,
        ExecutablePath = Env.ExecutablePath,
        TracesDir = Path.Combine(Env.OutputDir, Utils.ArtifactsFolderName(WorkerIndex), "traces"),
    };

    /// <summary>
    /// The options of the contexts of the test, <c>_combinedContextOptions</c> upstream: what
    /// <c>test.use({ hasTouch: true })</c> and the defaults of Playwright Test set. Upstream adds them to
    /// every context of the test; a port passes <c>new(ContextOptions()) { ... }</c>. Override it for
    /// <c>test.use()</c>.
    /// </summary>
    public virtual BrowserNewContextOptions ContextOptions() => new()
    {
        Locale = "en-US",
    };

    /// <summary>The same for <c>browser.newPage(options)</c>.</summary>
    public BrowserNewPageOptions NewPageOptions() => Utils.MergeOptions(new BrowserNewPageOptions(), ContextOptions());

    /// <summary>The same for <c>browserType.launchPersistentContext(dir, options)</c>.</summary>
    public BrowserTypeLaunchPersistentContextOptions LaunchPersistentContextOptions() =>
        Utils.MergeOptions(Utils.MergeOptions(new BrowserTypeLaunchPersistentContextOptions(), LaunchOptions()), ContextOptions());

    /// <summary>
    /// The <c>context</c> fixture: created when a test asks for it, <c>var context = await ContextFixture();</c>,
    /// and closed when the test ends.
    /// </summary>
    public Task<IBrowserContext> ContextFixture() => _context ??= ContextFactory();

    /// <summary>The <c>page</c> fixture, a page of <see cref="ContextFixture"/>: <c>var page = await PageFixture();</c>.</summary>
    public Task<IPage> PageFixture()
    {
        return _page ??= NewPage();

        async Task<IPage> NewPage() => await (await ContextFixture()).NewPageAsync();
    }

    /// <summary><c>contextFactory(options)</c>.</summary>
    public Task<IBrowserContext> ContextFactory(BrowserNewContextOptions options = null) =>
        Browser.NewContextAsync(Utils.MergeOptions(options == null ? new BrowserNewContextOptions() : new BrowserNewContextOptions(options), ContextOptions()));

    /// <summary><c>createUserDataDir()</c>: a new directory that is removed when the test ends.</summary>
    public Task<string> CreateUserDataDir()
    {
        // We do not put user data dir in testOutputPath,
        // because we do not want to upload them as test result artifacts.
        var dir = Directory.CreateTempSubdirectory("playwright-test-").FullName;
        _userDataDirs.Add(dir);
        return Task.FromResult(dir);
    }

    /// <summary><c>const { context, page } = await launchPersistent(options)</c>.</summary>
    public async Task<(IBrowserContext Context, IPage Page)> LaunchPersistent(BrowserTypeLaunchPersistentContextOptions options = null)
    {
        if (_persistentContext != null)
        {
            throw new InvalidOperationException("can only launch one persistent context");
        }
        var userDataDir = await CreateUserDataDir();
        _persistentContext = await BrowserType.LaunchPersistentContextAsync(userDataDir, Utils.MergeOptions(options == null ? new BrowserTypeLaunchPersistentContextOptions() : new BrowserTypeLaunchPersistentContextOptions(options), LaunchPersistentContextOptions()));
        _persistentContext.SetDefaultTimeout(0);
        _persistentContext.SetDefaultNavigationTimeout(0);
        return (_persistentContext, _persistentContext.Pages[0]);
    }

    /// <summary>
    /// <c>await startRemoteServer(kind, options)</c>: 'launchServer' gives a <see cref="RemoteServer"/>,
    /// 'run-server' a <see cref="RunServer"/>. One server per test, closed when the test ends.
    /// </summary>
    public async Task<PlaywrightServer> StartRemoteServer(string kind, RemoteServerOptions options = null)
    {
        if (_remoteServer != null)
        {
            throw new InvalidOperationException("can only start one remote server");
        }
        if (kind == "launchServer")
        {
            var remoteServer = new RemoteServer();
            _remoteServer = remoteServer;
            await remoteServer.Start(ChildProcess, BrowserType, Channel, LaunchOptions(), options);
        }
        else if (kind == "run-server")
        {
            var runServer = new RunServer();
            _remoteServer = runServer;
            await runServer.Start(ChildProcess, new() { ArtifactsDir = options?.ArtifactsDir, Unsafe = options?.Unsafe ?? false, Env = options?.Env });
        }
        else
        {
            throw new ArgumentException($"Unknown kind of a remote server: {kind}", nameof(kind));
        }
        return _remoteServer;
    }

    /// <summary><c>await pageWithHar(options)</c>: a page of a context that records a HAR.</summary>
    public async Task<PageWithHarResult> PageWithHar(PageWithHarOptions options = null)
    {
        var harPath = TestInfo.OutputPath(options?.OutputPath ?? "test.har");
        var context = await ContextFactory(new() { RecordHarPath = harPath, RecordHarContent = options?.Content, RecordHarOmitContent = options?.OmitContent, IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        return new PageWithHarResult(context, page, harPath);
    }

    [SetUp]
    public async Task BrowserSetup()
    {
        Browser = await BrowserService.Launch(this, BrowserType, LaunchOptions());
    }

    [TearDown]
    public async Task BrowserTearDown()
    {
        // After a failure the worker closes the browser and the driver.
        if (TestOk())
        {
            // Also the contexts that the test left open.
            await Task.WhenAll(Browser.Contexts.Select(context => context.CloseAsync(new() { Reason = "Test ended." })));
            if (_persistentContext != null)
            {
                await _persistentContext.CloseAsync();
            }
        }
        _persistentContext = null;
        _context = null;
        _page = null;
        if (_remoteServer != null)
        {
            await _remoteServer.Close();
            _remoteServer = null;
            // Give any connected browsers a chance to disconnect to avoid
            // poisoning next test with quasy-alive browsers.
            await Task.Delay(1000);
        }
        await Utils.RemoveFoldersAsync(_userDataDirs);
        _userDataDirs.Clear();
    }

    private sealed class BrowserService : IWorkerService
    {
        private string _launchOptions;

        private IBrowser _browser;

        /// <summary>One browser per worker. Other launch options start another browser, as another worker upstream.</summary>
        public static async Task<IBrowser> Launch(WorkerAwareTest test, IBrowserType browserType, BrowserTypeLaunchOptions options)
        {
            var service = await test.RegisterService("Upstream.Browser", () => Task.FromResult(new BrowserService()));
            var launchOptions = JsonSerializer.Serialize(options);
            if (service._launchOptions != launchOptions || !service._browser.IsConnected)
            {
                await service.DisposeAsync();
                service._browser = await browserType.LaunchAsync(options);
                service._launchOptions = launchOptions;
                // actionTimeout and navigationTimeout are 0 in Playwright Test, for every context of a test:
                // the timeout of the test is the limit.
                service._browser.Context += (_, context) =>
                {
                    context.SetDefaultTimeout(0);
                    context.SetDefaultNavigationTimeout(0);
                };
            }
            return service._browser;
        }

        public Task ResetAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            // The driver of the worker is closed before this, and its browsers with it.
            if (_browser?.IsConnected == true)
            {
                await _browser.CloseAsync(new() { Reason = "Test ended." });
            }
            _browser = null;
            _launchOptions = null;
        }
    }
}
