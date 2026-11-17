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

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/client-certificates.spec.ts.</summary>
public class ClientCertificatesTests : UpstreamBrowserTest
{
    public sealed class StartCCServerOptions
    {
        public bool? Http2 { get; set; }

        public bool? EnableHTTP1FallbackWhenUsingHttp2 { get; set; }

        public bool? UseFakeLocalhost { get; set; }

        public bool? RejectUnauthorized { get; set; }
    }

    private TlsServer _ccServer;

    private async Task<string> StartCCServer(StartCCServerOptions options = null)
    {
        Environment.SetEnvironmentVariable("PWTEST_UNSUPPORTED_CUSTOM_CA", Asset("client-certificates/server/server_cert.pem"));
        _ccServer = (options?.Http2 == true ? (Func<TlsServerOptions, Action<TlsServerRequest, TlsServerResponse>, TlsServer>)CreateHttp2Server : CreateHttpsServer)(new()
        {
            Key = File.ReadAllBytes(Asset("client-certificates/server/server_key.pem")),
            Cert = File.ReadAllBytes(Asset("client-certificates/server/server_cert.pem")),
            Ca =
            [
                File.ReadAllBytes(Asset("client-certificates/server/server_cert.pem")),
            ],
            RequestCert = true,
            RejectUnauthorized = options?.RejectUnauthorized ?? false,
            AllowHTTP1 = options?.EnableHTTP1FallbackWhenUsingHttp2,
        }, (req, res) =>
        {
            var tlsSocket = req.Socket;
            var parts = new List<(string key, object value)>();
            parts.Add(("alpn-protocol", tlsSocket.AlpnProtocol));
            parts.Add(("servername", tlsSocket.Servername));
            var cert = tlsSocket.GetPeerCertificate();
            if (tlsSocket.Authorized)
            {
                res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
                parts.Add(("message", $"Hello {cert.Subject.CN}, your certificate was issued by {cert.Issuer.CN}!"));
            }
            else if (cert.Subject != null)
            {
                res.WriteHead(403, new() { ["Content-Type"] = "text/html" });
                parts.Add(("message", $"Sorry {cert.Subject.CN}, certificates from {cert.Issuer.CN} are not welcome here."));
            }
            else
            {
                res.WriteHead(401, new() { ["Content-Type"] = "text/html" });
                parts.Add(("message", "Sorry, but you need to provide a client certificate to continue."));
            }
            res.End(string.Join(string.Empty, parts.Select(part => $"<div data-testid=\"{part.key}\">{part.value}</div>")));
        });
        await _ccServer.Listen(0, "127.0.0.1");
        var host = options?.UseFakeLocalhost == true ? "local.playwright" : "127.0.0.1";
        return $"https://{host}:{_ccServer.Address().Port}/";
    }

    [TearDown]
    public async Task StartCCServerTearDown()
    {
        if (_ccServer != null)
        {
            await _ccServer.Close();
            _ccServer = null;
        }
    }

    // .NET: __filename is the path of this source file.
    private static readonly string kDummyFileName = Filename();

    // .NET: the options of the three calls that take a sub test (APIRequest.NewContextAsync, Browser.NewContextAsync
    // and launchPersistent) are three types in .NET, so a sub test holds the clientCertificates of its options.
    private static readonly (ClientCertificate[] clientCertificates, string expected)[] kValidationSubTests =
    [
        ([new() { Origin = "test" }], "None of cert, key, passphrase or pfx is specified"),
        ([
            new()
            {
                Origin = "test",
                CertPath = kDummyFileName,
                KeyPath = kDummyFileName,
                PfxPath = kDummyFileName,
                Passphrase = kDummyFileName,
            }
        ], "pfx is specified together with cert, key or passphrase"),
    ];

    private static string Filename([CallerFilePath] string filename = null) => filename;

    // .NET: the class around has no tests of its own, so the describes derive from it for its fixture.
    public class Fetch : ClientCertificatesTests
    {
        [UpstreamTest("CC-001 validate input")]
        public async Task ValidateInput()
        {
            foreach (var (clientCertificates, expected) in kValidationSubTests)
            {
                var error = await ThrowsAsync<PlaywrightException>(() => Playwright.APIRequest.NewContextAsync(new() { ClientCertificates = clientCertificates }));
                Assert.That(error.Message, Does.Contain(expected));
            }
        }

        [UpstreamTest("CC-002 should fail with no client certificates provided")]
        public async Task ShouldFailWithNoClientCertificatesProvided()
        {
            var serverURL = await StartCCServer();
            var request = await Playwright.APIRequest.NewContextAsync(new() { IgnoreHTTPSErrors = true });
            var response = await request.GetAsync(serverURL);
            Assert.That(response.Status, Is.EqualTo(401));
            Assert.That(await response.TextAsync(), Does.Contain("Sorry, but you need to provide a client certificate to continue."));
            await request.DisposeAsync();
        }

        [UpstreamTest("CC-003 should keep supporting http")]
        public async Task ShouldKeepSupportingHttp()
        {
            var request = await Playwright.APIRequest.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(Server.Prefix).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            var response = await request.GetAsync(Server.Prefix + "/one-style.html");
            Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/one-style.html"));
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(await response.TextAsync(), Does.Contain("<div>hello, world!</div>"));
            await request.DisposeAsync();
        }

