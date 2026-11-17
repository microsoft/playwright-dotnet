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

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// The base class of the ported tests: the fixtures of <c>tests/config/baseTest.ts</c> (the driver, the
/// test servers, the platform) as properties, named after the upstream fixtures. The driver and the servers
/// are services of the worker, see <see cref="WorkerAwareTest"/>: a failure or a timeout closes them.
/// A test has its own instance of the class, also on a retry, so a test that goes on after its timeout
/// has nothing of the next test, and its driver is gone.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class UpstreamPlaywrightTest : WorkerAwareTest
{
    private const float DefaultExpectTimeout = 5_000;
    private static readonly object _logLock = new();
    private static readonly ConcurrentDictionary<string, int> _runs = new();
    private readonly List<TestChildProcess> _childProcesses = [];
    private CancellationTokenSource _waitForPort;
    private Task<IAPIRequestContext> _request;

    static UpstreamPlaywrightTest()
    {
        // Playwright Test removes the output directory at the start of a run.
        try
        {
            Directory.Delete(Env.OutputDir, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public IPlaywright Playwright { get; private set; }

    public IBrowserType BrowserType { get; private set; }

    public static string BrowserName => Env.BrowserName;

    public Server Server { get; private set; }

    public Server HttpsServer { get; private set; }

    public TestProxy ProxyServer { get; private set; }

    public int SocksPort { get; private set; }

    /// <summary><c>testInfo</c> and <c>test.info()</c>, and the calls on <c>test</c>: <c>Info.Skip(condition)</c>.</summary>
    public TestInfo Info { get; private set; }

    /// <summary><c>PWTEST_SERVER_REGISTRY</c>: where the driver keeps the descriptors of the servers of <c>browser.bind()</c>.</summary>
    public static string ServerRegistry => Env.ServerRegistry;

    public static string Channel => Env.Channel;

    public static bool Headless => !Env.Headed;

    public static string Mode => "default";

    public static string Video => "off";

    public static string Trace => "off";

    public static string Platform => Env.Platform;

    public static bool IsWindows => Env.IsWindows;

    public static bool IsMac => Env.IsMac;

    public static bool IsLinux => Env.IsLinux;

    public static int MacVersion => Env.MacVersion;

    /// <summary>
    /// The version of the Node.js that runs the tests upstream. A .NET test does not run in Node.js, so
    /// no test is skipped for an old version.
    /// </summary>
    public static (int Major, int Minor, int Patch) NodeVersion => (int.MaxValue, 0, 0);

    /// <summary>The <c>loopback</c> option of <c>serverFixtures.ts</c>: not set.</summary>
    public static string Loopback => null;

    public static bool IsAndroid => false;

    public static bool IsElectron => false;

    public static int ElectronMajorVersion => 0;

    public static bool IsBidi => Utils.IsBidiChannel(Channel);

    public static bool IsHeadlessShell => BrowserName == "chromium" && (Channel == "chromium-headless-shell" || (Channel == null && Headless));

    public static bool IsFrozenWebkit =>
        BrowserName == "webkit" && (Utils.HostPlatform.StartsWith("debian11", StringComparison.Ordinal) || Utils.HostPlatform.StartsWith("ubuntu20.04", StringComparison.Ordinal) || (IsMac && MacVersion < 15));

    public static bool AllowsThirdParty => BrowserName == "firefox";

    public static string DefaultSameSiteCookieValue
    {
        get
        {
            if (BrowserName == "chromium" || IsBidi)
            {
                return "Lax";
            }
            if (BrowserName == "webkit" && (Platform == "linux" || Channel == "webkit-wsl"))
            {
                return "Lax";
            }
            return "None";
        }
    }

    /// <summary>The <c>request</c> fixture: created when a test asks for it, disposed when the test ends.</summary>
    public Task<IAPIRequestContext> RequestFixture() => _request ??= Playwright.APIRequest.NewContextAsync();

    [SetUp]
    public async Task UpstreamSetup()
    {
        var test = TestContext.CurrentContext.Test;
        var retry = _runs.AddOrUpdate(test.ID, 0, (_, runs) => runs + 1);
        Log($"start {test.FullName}");

        Playwright = (await PlaywrightService.Register(this)).Playwright;
        BrowserType = Playwright[BrowserName];
        // testIdAttribute of Playwright Test.
        Playwright.Selectors.SetTestIdAttribute("data-testid");

        var servers = await ServerService.Register(this);
        Server = servers.Server;
        HttpsServer = servers.HttpsServer;
        ProxyServer = servers.ProxyServer;
        SocksPort = servers.SocksPort;

        var method = test.Method.MethodInfo;
        var attribute = method.GetCustomAttribute<UpstreamTestAttribute>(false);
        Info = new TestInfo(attribute.Title, attribute.Id, UpstreamRegistry.Find(attribute.Id)?.File, test.Name, retry, WorkerIndex, servers.ParallelIndex);

        // expect: { timeout: 10000 } upstream.
        Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(Env.ExpectTimeout);
    }

    /// <summary><c>childProcess({ command: [...] })</c>: a process that is killed when the test ends.</summary>
    public TestChildProcess ChildProcess(TestChildParams @params)
    {
        var process = new TestChildProcess(@params);
        _childProcesses.Add(process);
        return process;
    }

    /// <summary><c>await waitForPort(port)</c>.</summary>
    public async Task WaitForPort(int port)
    {
        _waitForPort ??= new();
        var token = _waitForPort.Token;
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", port);
                return;
            }
            catch (SocketException)
            {
            }
            await Task.Delay(100);
        }
    }

    [TearDown]
    public async Task UpstreamTearDown()
    {
        if (_request != null)
        {
            await (await _request).DisposeAsync();
            _request = null;
        }
        _waitForPort?.Cancel();
        _waitForPort = null;
        await Task.WhenAll(_childProcesses.Select(child => child.Kill()));
        if (!TestOk() && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PWTEST_DEBUG")))
        {
            foreach (var process in _childProcesses)
            {
                Console.WriteLine("====== " + string.Join(" ", process.Params.Command));
                Console.WriteLine(process.FullOutput.Replace("\u001Bc", string.Empty));
                Console.WriteLine("=========================================");
            }
        }
        _childProcesses.Clear();
        Microsoft.Playwright.Assertions.SetDefaultExpectTimeout(DefaultExpectTimeout);
        Log($"end   {TestContext.CurrentContext.Test.FullName} {Info?.Status}");
    }

    /// <summary>
    /// <c>PWTEST_LOG=file</c> appends a line when a test starts and when it ends. The test runner prints
    /// the results of a long run late, so this is the way to see which test a run hangs in.
    /// </summary>
    private static void Log(string line)
    {
        if (Env.LogFile == null)
        {
            return;
        }
        lock (_logLock)
        {
            File.AppendAllText(Env.LogFile, $"{DateTime.Now:HH:mm:ss.fff} {line}\n");
        }
    }

    private sealed class PlaywrightService : IWorkerService
    {
        public IPlaywright Playwright { get; private init; }

        public static Task<PlaywrightService> Register(WorkerAwareTest test) =>
            test.RegisterService("Upstream.Playwright", async () => new PlaywrightService { Playwright = await Microsoft.Playwright.Playwright.CreateAsync() });

        public Task ResetAsync() => Task.CompletedTask;

        public Task DisposeAsync()
        {
            Playwright.Dispose();
            return Task.CompletedTask;
        }
    }

    private sealed class ServerService : IWorkerService
    {
        private static readonly HashSet<int> _usedParallelIndexes = [];
        private static int _portBlock;

        public Server Server { get; private init; }

        public Server HttpsServer { get; private init; }

        public MockSocksServer SocksServer { get; private init; }

        public int SocksPort { get; private init; }

        public TestProxy ProxyServer { get; private init; }

        /// <summary>The index of this worker among the ones that are alive. It decides the ports.</summary>
        public int ParallelIndex { get; private init; }

        public static Task<ServerService> Register(WorkerAwareTest test) => test.RegisterService("Upstream.Servers", async () =>
        {
            var parallelIndex = 0;
            lock (_usedParallelIndexes)
            {
                while (!_usedParallelIndexes.Add(parallelIndex))
                {
                    parallelIndex++;
                }
            }

            Server server = null;
            Server httpsServer = null;
            var port = 0;
            while (server == null)
            {
                // Upstream uses 8907 + workerIndex * 4; the old tests of this repository use those ports.
                // Another test run on this machine takes the next block of ports.
                port = 10907 + (_portBlock * 200) + (parallelIndex * 4);
                try
                {
                    // The SOCKS server and the proxy of the worker take the next two ports.
                    foreach (var probe in new[] { port + 2, port + 3 })
                    {
                        var listener = new TcpListener(IPAddress.Loopback, probe);
                        listener.Start();
                        listener.Stop();
                    }
                    server = Server.Create(Env.AssetsDir, port);
                    httpsServer = Server.CreateHttps(Env.AssetsDir, port + 1);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse && _portBlock < 100)
                {
                    if (server != null)
                    {
                        await server.StopAsync();
                    }
                    server = null;
                    _portBlock++;
                }
            }
            var cachedPath = Path.Combine(Env.AssetsDir, "cached");
            server.EnableHTTPCache(cachedPath);
            httpsServer.EnableHTTPCache(cachedPath);
            var socksServer = new MockSocksServer();
            socksServer.Listen(port + 2);
            return new ServerService
            {
                Server = server,
                HttpsServer = httpsServer,
                SocksServer = socksServer,
                SocksPort = port + 2,
                ProxyServer = TestProxy.Create(port + 3),
                ParallelIndex = parallelIndex,
            };
        });

        public Task ResetAsync()
        {
            Server.Reset();
            HttpsServer.Reset();
            ProxyServer.Reset();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await Task.WhenAll(Server.StopAsync(), HttpsServer.StopAsync(), SocksServer.CloseAsync(), ProxyServer.StopAsync());
            lock (_usedParallelIndexes)
            {
                _usedParallelIndexes.Remove(ParallelIndex);
            }
        }
    }
}
