/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/fetch-proxy.spec.ts.</summary>
public class FetchProxyTests
{
    [BeforeEach]
    public static void Skip(string mode) => Info.Skip(mode != "default");

    [UpstreamTest("PR-041 context request should pick up proxy credentials")]
    public async Task ContextRequestShouldPickUpProxyCredentials(IBrowserType browserType, Server server, TestProxy proxyServer, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        proxyServer.ForwardTo(server.Port, new() { AllowConnectRequests = true });
        var authPromise = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        proxyServer.SetAuthHandler(req =>
        {
            var header = req.Headers["proxy-authorization"];
            // Browser can issue various unrelated requests over the proxy,
            // but we are only interested in our own request.
            if (proxyServer.ConnectHosts.Contains("non-existent.com:80"))
            {
                authPromise.TrySetResult(header);
            }
            return !string.IsNullOrEmpty(header);
        });
        var browser = await browserType.LaunchAsync(new(launchOptions)
        {
            Proxy = new() { Server = $"localhost:{proxyServer.Port}", Username = "user", Password = "secret" },
        });
        var context = await browser.NewContextAsync(contextOptions);
        var response = await context.APIRequest.GetAsync("http://non-existent.com/simple.json");
        Assert.That(proxyServer.ConnectHosts, Does.Contain("non-existent.com:80"));
        var auth = await authPromise.Task;
        Assert.That(auth, Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:secret"))));
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
        await browser.CloseAsync();
    }

    [UpstreamTest("PR-042 global request should pick up proxy credentials")]
    public async Task GlobalRequestShouldPickUpProxyCredentials(IPlaywright playwright, Server server, TestProxy proxyServer)
    {
        proxyServer.ForwardTo(server.Port, new() { AllowConnectRequests = true });
        string auth = null;
        proxyServer.SetAuthHandler(req =>
        {
            auth = req.Headers["proxy-authorization"];
            return !string.IsNullOrEmpty(auth);
        });
        var request = await playwright.APIRequest.NewContextAsync(new()
        {
            Proxy = new() { Server = $"localhost:{proxyServer.Port}", Username = "user", Password = "secret" },
        });
        var response = await request.GetAsync("http://non-existent.com/simple.json");
        Assert.That(proxyServer.ConnectHosts, Does.Contain("non-existent.com:80"));
        Assert.That(auth, Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:secret"))));
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
        await request.DisposeAsync();
    }

    [UpstreamTest("PR-043 should work with context level proxy")]
    public async Task ShouldWorkWithContextLevelProxy(ContextFactory contextFactory, BrowserNewContextOptions contextOptions, Server server, TestProxy proxyServer)
    {
        server.SetRoute("/target.html", (req, res) =>
        {
            res.End("<title>Served by the proxy</title>");
        });

        proxyServer.ForwardTo(server.Port, new() { AllowConnectRequests = true });
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = $"localhost:{proxyServer.Port}" },
        });

