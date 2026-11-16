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

using System.Net.Sockets;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary><c>childProcess({ command: [...] })</c>: a process that the harness kills when the test ends.</summary>
public delegate TestChildProcess ChildProcess(TestChildParams @params);

/// <summary><c>await waitForPort(port)</c>.</summary>
public delegate Task WaitForPort(int port);

/// <summary>
/// <c>await startRemoteServer(kind, options)</c>: 'launchServer' gives a <see cref="RemoteServer"/>,
/// 'run-server' a <see cref="RunServer"/>. One server per test; the harness closes it when the test ends.
/// </summary>
public delegate Task<PlaywrightServer> StartRemoteServer(string kind, RemoteServerOptions options = null);

/// <summary>
/// The fixtures of <c>tests/config/commonFixtures.ts</c> that the tests of <c>tests/page</c> and
/// <c>tests/library</c> use, and <c>startRemoteServer</c> of <c>tests/config/browserTest.ts</c>.
/// </summary>
[FixtureProvider(Order = 2)]
public static class ProcessFixtures
{
    [Fixture]
    public static async Task ChildProcess(TestInfo testInfo, Func<ChildProcess, Task> use)
    {
        var processes = new List<TestChildProcess>();
        var ended = false;
        await use(@params =>
        {
            lock (processes)
            {
                // A test that timed out is still running, and nothing would kill the process.
                if (ended)
                {
                    throw new InvalidOperationException("childProcess() is called after the test ended");
                }
                var process = new TestChildProcess(@params);
                processes.Add(process);
                return process;
            }
        }).ConfigureAwait(false);
        lock (processes)
        {
            ended = true;
        }
        await Task.WhenAll(processes.Select(child => child.Kill())).ConfigureAwait(false);
        if (testInfo.Status != "passed" && testInfo.Status != "skipped" && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PWTEST_DEBUG")))
        {
            foreach (var process in processes)
            {
                Console.WriteLine("====== " + string.Join(" ", process.Params.Command));
                Console.WriteLine(process.FullOutput.Replace("\u001Bc", string.Empty));
                Console.WriteLine("=========================================");
            }
        }
    }

    [Fixture]
    public static async Task WaitForPort(Func<WaitForPort, Task> use)
    {
        var canceled = false;
        await use(async port =>
        {
            while (!Volatile.Read(ref canceled))
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync("127.0.0.1", port).ConfigureAwait(false);
                    return;
                }
                catch (SocketException)
                {
                }
                await Task.Delay(100).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
        Volatile.Write(ref canceled, true);
    }

    [Fixture]
    public static async Task StartRemoteServer(ChildProcess childProcess, IBrowserType browserType, string channel, string mode, BrowserTypeLaunchOptions launchOptions, Func<StartRemoteServer, Task> use)
    {
        It.Skip(mode != "default", "Starting remote server is not supported in remote modes");

        PlaywrightServer server = null;
        await use(async (kind, options) =>
        {
            if (server != null)
            {
                throw new InvalidOperationException("can only start one remote server");
            }
            if (kind == "launchServer")
            {
                var remoteServer = new RemoteServer();
                await remoteServer.Start(childProcess, browserType, channel, launchOptions, options).ConfigureAwait(false);
                server = remoteServer;
            }
            else if (kind == "run-server")
            {
                var runServer = new RunServer();
                await runServer.Start(childProcess, new() { ArtifactsDir = options?.ArtifactsDir, Unsafe = options?.Unsafe ?? false, Env = options?.Env }).ConfigureAwait(false);
                server = runServer;
            }
            else
            {
                throw new ArgumentException($"Unknown kind of a remote server: {kind}", nameof(kind));
            }
            return server;
        }).ConfigureAwait(false);
        if (server != null)
        {
            await server.Close().ConfigureAwait(false);
            // Give any connected browsers a chance to disconnect to avoid
            // poisoning next test with quasy-alive browsers.
            await Task.Delay(1000).ConfigureAwait(false);
        }
    }
}
