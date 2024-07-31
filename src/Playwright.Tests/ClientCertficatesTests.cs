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

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Microsoft.Playwright.Tests;

public class ClientCertificatesTests : BrowserTestEx
{
    private IWebHost _webHost;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        var serverCert = X509Certificate2.CreateFromPem(File.ReadAllText(TestUtils.GetAsset("client-certificates/server/server_cert.pem")), File.ReadAllText(TestUtils.GetAsset("client-certificates/server/server_key.pem")));
        // Windows requires the private key in PFX format: https://stackoverflow.com/a/72101855/6512681
        serverCert = new X509Certificate2(serverCert.Export(X509ContentType.Pfx));
        _webHost = new WebHostBuilder()
            .Configure((app) => app
                .Use(middleware: async (HttpContext context, Func<Task> next) =>
                {
                    var clientCertificate = await context.Connection.GetClientCertificateAsync();
                    if (clientCertificate == null)
                    {
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync("Forbidden: No client certificate provided");
                        return;
                    }

                    // Create a chain for the client certificate
                    using (var chain = new X509Chain())
                    {
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                        chain.ChainPolicy.ExtraStore.Add(serverCert);

                        bool isValid = chain.Build(clientCertificate);

                        if (!isValid || chain.ChainElements.Count <= 1 || !chain.ChainElements[1].Certificate.Equals(serverCert))
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsync("Forbidden: Invalid client certificate");
                            return;
                        }

                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync($"Hello {clientCertificate.Subject}");
                    }
                }))
            .UseKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, 10000, listenOptions =>
                {
                    listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                    {
                        ServerCertificate = serverCert,
                        ClientCertificateMode = ClientCertificateMode.AllowCertificate,
                        // Actual client certificate validation is done in the middleware
                        ClientCertificateValidation = (cert, chain, errors) => true,
                    });
                });
            })
            .Build();
        await _webHost.StartAsync();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await _webHost.StopAsync();
    }

    [SetUp]
    public void Setup()
    {
        if (TestConstants.IsWebKit && TestConstants.IsMacOSX)
        {
            Assert.Ignore("WebKit on macOS doesn't proxy localhost requests");
        }
    }

    [PlaywrightTest("", "")]
    public async Task ShouldWorkWithNewContext()
    {
        var context = await Browser.NewContextAsync(new()
        {
            // TODO: Remove this once we can pass a custom CA.
            IgnoreHTTPSErrors = true,
            ClientCertificates =
            [
                new()
                {
                    Origin = "https://localhost:10000",
                    CertPath = TestUtils.GetAsset("client-certificates/client/trusted/cert.pem"),
                    KeyPath = TestUtils.GetAsset("client-certificates/client/trusted/key.pem"),
                }
            ]
        });
        var page = await context.NewPageAsync();
        {
            await page.GotoAsync("https://127.0.0.1:10000");
            await Expect(page.GetByText("Forbidden: No client certificate provided")).ToBeVisibleAsync();

            var response = await page.APIRequest.GetAsync("https://127.0.0.1:10000");
            Assert.AreEqual("Forbidden: No client certificate provided", await response.TextAsync());
        }
        {
            await page.GotoAsync("https://localhost:10000");
            await Expect(page.GetByText("Hello CN=Alice")).ToBeVisibleAsync();

            var response = await page.APIRequest.GetAsync("https://localhost:10000");
            Assert.AreEqual("Hello CN=Alice", await response.TextAsync());
        }
        await context.CloseAsync();
    }

    [PlaywrightTest("", "")]
    public async Task ShouldWorkWithNewPage()
    {
        var page = await Browser.NewPageAsync(new()
        {
            // TODO: Remove this once we can pass a custom CA.
            IgnoreHTTPSErrors = true,
            ClientCertificates =
            [
                new()
                {
                    Origin = "https://localhost:10000",
                    CertPath = TestUtils.GetAsset("client-certificates/client/trusted/cert.pem"),
                    KeyPath = TestUtils.GetAsset("client-certificates/client/trusted/key.pem"),
                }
            ]
        });
        {
            await page.GotoAsync("https://127.0.0.1:10000");
            await Expect(page.GetByText("Forbidden: No client certificate provided")).ToBeVisibleAsync();
        }
        {
            await page.GotoAsync("https://localhost:10000");
            await Expect(page.GetByText("Hello CN=Alice")).ToBeVisibleAsync();
        }
        await page.CloseAsync();
    }

    [PlaywrightTest("", "")]
    public async Task ShouldWorkWithLaunchPersistentContext()
    {
        var context = await BrowserType.LaunchPersistentContextAsync("", new()
        {
            // TODO: Remove this once we can pass a custom CA.
            IgnoreHTTPSErrors = true,
            ClientCertificates =
            [
                new()
                {
                    Origin = "https://localhost:10000",
                    CertPath = TestUtils.GetAsset("client-certificates/client/trusted/cert.pem"),
                    KeyPath = TestUtils.GetAsset("client-certificates/client/trusted/key.pem"),
                }
            ]
        });
        var page = context.Pages[0];
        {
            await page.GotoAsync("https://127.0.0.1:10000");
            await Expect(page.GetByText("Forbidden: No client certificate provided")).ToBeVisibleAsync();
        }
        {
            await page.GotoAsync("https://localhost:10000");
            await Expect(page.GetByText("Hello CN=Alice")).ToBeVisibleAsync();
        }
        await context.CloseAsync();
    }

    [PlaywrightTest("", "")]
    public async Task ShouldWorkWithAPIRequestContext()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new()
        {
            // TODO: Remove this once we can pass a custom CA.
            IgnoreHTTPSErrors = true,
            ClientCertificates =
            [
                new()
                {
                    Origin = "https://localhost:10000",
                    CertPath = TestUtils.GetAsset("client-certificates/client/trusted/cert.pem"),
                    KeyPath = TestUtils.GetAsset("client-certificates/client/trusted/key.pem"),
                }
            ]
        });
        {
            var response = await request.GetAsync("https://127.0.0.1:10000");
            Assert.AreEqual("Forbidden: No client certificate provided", await response.TextAsync());
        }
        {
            var response = await request.GetAsync("https://localhost:10000");
            Assert.AreEqual("Hello CN=Alice", await response.TextAsync());
        }
        await request.DisposeAsync();
    }
}
