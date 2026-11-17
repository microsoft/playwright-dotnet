/**
 * Copyright Microsoft Corporation. All rights reserved.
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

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-cookies-third-party.spec.ts.</summary>
public class BrowsercontextCookiesThirdPartyTests
{
    public sealed class TestUrls
    {
        public string origin1;

        public string origin2;

        public string read_origin1;

        public string read_origin2_origin1;

        public string read_origin1_origin1;

        public string read_origin1_origin2_origin1;

        public string set_origin1;

        public string set_origin2_origin1;

        public string set_origin1_origin2_origin1;
    }

    [Fixture]
    public static TestUrls Urls(Server httpsServer)
    {
        var origin1 = httpsServer.Prefix;
        var origin2 = httpsServer.CrossProcessPrefix;
        return new TestUrls
        {
            origin1 = origin1,
            origin2 = origin2,
            read_origin1 = origin1 + "/read-cookie.html",
            read_origin2_origin1 = origin2 + "/frame-read-cookie.html",
            read_origin1_origin1 = origin1 + "/frame-read-cookie.html",
            read_origin1_origin2_origin1 = origin1 + "/nested-frame-read-cookie.html",
            set_origin1 = origin1 + "/set-cookie.html",
            set_origin2_origin1 = origin2 + "/frame-set-cookie.html",
            set_origin1_origin2_origin1 = origin1 + "/nested-frame-set-cookie.html",
        };
    }

    [Fixture]
    public static bool WebkitPartitions(string browserName, bool isMac, int macVersion) => browserName == "webkit" && isMac && macVersion >= 26;

    // test.use({ ignoreHTTPSErrors: true });
    [Fixture]
    public static BrowserNewContextOptions ContextOptions(BrowserNewContextOptions contextOptions) => new(contextOptions) { IgnoreHTTPSErrors = true };

    /**
     * origin1:
     *   top-level-partitioned=value
     *   top-level-non-partitioned=value
     *
     * origin2:
     *   origin1:
     *     frame-partitioned=value
     *     frame-non-partitioned=value
     *
     * origin1:
     *   origin2:
     *     origin1:
     *       frame-partitioned=value
     *       frame-non-partitioned=value
     *
     * origin1 = httpsServer.PREFIX
     * origin2 = httpsServer.CROSS_PROCESS_PREFIX
     */
    private static void AddCommonCookieHandlers(Server httpsServer, TestUrls urls)
    {
        // '/set-cookie.html' handlers are added in the tests.
        httpsServer.SetRoute("/read-cookie.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            var cookies = req.Headers["cookie"] == null ? null : string.Join("; ", req.Headers["cookie"].Split(';').Select(c => c.Trim()).Order(StringComparer.Ordinal));
            // .NET: undefined in a template literal is the text "undefined".
            res.End($"Received cookie: {cookies ?? "undefined"}");
        });
        httpsServer.SetRoute("/frame-set-cookie.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End($"<iframe src='{urls.origin1}/set-cookie.html'></iframe>");
        });
        httpsServer.SetRoute("/frame-read-cookie.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End($"<iframe src='{urls.origin1}/read-cookie.html'></iframe>");
        });
        // Nested cross-origin iframe:
        //   main frame: (origin1 or origin2) -> iframe1: origin2 -> iframe2: origin1
        httpsServer.SetRoute("/nested-frame-set-cookie.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End($"<iframe src='{urls.origin2}/frame-set-cookie.html'></iframe>");
        });
        httpsServer.SetRoute("/nested-frame-read-cookie.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End($"<iframe src='{urls.origin2}/frame-read-cookie.html'></iframe>");
        });
    }

    private static BrowserContextCookiesResult FindCookie(IReadOnlyList<BrowserContextCookiesResult> cookies, string name)
    {
        var result = cookies.FirstOrDefault(cookie => cookie.Name == name);
        Assert.That(result, Is.Not.Null, $"Cookie {name} not found in {JsonSerializer.Serialize(cookies, new JsonSerializerOptions { WriteIndented = true })}");
        return result;
    }

    private static void ExpectPartitionKey(IReadOnlyList<BrowserContextCookiesResult> cookies, string name, string partitionKey)
    {
        var cookie = FindCookie(cookies, name);
        if (partitionKey != cookie.PartitionKey)
        {
            throw new Exception($"Cookie {name} has partitionKey {cookie.PartitionKey ?? "undefined"} but expected {partitionKey ?? "undefined"}.");
        }
    }

    private static async Task<(string expectedTopLevel, string expectedThirdParty)> RunNonPartitionedTest(IPage page, Server httpsServer, string browserName, bool isMac, bool isLinux, bool isBidi, string channel, TestUrls urls)
    {
        AddCommonCookieHandlers(httpsServer, urls);
        httpsServer.SetRoute("/set-cookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", $"{(!string.IsNullOrEmpty(req.Headers["referer"]) ? "frame" : "top-level")}=value; SameSite=None; Path=/; Secure;");
            res.SetHeader("Content-Type", "text/html");
            res.End();
        });

        await page.GotoAsync(urls.set_origin1);
        await page.GotoAsync(urls.read_origin1);
        Assert.That(await page.Locator("body").TextContentAsync(), Is.EqualTo("Received cookie: top-level=value"));

        await page.GotoAsync(urls.read_origin2_origin1);
        var frameBody = page.Locator("iframe").ContentFrame.Locator("body");

        // WebKit does not support third-party cookies without a 'Partition' attribute.
        if ((browserName == "webkit" && isMac) || (browserName == "firefox" && isBidi))
        {
            await Expect(frameBody).ToHaveTextAsync("Received cookie: undefined");
        }
        else
        {
            await Expect(frameBody).ToHaveTextAsync("Received cookie: top-level=value");
        }

        // Set cookie and do second navigation.
        await page.GotoAsync(urls.set_origin2_origin1);
        await page.GotoAsync(urls.read_origin2_origin1);
        var expectedThirdParty = "Received cookie: ";
        if (browserName == "webkit" && isMac)
        {
            expectedThirdParty += "undefined";
        }
        else if (browserName == "webkit" && (isLinux || channel == "webkit-wsl"))
        {
            expectedThirdParty += "top-level=value";
        }
        else if (browserName == "firefox" && isBidi)
        {
            expectedThirdParty += "frame=value";
        }
        else
        {
            expectedThirdParty += "frame=value; top-level=value";
        }
        await Expect(frameBody).ToHaveTextAsync(expectedThirdParty, new() { Timeout = 1000 });

        // Check again the top-level cookie.
        await page.GotoAsync(urls.read_origin1);
        var expectedTopLevel = (browserName == "webkit" && (isMac || isLinux || channel == "webkit-wsl")) || (browserName == "firefox" && isBidi) ?
            "Received cookie: top-level=value" :
            "Received cookie: frame=value; top-level=value";
        Assert.That(await page.Locator("body").TextContentAsync(), Is.EqualTo(expectedTopLevel));

        return (
            expectedTopLevel,
            expectedThirdParty);
    }

    [UpstreamTest("CO-052 third party non-partitioned cookies")]
    public async Task ThirdPartyNonPartitionedCookies(IPage page, string browserName, Server httpsServer, bool isMac, bool isLinux, bool isBidi, TestUrls urls, string channel)
    {
        await RunNonPartitionedTest(page, httpsServer, browserName, isMac, isLinux, isBidi, channel, urls);
    }

    [UpstreamTest("CO-053 save/load third party non-partitioned cookies")]
    public async Task SaveLoadThirdPartyNonPartitionedCookies(IPage page, string browserName, Server httpsServer, bool isMac, bool isLinux, bool isBidi, IBrowser browser, TestUrls urls, string channel, BrowserNewContextOptions contextOptions)
    {
        // Run the test to populate the cookies.
        var (expectedTopLevel, expectedThirdParty) = await RunNonPartitionedTest(page, httpsServer, browserName, isMac, isLinux, isBidi, channel, urls);

        async Task checkCookies(IPage page)
        {
            // Check top-level cookie first.
            await page.GotoAsync(urls.read_origin1);
            Assert.That(await page.Locator("body").TextContentAsync(), Is.EqualTo(expectedTopLevel));

            // Check third-party cookie.
            await page.GotoAsync(urls.read_origin2_origin1);
            var frameBody = page.Locator("iframe").ContentFrame.Locator("body");
            await Expect(frameBody).ToHaveTextAsync(expectedThirdParty);
        }

        await checkCookies(page);

        await Info.Step("export via cookies/addCookies", async () =>
        {
            var cookies = await page.Context.CookiesAsync();
            var context2 = await browser.NewContextAsync(contextOptions);
            // .NET: CookiesAsync gives BrowserContextCookiesResult objects, and AddCookiesAsync takes Cookie objects.
            await context2.AddCookiesAsync(cookies.Select(c => new Cookie { Name = c.Name, Value = c.Value, Domain = c.Domain, Path = c.Path, Expires = c.Expires, HttpOnly = c.HttpOnly, Secure = c.Secure, SameSite = c.SameSite, PartitionKey = c.PartitionKey }));
            var page2 = await context2.NewPageAsync();
            await checkCookies(page2);
        });

        await Info.Step("export via storageState", async () =>
        {
            var storageState = await page.Context.StorageStateAsync();
            var context3 = await browser.NewContextAsync(new(contextOptions) { StorageState = storageState });
            var page3 = await context3.NewPageAsync();
            await checkCookies(page3);
        });
    }

    private static async Task RunPartitionedTest(IPage page, Server httpsServer, string browserName, bool isMac, bool webkitPartitions, bool isBidi, TestUrls urls)
    {
        AddCommonCookieHandlers(httpsServer, urls);
        httpsServer.SetRoute("/set-cookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[]
            {
                $"{(!string.IsNullOrEmpty(req.Headers["referer"]) ? "frame" : "top-level")}-partitioned=value; SameSite=None; Path=/; Secure; Partitioned;",
                $"{(!string.IsNullOrEmpty(req.Headers["referer"]) ? "frame" : "top-level")}-non-partitioned=value; SameSite=None; Path=/; Secure;",
            });
            res.End();
        });

        await page.GotoAsync(urls.set_origin1);
        await page.GotoAsync(urls.read_origin1);
        Assert.That(await page.Locator("body").TextContentAsync(), Is.EqualTo("Received cookie: top-level-non-partitioned=value; top-level-partitioned=value"));

        await page.GotoAsync(urls.read_origin2_origin1);
        var frameBody = page.Locator("iframe").ContentFrame.Locator("body");

        // Firefox cookie partitioning is disabled in Firefox(Juggler).
        // TODO: reenable cookie partitioning?
        if (browserName == "firefox" && !isBidi)
        {
            await Expect(frameBody).ToHaveTextAsync("Received cookie: top-level-non-partitioned=value; top-level-partitioned=value");
            return;
        }

        // Linux and Windows WebKit builds do not partition third-party cookies at all.
        if (browserName == "webkit" && !isMac)
        {
            await Expect(frameBody).ToHaveTextAsync("Received cookie: top-level-non-partitioned=value; top-level-partitioned=value");
            return;
        }

        if (browserName == "webkit" || (browserName == "firefox" && isBidi))
        {
            // WebKit will only send 'Partitioned' third-party cookies exactly matching the partition.
            await Expect(frameBody).ToHaveTextAsync("Received cookie: undefined");
        }
        else
        {
            // For non-partitioned cookies, the cookie is sent to the iframe right away,
            // if third-party cookies are supported by the browser.
            await Expect(frameBody).ToHaveTextAsync("Received cookie: top-level-non-partitioned=value");
        }

        // First navigation:
        // - no cookie sent, as it was only set on the top-level site
        // - sets the third-party cookie for the top-level context
        // Second navigation:
        // - sends the cookie as it was just set for the (top-level site, iframe url) partition.
        await page.GotoAsync(urls.set_origin2_origin1);
        await page.GotoAsync(urls.read_origin2_origin1);
        if (browserName == "webkit")
        {
            await Expect(frameBody).ToHaveTextAsync(webkitPartitions ? "Received cookie: frame-partitioned=value" : "Received cookie: undefined");
        }
        else if (browserName == "firefox" && isBidi)
        {
            await Expect(frameBody).ToHaveTextAsync("Received cookie: frame-non-partitioned=value; frame-partitioned=value");
        }
        else
        {
            await Expect(frameBody).ToHaveTextAsync("Received cookie: frame-non-partitioned=value; frame-partitioned=value; top-level-non-partitioned=value");
        }
    }

    [UpstreamTest("CO-054 third party 'Partitioned;' cookies")]
    public async Task ThirdPartyPartitionedCookies(IPage page, string browserName, Server httpsServer, bool isMac, bool webkitPartitions, bool isBidi, TestUrls urls)
    {
        await RunPartitionedTest(page, httpsServer, browserName, isMac, webkitPartitions, isBidi, urls);
    }

    [UpstreamTest("CO-055 save/load third party 'Partitioned;' cookies")]
    [LocalFixme("dotnet: BrowserContextCookiesResult and Cookie have no _crHasCrossSiteAncestor: a partitioned cookie that goes from CookiesAsync to AddCookiesAsync has a cross-site ancestor for Chromium, and the top-level page does not send it (only in chromium)")]
    public async Task SaveLoadThirdPartyPartitionedCookies(IPage page, string browserName, Server httpsServer, bool isMac, bool webkitPartitions, bool isBidi, IBrowser browser, TestUrls urls, BrowserNewContextOptions contextOptions)
    {
        Info.Fixme(browserName == "firefox" && !isBidi, "Firefox cookie partitioning is disabled in Firefox(Juggler).");
        Info.Fixme(browserName == "webkit" && !isMac, "Linux and Windows WebKit builds do not partition third-party cookies at all.");

        await RunPartitionedTest(page, httpsServer, browserName, isMac, webkitPartitions, isBidi, urls);

        async Task checkCookies(IPage page)
        {
            {
                // Check top-level cookie first.
                await page.GotoAsync(urls.read_origin1);
                var expectedTopLevel = browserName == "webkit" && isMac ?
                    "Received cookie: top-level-non-partitioned=value; top-level-partitioned=value" :
                    "Received cookie: frame-non-partitioned=value; top-level-non-partitioned=value; top-level-partitioned=value";
                Assert.That(await page.Locator("body").TextContentAsync(), Is.EqualTo(expectedTopLevel));
            }
            {
                // Check third-party cookie.
                await page.GotoAsync(urls.read_origin2_origin1);
                var frameBody = page.Locator("iframe").ContentFrame.Locator("body");
                var expectedThirdParty = browserName == "webkit" ?
                    (webkitPartitions ? "Received cookie: frame-partitioned=value" : "Received cookie: undefined") :
                    "Received cookie: frame-non-partitioned=value; frame-partitioned=value; top-level-non-partitioned=value";
                await Expect(frameBody).ToHaveTextAsync(expectedThirdParty, new() { Timeout = 1000 });
            }
            {
                await page.GotoAsync(urls.read_origin1_origin2_origin1); // read-origin1-origin2-origin1.html
                var frameBody = page.Locator("iframe").ContentFrame.Locator("iframe").ContentFrame.Locator("body");
                var expectedThirdParty = browserName == "webkit" ?
                    "Received cookie: top-level-non-partitioned=value; top-level-partitioned=value" :
                    "Received cookie: frame-non-partitioned=value; top-level-non-partitioned=value";
                await Expect(frameBody).ToHaveTextAsync(expectedThirdParty, new() { Timeout = 1000 });
            }
        }

        await checkCookies(page);

        void checkStorageCookies(IReadOnlyList<BrowserContextCookiesResult> cookies)
        {
            var webkitNoPartition = browserName == "webkit" && !webkitPartitions;
            ExpectPartitionKey(cookies, "top-level-partitioned", webkitNoPartition ? null : "https://localhost");
            ExpectPartitionKey(cookies, "top-level-non-partitioned", null);
            if (webkitNoPartition)
            {
                Assert.That(cookies.FirstOrDefault(cookie => cookie.Name == "frame-partitioned"), Is.Null);
                Assert.That(cookies.FirstOrDefault(cookie => cookie.Name == "frame-non-partitioned"), Is.Null);
            }
            else
            {
                ExpectPartitionKey(cookies, "frame-partitioned", "https://127.0.0.1");
                if (browserName == "webkit")
                {
                    Assert.That(cookies.FirstOrDefault(cookie => cookie.Name == "frame-non-partitioned"), Is.Null);
                }
                else
                {
                    ExpectPartitionKey(cookies, "frame-non-partitioned", null);
                }
            }
        }
        checkStorageCookies(await page.Context.CookiesAsync());
        // .NET: StorageStateAsync gives the JSON text of the storage state.
        checkStorageCookies(JsonSerializer.Deserialize<BrowserContextCookiesResult[]>(JsonDocument.Parse(await page.Context.StorageStateAsync()).RootElement.GetProperty("cookies").GetRawText(), new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } }));

        await Info.Step("export via cookies/addCookies", async () =>
        {
            var cookies = await page.Context.CookiesAsync();
            var context2 = await browser.NewContextAsync(contextOptions);
            // .NET: CookiesAsync gives BrowserContextCookiesResult objects, and AddCookiesAsync takes Cookie objects.
            await context2.AddCookiesAsync(cookies.Select(c => new Cookie { Name = c.Name, Value = c.Value, Domain = c.Domain, Path = c.Path, Expires = c.Expires, HttpOnly = c.HttpOnly, Secure = c.Secure, SameSite = c.SameSite, PartitionKey = c.PartitionKey }));
            var page2 = await context2.NewPageAsync();
            await checkCookies(page2);
        });

        await Info.Step("export via storageState", async () =>
        {
            var storageState = await page.Context.StorageStateAsync();
            var context3 = await browser.NewContextAsync(new(contextOptions) { StorageState = storageState });
            var page3 = await context3.NewPageAsync();
            await checkCookies(page3);
        });
    }

    [UpstreamTest("CO-056 add 'Partitioned;' cookie via API")]
    [LocalFixme("dotnet: Cookie has no _crHasCrossSiteAncestor, which upstream sets to false for top-level-partitioned: the cookie has a cross-site ancestor for Chromium, and the top-level page does not send it (only in chromium)")]
    public async Task AddPartitionedCookieViaAPI(IPage page, IBrowserContext context, string browserName, Server httpsServer, bool isMac, bool webkitPartitions, bool isBidi, TestUrls urls)
    {
        AddCommonCookieHandlers(httpsServer, urls);

        // .NET: the cookies with a partitionKey have no "_crHasCrossSiteAncestor: false" and "_crHasCrossSiteAncestor: true", upstream passes them "as any": Cookie does not have the property.
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = "top-level-partitioned",
                Value = "value",
                Domain = httpsServer.Hostname,
                Path = "/",
                Expires = -1,
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteAttribute.None,
                PartitionKey = "https://localhost",
            },
            new Cookie
            {
                Name = "top-level-non-partitioned",
                Value = "value",
                Domain = httpsServer.Hostname,
                Path = "/",
                Expires = -1,
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteAttribute.None,
            },
            new Cookie
            {
                Name = "frame-partitioned",
                Value = "value",
                Domain = httpsServer.Hostname,
                Path = "/",
                Expires = -1,
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteAttribute.None,
                PartitionKey = "https://127.0.0.1",
            },
            new Cookie
            {
                Name = "frame-non-partitioned",
                Value = "value",
                Domain = httpsServer.Hostname,
                Path = "/",
                Expires = -1,
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteAttribute.None,
            },
        });

        async Task checkCookies(IPage page)
        {
            {
                // Check top-level cookie first.
                await page.GotoAsync(urls.read_origin1);
                string expectedTopLevel;
                if (browserName == "webkit" && isMac && !webkitPartitions)
                {
                    expectedTopLevel = "Received cookie: frame-non-partitioned=value; top-level-non-partitioned=value";
                }
                else if ((browserName == "webkit" && !webkitPartitions) || (browserName == "firefox" && !isBidi))
                {
                    expectedTopLevel = "Received cookie: frame-non-partitioned=value; frame-partitioned=value; top-level-non-partitioned=value; top-level-partitioned=value";
                }
                else
                {
                    expectedTopLevel = "Received cookie: frame-non-partitioned=value; top-level-non-partitioned=value; top-level-partitioned=value";
                }
                Assert.That(await page.Locator("body").TextContentAsync(), Is.EqualTo(expectedTopLevel));
            }
            {
                // Check third-party cookie.
                await page.GotoAsync(urls.read_origin2_origin1);
                var frameBody = page.Locator("iframe").ContentFrame.Locator("body");
                var expectedThirdParty = "Received cookie: ";
                if (webkitPartitions || (browserName == "firefox" && isBidi))
                {
                    expectedThirdParty += "frame-partitioned=value";
                }
                else if (browserName == "webkit" && isMac)
                {
                    expectedThirdParty += "undefined";
                }
                else if (browserName == "chromium")
                {
                    expectedThirdParty += "frame-non-partitioned=value; frame-partitioned=value; top-level-non-partitioned=value";
                }
                else
                {
                    // Firefox(Juggler) and WebKit on Linux/Windows do not partition third-party cookies.
                    expectedThirdParty += "frame-non-partitioned=value; frame-partitioned=value; top-level-non-partitioned=value; top-level-partitioned=value";
                }
                await Expect(frameBody).ToHaveTextAsync(expectedThirdParty, new() { Timeout = 1000 });
            }
            {
                await page.GotoAsync(urls.read_origin1_origin2_origin1); // read-origin1-origin2-origin1.html
                var frameBody = page.Locator("iframe").ContentFrame.Locator("iframe").ContentFrame.Locator("body");
                var expectedThirdParty = "Received cookie: ";
                if (webkitPartitions)
                {
                    expectedThirdParty += "frame-non-partitioned=value; top-level-non-partitioned=value; top-level-partitioned=value";
                }
                else if (browserName == "webkit" && isMac)
                {
                    expectedThirdParty += "frame-non-partitioned=value; top-level-non-partitioned=value";
                }
                else if (browserName == "webkit" || (browserName == "firefox" && !isBidi))
                {
                    expectedThirdParty += "frame-non-partitioned=value; frame-partitioned=value; top-level-non-partitioned=value; top-level-partitioned=value";
                }
                else if (browserName == "firefox" && isBidi)
                {
                    expectedThirdParty += "undefined";
                }
                else
                {
                    expectedThirdParty += "frame-non-partitioned=value; top-level-non-partitioned=value";
                }
                await Expect(frameBody).ToHaveTextAsync(expectedThirdParty, new() { Timeout = 1000 });
            }
        }

        await checkCookies(page);
    }

    [UpstreamTest("CO-057 same origin third party 'Partitioned;' cookie with different origin intermediate iframe")]
    public async Task SameOriginThirdPartyPartitionedCookieWithDifferentOriginIntermediateIframe(IPage page, Server httpsServer, IBrowser browser, bool isBidi, TestUrls urls, BrowserNewContextOptions contextOptions)
    {
        AddCommonCookieHandlers(httpsServer, urls);
        httpsServer.SetRoute("/set-cookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[]
            {
                $"{(!string.IsNullOrEmpty(req.Headers["referer"]) ? "frame" : "top-level")}-partitioned=value; SameSite=None; Path=/; Secure; Partitioned;",
                $"{(!string.IsNullOrEmpty(req.Headers["referer"]) ? "frame" : "top-level")}-non-partitioned=value; SameSite=None; Path=/; Secure;",
            });
            res.End();
        });
        // main frame: origin1 -> iframe1: origin2 -> iframe2: origin1
        // In this case the cookie in iframe2 is a third-party partitioned cookie, even though
        // it's the same origin as the main frame.
        await page.GotoAsync(urls.set_origin1_origin2_origin1);

        async Task checkCookies(IPage page)
        {
            await page.GotoAsync(urls.read_origin1_origin2_origin1);
            var frameBody = page.Locator("iframe").ContentFrame.Locator("iframe").ContentFrame.Locator("body");
            await Expect(frameBody).ToHaveTextAsync("Received cookie: frame-non-partitioned=value; frame-partitioned=value");
        }

        await checkCookies(page);

        await Info.Step("export via cookies/addCookies", async () =>
        {
            var cookies = await page.Context.CookiesAsync();
            var context2 = await browser.NewContextAsync(contextOptions);
            // .NET: CookiesAsync gives BrowserContextCookiesResult objects, and AddCookiesAsync takes Cookie objects.
            await context2.AddCookiesAsync(cookies.Select(c => new Cookie { Name = c.Name, Value = c.Value, Domain = c.Domain, Path = c.Path, Expires = c.Expires, HttpOnly = c.HttpOnly, Secure = c.Secure, SameSite = c.SameSite, PartitionKey = c.PartitionKey }));
            var page2 = await context2.NewPageAsync();
            await checkCookies(page2);
        });

        await Info.Step("export via storageState", async () =>
        {
            var storageState = await page.Context.StorageStateAsync();
            var context3 = await browser.NewContextAsync(new(contextOptions) { StorageState = storageState });
            var page3 = await context3.NewPageAsync();
            await checkCookies(page3);
        });
    }

    [UpstreamTest("CO-058 top level 'Partitioned;' cookie and same origin iframe")]
    [LocalFixme("dotnet: BrowserContextCookiesResult and Cookie have no _crHasCrossSiteAncestor: a partitioned cookie that goes from CookiesAsync to AddCookiesAsync has a cross-site ancestor for Chromium, and the top-level page does not send it (only in chromium)")]
    public async Task TopLevelPartitionedCookieAndSameOriginIframe(IPage page, string browserName, Server httpsServer, IBrowser browser, bool isBidi, TestUrls urls, BrowserNewContextOptions contextOptions)
    {
        AddCommonCookieHandlers(httpsServer, urls);
        httpsServer.SetRoute("/set-cookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[]
            {
                $"{(!string.IsNullOrEmpty(req.Headers["referer"]) ? "frame" : "top-level")}=value; SameSite=None; Path=/; Secure; Partitioned;",
                $"{(!string.IsNullOrEmpty(req.Headers["referer"]) ? "frame" : "top-level")}-non-partitioned=value; SameSite=None; Path=/; Secure;",
            });
            res.End();
        });

        // Same origin iframe cookies are partitioned the same way as top-level cookies.
        await page.GotoAsync(urls.set_origin1);
        await page.Context.StorageStateAsync(new() { Path = "/tmp/state2.json" });

        async Task checkCookies(IPage page)
        {
            {
                // Check top-level cookie first.
                await page.GotoAsync(urls.read_origin1);
                Assert.That(await page.Locator("body").TextContentAsync(), Is.EqualTo("Received cookie: top-level-non-partitioned=value; top-level=value"));
            }
            {
                // Same origin iframe.
                await page.GotoAsync(urls.read_origin1_origin1);
                var frameBody = page.Locator("iframe").ContentFrame.Locator("body");
                await Expect(frameBody).ToHaveTextAsync("Received cookie: top-level-non-partitioned=value; top-level=value", new() { Timeout = 1000 });
            }
            {
                // Check third-party cookie.
                // main frame: origin1 -> iframe1: origin2 -> iframe2: origin1
                await page.GotoAsync(urls.read_origin1_origin2_origin1);
                var frameBody = page.Locator("iframe").ContentFrame.Locator("iframe").ContentFrame.Locator("body");
                var expectedThirdParty = "Received cookie: ";
                if (browserName == "chromium")
                {
                    expectedThirdParty += "top-level-non-partitioned=value";
                }
                else if (browserName == "firefox" && isBidi)
                {
                    expectedThirdParty += "undefined";
                }
                else
                {
                    expectedThirdParty += "top-level-non-partitioned=value; top-level=value";
                }
                await Expect(frameBody).ToHaveTextAsync(expectedThirdParty, new() { Timeout = 1000 });
            }
        }

        await checkCookies(page);

        await Info.Step("export via cookies/addCookies", async () =>
        {
            var cookies = await page.Context.CookiesAsync();
            var context2 = await browser.NewContextAsync(contextOptions);
            // .NET: CookiesAsync gives BrowserContextCookiesResult objects, and AddCookiesAsync takes Cookie objects.
            await context2.AddCookiesAsync(cookies.Select(c => new Cookie { Name = c.Name, Value = c.Value, Domain = c.Domain, Path = c.Path, Expires = c.Expires, HttpOnly = c.HttpOnly, Secure = c.Secure, SameSite = c.SameSite, PartitionKey = c.PartitionKey }));
            var page2 = await context2.NewPageAsync();
            await checkCookies(page2);
        });

        await Info.Step("export via storageState", async () =>
        {
            var storageState = await page.Context.StorageStateAsync();
            var context3 = await browser.NewContextAsync(new(contextOptions) { StorageState = storageState });
            var page3 = await context3.NewPageAsync();
            await checkCookies(page3);
        });
    }

    [UpstreamTest("CO-059 should be able to send third party cookies via an iframe")]
    public async Task ShouldBeAbleToSendThirdPartyCookiesViaAnIframe(IBrowser browser, Server httpsServer, string browserName, bool isMac, bool isBidi, BrowserNewContextOptions contextOptions)
    {
        Info.Fixme(browserName == "webkit" && isMac);
        Info.Skip(browserName == "firefox" && isBidi, "Firefox's Total Cookie Protection blocks this third-party cookie");

        var context = await browser.NewContextAsync(new(contextOptions)
        {
            IgnoreHTTPSErrors = true,
        });
        try
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(httpsServer.EmptyPage);
            await context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Domain = new Uri(httpsServer.CrossProcessPrefix).Host,
                    Path = "/",
                    Name = "cookie1",
                    Value = "yes",
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteAttribute.None,
                },
            });
            var response = await PromiseAll(
                httpsServer.WaitForRequest("/grid.html"),
                page.SetContentAsync($"<iframe src=\"{httpsServer.CrossProcessPrefix}/grid.html\"></iframe>"));
            Assert.That(response.Headers["cookie"], Is.EqualTo("cookie1=yes"));
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [UpstreamTest("CO-060 should(not) block third party cookies - persistent context")]
    public async Task ShouldNotBlockThirdPartyCookiesPersistentContext(Server httpsServer, LaunchPersistent launchPersistent, bool allowsThirdParty, string defaultSameSiteCookieValue)
    {
        var (context, page) = await launchPersistent();
        await TestThirdPartyCookiesAreBlocked(page, context, httpsServer, allowsThirdParty, defaultSameSiteCookieValue);
    }

    [UpstreamTest("CO-061 should(not) block third party cookies - ephemeral context")]
    public async Task ShouldNotBlockThirdPartyCookiesEphemeralContext(IPage page, IBrowserContext context, Server httpsServer, bool allowsThirdParty, string defaultSameSiteCookieValue)
    {
        await TestThirdPartyCookiesAreBlocked(page, context, httpsServer, allowsThirdParty, defaultSameSiteCookieValue);
    }

    private static async Task TestThirdPartyCookiesAreBlocked(IPage page, IBrowserContext context, Server server, bool allowsThirdParty, string defaultSameSiteCookieValue)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"src => {
    let fulfill;
    const promise = new Promise(x => fulfill = x);
    const iframe = document.createElement('iframe');
    document.body.appendChild(iframe);
    iframe.onload = fulfill;
    iframe.src = src;
    return promise;
  }", server.CrossProcessPrefix + "/grid.html");
        var documentCookie = await page.Frames[1].EvaluateAsync<string>(@"() => {
    document.cookie = 'username=John Doe';
    return document.cookie;
  }");
        await page.WaitForTimeoutAsync(2000);
        Assert.That(documentCookie, Is.EqualTo(allowsThirdParty ? "username=John Doe" : ""));
        var cookies = await context.CookiesAsync(server.CrossProcessPrefix + "/grid.html");
        if (allowsThirdParty)
        {
            ExpectToEqual(cookies, new[]
            {
                new
                {
                    domain = "127.0.0.1",
                    expires = -1,
                    httpOnly = false,
                    name = "username",
                    path = "/",
                    sameSite = defaultSameSiteCookieValue,
                    secure = false,
                    value = "John Doe",
                },
            });
        }
        else
        {
            ExpectToEqual(cookies, Array.Empty<object>());
        }
    }

    [UpstreamTest("CO-062 should not block third party SameSite=None cookies")]
    public async Task ShouldNotBlockThirdPartySameSiteNoneCookies(ContextFactory contextFactory, Server httpsServer, string browserName)
    {
        Info.Skip(browserName == "webkit", "No third party cookies in WebKit");
        Info.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");
        var context = await contextFactory(new()
        {
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();

        httpsServer.SetRoute("/empty.html", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/html",
            });
            res.End($"<iframe src=\"{httpsServer.CrossProcessPrefix}/grid.html\"></iframe>");
        });

        httpsServer.SetRoute("/grid.html", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Set-Cookie"] = new[] { "a=b; Path=/; Max-Age=3600; SameSite=None; Secure" },
                ["Content-Type"] = "text/html",
            });
            res.End(@"Hello world
    <script>
    setTimeout(() => fetch('/json'), 1000);
    </script>");
        });

        var cookie = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        httpsServer.SetRoute("/json", (req, res) =>
        {
            cookie.TrySetResult(req.Headers["cookie"]);
            res.End();
        });

        await page.GotoAsync(httpsServer.EmptyPage);
        Assert.That(await cookie.Task, Is.EqualTo("a=b"));
    }
}