        [UpstreamTest("CC-004 should throw with untrusted client certs")]
        public async Task ShouldThrowWithUntrustedClientCerts()
        {
            var serverURL = await StartCCServer();
            var request = await Playwright.APIRequest.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/self-signed/cert.pem"),
                        KeyPath = Asset("client-certificates/client/self-signed/key.pem"),
                    }
                ],
            });
            var response = await request.GetAsync(serverURL);
            Assert.That(response.Url, Is.EqualTo(serverURL));
            Assert.That(response.Status, Is.EqualTo(403));
            Assert.That(await response.TextAsync(), Does.Contain("Sorry Bob, certificates from Bob are not welcome here."));
            await request.DisposeAsync();
        }

        [UpstreamTest("CC-005 pass with trusted client certificates")]
        public async Task PassWithTrustedClientCertificates()
        {
            var serverURL = await StartCCServer();
            var request = await Playwright.APIRequest.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            var response = await request.GetAsync(serverURL);
            Assert.That(response.Url, Is.EqualTo(serverURL));
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(await response.TextAsync(), Does.Contain("Hello Alice, your certificate was issued by localhost!"));
            await request.DisposeAsync();
        }

        [UpstreamTest("CC-006 should not leak client certificate to cross-origin redirect target")]
        public async Task ShouldNotLeakClientCertificateToCrossOriginRedirectTarget()
        {
            var targetURL = await StartCCServer();

            // Standalone HTTPS server (not cert-required) that 302-redirects to the cert-required server.
            // .NET: createHttpsServer({ key, cert }, (req, res) => { ... }) that listens on the port 0 is a Server with a route.
            var redirectServer = Server.CreateHttps(Env.AssetsDir, 0, null, Server.Certificate(Asset("client-certificates/server/server_cert.pem"), Asset("client-certificates/server/server_key.pem")));
            redirectServer.SetRoute("/redir", (req, res) =>
            {
                res.WriteHead(302, new() { ["Location"] = targetURL });
                res.End();
            });
            // Use 'localhost' for the redirect origin so it differs from the target's '127.0.0.1' origin
            // even though both resolve to the loopback interface and share the same trusted CA.
            var redirectURL = $"https://localhost:{redirectServer.Port}/redir";

            var request = await Playwright.APIRequest.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true,
                // Cert is scoped to the redirect *origin*, not the target — it must not follow the redirect.
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(redirectURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            var response = await request.GetAsync(redirectURL);
            Assert.That(response.Url, Is.EqualTo(targetURL));
            Assert.That(response.Status, Is.EqualTo(401));
            Assert.That(await response.TextAsync(), Does.Contain("you need to provide a client certificate"));
            await request.DisposeAsync();
            await redirectServer.StopAsync();
        }

        [UpstreamTest("CC-007 pass with trusted client certificates in pfx format")]
        public async Task PassWithTrustedClientCertificatesInPfxFormat()
        {
            var serverURL = await StartCCServer();
            var request = await Playwright.APIRequest.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        PfxPath = Asset("client-certificates/client/trusted/cert.pfx"),
                        Passphrase = "secure",
                    }
                ],
            });
            var response = await request.GetAsync(serverURL);
            Assert.That(response.Url, Is.EqualTo(serverURL));
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(await response.TextAsync(), Does.Contain("Hello Alice, your certificate was issued by localhost!"));
            await request.DisposeAsync();
        }

        [UpstreamTest("CC-008 pass with trusted client certificates and when a http proxy is used")]
        public async Task PassWithTrustedClientCertificatesAndWhenAHttpProxyIsUsed()
        {
            var serverURL = await StartCCServer();
            ProxyServer.ForwardTo(new Uri(serverURL).Port, new() { AllowConnectRequests = true });
            var request = await Playwright.APIRequest.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
                Proxy = new() { Server = $"localhost:{ProxyServer.Port}" },
            });
            Assert.That(ProxyServer.ConnectHosts, Is.EqualTo(Array.Empty<string>()));
            var response = await request.GetAsync(serverURL);
            Assert.That(ProxyServer.ConnectHosts, Is.EqualTo(new[] { new Uri(serverURL).Authority }));
            Assert.That(response.Url, Is.EqualTo(serverURL));
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(await response.TextAsync(), Does.Contain("Hello Alice, your certificate was issued by localhost!"));
            await request.DisposeAsync();
        }

        [UpstreamTest("CC-009 pass with trusted client certificates and when a socks proxy is used")]
        public async Task PassWithTrustedClientCertificatesAndWhenASocksProxyIsUsed()
        {
            var serverURL = await StartCCServer();
            var serverPort = new Uri(serverURL).Port;
            // .NET: connectHosts of upstream is a list that the server keeps adding to: here it is read from the server each time.
            var socksForwardingServer = await TestProxy.SetupSocksForwardingServer(
                port: Info.WorkerIndex + 2048 + 2,
                forwardPort: serverPort,
                allowedTargetPort: serverPort);
            var proxyServerAddr = socksForwardingServer.ProxyServerAddr;
            var closeProxyServer = socksForwardingServer.CloseProxyServer;
            var request = await Playwright.APIRequest.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
                Proxy = new() { Server = proxyServerAddr },
            });
            Assert.That(socksForwardingServer.ConnectHosts, Is.EqualTo(Array.Empty<string>()));
            var response = await request.GetAsync(serverURL);
            Assert.That(socksForwardingServer.ConnectHosts, Is.EqualTo(new[] { new Uri(serverURL).Authority }));
            Assert.That(response.Url, Is.EqualTo(serverURL));
            Assert.That(response.Status, Is.EqualTo(200));
            Assert.That(await response.TextAsync(), Does.Contain("Hello Alice, your certificate was issued by localhost!"));
            await request.DisposeAsync();
            await closeProxyServer();
        }

        [UpstreamTest("CC-010 should throw a http error if the pfx passphrase is incorect")]
        public async Task ShouldThrowAHttpErrorIfThePfxPassphraseIsIncorect()
        {
            var serverURL = await StartCCServer();
            var request = await Playwright.APIRequest.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        PfxPath = Asset("client-certificates/client/trusted/cert.pfx"),
                        Passphrase = "this-password-is-incorrect",
                    }
                ],
            });
            var error = await ThrowsAsync<PlaywrightException>(() => request.GetAsync(serverURL));
            Assert.That(error.Message, Does.Contain("mac verify failure"));
            await request.DisposeAsync();
        }

        [UpstreamTest("CC-011 should fail with matching certificates in legacy pfx format")]
        public async Task ShouldFailWithMatchingCertificatesInLegacyPfxFormat()
        {
            var serverURL = await StartCCServer();
            var request = await Playwright.APIRequest.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        PfxPath = Asset("client-certificates/client/trusted/cert-legacy.pfx"),
                        Passphrase = "secure",
                    }
                ],
            });
            var error = await ThrowsAsync<PlaywrightException>(() => request.GetAsync(serverURL));
            Assert.That(error.Message, Does.Contain("Unsupported TLS certificate"));
            await request.DisposeAsync();
        }

        [UpstreamTest("CC-012 should work in the browser with request interception")]
        public async Task ShouldWorkInTheBrowserWithRequestInterception()
        {
            var serverURL = await StartCCServer();
            var request = await Playwright.APIRequest.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            var page = await Browser.NewPageAsync(new(NewPageOptions()) { IgnoreHTTPSErrors = true });
            await page.RouteAsync("**/*", async route =>
            {
                var response = await request.FetchAsync(route.Request);
                await route.FulfillAsync(new() { Response = response });
            });
            await page.GotoAsync(serverURL);
            await Expect(page.GetByTestId("message")).ToHaveTextAsync("Hello Alice, your certificate was issued by localhost!");
            await page.CloseAsync();
            await request.DisposeAsync();
        }
    }

    public class BrowserDescribe : ClientCertificatesTests
    {
        [UpstreamTest("CC-013 validate input")]
        public async Task ValidateInput()
        {
            foreach (var (clientCertificates, expected) in kValidationSubTests)
            {
                var error = await ThrowsAsync<PlaywrightException>(() => Browser.NewContextAsync(new(ContextOptions()) { ClientCertificates = clientCertificates }));
                Assert.That(error.Message, Does.Contain(expected));
            }
        }

        [UpstreamTest("CC-014 should keep supporting http")]
        public async Task ShouldKeepSupportingHttp()
        {
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(Server.Prefix).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            await page.GotoAsync(Server.Prefix + "/one-style.html");
            await Expect(page.GetByText("hello, world!")).ToBeVisibleAsync();
            await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
            await page.CloseAsync();
        }

        [UpstreamTest("CC-015 should pass through to non-matching origin with self-signed cert")]
        public async Task ShouldPassThroughToNonMatchingOriginWithSelfSignedCert()
        {
            HttpsServer.SetRoute("/hello.html", (req, res) =>
            {
                res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
                res.End("<html><body><div data-testid=\"message\">hello</div></body></html>");
            });
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                ClientCertificates =
                [
                    new()
                    {
                        Origin = "https://not-matching.com",
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            await page.GotoAsync(HttpsServer.Prefix + "/hello.html");
            await Expect(page.GetByTestId("message")).ToHaveTextAsync("hello");
            await page.CloseAsync();
        }

        [UpstreamTest("CC-016 should not intercept TLS for origins without a client certificate")]
        public async Task ShouldNotInterceptTLSForOriginsWithoutAClientCertificate()
        {
            // If the proxy intercepted this origin, the browser would see its self-signed cert (CN=localhost)
            // instead of the real server cert (CN=playwright-test).
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                ClientCertificates =
                [
                    new()
                    {
                        Origin = "https://not-matching.com",
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            var response = await page.GotoAsync(HttpsServer.EmptyPage);
            Assert.That(response.Ok, Is.EqualTo(true));
            var securityDetails = await response.SecurityDetailsAsync();
            Assert.That(securityDetails.SubjectName, Does.Contain("playwright-test"));
            await page.CloseAsync();
        }

        [UpstreamTest("CC-017 should fail with no client certificates")]
        public async Task ShouldFailWithNoClientCertificates()
        {
            var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = "https://not-matching.com",
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            await page.GotoAsync(serverURL);
            await Expect(page.GetByTestId("message")).ToHaveTextAsync("Sorry, but you need to provide a client certificate to continue.");
            await page.CloseAsync();
        }

        [UpstreamTest("CC-018 should fail with self-signed client certificates")]
        public async Task ShouldFailWithSelfSignedClientCertificates()
        {
            var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/self-signed/cert.pem"),
                        KeyPath = Asset("client-certificates/client/self-signed/key.pem"),
                    }
                ],
            });
            await page.GotoAsync(serverURL);
            await Expect(page.GetByTestId("message")).ToHaveTextAsync("Sorry Bob, certificates from Bob are not welcome here.");
            await page.CloseAsync();
        }

        [UpstreamTest("CC-019 should complete browser TLS handshake when server rejects client certificate after it")]
        public async Task ShouldCompleteBrowserTLSHandshakeWhenServerRejectsClientCertificateAfterIt()
        {
            var serverURL = await StartCCServer(new() { RejectUnauthorized = true, UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/self-signed/cert.pem"),
                        KeyPath = Asset("client-certificates/client/self-signed/key.pem"),
                    }
                ],
            });
            // .NET: 'result' of upstream is the response or the error: here these are two variables.
            IResponse result = null;
            var error = await Catch(async () => result = await page.GotoAsync(serverURL));
            if (error != null)
            {
                // The server closed the connection with FIN after the TLS handshake.
                Assert.That(error.Message, Does.Match("net::ERR_EMPTY_RESPONSE|NS_ERROR_NET_RESET|Connection terminated unexpectedly|The network connection was lost|Failure when receiving data from the peer"));
            }
            else
            {
                // The server reset the connection, which the proxy reports as a client certificate error.
                Assert.That(result.Status, Is.EqualTo(503));
                await Expect(page.GetByText("Playwright client-certificate error")).ToBeVisibleAsync();
            }
            await page.CloseAsync();
        }

        [UpstreamTest("CC-020 should pass with matching certificates")]
        public async Task ShouldPassWithMatchingCertificates()
        {
            var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            await page.GotoAsync(serverURL);
            await Expect(page.GetByTestId("message")).ToHaveTextAsync("Hello Alice, your certificate was issued by localhost!");
            await page.CloseAsync();
        }

        [UpstreamTest("CC-021 should pass with matching certificates when passing as content")]
        public async Task ShouldPassWithMatchingCertificatesWhenPassingAsContent()
        {
            var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        Cert = await File.ReadAllBytesAsync(Asset("client-certificates/client/trusted/cert.pem")),
                        Key = await File.ReadAllBytesAsync(Asset("client-certificates/client/trusted/key.pem")),
                    }
                ],
            });
            await page.GotoAsync(serverURL);
            await Expect(page.GetByTestId("message")).ToHaveTextAsync("Hello Alice, your certificate was issued by localhost!");
            await page.CloseAsync();
        }

        [UpstreamTest("CC-022 should pass with matching certificates and when a http proxy is used")]
        public async Task ShouldPassWithMatchingCertificatesAndWhenAHttpProxyIsUsed()
        {
            var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            ProxyServer.ForwardTo(new Uri(serverURL).Port, new() { AllowConnectRequests = true });
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
                Proxy = new() { Server = $"localhost:{ProxyServer.Port}" },
            });
            Assert.That(ProxyServer.ConnectHosts, Is.EqualTo(Array.Empty<string>()));
            await page.GotoAsync(serverURL);
            var host = BrowserName == "webkit" && IsMac ? "localhost" : "127.0.0.1";
            Assert.That(ProxyServer.ConnectHosts.Distinct(), Is.EqualTo(new[] { $"{host}:{new Uri(serverURL).Port}" }));
            await Expect(page.GetByTestId("message")).ToHaveTextAsync("Hello Alice, your certificate was issued by localhost!");
            await page.CloseAsync();
        }

        [UpstreamTest("CC-025 should respect launch proxy and proxy bypass")]
        public async Task ShouldRespectLaunchProxyAndProxyBypass()
        {
            var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            var (origin, hostname, port) = (new Uri(serverURL).GetLeftPart(UriPartial.Authority), new Uri(serverURL).Host, new Uri(serverURL).Port);
            ProxyServer.ForwardTo(port, new() { AllowConnectRequests = true });
            var proxy = new Proxy { Server = $"localhost:{ProxyServer.Port}" };
            var serverHost = $"{(BrowserName == "webkit" && IsMac ? "localhost" : "127.0.0.1")}:{port}";
            var clientCertificates = new ClientCertificate[]
            {
                new()
                {
                    Origin = origin,
                    CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                    KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                },
            };
            var browser = await BrowserType.LaunchAsync(new(LaunchOptions()) { Proxy = proxy });
            {
                var page = await browser.NewPageAsync(new(NewPageOptions()) { IgnoreHTTPSErrors = true, ClientCertificates = clientCertificates });
                await page.GotoAsync(serverURL);
                await Expect(page.GetByTestId("message")).ToHaveTextAsync("Hello Alice, your certificate was issued by localhost!");
                // Headed Chromium also sends its own background requests through the launch proxy.
                Assert.That(ProxyServer.ConnectHosts, Does.Contain(serverHost));
                await page.CloseAsync();
            }
            ProxyServer.ConnectHosts = [];
            {
                var page = await browser.NewPageAsync(new(NewPageOptions()) { IgnoreHTTPSErrors = true, ClientCertificates = clientCertificates, Proxy = new() { Server = proxy.Server, Bypass = hostname } });
                await page.GotoAsync(serverURL);
                await Expect(page.GetByTestId("message")).ToHaveTextAsync("Hello Alice, your certificate was issued by localhost!");
                Assert.That(ProxyServer.ConnectHosts, Does.Not.Contain(serverHost));
                await page.CloseAsync();
            }
            await browser.CloseAsync();
        }

        [UpstreamTest("CC-026 should pass with matching certificates and when a socks proxy is used")]
        public async Task ShouldPassWithMatchingCertificatesAndWhenASocksProxyIsUsed()
        {
            var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            var serverPort = new Uri(serverURL).Port;
            // .NET: connectHosts of upstream is a list that the server keeps adding to: here it is read from the server each time.
            var socksForwardingServer = await TestProxy.SetupSocksForwardingServer(
                port: Info.WorkerIndex + 2048 + 2,
                forwardPort: serverPort,
                allowedTargetPort: serverPort);
            var proxyServerAddr = socksForwardingServer.ProxyServerAddr;
            var closeProxyServer = socksForwardingServer.CloseProxyServer;
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
                Proxy = new() { Server = proxyServerAddr },
            });
            Assert.That(socksForwardingServer.ConnectHosts, Is.EqualTo(Array.Empty<string>()));
            await page.GotoAsync(serverURL);
            var host = BrowserName == "webkit" && IsMac ? "localhost" : "127.0.0.1";
            Assert.That(socksForwardingServer.ConnectHosts, Is.EqualTo(new[] { $"{host}:{serverPort}" }));
            await Expect(page.GetByTestId("message")).ToHaveTextAsync("Hello Alice, your certificate was issued by localhost!");
            await page.CloseAsync();
            await closeProxyServer();
        }

        [UpstreamTest("CC-027 should not hang on tls errors during TLS 1.2 handshake")]
        public async Task ShouldNotHangOnTlsErrorsDuringTLS12Handshake()
        {
            foreach (var tlsVersion in new[] { "TLSv1.3", "TLSv1.2" })
            {
                await Info.Step($"TLS version: {tlsVersion}", async () =>
                {
                    var server = CreateTlsServer(new()
                    {
                        Key = File.ReadAllBytes(Asset("client-certificates/server/server_key.pem")),
                        Cert = File.ReadAllBytes(Asset("client-certificates/server/server_cert.pem")),
                        Ca =
                        [
                            File.ReadAllBytes(Asset("client-certificates/server/server_cert.pem")),
                        ],
                        RequestCert = true,
                        RejectUnauthorized = true,
                        MinVersion = tlsVersion,
                        MaxVersion = tlsVersion,
                        // .NET: cb(new Error('Connection rejected'), null) is a throw.
                        SNICallback = servername =>
                        {
                            // Always reject the connection by passing an error
                            throw new Exception("Connection rejected");
                        },
                    }, () =>
                    {
                        // Do nothing
                    });
                    // .NET: upstream makes the URL in the callback of listen().
                    await server.Listen(0, "localhost");
                    var host = BrowserName == "webkit" && Platform == "darwin" ? "local.playwright" : "localhost";
                    var serverURL = $"https://{host}:{server.Address().Port}/";
                    var page = await Browser.NewPageAsync(new(NewPageOptions())
                    {
                        IgnoreHTTPSErrors = true,
                        ClientCertificates =
                        [
                            new()
                            {
                                Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                                CertPath = Asset("client-certificates/client/self-signed/cert.pem"),
                                KeyPath = Asset("client-certificates/client/self-signed/key.pem"),
                            }
                        ],
                    });
                    await page.GotoAsync(serverURL);
                    await Expect(page.GetByText("Playwright client-certificate error: Client network socket disconnected before secure TLS connection was established")).ToBeVisibleAsync();
                    await page.CloseAsync();
                    await server.Close();
                });
            }
        }

        [UpstreamTest("CC-028 should pass with matching certificates in pfx format")]
        public async Task ShouldPassWithMatchingCertificatesInPfxFormat()
        {
            var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        PfxPath = Asset("client-certificates/client/trusted/cert.pfx"),
                        Passphrase = "secure",
                    }
                ],
            });
            await page.GotoAsync(serverURL);
            await Expect(page.GetByTestId("message")).ToHaveTextAsync("Hello Alice, your certificate was issued by localhost!");
            await page.CloseAsync();
        }

        [UpstreamTest("CC-030 should pass with matching certificates in pfx format when passing as content")]
        public async Task ShouldPassWithMatchingCertificatesInPfxFormatWhenPassingAsContent()
        {
            var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        Pfx = await File.ReadAllBytesAsync(Asset("client-certificates/client/trusted/cert.pfx")),
                        Passphrase = "secure",
                    }
                ],
            });
            await page.GotoAsync(serverURL);
            await Expect(page.GetByTestId("message")).ToHaveTextAsync("Hello Alice, your certificate was issued by localhost!");
            await page.CloseAsync();
        }

        [UpstreamTest("CC-031 should fail with matching certificates in legacy pfx format")]
        public async Task ShouldFailWithMatchingCertificatesInLegacyPfxFormat()
        {
            var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            var error = await ThrowsAsync<PlaywrightException>(() => Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        PfxPath = Asset("client-certificates/client/trusted/cert-legacy.pfx"),
                        Passphrase = "secure",
                    }
                ],
            }));
            Assert.That(error.Message, Does.Contain("Unsupported TLS certificate"));
        }

        [UpstreamTest("CC-032 should throw a http error if the pfx passphrase is incorect")]
        public async Task ShouldThrowAHttpErrorIfThePfxPassphraseIsIncorect()
        {
            var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            var error = await ThrowsAsync<PlaywrightException>(() => Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        PfxPath = Asset("client-certificates/client/trusted/cert.pfx"),
                        Passphrase = "this-password-is-incorrect",
                    }
                ],
            }));
            Assert.That(error.Message, Does.Contain("Failed to load client certificate: mac verify failure"));
        }

        [UpstreamTest("CC-033 should pass with matching certificates on context APIRequestContext instance")]
        public async Task ShouldPassWithMatchingCertificatesOnContextAPIRequestContextInstance()
        {
            var serverURL = await StartCCServer();
            var baseOptions = new ClientCertificate
            {
                CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                KeyPath = Asset("client-certificates/client/trusted/key.pem"),
            };
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                // .NET: the spread of baseOptions is its two properties.
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = baseOptions.CertPath,
                        KeyPath = baseOptions.KeyPath,
                    },
                    new()
                    {
                        Origin = ReplaceFirst(new Uri(serverURL).GetLeftPart(UriPartial.Authority), "localhost", "127.0.0.1"),
                        CertPath = baseOptions.CertPath,
                        KeyPath = baseOptions.KeyPath,
                    }
                ],
            });
            foreach (var url in new[] { serverURL, ReplaceFirst(serverURL, "localhost", "127.0.0.1") })
            {
                var response = await page.APIRequest.GetAsync(url);
                Assert.That(response.Status, Is.EqualTo(200));
                Assert.That(await response.TextAsync(), Does.Contain("Hello Alice, your certificate was issued by localhost!"));
            }
            await page.CloseAsync();
        }

        [UpstreamTest("CC-034 should pass with matching certificates and trailing slash")]
        public async Task ShouldPassWithMatchingCertificatesAndTrailingSlash()
        {
            var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = serverURL,
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            await page.GotoAsync(serverURL);
            await Expect(page.GetByText("Hello Alice, your certificate was issued by localhost!")).ToBeVisibleAsync();
            await page.CloseAsync();
        }

        [UpstreamTest("CC-035 should have ignoreHTTPSErrors=false by default")]
        public async Task ShouldHaveIgnoreHTTPSErrorsFalseByDefault()
        {
            var targetURL = BrowserName == "webkit" && Platform == "darwin" ? ReplaceFirst(HttpsServer.EmptyPage, "localhost", "local.playwright") : HttpsServer.EmptyPage;
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(targetURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            await page.GotoAsync(targetURL);
            await Expect(page.GetByText("Playwright client-certificate error: self-signed certificate")).ToBeVisibleAsync();
            await page.CloseAsync();
        }

        [UpstreamTest("CC-036 support http2")]
        public async Task SupportHttp2()
        {
            Info.Skip(BrowserName == "webkit" && IsMac, "WebKit on macOS doesn\n proxy localhost");
            var serverURL = await StartCCServer(new() { Http2 = true });
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            {
                await page.GotoAsync(ReplaceFirst(serverURL, "127.0.0.1", "local.playwright"));
                await Expect(page.GetByTestId("message")).ToHaveTextAsync("Sorry, but you need to provide a client certificate to continue.");
                await Expect(page.GetByTestId("alpn-protocol")).ToHaveTextAsync("h2");
                await Expect(page.GetByTestId("servername")).ToHaveTextAsync("local.playwright");
            }
            {
                await page.GotoAsync(serverURL);
                await Expect(page.GetByTestId("message")).ToHaveTextAsync("Hello Alice, your certificate was issued by localhost!");
                await Expect(page.GetByTestId("alpn-protocol")).ToHaveTextAsync("h2");
            }
            await page.CloseAsync();
        }

        [UpstreamTest("CC-037 support http2 if the browser only supports http1.1")]
        public async Task SupportHttp2IfTheBrowserOnlySupportsHttp11()
        {
            Info.Skip(BrowserName != "chromium");
            var serverURL = await StartCCServer(new() { Http2 = true, EnableHTTP1FallbackWhenUsingHttp2 = true });
            var browser = await BrowserType.LaunchAsync(new(LaunchOptions()) { Args = ["--disable-http2"] });
            var page = await browser.NewPageAsync(new(NewPageOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            {
                await page.GotoAsync(ReplaceFirst(serverURL, "127.0.0.1", "local.playwright"));
                await Expect(page.GetByTestId("message")).ToHaveTextAsync("Sorry, but you need to provide a client certificate to continue.");
                await Expect(page.GetByTestId("alpn-protocol")).ToHaveTextAsync("http/1.1");
            }
            {
                await page.GotoAsync(serverURL);
                await Expect(page.GetByTestId("message")).ToHaveTextAsync("Hello Alice, your certificate was issued by localhost!");
                await Expect(page.GetByTestId("alpn-protocol")).ToHaveTextAsync("http/1.1");
            }
            await browser.CloseAsync();
        }

        [UpstreamTest("CC-038 should return target connection errors when using http2")]
        public async Task ShouldReturnTargetConnectionErrorsWhenUsingHttp2()
        {
            Info.Skip(BrowserName == "webkit" && IsMac, "WebKit on macOS does not proxy localhost");
            Info.Skip(NodeVersion.Major < 20, "http2.performServerHandshake is not supported in older Node.js versions");

            var serverURL = await StartCCServer(new() { Http2 = true });
            var page = await Browser.NewPageAsync(new(NewPageOptions())
            {
                ClientCertificates =
                [
                    new()
                    {
                        Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });
            await page.GotoAsync(serverURL);
            await Expect(page.GetByText("Playwright client-certificate error: self-signed certificate")).ToBeVisibleAsync();
            await page.CloseAsync();
        }

        [UpstreamTest("CC-039 should handle rejected certificate in handshake with HTTP/2")]
        public async Task ShouldHandleRejectedCertificateInHandshakeWithHTTP2()
        {
            var server = CreateHttp2Server(new()
            {
                Key = File.ReadAllBytes(Asset("client-certificates/server/server_key.pem")),
                Cert = File.ReadAllBytes(Asset("client-certificates/server/server_cert.pem")),
                Ca = [File.ReadAllBytes(Asset("client-certificates/server/server_cert.pem"))],
                RequestCert = true,
            }, (req, res) =>
            {
                res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
                res.End("Hello world");
            });

            await server.Listen(0, "localhost");
            var port = server.Address().Port;
            var serverUrl = "https://" + (BrowserName == "webkit" && Platform == "darwin" ? "local.playwright" : "localhost") + ":" + port;

            var context = await Browser.NewContextAsync(new(ContextOptions())
            {
                IgnoreHTTPSErrors = true,
                ClientCertificates =
                [
                    new()
                    {
                        Origin = "https://just-there-that-the-client-certificates-proxy-server-is-getting-launched.com",
                        CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                        KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                    }
                ],
            });

            var page = await context.NewPageAsync();

            // This was triggering an unhandled error before.
            await Catch(() => page.GotoAsync(serverUrl));

            await context.CloseAsync();
            await server.Close();
        }

        public class PersistentContext : ClientCertificatesTests
        {
            [UpstreamTest("CC-040 validate input")]
            public async Task ValidateInput()
            {
                Info.Slow();
                foreach (var (clientCertificates, expected) in kValidationSubTests)
                {
                    var error = await ThrowsAsync<PlaywrightException>(() => LaunchPersistent(new() { ClientCertificates = clientCertificates }));
                    Assert.That(error.Message, Does.Contain(expected));
                }
            }

            [UpstreamTest("CC-041 should pass with matching certificates")]
            public async Task ShouldPassWithMatchingCertificates()
            {
                var serverURL = await StartCCServer(new() { UseFakeLocalhost = BrowserName == "webkit" && IsMac });
                var (_, page) = await LaunchPersistent(new()
                {
                    IgnoreHTTPSErrors = true,
                    ClientCertificates =
                    [
                        new()
                        {
                            Origin = new Uri(serverURL).GetLeftPart(UriPartial.Authority),
                            CertPath = Asset("client-certificates/client/trusted/cert.pem"),
                            KeyPath = Asset("client-certificates/client/trusted/key.pem"),
                        }
                    ],
                });
                await page.GotoAsync(serverURL);
                await Expect(page.GetByTestId("message")).ToHaveTextAsync("Hello Alice, your certificate was issued by localhost!");
            }
        }
    }

    // .NET: the rest of the class is the servers that upstream makes with createHttpsServer(), createHttp2Server()
    // and tls.createServer() of Node.js. The first two run on Kestrel, the third on an SslStream. They have the
    // options and the members that the tests of this file use, under the names of Node.js.
    private static TlsServer CreateHttpsServer(TlsServerOptions options, Action<TlsServerRequest, TlsServerResponse> requestListener) =>
        CreateKestrelServer(options, HttpProtocols.Http1, requestListener);

    private static TlsServer CreateHttp2Server(TlsServerOptions options, Action<TlsServerRequest, TlsServerResponse> requestListener) =>
        CreateKestrelServer(options, options.AllowHTTP1 == true ? HttpProtocols.Http1AndHttp2 : HttpProtocols.Http2, requestListener);

    private static TlsServer CreateKestrelServer(TlsServerOptions options, HttpProtocols protocols, Action<TlsServerRequest, TlsServerResponse> requestListener)
    {
        var ca = options.LoadCa();
        var createSslOptions = options.LoadSslOptions(ca);
        return new TlsServer(async address =>
        {
            var host = new WebHostBuilder()
                .UseKestrel(kestrel =>
                {
                    // res.End(data) writes the body before it returns.
                    kestrel.AllowSynchronousIO = true;
                    kestrel.Listen(address, 0, listen =>
                    {
                        listen.Protocols = protocols;
                        listen.UseHttps(new TlsHandshakeCallbackOptions
                        {
                            OnConnection = _ => ValueTask.FromResult(createSslOptions()),
                        });
                    });
                })
                .Configure(app => app.Run(context =>
                {
                    requestListener(new TlsServerRequest(new TlsSocket(context, ca)), new TlsServerResponse(context.Response));
                    return Task.CompletedTask;
                }))
                .Build();
            await host.StartAsync();
            var port = new Uri(host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.Single()).Port;
            return (port, async () =>
            {
                await host.StopAsync();
                host.Dispose();
            }
            );
        });
    }

    private static TlsServer CreateTlsServer(TlsServerOptions options, Action secureConnectionListener)
    {
        var createSslOptions = options.LoadSslOptions(options.LoadCa());
        return new TlsServer(address =>
        {
            var listener = new TcpListener(address, 0);
            listener.Start();

            async Task ServeAsync(TcpClient client)
            {
                using (client)
                {
                    using var socket = new SslStream(client.GetStream());
                    // A callback that throws leaves the handshake without an answer, and the connection is closed.
                    await socket.AuthenticateAsServerAsync(
                        (_, clientHello, _, _) =>
                        {
                            if (!string.IsNullOrEmpty(clientHello.ServerName))
                            {
                                options.SNICallback?.Invoke(clientHello.ServerName);
                            }
                            return ValueTask.FromResult(createSslOptions());
                        },
                        null);
                    secureConnectionListener();
                    while (await socket.ReadAsync(new byte[1024]) > 0)
                    {
                    }
                }
            }

            async Task AcceptLoopAsync()
            {
                while (true)
                {
                    IgnoreErrors(ServeAsync(await listener.AcceptTcpClientAsync()));
                }
            }

            IgnoreErrors(AcceptLoopAsync());
            return Task.FromResult<(int, Func<Task>)>((((IPEndPoint)listener.LocalEndpoint).Port, () =>
            {
                listener.Stop();
                return Task.CompletedTask;
            }
            ));
        });
    }

    // tlsSocket.authorized: the certificate of the peer is signed by one of the 'ca' certificates.
    private static bool IsAuthorized(X509Certificate certificate, X509Certificate2[] ca)
    {
        if (certificate == null)
        {
            return false;
        }
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.AddRange(ca);
        return chain.Build(certificate as X509Certificate2 ?? new X509Certificate2(certificate));
    }

    private sealed class TlsServerOptions
    {
        public byte[] Key { get; set; }

        public byte[] Cert { get; set; }

        public byte[][] Ca { get; set; }

        public bool RequestCert { get; set; }

        public bool? RejectUnauthorized { get; set; }

        public bool? AllowHTTP1 { get; set; }

        public string MinVersion { get; set; }

        public string MaxVersion { get; set; }

        /// <summary><c>SNICallback(servername, cb)</c>: throws where upstream passes an error to <c>cb</c>.</summary>
        public Action<string> SNICallback { get; set; }

        public X509Certificate2[] LoadCa() => (Ca ?? []).Select(pem => X509Certificate2.CreateFromPem(Encoding.ASCII.GetString(pem))).ToArray();

        /// <summary>Makes the options of the handshake of a connection.</summary>
        public Func<SslServerAuthenticationOptions> LoadSslOptions(X509Certificate2[] ca)
        {
            using var pem = X509Certificate2.CreateFromPem(Encoding.ASCII.GetString(Cert), Encoding.ASCII.GetString(Key));
            // SslStream on Windows and macOS needs a key that is not ephemeral.
            var certificate = new X509Certificate2(pem.Export(X509ContentType.Pfx));
            // Node.js sends the names of the 'ca' certificates with the request for the client certificate.
            // Firefox does not answer a request without names. SslStream can not send the names of a collection on Windows.
            var trust = ca.Length == 0 || OperatingSystem.IsWindows() ? null : SslCertificateTrust.CreateForX509Collection(new X509Certificate2Collection(ca), sendTrustInHandshake: true);
            var certificateContext = SslStreamCertificateContext.Create(certificate, null, offline: true, trust);
            // rejectUnauthorized is true when not set, and it only has an effect with requestCert.
            var rejectUnauthorized = RequestCert && (RejectUnauthorized ?? true);
            var versions = new[] { ("TLSv1.2", SslProtocols.Tls12), ("TLSv1.3", SslProtocols.Tls13) };
            var min = MinVersion == null ? 0 : Array.FindIndex(versions, version => version.Item1 == MinVersion);
            var max = MaxVersion == null ? versions.Length - 1 : Array.FindIndex(versions, version => version.Item1 == MaxVersion);
            var protocols = MinVersion == null && MaxVersion == null ? SslProtocols.None : versions[min..(max + 1)].Aggregate(SslProtocols.None, (all, version) => all | version.Item2);
            return () => new SslServerAuthenticationOptions
            {
                ServerCertificateContext = certificateContext,
                EnabledSslProtocols = protocols,
                ClientCertificateRequired = RequestCert,
                RemoteCertificateValidationCallback = (_, clientCertificate, _, _) => !rejectUnauthorized || IsAuthorized(clientCertificate, ca),
            };
        }
    }

    /// <summary><c>https.Server</c>, <c>http2.Http2SecureServer</c> or <c>tls.Server</c>.</summary>
    private sealed class TlsServer
    {
        private readonly Func<IPAddress, Task<(int Port, Func<Task> Close)>> _listen;
        private (int Port, Func<Task> Close) _listening;

        public TlsServer(Func<IPAddress, Task<(int Port, Func<Task> Close)>> listen)
        {
            _listen = listen;
        }

        /// <summary><c>server.listen(0, host, callback)</c>.</summary>
        public async Task Listen(int port, string host)
        {
            if (port != 0)
            {
                throw new ArgumentException("Only port 0 is supported", nameof(port));
            }
            _listening = await _listen(host == "localhost" ? IPAddress.Loopback : IPAddress.Parse(host));
        }

        /// <summary><c>server.address()</c>.</summary>
        public AddressInfo Address() => new(_listening.Port);

        /// <summary><c>server.close(callback)</c>.</summary>
        public Task Close() => _listening.Close();
    }

    private sealed record AddressInfo(int Port);

    private sealed class TlsServerRequest
    {
        public TlsServerRequest(TlsSocket socket)
        {
            Socket = socket;
        }

        public TlsSocket Socket { get; }
    }

    private sealed class TlsServerResponse
    {
        private readonly HttpResponse _response;

        public TlsServerResponse(HttpResponse response)
        {
            _response = response;
        }

        public void WriteHead(int statusCode, Dictionary<string, string> headers = null)
        {
            _response.StatusCode = statusCode;
            foreach (var (name, value) in headers ?? [])
            {
                _response.Headers[name] = value;
            }
        }

        public void End(string data = null)
        {
            if (data != null)
            {
                _response.Body.Write(Encoding.UTF8.GetBytes(data));
            }
        }
    }

    /// <summary><c>req.socket</c>, a <c>tls.TLSSocket</c>.</summary>
    private sealed class TlsSocket
    {
        private readonly HttpContext _context;
        private readonly X509Certificate2[] _ca;

        public TlsSocket(HttpContext context, X509Certificate2[] ca)
        {
            _context = context;
            _ca = ca;
        }

        /// <summary>The text of <c>tlsSocket.alpnProtocol</c>: 'false' when no protocol was negotiated.</summary>
        public string AlpnProtocol
        {
            get
            {
                var protocol = _context.Features.Get<ITlsApplicationProtocolFeature>()?.ApplicationProtocol ?? default;
                return protocol.IsEmpty ? "false" : Encoding.ASCII.GetString(protocol.Span);
            }
        }

        /// <summary>The text of <c>tlsSocket.servername</c>: 'false' when the client sent no server name.</summary>
        public string Servername
        {
            get
            {
                var hostName = _context.Features.Get<ITlsHandshakeFeature>()?.HostName;
                return string.IsNullOrEmpty(hostName) ? "false" : hostName;
            }
        }

        public bool Authorized => IsAuthorized(_context.Connection.ClientCertificate, _ca);

        /// <summary><c>tlsSocket.getPeerCertificate()</c>: an object without a subject when the client sent no certificate.</summary>
        public PeerCertificate GetPeerCertificate()
        {
            var certificate = _context.Connection.ClientCertificate;
            return certificate == null
                ? new PeerCertificate(null, null)
                : new PeerCertificate(new(certificate.GetNameInfo(X509NameType.SimpleName, false)), new(certificate.GetNameInfo(X509NameType.SimpleName, true)));
        }
    }

    private sealed record PeerCertificate(PeerCertificateName Subject, PeerCertificateName Issuer);

    private sealed record PeerCertificateName(string CN);
}
