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

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>The proxies of a worker: the rest of <c>__servers</c> upstream, next to <see cref="WorkerServers"/>.</summary>
public sealed class WorkerProxies
{
    public int SocksPort { get; init; }

    public TestProxy ProxyServer { get; init; }
}

/// <summary>The <c>socksPort</c> and <c>proxyServer</c> fixtures of <c>tests/config/serverFixtures.ts</c>.</summary>
[FixtureProvider(Order = 1)]
public static class ProxyFixtures
{
    [Fixture(Scope = FixtureScope.Worker)]
    public static async Task WorkerProxies(WorkerServers workerServers, Func<WorkerProxies, Task> use)
    {
        var port = workerServers.Server.Port;

        var socksServer = new MockSocksServer();
        var socksPort = port + 2;
        socksServer.Listen(socksPort);

        var proxyPort = port + 3;
        var proxyServer = TestProxy.Create(proxyPort);

        await use(new WorkerProxies { SocksPort = socksPort, ProxyServer = proxyServer }).ConfigureAwait(false);

        await Task.WhenAll(socksServer.CloseAsync(), proxyServer.StopAsync()).ConfigureAwait(false);
    }

    [Fixture]
    public static int SocksPort(WorkerProxies workerProxies) => workerProxies.SocksPort;

    [Fixture]
    public static TestProxy ProxyServer(WorkerProxies workerProxies)
    {
        workerProxies.ProxyServer.Reset();
        return workerProxies.ProxyServer;
    }
}
