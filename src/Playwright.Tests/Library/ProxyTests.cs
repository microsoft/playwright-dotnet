/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/proxy.spec.ts.</summary>
public class ProxyTests : UpstreamBrowserTest
{
    [UpstreamTest("PR-002 should use proxy @smoke")]
    public async Task ShouldUseProxy()
    {
        Server.SetRoute("/target.html", (req, res) =>
        {
            res.End("<html><title>Served by the proxy</title></html>");
        });
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = Server.Host },
        });
        var page = await browser.NewPageAsync(NewPageOptions());
        var url = "http://staging.example.org/target.html";
        var requests = new List<string>();
        page.Request += (_, request) =>
        {
            if (request.IsNavigationRequest)
            {
                requests.Add(request.Url);
            }
        };
        await page.GotoAsync(url);
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
        Assert.That(page.Url, Is.EqualTo(url));
        Assert.That(requests, Is.EqualTo(new[] { url }));
        await browser.CloseAsync();
    }

    [UpstreamTest("PR-003 should use proxy for second page")]
    public async Task ShouldUseProxyForSecondPage()
    {
        Server.SetRoute("/target.html", (req, res) =>
        {
            res.End("<html><title>Served by the proxy</title></html>");
        });
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = $"localhost:{Server.Port}" },
        });

        var page = await browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync("http://non-existent.test/target.html");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));

        var page2 = await browser.NewPageAsync(NewPageOptions());
        await page2.GotoAsync("http://non-existent.test/target.html");
        Assert.That(await page2.TitleAsync(), Is.EqualTo("Served by the proxy"));

        await browser.CloseAsync();
    }

    [UpstreamTest("PR-004 should work with IP:PORT notion")]
    public async Task ShouldWorkWithIPPORTNotion()
    {
        Server.SetRoute("/target.html", (req, res) =>
        {
            res.End("<html><title>Served by the proxy</title></html>");
        });
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = $"127.0.0.1:{Server.Port}" },
        });
        var page = await browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync("http://non-existent.test/target.html");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
        await browser.CloseAsync();
    }

    public class ShouldProxyLocalNetworkRequests : UpstreamBrowserTest
    {
        [TestFixture(false)]
        [TestFixture(true)]
        public class Describe : UpstreamBrowserTest
        {
            private readonly bool additionalBypass;

            public Describe(bool additionalBypass)
            {
                this.additionalBypass = additionalBypass;
            }

            // .NET: target and description are the properties of params.
            [UpstreamTest("PR-005 ${params.description}")]
            [LocalFixme("harness: on the macOS bot the navigation through the proxy fails with 'Could not connect to the server.' (only in webkit on darwin for \"localhost\" and \"127.0.0.1\" without the additional bypass)")]
            [TestCase("localhost", "localhost")]
            [TestCase("127.0.0.1", "loopback address")]
            [TestCase("169.254.3.4", "link-local")]
            public async Task Test(string target, string description)
            {
                Info.Skip(BrowserName == "webkit" && Platform == "darwin" && new[] { "localhost", "127.0.0.1" }.Contains(target) && additionalBypass, "Mac webkit does not proxy localhost when bypass rules are set.");
                Info.Skip(Channel?.StartsWith("msedge", StringComparison.Ordinal) == true, "times out while loading the page");

                // .NET: a boolean in a template literal is 'true' or 'false'.
                var path = $"/target-{(additionalBypass ? "true" : "false")}-{target}.html";
                Server.SetRoute(path, (req, res) =>
                {
                    res.End("<html><title>Served by the proxy</title></html>");
                });

                var url = $"http://{target}:55555{path}";
                ProxyServer.ForwardTo(Server.Port);
                var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
                {
                    Proxy = new() { Server = $"localhost:{ProxyServer.Port}", Bypass = additionalBypass ? "1.non.existent.domain.for.the.test" : null },
                });

                var page = await browser.NewPageAsync(NewPageOptions());
                await page.GotoAsync(url);
                Assert.That(ProxyServer.RequestUrls, Does.Contain(url));
                Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));

                await Catch(() => page.GotoAsync("http://1.non.existent.domain.for.the.test/foo.html"));
                if (additionalBypass)
                {
                    Assert.That(ProxyServer.RequestUrls, Does.Not.Contain("http://1.non.existent.domain.for.the.test/foo.html"));
                }
                else
                {
                    Assert.That(ProxyServer.RequestUrls, Does.Contain("http://1.non.existent.domain.for.the.test/foo.html"));
                }

                await browser.CloseAsync();
            }
        }
    }

    [UpstreamTest("PR-006 should allow bypassing ${host} requests")]
    [TestCase("localhost")]
    [TestCase("127.0.0.1")]
    public async Task ShouldAllowBypassingRequests(string host)
    {
        Server.SetRoute("/proxied/target.html", (req, res) =>
        {
            res.End("<html><title>Served by the server</title></html>");
        });
        Server.SetRoute("/target.html", (req, res) =>
        {
            res.End("<html><title>Served by the proxy</title></html>");
        });
        ProxyServer.ForwardTo(Server.Port, new() { RemovePrefix = "/proxied" });

        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = $"localhost:{ProxyServer.Port}", Bypass = host },
        });
        var page = await browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync($"http://{host}:{Server.Port}/proxied/target.html");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the server"));
        await browser.CloseAsync();
    }

    [UpstreamTest("PR-007 should authenticate")]
    public async Task ShouldAuthenticate()
    {
        Server.SetRoute("/target.html", (req, res) =>
        {
            var auth = req.Headers["proxy-authorization"];
            if (string.IsNullOrEmpty(auth))
            {
                res.WriteHead(407, "Proxy Authentication Required", new()
                {
                    ["Proxy-Authenticate"] = "Basic realm=\"Access to internal site\"",
                });
                res.End();
            }
            else
            {
                res.End($"<html><title>{auth}</title></html>");
            }
        });
        Server.SetRoute("/protected.html", (req, res) =>
        {
            if (string.IsNullOrEmpty(req.Headers["proxy-authorization"]))
            {
                res.WriteHead(407, new() { ["Proxy-Authenticate"] = "Basic realm=\"Access to internal site\"" });
                res.End();
                return;
            }
            res.WriteHead(401, new() { ["WWW-Authenticate"] = "Basic realm=\"site\"" });
            res.End(string.IsNullOrEmpty(req.Headers["authorization"]) ? "no credentials" : req.Headers["authorization"]);
        });
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = Server.Host, Username = "user", Password = "secret" },
        });
        var page = await browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync("http://non-existent.test/target.html");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:secret"))));
        // Proxy credentials must not be sent to the website's own auth challenge.
        // .NET: responseOrError is the response, or the error of the navigation.
        IResponse response = null;
        var error = await Catch(async () => response = await page.GotoAsync("http://non-existent.test/protected.html"));
        if (BrowserName == "chromium" && !IsHeadlessShell)
        {
            // Branded Chromium shows a login dialog for the 401 and fails the navigation.
            Assert.That(error.Message, Does.Contain("net::ERR_INVALID_AUTH_CREDENTIALS"));
        }
        else
        {
            Assert.That(response.Status, Is.EqualTo(401));
            Assert.That(await page.Locator("body").TextContentAsync(), Is.EqualTo("no credentials"));
        }
        await browser.CloseAsync();
    }

    [UpstreamTest("PR-008 should reconnect with credentials after CONNECT 407 closes the socket")]
    public async Task ShouldReconnectWithCredentialsAfterCONNECT407ClosesTheSocket()
    {
        // Some HTTP proxies send 407 and close the socket on the first CONNECT, expecting
        // the client to reconnect on a new TCP connection with Proxy-Authorization.
        HttpsServer.SetRoute("/target.html", (req, res) =>
        {
            res.End("<html><title>Served by https server via proxy</title></html>");
        });
        ProxyServer.ForwardTo(HttpsServer.Port, new() { AllowConnectRequests = true });

        var connectAttempts = new List<Dictionary<string, bool>>();
        var closedFirstConnect = false;
        ProxyServer.SetAuthHandler(req =>
        {
            // WebKit on Windows uses libcurl and sends Proxy-Authorization preemptively
            // on every CONNECT, while libsoup on Linux/macOS sends it only after a 407.
            // Force the first CONNECT to fail regardless to deterministically exercise
            // the reconnect path on all platforms.
            if (req.Method != "CONNECT" || req.Headers["host"]?.StartsWith("non-existent.test", StringComparison.Ordinal) != true)
            {
                return true;
            }
            connectAttempts.Add(new() { ["hadAuth"] = !string.IsNullOrEmpty(req.Headers["proxy-authorization"]) });
            if (!closedFirstConnect)
            {
                closedFirstConnect = true;
                return false;
            }
            return !string.IsNullOrEmpty(req.Headers["proxy-authorization"]);
        });

        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = ProxyServer.Host, Username = "user", Password = "secret" },
        });
        var page = await browser.NewPageAsync(new(NewPageOptions()) { IgnoreHTTPSErrors = true });
        await page.GotoAsync("https://non-existent.test/target.html");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by https server via proxy"));
        Assert.That(connectAttempts.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(connectAttempts.Any(a => a["hadAuth"]), Is.EqualTo(true));
        await browser.CloseAsync();
    }

    [UpstreamTest("PR-009 should work with authenticate followed by redirect")]
    public async Task ShouldWorkWithAuthenticateFollowedByRedirect()
    {
        bool hasAuth(ServerRequest req, ServerResponse res)
        {
            var auth = req.Headers["proxy-authorization"];
            if (string.IsNullOrEmpty(auth))
            {
                res.WriteHead(407, "Proxy Authentication Required", new()
                {
                    ["Proxy-Authenticate"] = "Basic realm=\"Access to internal site\"",
                });
                res.End();
                return false;
            }
            return true;
        }
        Server.SetRoute("/page1.html", (req, res) =>
        {
            if (!hasAuth(req, res))
            {
                return;
            }
            res.WriteHead(302, new() { ["location"] = "/page2.html" });
            res.End();
        });
        Server.SetRoute("/page2.html", (req, res) =>
        {
            if (!hasAuth(req, res))
            {
                return;
            }
            res.End("<html><title>Served by the proxy</title></html>");
        });
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = Server.Host, Username = "user", Password = "secret" },
        });
        var page = await browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync("http://non-existent.test/page1.html");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
        await browser.CloseAsync();
    }

    [UpstreamTest("PR-010 should exclude patterns")]
    public async Task ShouldExcludePatterns()
    {
        Info.Skip(Channel?.StartsWith("msedge", StringComparison.Ordinal) == true, "times out while loading the page");

        Server.SetRoute("/target.html", (req, res) =>
        {
            res.End("<html><title>Served by the proxy</title></html>");
        });
        // FYI: using long and weird domain names to avoid ATT DNS hijacking
        // that resolves everything to some weird search results page.
        //
        // @see https://gist.github.com/CollinChaffin/24f6c9652efb3d6d5ef2f5502720ef00
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = Server.Host, Bypass = "1.non.existent.domain.for.the.test, 2.non.existent.domain.for.the.test, .another.test" },
        });

        {
            var page = await browser.NewPageAsync(NewPageOptions());
            await page.GotoAsync("http://0.non.existent.domain.for.the.test/target.html");
            Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
            await page.CloseAsync();
        }

        {
            var page = await browser.NewPageAsync(NewPageOptions());
            var error = await Catch(() => page.GotoAsync("http://1.non.existent.domain.for.the.test/target.html"));
            Assert.That(error.Message, Is.Not.Empty);
            await page.CloseAsync();
        }

        {
            var page = await browser.NewPageAsync(NewPageOptions());
            var error = await Catch(() => page.GotoAsync("http://2.non.existent.domain.for.the.test/target.html"));
            Assert.That(error.Message, Is.Not.Empty);
            await page.CloseAsync();
        }

        {
            var page = await browser.NewPageAsync(NewPageOptions());
            var error = await Catch(() => page.GotoAsync("http://foo.is.the.another.test/target.html"));
            Assert.That(error.Message, Is.Not.Empty);
            await page.CloseAsync();
        }

        {
            var page = await browser.NewPageAsync(NewPageOptions());
            await page.GotoAsync("http://3.non.existent.domain.for.the.test/target.html");
            Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
            await page.CloseAsync();
        }

        await browser.CloseAsync();
    }

    [UpstreamTest("PR-011 should bypass proxy for ${host} when ${host} is in bypass list")]
    [TestCase("localhost")]
    [TestCase("127.0.0.1")]
    public async Task ShouldBypassProxyForWhenIsInBypassList(string host)
    {
        ProxyServer.ForwardTo(Server.Port, new() { RemovePrefix = "/proxied" });
        Server.SetRoute("/proxied/target.html", (req, res) =>
        {
            res.End("<html><title>Served by the server</title></html>");
        });
        Server.SetRoute("/target.html", (req, res) =>
        {
            res.End("<html><title>Served by the proxy</title></html>");
        });
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = $"localhost:{ProxyServer.Port}", Bypass = host },
        });
        var page = await browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync($"http://{host}:{Server.Port}/proxied/target.html");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the server"));
        await browser.CloseAsync();
    }

    [UpstreamTest("PR-012 should use socks proxy")]
    public async Task ShouldUseSocksProxy()
    {
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = $"socks5://localhost:{SocksPort}" },
        });
        var page = await browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync("http://non-existent.test");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the SOCKS proxy"));
        await browser.CloseAsync();
    }

    [UpstreamTest("PR-013 should use socks proxy in second page")]
    public async Task ShouldUseSocksProxyInSecondPage()
    {
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = $"socks5://localhost:{SocksPort}" },
        });

        var page = await browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync("http://non-existent.test");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the SOCKS proxy"));

        var page2 = await browser.NewPageAsync(NewPageOptions());
        await page2.GotoAsync("http://non-existent.test");
        Assert.That(await page2.TitleAsync(), Is.EqualTo("Served by the SOCKS proxy"));

        await browser.CloseAsync();
    }

    [UpstreamTest("PR-014 does launch without a port")]
    public async Task DoesLaunchWithoutAPort()
    {
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = "http://localhost" },
        });
        await browser.CloseAsync();
    }

    [UpstreamTest("PR-015 should use proxy with emulated user agent")]
    public async Task ShouldUseProxyWithEmulatedUserAgent()
    {
        Info.Fixme(true, "Non-emulated user agent is used in proxy CONNECT");

        var requestText = string.Empty;
        // This is our proxy server
        // .NET: net.createServer(socket => socket.on('data', ...)) is a TcpListener with an accept loop.
        var server = new TcpListener(IPAddress.IPv6Any, 0);
        server.Server.DualMode = true;
        server.Start();
        IgnoreErrors(Task.Run(async () =>
        {
            while (true)
            {
                using var socket = await server.AcceptTcpClientAsync();
                var data = new byte[65536];
                var length = await socket.GetStream().ReadAsync(data);
                requestText = Encoding.UTF8.GetString(data, 0, length);
                socket.Close();
            }
        }));

        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = $"http://127.0.0.1:{((IPEndPoint)server.LocalEndpoint).Port}" },
        });

        var page = await browser.NewPageAsync(new(NewPageOptions())
        {
            UserAgent = "MyUserAgent",
        });

        // HTTPS over HTTP proxy will start with CONNECT request.
        await Catch(() => page.GotoAsync("https://bing.com/"));
        await browser.CloseAsync();
        server.Stop();
        // This connect request should have emulated user agent.
        Assert.That(requestText, Does.Contain("MyUserAgent"));
    }

    [UpstreamTest("PR-016 should use SOCKS proxy for websocket requests")]
    public async Task ShouldUseSOCKSProxyForWebsocketRequests()
    {
        var socksForwardingServer = await TestProxy.SetupSocksForwardingServer(
            port: Info.WorkerIndex + 2048 + 2,
            forwardPort: Server.Port,
            allowedTargetPort: 1337);
        var proxyServerAddr = socksForwardingServer.ProxyServerAddr;
        var closeProxyServer = socksForwardingServer.CloseProxyServer;
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new()
            {
                Server = proxyServerAddr,
            },
        });
        Server.SendOnWebSocketConnection("incoming");
        Server.SetRoute("/target.html", (req, res) =>
        {
            res.End("<html><title>Served by the proxy</title></html>");
        });

        var page = await browser.NewPageAsync(NewPageOptions());

        // Hosts get resolved by the client
        await page.GotoAsync("http://fake-localhost-127-0-0-1.nip.io:1337/target.html");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));

        var value = await page.EvaluateAsync<string>(@"() => {
    let cb;
    const result = new Promise(f => cb = f);
    const ws = new WebSocket('ws://fake-localhost-127-0-0-1.nip.io:1337/ws');
    ws.addEventListener('message', data => { ws.close(); cb(data.data); });
    return result;
  }");
        Assert.That(value, Is.EqualTo("incoming"));

        await browser.CloseAsync();
        await closeProxyServer();
    }

    [UpstreamTest("PR-017 should use http proxy for websocket requests")]
    public async Task ShouldUseHttpProxyForWebsocketRequests()
    {
        Info.Skip(IsMac && MacVersion == 13, "Times out on Mac 13");

        ProxyServer.ForwardTo(Server.Port, new() { AllowConnectRequests = true });
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Proxy = new() { Server = $"localhost:{ProxyServer.Port}" },
        });

        Server.SendOnWebSocketConnection("incoming");
        Server.SetRoute("/target.html", (req, res) =>
        {
            res.End("<html><title>Served by the proxy</title></html>");
        });

        var page = await browser.NewPageAsync(NewPageOptions());

        await page.GotoAsync("http://fake-localhost-127-0-0-1.nip.io:1337/target.html");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));

        var value = await page.EvaluateAsync<string>(@"() => {
    let cb;
    const result = new Promise(f => cb = f);
    const ws = new WebSocket('ws://fake-localhost-127-0-0-1.nip.io:1337/ws');
    ws.addEventListener('message', data => { ws.close(); cb(data.data); });
    return result;
  }");
        Assert.That(value, Is.EqualTo("incoming"));

        // WebKit does not use CONNECT for websockets, but other browsers do.
        if (BrowserName == "webkit")
        {
            Assert.That(ProxyServer.WsUrls, Does.Contain((IsWindows && Channel == null) ? "/ws" : "ws://fake-localhost-127-0-0-1.nip.io:1337/ws"));
        }
        else
        {
            Assert.That(ProxyServer.ConnectHosts, Does.Contain("fake-localhost-127-0-0-1.nip.io:1337"));
        }

        await browser.CloseAsync();
    }
}
