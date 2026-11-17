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

using System.Text;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-proxy.spec.ts.</summary>
public class BrowsercontextProxyTests
{
    [BeforeEach]
    public static void BeforeEach(Server server)
    {
        server.SetRoute("/target.html", (req, res) =>
        {
            res.End("<html><title>Served by the proxy</title></html>");
        });
    }

    [UpstreamTest("PR-018 should work when passing the proxy only on the context level")]
    public async Task ShouldWorkWhenPassingTheProxyOnlyOnTheContextLevel(string browserName, string platform, IBrowserType browserType, Server server, TestProxy proxyServer, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        proxyServer.ForwardTo(server.Port);
        IBrowser browser = null;
        try
        {
            browser = await browserType.LaunchAsync(new(launchOptions)
            {
                Proxy = null,
            });
            var context = await browser.NewContextAsync(new(contextOptions)
            {
                Proxy = new() { Server = proxyServer.Host },
            });

            var page = await context.NewPageAsync();
            await page.GotoAsync("http://non-existent.test/target.html");
            Assert.That(proxyServer.RequestUrls, Does.Contain("http://non-existent.test/target.html"));
            Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
        }
        finally
        {
            await browser.CloseAsync();
        }
    }

    [UpstreamTest("PR-019 should ignore legacy '${launchProxy}' launch proxy")]
    [Case("per-context")]
    [Case("http://per-context")]
    public async Task ShouldIgnoreLegacyLaunchProxy(string launchProxy, IBrowserType browserType, Server server, TestProxy proxyServer, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        proxyServer.ForwardTo(server.Port, new() { AllowConnectRequests = true });
        var browser = await browserType.LaunchAsync(new(launchOptions) { Proxy = new() { Server = launchProxy } });
        try
        {
            var context = await browser.NewContextAsync(new(contextOptions) { Proxy = new() { Server = proxyServer.Host } });
            var page = await context.NewPageAsync();
            await page.GotoAsync("http://non-existent.test/target.html");
            Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
            var response = await context.APIRequest.GetAsync("http://non-existent.test/target.html");
            Assert.That(await response.TextAsync(), Does.Contain("Served by the proxy"));
            Assert.That(proxyServer.ConnectHosts, Does.Contain("non-existent.test:80"));

            var directContext = await browser.NewContextAsync(contextOptions);
            var directPage = await directContext.NewPageAsync();
            await directPage.GotoAsync(server.Prefix + "/target.html");
            Assert.That(await directPage.TitleAsync(), Is.EqualTo("Served by the proxy"));
            var directResponse = await directContext.APIRequest.GetAsync(server.Prefix + "/target.html");
            Assert.That(directResponse.Ok, Is.EqualTo(true));
        }
        finally
        {
            await browser.CloseAsync();
        }
    }

