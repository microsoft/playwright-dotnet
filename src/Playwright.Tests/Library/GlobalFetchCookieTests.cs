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

using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/global-fetch-cookie.spec.ts.</summary>
public class GlobalFetchCookieTests : UpstreamBrowserTest
{
    private Task<IAPIRequestContext> _request;

    private Task<IAPIRequestContext> Request() => _request ??= Playwright.APIRequest.NewContextAsync(new() { IgnoreHTTPSErrors = true });

    [TearDown]
    public async Task RequestTearDown()
    {
        if (_request != null)
        {
            await (await _request).DisposeAsync();
            _request = null;
        }
    }

    [SetUp]
    public void Skip() => Info.Skip(Mode != "default");

    [UpstreamTest("FE-055 addCookies should add cookies to the cookie jar")]
    public async Task AddCookiesShouldAddCookiesToTheCookieJar()
    {
        var request = await Request();
        await request.AddCookiesAsync(new Cookie[]
        {
            new() { Name = "a", Value = "b", Url = Server.EmptyPage },
            new() { Name = "c", Value = "d", Domain = "localhost", Path = "/input", HttpOnly = true, SameSite = SameSiteAttribute.Strict },
            new() { Name = "e", Value = "f", Domain = "other.com", Path = "/" },
        });
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/input/button.html"),
            request.GetAsync($"{Server.Prefix}/input/button.html"));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("a=b; c=d"));
        // .NET: StorageStateAsync gives the JSON text of the storage state.
        var state = JsonSerializer.Deserialize<JsonElement>(await request.StorageStateAsync());
        ExpectToEqual(state.GetProperty("cookies"), new[]
        {
            new { name = "a", value = "b", domain = "localhost", path = "/", expires = -1, httpOnly = false, secure = false, sameSite = "Lax" },
            new { name = "c", value = "d", domain = "localhost", path = "/input", expires = -1, httpOnly = true, secure = false, sameSite = "Strict" },
            new { name = "e", value = "f", domain = "other.com", path = "/", expires = -1, httpOnly = false, secure = false, sameSite = "Lax" },
        });
    }

    [UpstreamTest("FE-056 addCookies should validate cookies")]
    public async Task AddCookiesShouldValidateCookies()
    {
        var request = await Request();
        var error = await Catch(() => request.AddCookiesAsync(new Cookie[] { new() { Name = "a", Value = "b" } }));
        Assert.That(error.Message, Does.Contain("Cookie should have a url or a domain/path pair"));
    }

    [UpstreamTest("FE-057 cookies should return cookies filtered by urls")]
    public async Task CookiesShouldReturnCookiesFilteredByUrls()
    {
        var request = await Request();
        await request.AddCookiesAsync(new Cookie[]
        {
            new() { Name = "a", Value = "b", Domain = "localhost", Path = "/" },
            new() { Name = "c", Value = "d", Domain = "localhost", Path = "/input" },
            new() { Name = "e", Value = "f", Domain = ".one.com", Path = "/" },
            new() { Name = "g", Value = "h", Domain = "two.com", Path = "/", Secure = true },
        });
        Assert.That((await request.CookiesAsync()).Select(c => c.Name), Is.EqualTo(new[] { "a", "c", "e", "g" }));
        Assert.That((await request.CookiesAsync(Server.EmptyPage)).Select(c => c.Name), Is.EqualTo(new[] { "a" }));
        Assert.That((await request.CookiesAsync($"{Server.Prefix}/input/button.html")).Select(c => c.Name), Is.EqualTo(new[] { "a", "c" }));
        Assert.That((await request.CookiesAsync(new[] { "http://sub.one.com/", "http://two.com/" })).Select(c => c.Name), Is.EqualTo(new[] { "e" }));
        Assert.That((await request.CookiesAsync(new[] { "http://sub.one.com/", "https://two.com/" })).Select(c => c.Name), Is.EqualTo(new[] { "e", "g" }));
        ExpectToEqual(await request.CookiesAsync("http://other.com/"), Array.Empty<object>());
    }

    [UpstreamTest("FE-058 cookies should only return cookies that apply to the url")]
    public async Task CookiesShouldOnlyReturnCookiesThatApplyToTheUrl()
    {
        var request = await Request();
        await request.AddCookiesAsync(new Cookie[]
        {
            new() { Name = "hostonly", Value = "v", Domain = "one.com", Path = "/api" },
            new() { Name = "subdomains", Value = "v", Domain = ".one.com", Path = "/api" },
            new() { Name = "slash", Value = "v", Domain = "one.com", Path = "/foo/" },
        });
        async Task<string[]> names(string url) => (await request.CookiesAsync(url)).Select(c => c.Name).ToArray();
        Assert.That(await names("http://one.com/api"), Is.EqualTo(new[] { "hostonly", "subdomains" }));
        Assert.That(await names("http://one.com/api/x"), Is.EqualTo(new[] { "hostonly", "subdomains" }));
        Assert.That(await names("http://one.com/apiv2"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await names("http://sub.one.com/api"), Is.EqualTo(new[] { "subdomains" }));
        Assert.That(await names("http://notone.com/api"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await names("http://one.com/foo"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await names("http://one.com/foo/"), Is.EqualTo(new[] { "slash" }));
        Assert.That(await names("http://one.com/foo/bar"), Is.EqualTo(new[] { "slash" }));
    }

    [UpstreamTest("FE-060 cookies should include cookies from Set-Cookie header")]
    public async Task CookiesShouldIncludeCookiesFromSetCookieHeader()
    {
        var request = await Request();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=b; HttpOnly; SameSite=Strict", "c=d; path=/input" });
            res.End();
        });
        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        ExpectToEqual(await request.CookiesAsync(), new[]
        {
            new { name = "a", value = "b", domain = "localhost", path = "/", expires = -1, httpOnly = true, secure = false, sameSite = "Strict" },
            new { name = "c", value = "d", domain = "localhost", path = "/input", expires = -1, httpOnly = false, secure = false, sameSite = "Lax" },
        });
    }

    [UpstreamTest("FE-061 clearCookies should remove all cookies")]
    public async Task ClearCookiesShouldRemoveAllCookies()
    {
        var request = await Request();
        await request.AddCookiesAsync(new Cookie[]
        {
            new() { Name = "a", Value = "b", Url = Server.EmptyPage },
            new() { Name = "c", Value = "d", Domain = "one.com", Path = "/" },
        });
        await request.ClearCookiesAsync();
        ExpectToEqual(await request.CookiesAsync(), Array.Empty<object>());
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.Null);
    }

    [UpstreamTest("FE-062 clearCookies should filter by name, domain and path")]
    public async Task ClearCookiesShouldFilterByNameDomainAndPath()
    {
        var request = await Request();
        await request.AddCookiesAsync(new Cookie[]
        {
            new() { Name = "session", Value = "1", Domain = "one.com", Path = "/" },
            new() { Name = "session", Value = "2", Domain = "two.com", Path = "/" },
            new() { Name = "session", Value = "3", Domain = "two.com", Path = "/api" },
            new() { Name = "other", Value = "4", Domain = "one.com", Path = "/" },
        });
        async Task<string[]> values() => (await request.CookiesAsync()).Select(c => c.Value).Order(StringComparer.Ordinal).ToArray();

        await request.ClearCookiesAsync(new() { Name = "session", Domain = "two.com", Path = "/api" });
        Assert.That(await values(), Is.EqualTo(new[] { "1", "2", "4" }));

        await request.ClearCookiesAsync(new() { DomainRegex = new Regex("one\\.com$") });
        Assert.That(await values(), Is.EqualTo(new[] { "2" }));

        await request.ClearCookiesAsync(new() { NameRegex = new Regex("^sess") });
        Assert.That(await values(), Is.EqualTo(Array.Empty<string>()));
    }

    [UpstreamTest("FE-063 clearCookies should remove nameless cookies by empty name")]
    public async Task ClearCookiesShouldRemoveNamelessCookiesByEmptyName()
    {
        var request = await Request();
        await request.AddCookiesAsync(new Cookie[]
        {
            new() { Name = "", Value = "nameless", Domain = "one.com", Path = "/" },
            new() { Name = "session", Value = "secret", Domain = "one.com", Path = "/" },
        });
        await request.ClearCookiesAsync(new() { Name = "" });
        Assert.That((await request.CookiesAsync()).Select(c => c.Name), Is.EqualTo(new[] { "session" }));
    }

    [UpstreamTest("FE-064 should send nameless cookies as value only")]
    public async Task ShouldSendNamelessCookiesAsValueOnly()
    {
        var request = await Request();
        await request.AddCookiesAsync(new Cookie[]
        {
            new() { Name = "", Value = "nameless", Url = Server.EmptyPage },
            new() { Name = "a", Value = "b", Url = Server.EmptyPage },
        });
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("nameless; a=b"));
    }

    [UpstreamTest("FE-065 should store Set-Cookie without equals sign as nameless cookie")]
    public async Task ShouldStoreSetCookieWithoutEqualsSignAsNamelessCookie()
    {
        var request = await Request();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "token; path=/", "a=; path=/" });
            res.End();
        });
        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        ExpectToEqual((await request.CookiesAsync()).Select(c => new { name = c.Name, value = c.Value }), new[]
        {
            new { name = "", value = "token" },
            new { name = "a", value = "" },
        });
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("token; a="));
    }

    [UpstreamTest("FE-066 should filter outgoing cookies by path")]
    public async Task ShouldFilterOutgoingCookiesByPath()
    {
        var request = await Request();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=v; path=/input/subfolder", "b=v; path=/input", "c=v;" });
            res.End();
        });
        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/input/button.html"),
            request.GetAsync($"{Server.Prefix}/input/button.html"));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("b=v; c=v"));
    }

    [UpstreamTest("FE-070 should send secure cookie over https")]
    public async Task ShouldSendSecureCookieOverHttps()
    {
        var request = await Request();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=v; secure", "b=v" });
            res.End();
        });
        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        var (serverRequest, _) = await PromiseAll(
            HttpsServer.WaitForRequest("/empty.html"),
            request.GetAsync(HttpsServer.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("a=v; b=v"));
    }

    [UpstreamTest("FE-071 should send secure cookie over http for localhost")]
    public async Task ShouldSendSecureCookieOverHttpForLocalhost()
    {
        var request = await Request();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=v; secure", "b=v" });
            res.End();
        });
        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("a=v; b=v"));
    }

    [UpstreamTest("FE-073 should send secure cookie over http for ${host}")]
    [TestCase("127.0.0.1")]
    [TestCase("[::1]")]
    public async Task ShouldSendSecureCookieOverHttpFor(string host)
    {
        var request = await Request();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=v; secure", "b=v" });
            res.End();
        });
        var prefix = $"http://{host}:{Server.Port}";
        await request.GetAsync($"{prefix}/setcookie.html");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync($"{prefix}/empty.html"));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("a=v; b=v"));
        Assert.That((await request.CookiesAsync($"{prefix}/")).Select(c => c.Name), Is.EqualTo(new[] { "a", "b" }));
    }

    [UpstreamTest("FE-074 should send not expired cookies")]
    public async Task ShouldSendNotExpiredCookies()
    {
        var request = await Request();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            var tomorrow = DateTime.Now;
            tomorrow = tomorrow.AddDays(1);
            // .NET: toUTCString()
            res.SetHeader("Set-Cookie", new[] { "a=v", $"b=v; expires={tomorrow.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture)}" });
            res.End();
        });
        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("a=v; b=v"));
    }

    [UpstreamTest("FE-075 should remove expired cookies")]
    public async Task ShouldRemoveExpiredCookies()
    {
        var request = await Request();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            // .NET: toUTCString()
            res.SetHeader("Set-Cookie", new[] { "a=v", $"b=v; expires={DateTime.Now.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture)}" });
            res.End();
        });
        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("a=v"));
    }

    [UpstreamTest("FE-076 should remove cookie with negative max-age")]
    public async Task ShouldRemoveCookieWithNegativeMaxAge()
    {
        var request = await Request();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=v; max-age=100000", "b=v; max-age=100000", "c=v" });
            res.End();
        });
        Server.SetRoute("/removecookie.html", (req, res) =>
        {
            var maxAge = -2 * DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            res.SetHeader("Set-Cookie", new[] { $"a=v; max-age={maxAge}", "b=v; max-age=-1" });
            res.End();
        });
        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        await request.GetAsync($"{Server.Prefix}/removecookie.html");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("c=v"));
    }

    [UpstreamTest("FE-077 should prefer max-age over expires regardless of the order")]
    public async Task ShouldPreferMaxAgeOverExpiresRegardlessOfTheOrder()
    {
        var request = await Request();
        const string past = "Thu, 01 Jan 1970 00:00:00 GMT";
        // .NET: toUTCString()
        var future = DateTime.UnixEpoch.AddMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 3600_000).ToString("R", CultureInfo.InvariantCulture);
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[]
            {
                $"a=v; max-age=3600; expires={past}",
                $"b=v; expires={past}; max-age=3600",
                $"c=v; max-age=-1; expires={future}",
                $"d=v; expires={future}; max-age=-1",
            });
            res.End();
        });
        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("a=v; b=v"));
    }

    [UpstreamTest("FE-078 should ignore max-age that is not a number")]
    public async Task ShouldIgnoreMaxAgeThatIsNotANumber()
    {
        var request = await Request();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=v; max-age=-1abc", "b=v; max-age=3600abc", "c=v; max-age=" });
            res.End();
        });
        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("a=v; b=v; c=v"));
        // .NET: StorageStateAsync gives the JSON text of the storage state.
        var cookies = JsonSerializer.Deserialize<JsonElement>(await request.StorageStateAsync()).GetProperty("cookies");
        Assert.That(cookies.EnumerateArray().Select(c => c.GetProperty("expires").GetDouble()), Is.EqualTo(new[] { -1, -1, -1 }));
    }

    [UpstreamTest("FE-079 should remove cookie with expires far in the past")]
    public async Task ShouldRemoveCookieWithExpiresFarInThePast()
    {
        var request = await Request();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=v; max-age=1000000" });
            res.End();
        });
        Server.SetRoute("/removecookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=v; expires=1 Jan 1000 00:00:00 +0000 (UTC)" });
            res.End();
        });
        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        await request.GetAsync($"{Server.Prefix}/removecookie.html");
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage));
        Assert.That(serverRequest.Headers["cookie"], Is.Null.Or.Empty);
    }

    [UpstreamTest("FE-081 should override cookie from Set-Cookie header")]
    public async Task ShouldOverrideCookieFromSetCookieHeader()
    {
        var request = await Request();
        var tomorrow = DateTime.Now;
        tomorrow = tomorrow.AddDays(1);

        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            // .NET: toUTCString()
            res.SetHeader("Set-Cookie", new[] { $"a=old; expires={tomorrow.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture)}" });
            res.End();
        });

        var dayAfterTomorrow = tomorrow;
        dayAfterTomorrow = dayAfterTomorrow.AddDays(1);
        var dayAfterTomorrowInSeconds = Math.Floor(new DateTimeOffset(dayAfterTomorrow).ToUnixTimeMilliseconds() / 1000d);
        Server.SetRoute("/updatecookie.html", (req, res) =>
        {
            // .NET: toUTCString()
            res.SetHeader("Set-Cookie", new[] { $"a=new; expires={dayAfterTomorrow.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture)}" });
            res.End();
        });

        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        await request.GetAsync($"{Server.Prefix}/updatecookie.html");

        // .NET: StorageStateAsync gives the JSON text of the storage state.
        var state = JsonSerializer.Deserialize<JsonElement>(await request.StorageStateAsync());

        Assert.That(state.GetProperty("cookies").GetArrayLength(), Is.EqualTo(1));
        Assert.That(state.GetProperty("cookies")[0].GetProperty("name").GetString(), Is.EqualTo("a"));
        Assert.That(state.GetProperty("cookies")[0].GetProperty("value").GetString(), Is.EqualTo("new"));
        Assert.That(state.GetProperty("cookies")[0].GetProperty("expires").GetDouble(), Is.EqualTo(dayAfterTomorrowInSeconds));
    }

    [UpstreamTest("FE-082 should override cookie from Set-Cookie header even if it expired")]
    public async Task ShouldOverrideCookieFromSetCookieHeaderEvenIfItExpired()
    {
        var request = await Request();
        var tomorrow = DateTime.Now;
        tomorrow = tomorrow.AddDays(1);

        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            // .NET: toUTCString()
            res.SetHeader("Set-Cookie", new[] { "a=ok", $"b=ok; expires={tomorrow.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture)}" });
            res.End();
        });

        Server.SetRoute("/unsetsetcookie.html", (req, res) =>
        {
            // .NET: toUTCString()
            var pastDateString = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local).ToUniversalTime().ToString("R", CultureInfo.InvariantCulture);
            res.SetHeader("Set-Cookie", new[] { $"a=; expires={pastDateString}", $"b=; expires={pastDateString}" });
            res.End();
        });

        await request.GetAsync($"{Server.Prefix}/setcookie.html");
        await request.GetAsync($"{Server.Prefix}/unsetsetcookie.html");

        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage));

        Assert.That(serverRequest.Headers["cookie"], Is.Null.Or.Empty);
    }

    [UpstreamTest("FE-084 should preserve origin storage on import/export of storage state")]
    public async Task ShouldPreserveOriginStorageOnImportExportOfStorageState()
    {
        var storageState = new
        {
            cookies = new[]
            {
                new
                {
                    name = "a",
                    value = "b",
                    domain = "a.b.one.com",
                    path = "/",
                    expires = -1,
                    httpOnly = false,
                    secure = false,
                    sameSite = "Lax",
                },
            },
            origins = new[]
            {
                new
                {
                    origin = "https://www.example.com",
                    localStorage = new[]
                    {
                        new
                        {
                            name = "name1",
                            value = "value1",
                        },
                    },
                    opfs = new object[]
                    {
                        new
                        {
                            path = "hello.txt",
                            type = "file",
                            base64 = "SGVsbG8sIHdvcmxkIQ==",
                        },
                        new
                        {
                            path = "empty",
                            type = "directory",
                        },
                    },
                    indexedDB = new[]
                    {
                        new
                        {
                            name = "db",
                            version = 5,
                            stores = new[]
                            {
                                new
                                {
                                    name = "store",
                                    keyPath = "id",
                                    autoIncrement = false,
                                    indexes = Array.Empty<object>(),
                                    records = new[]
                                    {
                                        new
                                        {
                                            value = new { id = "foo", name = "John Doe" },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            },
        };
        // .NET: the storage state is its JSON text, for StorageState and from StorageStateAsync.
        var request = await Playwright.APIRequest.NewContextAsync(new() { StorageState = JsonStringify(storageState) });
        await request.GetAsync(Server.EmptyPage);
        var exportedState = JsonSerializer.Deserialize<JsonElement>(await request.StorageStateAsync(new() { IndexedDB = true, Opfs = true }));
        ExpectToEqual(exportedState, storageState);
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-086 storage state should round-trip through file")]
    [LocalFixme("dotnet: StorageStateAsync writes the JSON to Path on one line, upstream writes JSON.stringify(state, undefined, 2)")]
    public async Task StorageStateShouldRoundTripThroughFile()
    {
        var storageState = new
        {
            cookies = new[]
            {
                new
                {
                    name = "a",
                    value = "b",
                    domain = "a.b.one.com",
                    path = "/",
                    expires = -1,
                    httpOnly = false,
                    secure = false,
                    sameSite = "Lax",
                },
            },
            origins = Array.Empty<object>(),
        };

        // .NET: the storage state is its JSON text, for StorageState and from StorageStateAsync.
        var request1 = await Playwright.APIRequest.NewContextAsync(new() { StorageState = JsonStringify(storageState) });
        var path = Info.OutputPath("storage-state.json");
        var state1 = JsonSerializer.Deserialize<JsonElement>(await request1.StorageStateAsync(new() { Path = path }));
        ExpectToEqual(state1, storageState);

        var written = await File.ReadAllTextAsync(path);
        // .NET: JSON.stringify(state1, undefined, 2)
        Assert.That(JsonSerializer.Serialize(state1, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }), Is.EqualTo(written));

        var request2 = await Playwright.APIRequest.NewContextAsync(new() { StorageStatePath = path });
        var state2 = JsonSerializer.Deserialize<JsonElement>(await request2.StorageStateAsync());
        ExpectToEqual(state2, storageState);
    }

    [UpstreamTest("FE-087 should work with empty storage state")]
    public async Task ShouldWorkWithEmptyStorageState()
    {
        var storageState = Info.OutputPath("storage-state.json");
        await File.WriteAllTextAsync(storageState, "{}");
        var request1 = await Playwright.APIRequest.NewContextAsync(new() { StorageStatePath = storageState });

        // .NET: StorageStateAsync gives the JSON text of the storage state.
        var state1 = JsonSerializer.Deserialize<JsonElement>(await request1.StorageStateAsync());
        ExpectToEqual(state1, new { cookies = Array.Empty<object>(), origins = Array.Empty<object>() });
        await Expect(await request1.GetAsync(Server.EmptyPage)).ToBeOKAsync();
        await request1.DisposeAsync();
    }
}