        var (request, response) = await PromiseAll(
            server.WaitForRequest("/target.html"),
            context.APIRequest.GetAsync("http://non-existent.com/target.html"));
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(request.Url, Is.EqualTo("/target.html"));
    }

    [UpstreamTest("PR-044 should support proxy.bypass")]
    public async Task ShouldSupportProxyBypass(ContextFactory contextFactory, BrowserNewContextOptions contextOptions, Server server, TestProxy proxyServer)
    {
        server.SetRoute("/target.html", (req, res) =>
        {
            res.End("Served by the proxy");
        });
        // FYI: using long and weird domain names to avoid ATT DNS hijacking
        // that resolves everything to some weird search results page.
        //
        // @see https://gist.github.com/CollinChaffin/24f6c9652efb3d6d5ef2f5502720ef00
        proxyServer.ForwardTo(server.Port, new() { AllowConnectRequests = true });
        var context = await contextFactory(new(contextOptions)
        {
            Proxy = new() { Server = $"localhost:{proxyServer.Port}", Bypass = "1.non.existent.domain.for.the.test, 2.non.existent.domain.for.the.test, .another.test" },
        });

        {
            var res = await context.APIRequest.GetAsync(server.CrossProcessPrefix + "/target.html");
            Assert.That(await res.TextAsync(), Does.Contain("Served by the proxy"));
            Assert.That(proxyServer.ConnectHosts, Does.Contain(new Uri(server.CrossProcessPrefix).Authority));
            proxyServer.ConnectHosts = [];
        }

        {
            var res = await context.APIRequest.GetAsync("http://0.non.existent.domain.for.the.test/target.html");
            Assert.That(await res.TextAsync(), Does.Contain("Served by the proxy"));
            proxyServer.ConnectHosts = [];
        }

        {
            var error = await Catch(() => context.APIRequest.GetAsync("http://1.non.existent.domain.for.the.test/target.html"));
            Assert.That(error.Message, Is.Not.Empty);
            Assert.That(proxyServer.ConnectHosts, Is.EqualTo(Array.Empty<string>()));
        }

        {
            var error = await Catch(() => context.APIRequest.GetAsync("http://2.non.existent.domain.for.the.test/target.html"));
            Assert.That(error.Message, Is.Not.Empty);
            Assert.That(proxyServer.ConnectHosts, Is.EqualTo(Array.Empty<string>()));
        }

        {
            var error = await Catch(() => context.APIRequest.GetAsync("http://foo.is.the.another.test/target.html"));
            Assert.That(error.Message, Is.Not.Empty);
            Assert.That(proxyServer.ConnectHosts, Is.EqualTo(Array.Empty<string>()));
        }

        {
            var res = await context.APIRequest.GetAsync("http://3.non.existent.domain.for.the.test/target.html");
            Assert.That(await res.TextAsync(), Does.Contain("Served by the proxy"));
        }
    }

    [UpstreamTest("PR-045 should use socks proxy")]
    public async Task ShouldUseSocksProxy(IPlaywright playwright, Server server, int socksPort)
    {
        Info.Skip(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("INSIDE_DOCKER")), "connect ECONNREFUSED 127.0.0.1:<port>");

        var request = await playwright.APIRequest.NewContextAsync(new()
        {
            Proxy = new()
            {
                Server = $"socks5://localhost:{socksPort}",
            },
        });
        var response = await request.GetAsync(server.EmptyPage);
        Assert.That(await response.TextAsync(), Does.Contain("Served by the SOCKS proxy"));
    }

    [UpstreamTest("PR-046 should send correct ALPN protocol to HTTPS proxy")]
    public async Task ShouldSendCorrectALPNProtocolToHTTPSProxy(IPlaywright playwright, Server server, (int Major, int Minor, int Patch) nodeVersion)
    {
        Info.Skip(nodeVersion.Major < 22, "ALPNCallback is supported starting from Node 22");

        string[] offeredProtocols = null;
        // .NET: https.createServer({ ...(await TestServer.certOptions()), ALPNCallback }) is the ALPNCallbackServer below.
        // The upstream callback returns 'protocols[0]', which is undefined and rejects the connection.
        var proxy = new ALPNCallbackServer(protocols =>
        {
            offeredProtocols = protocols;
            return null;
        });

        var port = proxy.Listen(0);

        var request = await playwright.APIRequest.NewContextAsync(new()
        {
            Proxy = new() { Server = $"https://localhost:{port}" },
            IgnoreHTTPSErrors = true,
        });

        await ThrowsAsync<PlaywrightException>(() => request.GetAsync(server.EmptyPage));

        Assert.That(offeredProtocols, Does.Contain("http/1.1"));

        proxy.Close();
        await request.DisposeAsync();
    }

    [UpstreamTest("PR-047 should apply proxy.bypass to redirect targets")]
    public async Task ShouldApplyProxyBypassToRedirectTargets(ContextFactory contextFactory, Server server, TestProxy proxyServer)
    {
        var crossProcessHost = new Uri(server.CrossProcessPrefix).Authority;
        Info.Skip(crossProcessHost == server.Host, "Needs two different host names for the same server");

        proxyServer.ForwardTo(server.Port, new() { AllowConnectRequests = true });
        server.SetRedirect("/redirect-to-cross-process", server.CrossProcessPrefix + "/simple.json");
        server.SetRedirect("/redirect-to-same-origin", server.Prefix + "/simple.json");
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = $"localhost:{proxyServer.Port}", Bypass = server.Hostname },
        });

        {
            // Bypassed first hop redirects to a host that must go through the proxy.
            var response = await context.APIRequest.GetAsync(server.Prefix + "/redirect-to-cross-process");
            Assert.That(response.Url, Is.EqualTo(server.CrossProcessPrefix + "/simple.json"));
            ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
            Assert.That(proxyServer.ConnectHosts, Is.EqualTo(new[] { crossProcessHost }));
            proxyServer.ConnectHosts = [];
        }

        {
            // Proxied first hop redirects to a bypassed host.
            var response = await context.APIRequest.GetAsync(server.CrossProcessPrefix + "/redirect-to-same-origin");
            Assert.That(response.Url, Is.EqualTo(server.Prefix + "/simple.json"));
            ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
            Assert.That(proxyServer.ConnectHosts, Is.EqualTo(new[] { crossProcessHost }));
        }
    }

    // .NET: a TLS server with the ALPNCallback of Node.js. SslStream does not give the protocols that the
    // client offers, so the server reads them from the ClientHello. When the callback returns null, the
    // connection is rejected with a fatal 'no_application_protocol' alert, as Node.js does. This is the
    // only answer that the test needs, so the server has no certificate.
    private sealed class ALPNCallbackServer
    {
        private readonly Func<string[], string> _alpnCallback;
        private TcpListener _listener;

        public ALPNCallbackServer(Func<string[], string> alpnCallback)
        {
            _alpnCallback = alpnCallback;
        }

        public int Listen(int port)
        {
            _listener = new TcpListener(IPAddress.IPv6Any, port);
            _listener.Server.DualMode = true;
            _listener.Start();
            IgnoreErrors(AcceptLoopAsync());
            return ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        public void Close() => _listener.Stop();

        private async Task AcceptLoopAsync()
        {
            while (true)
            {
                IgnoreErrors(ServeAsync(await _listener.AcceptTcpClientAsync()));
            }
        }

        private async Task ServeAsync(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                // TLSPlaintext: type (1 byte), legacy_record_version (2 bytes), length (2 bytes).
                var header = new byte[5];
                await stream.ReadExactlyAsync(header);
                var fragment = new byte[BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(3))];
                await stream.ReadExactlyAsync(fragment);
                if (_alpnCallback(ParseALPNFromClientHello(fragment) ?? []) != null)
                {
                    throw new NotSupportedException("This server can only reject a connection.");
                }
                // Alert: level fatal (2), description no_application_protocol (120).
                await stream.WriteAsync(new byte[] { 21, 3, 3, 0, 2, 2, 120 });
            }
        }
    }

    // .NET: the protocol names of the ALPN extension (RFC 7301) of a ClientHello handshake message, or null.
    private static string[] ParseALPNFromClientHello(byte[] handshake)
    {
        try
        {
            // HandshakeType (1 byte): 0x01 = ClientHello.
            if (handshake[0] != 0x01)
            {
                return null;
            }
            // Handshake header: type (1) + length (3); legacy_version (2); random (32).
            var offset = 4 + 2 + 32;
            // legacy_session_id<0..32>
            offset += 1 + handshake[offset];
            // cipher_suites<2..2^16-2>
            offset += 2 + BinaryPrimitives.ReadUInt16BigEndian(handshake.AsSpan(offset));
            // legacy_compression_methods<1..2^8-1>
            offset += 1 + handshake[offset];
            // extensions<8..2^16-1>
            var extensionsEnd = offset + 2 + BinaryPrimitives.ReadUInt16BigEndian(handshake.AsSpan(offset));
            offset += 2;
            while (offset + 4 <= extensionsEnd)
            {
                var extensionType = BinaryPrimitives.ReadUInt16BigEndian(handshake.AsSpan(offset));
                var extensionLength = BinaryPrimitives.ReadUInt16BigEndian(handshake.AsSpan(offset + 2));
                offset += 4;
                if (extensionType == 16)
                {
                    // protocol_name_list<2..2^16-1> of ProtocolName<1..2^8-1>
                    var protocols = new List<string>();
                    var end = offset + extensionLength;
                    offset += 2;
                    while (offset < end)
                    {
                        protocols.Add(Encoding.UTF8.GetString(handshake, offset + 1, handshake[offset]));
                        offset += 1 + handshake[offset];
                    }
                    return [.. protocols];
                }
                offset += extensionLength;
            }
            return null;
        }
        catch (Exception e) when (e is IndexOutOfRangeException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