    [UpstreamTest("PR-021 should use proxy")]
    public async Task ShouldUseProxy(ContextFactory contextFactory, Server server, TestProxy proxyServer)
    {
        proxyServer.ForwardTo(server.Port);
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = proxyServer.Host },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync("http://non-existent.test/target.html");
        Assert.That(proxyServer.RequestUrls, Does.Contain("http://non-existent.test/target.html"));
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
        await context.CloseAsync();
    }

    [UpstreamTest("PR-022 should send secure cookies to subdomain.localhost")]
    public async Task ShouldSendSecureCookiesToSubdomainLocalhost(ContextFactory contextFactory, string browserName, Server server, bool isWindows, TestProxy proxyServer, string channel)
    {
        proxyServer.ForwardTo(server.Port);
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = proxyServer.Host },
        });
        server.SetRoute("/set-cookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "non-secure=1; HttpOnly", "secure=1; HttpOnly; Secure" });
            res.End();
        });
        server.SetRoute("/read-cookie.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End($"<div>Cookie: {string.Join("; ", req.Headers["cookie"].Split(';').Select(c => c.Trim()).Order(StringComparer.Ordinal))}</div>");
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync("http://subdomain.localhost/set-cookie.html");

        var cookies = await context.CookiesAsync("http://subdomain.localhost");
        ExpectToEqual(cookies.Select(cookie => new { name = cookie.Name, domain = cookie.Domain }), (object[])[
            new
            {
                name = "non-secure",
                domain = "subdomain.localhost",
            },
            .. (browserName == "webkit" && (!isWindows || channel == "webkit-wsl")) ? Array.Empty<object>() : new object[] { new
            {
                name = "secure",
                domain = "subdomain.localhost",
            } },
        ]);

        await page.GotoAsync("http://subdomain.localhost/read-cookie.html");
        await Expect(page.Locator("div")).ToHaveTextAsync(browserName == "webkit" ? "Cookie: non-secure=1" : "Cookie: non-secure=1; secure=1");

        await context.CloseAsync();
    }

    [UpstreamTest("PR-023 should set cookie for top-level domain")]
    public async Task ShouldSetCookieForTopLevelDomain(ContextFactory contextFactory, Server server, TestProxy proxyServer, string browserName, bool isLinux, string channel)
    {
        It.Fixme(browserName == "webkit" && (isLinux || channel == "webkit-wsl"));

        proxyServer.ForwardTo(server.Port, new() { AllowConnectRequests = true });
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = proxyServer.Host },
        });
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "name=val; Domain=codes; Path=/;");
            res.End();
        });

        await context.APIRequest.GetAsync("http://codes/empty.html");
        var cookie = (await context.CookiesAsync()).FirstOrDefault();
        Assert.That(cookie, Is.Not.Null);
        Assert.That(cookie.Name, Is.EqualTo("name"));
        Assert.That(cookie.Value, Is.EqualTo("val"));
        await context.CloseAsync();
    }

    public class ShouldProxyLocalNetworkRequests
    {
        [TestFixture(false)]
        [TestFixture(true)]
        public class Describe
        {
            private readonly bool additionalBypass;

            public Describe(bool additionalBypass)
            {
                this.additionalBypass = additionalBypass;
            }

            // .NET: the values are params.target and params.description.
            [UpstreamTest("PR-024 ${params.description}")]
            [Case("localhost", "localhost")]
            [Case("127.0.0.1", "loopback address")]
            [Case("169.254.3.4", "link-local")]
            public async Task Test(string target, string description, string platform, string browserName, ContextFactory contextFactory, Server server, TestProxy proxyServer)
            {
                It.Skip(browserName == "webkit" && platform == "darwin" && new[] { "localhost", "127.0.0.1" }.Contains(target) && additionalBypass, "Mac webkit does not proxy localhost when bypass rules are set");

                var path = $"/target-{(additionalBypass ? "true" : "false")}-{target}.html";
                server.SetRoute(path, (req, res) =>
                {
                    res.End("<html><title>Served by the proxy</title></html>");
                });

                var url = $"http://{target}:55555{path}";
                proxyServer.ForwardTo(server.Port);
                var context = await contextFactory(new()
                {
                    Proxy = new() { Server = proxyServer.Host, Bypass = additionalBypass ? "1.non.existent.domain.for.the.test" : null },
                });

                var page = await context.NewPageAsync();
                await page.GotoAsync(url);
                Assert.That(proxyServer.RequestUrls, Does.Contain(url));
                Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));

                await Catch(() => page.GotoAsync("http://1.non.existent.domain.for.the.test/foo.html"));
                if (additionalBypass)
                {
                    Assert.That(proxyServer.RequestUrls, Does.Not.Contain("http://1.non.existent.domain.for.the.test/foo.html"));
                }
                else
                {
                    Assert.That(proxyServer.RequestUrls, Does.Contain("http://1.non.existent.domain.for.the.test/foo.html"));
                }

                await context.CloseAsync();
            }
        }
    }

    [UpstreamTest("PR-025 should use ipv6 proxy")]
    public async Task ShouldUseIpv6Proxy(ContextFactory contextFactory, Server server, TestProxy proxyServer, string browserName, string channel, bool isBidi)
    {
        It.Fixme(browserName == "firefox" && !isBidi, "page.goto: NS_ERROR_UNKNOWN_HOST");
        It.Fixme(channel == "webkit-wsl", "WebKit on WSL does not support IPv6: https://github.com/microsoft/WSL/issues/10803");
        proxyServer.ForwardTo(server.Port);
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = $"[0:0:0:0:0:0:0:1]:{proxyServer.Port}" },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync("http://non-existent.test/target.html");
        Assert.That(proxyServer.RequestUrls, Does.Contain("http://non-existent.test/target.html"));
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
        await context.CloseAsync();
    }

    [UpstreamTest("PR-026 should use proxy twice")]
    public async Task ShouldUseProxyTwice(ContextFactory contextFactory, Server server, TestProxy proxyServer)
    {
        proxyServer.ForwardTo(server.Port);
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = proxyServer.Host },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync("http://non-existent.test/target.html");
        Assert.That(proxyServer.RequestUrls, Does.Contain("http://non-existent.test/target.html"));
        await page.GotoAsync("http://non-existent-2.test/target.html");
        Assert.That(proxyServer.RequestUrls, Does.Contain("http://non-existent-2.test/target.html"));
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
        await context.CloseAsync();
    }

    [UpstreamTest("PR-027 should use proxy for second page")]
    public async Task ShouldUseProxyForSecondPage(ContextFactory contextFactory, Server server, TestProxy proxyServer)
    {
        proxyServer.ForwardTo(server.Port);
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = proxyServer.Host },
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync("http://non-existent.test/target.html");
        Assert.That(proxyServer.RequestUrls, Does.Contain("http://non-existent.test/target.html"));
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));

        var page2 = await context.NewPageAsync();
        proxyServer.RequestUrls = [];
        await page2.GotoAsync("http://non-existent.test/target.html");
        Assert.That(proxyServer.RequestUrls, Does.Contain("http://non-existent.test/target.html"));
        Assert.That(await page2.TitleAsync(), Is.EqualTo("Served by the proxy"));

        await context.CloseAsync();
    }

    [UpstreamTest("PR-028 should use proxy for https urls")]
    public async Task ShouldUseProxyForHttpsUrls(ContextFactory contextFactory, Server httpsServer, TestProxy proxyServer)
    {
        httpsServer.SetRoute("/target.html", (req, res) =>
        {
            res.End("<html><title>Served by https server via proxy</title></html>");
        });
        proxyServer.ForwardTo(httpsServer.Port, new() { AllowConnectRequests = true });
        var context = await contextFactory(new()
        {
            IgnoreHTTPSErrors = true,
            Proxy = new() { Server = proxyServer.Host },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync("https://non-existent.test/target.html");
        Assert.That(proxyServer.ConnectHosts, Does.Contain("non-existent.test:443"));
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by https server via proxy"));
        await context.CloseAsync();
    }

    [UpstreamTest("PR-029 should work with IP:PORT notion")]
    public async Task ShouldWorkWithIPPORTNotion(ContextFactory contextFactory, Server server, TestProxy proxyServer)
    {
        proxyServer.ForwardTo(server.Port);
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = $"127.0.0.1:{proxyServer.Port}" },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync("http://non-existent.test/target.html");
        Assert.That(proxyServer.RequestUrls, Does.Contain("http://non-existent.test/target.html"));
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
        await context.CloseAsync();
    }

    [UpstreamTest("PR-030 should throw for socks5 authentication")]
    public async Task ShouldThrowForSocks5Authentication(ContextFactory contextFactory)
    {
        var error = await Catch(() => contextFactory(new()
        {
            Proxy = new() { Server = "socks5://localhost:1234", Username = "user", Password = "secret" },
        }));
        Assert.That(error.Message, Does.Contain("Browser does not support socks5 proxy authentication"));
    }

    [UpstreamTest("PR-031 should throw for socks4 authentication")]
    public async Task ShouldThrowForSocks4Authentication(ContextFactory contextFactory)
    {
        var error = await Catch(() => contextFactory(new()
        {
            Proxy = new() { Server = "socks4://localhost:1234", Username = "user", Password = "secret" },
        }));
        Assert.That(error.Message, Does.Contain("Socks4 proxy protocol does not support authentication"));
    }

    [UpstreamTest("PR-032 should authenticate")]
    public async Task ShouldAuthenticate(ContextFactory contextFactory, Server server, TestProxy proxyServer)
    {
        proxyServer.ForwardTo(server.Port);
        string auth = null;
        proxyServer.SetAuthHandler(req =>
        {
            auth = req.Headers["proxy-authorization"];
            return !string.IsNullOrEmpty(auth);
        });
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = proxyServer.Host, Username = "user", Password = "secret" },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync("http://non-existent.test/target.html");
        Assert.That(proxyServer.RequestUrls, Does.Contain("http://non-existent.test/target.html"));
        Assert.That(auth, Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:secret"))));
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
        await context.CloseAsync();
    }

    [UpstreamTest("PR-033 should authenticate with empty password")]
    public async Task ShouldAuthenticateWithEmptyPassword(ContextFactory contextFactory, Server server, TestProxy proxyServer)
    {
        proxyServer.ForwardTo(server.Port);
        string auth = null;
        proxyServer.SetAuthHandler(req =>
        {
            auth = req.Headers["proxy-authorization"];
            return !string.IsNullOrEmpty(auth);
        });
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = proxyServer.Host, Username = "user", Password = "" },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync("http://non-existent.test/target.html");
        Assert.That(auth, Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:"))));
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
        await context.CloseAsync();
    }

    [UpstreamTest("PR-034 should isolate proxy credentials between contexts")]
    public async Task ShouldIsolateProxyCredentialsBetweenContexts(ContextFactory contextFactory, Server server, string browserName, TestProxy proxyServer)
    {
        proxyServer.ForwardTo(server.Port);
        string auth = null;
        proxyServer.SetAuthHandler(req =>
        {
            auth = req.Headers["proxy-authorization"];
            return !string.IsNullOrEmpty(auth);
        });
        {
            var context = await contextFactory(new()
            {
                Proxy = new() { Server = proxyServer.Host, Username = "user1", Password = "secret1" },
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync("http://non-existent.test/target.html");
            Assert.That(auth, Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user1:secret1"))));
            Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
            await context.CloseAsync();
        }
        auth = null;
        {
            var context = await contextFactory(new()
            {
                Proxy = new() { Server = proxyServer.Host, Username = "user2", Password = "secret2" },
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync("http://non-existent.test/target.html");
            Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
            Assert.That(auth, Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user2:secret2"))));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("PR-035 should exclude patterns")]
    public async Task ShouldExcludePatterns(ContextFactory contextFactory, Server server, TestProxy proxyServer)
    {
        proxyServer.ForwardTo(server.Port);
        // FYI: using long and weird domain names to avoid ATT DNS hijacking
        // that resolves everything to some weird search results page.
        //
        // @see https://gist.github.com/CollinChaffin/24f6c9652efb3d6d5ef2f5502720ef00
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = proxyServer.Host, Bypass = "1.non.existent.domain.for.the.test, 2.non.existent.domain.for.the.test, .another.test" },
        });

        var nonFaviconUrls = () =>
        {
            return proxyServer.RequestUrls.Where(u => !u.Contains("favicon")).ToArray();
        };

        {
            proxyServer.RequestUrls = [];
            var page = await context.NewPageAsync();
            await page.GotoAsync("http://0.non.existent.domain.for.the.test/target.html");
            Assert.That(proxyServer.RequestUrls, Does.Contain("http://0.non.existent.domain.for.the.test/target.html"));
            Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
            await page.CloseAsync();
        }

        {
            proxyServer.RequestUrls = [];
            var page = await context.NewPageAsync();
            var error = await Catch(() => page.GotoAsync("http://1.non.existent.domain.for.the.test/target.html"));
            Assert.That(nonFaviconUrls(), Is.EqualTo(Array.Empty<string>()));
            Assert.That(error.Message, Is.Not.Empty);
            await page.CloseAsync();
        }

        {
            proxyServer.RequestUrls = [];
            var page = await context.NewPageAsync();
            var error = await Catch(() => page.GotoAsync("http://2.non.existent.domain.for.the.test/target.html"));
            Assert.That(nonFaviconUrls(), Is.EqualTo(Array.Empty<string>()));
            Assert.That(error.Message, Is.Not.Empty);
            await page.CloseAsync();
        }

        {
            proxyServer.RequestUrls = [];
            var page = await context.NewPageAsync();
            var error = await Catch(() => page.GotoAsync("http://foo.is.the.another.test/target.html"));
            Assert.That(nonFaviconUrls(), Is.EqualTo(Array.Empty<string>()));
            Assert.That(error.Message, Is.Not.Empty);
            await page.CloseAsync();
        }

        {
            proxyServer.RequestUrls = [];
            var page = await context.NewPageAsync();
            await page.GotoAsync("http://3.non.existent.domain.for.the.test/target.html");
            Assert.That(nonFaviconUrls(), Does.Contain("http://3.non.existent.domain.for.the.test/target.html"));
            Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the proxy"));
            await page.CloseAsync();
        }

        await context.CloseAsync();
    }

    [UpstreamTest("PR-036 should use socks proxy")]
    public async Task ShouldUseSocksProxy(ContextFactory contextFactory, int socksPort)
    {
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = $"socks5://localhost:{socksPort}" },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync("http://non-existent.test");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the SOCKS proxy"));
        await context.CloseAsync();
    }

    [UpstreamTest("PR-037 should use socks proxy in second page")]
    public async Task ShouldUseSocksProxyInSecondPage(ContextFactory contextFactory, int socksPort)
    {
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = $"socks5://localhost:{socksPort}" },
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync("http://non-existent.test");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Served by the SOCKS proxy"));

        var page2 = await context.NewPageAsync();
        await page2.GotoAsync("http://non-existent.test");
        Assert.That(await page2.TitleAsync(), Is.EqualTo("Served by the SOCKS proxy"));

        await context.CloseAsync();
    }

    [UpstreamTest("PR-038 does launch without a port")]
    public async Task DoesLaunchWithoutAPort(ContextFactory contextFactory)
    {
        var context = await contextFactory(new()
        {
            Proxy = new() { Server = "http://localhost" },
        });
        await context.CloseAsync();
    }

    [UpstreamTest("PR-039 should isolate proxy credentials between contexts on navigation")]
    public async Task ShouldIsolateProxyCredentialsBetweenContextsOnNavigation(ContextFactory contextFactory, Server server)
    {
        server.SetRoute("/target.html", (req, res) =>
        {
            var authHeader = req.Headers["proxy-authorization"];

            if (string.IsNullOrEmpty(authHeader))
            {
                res.WriteHead(407, new() { ["Proxy-Authenticate"] = "Basic realm=\"proxy\"" });
                res.End("Proxy authorization required");
                return;
            }

            var username = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Split(' ')[1])).Split(':')[0];
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End($"Hello <div data-testid=user>{username}</div>!\n");
        });

        var context1 = await contextFactory(new()
        {
            Proxy = new() { Server = server.Prefix, Username = "user1", Password = "secret1" },
        });
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync("http://non-existent.test/target.html");
        await Expect(page1.GetByTestId("user")).ToHaveTextAsync("user1");

        var context2 = await contextFactory(new()
        {
            Proxy = new() { Server = server.Prefix, Username = "user2", Password = "secret2" },
        });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync("http://non-existent.test/target.html");
        await Expect(page2.GetByTestId("user")).ToHaveTextAsync("user2");

        await page1.GotoAsync("http://non-existent.test/target.html");
        await Expect(page1.GetByTestId("user")).ToHaveTextAsync("user1");
    }
}
