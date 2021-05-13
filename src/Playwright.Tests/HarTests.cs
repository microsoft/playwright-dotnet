using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Microsoft.Playwright.Har;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Tests.Helpers;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    /// <playwright-file>har.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public sealed class HarTests : PlaywrightSharpPageBaseTest
    {
        private readonly TempDirectory _tempDir = new TempDirectory();
        private string _harPath;
        private IBrowserContext _context;
        private IPage _page;

        /// <inheritdoc/>
        public HarTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
        public override async Task InitializeAsync()
        {
            _harPath = Path.Combine(_tempDir.Path, "test.har");
            _context = await Browser.NewContextAsync(recordHarPath: _harPath, ignoreHTTPSErrors: true);
            _page = await _context.NewPageAsync();

            await base.InitializeAsync();
        }

        /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
        public override Task DisposeAsync()
            => Task.WhenAll(_context.CloseAsync(), base.DisposeAsync());

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public override void Dispose() => _tempDir.Dispose();

        [PlaywrightTest("har.spec.ts", "should throw without path")]
        [Fact(Skip = "We don't need this test.")]
        public void ShouldThrowWithoutPath()
        {
        }

        [PlaywrightTest("har.spec.ts", "should have version and creator")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveVersionAndCreator()
        {
            await _page.GotoAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            Assert.Equal("1.2", log.Version);
            Assert.Equal("Playwright", log.Creator.Name);
        }

        [PlaywrightTest("har.spec.ts", "should have browser")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveBrowser()
        {
            await _page.GotoAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            Assert.Equal(TestConstants.Product.ToLower(), log.Browser.Name);
            Assert.Equal(Browser.Version, log.Browser.Version);
        }

        [PlaywrightTest("har.spec.ts", "should have pages")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHavePages()
        {
            await _page.GotoAsync("data:text/html,<title>Hello</title>");
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            var log = await GetLogAsync();
            Assert.Single(log.Pages);
            var pageEntry = log.Pages.First();
            Assert.Equal("page_0", pageEntry.Id);
            Assert.Equal("Hello", pageEntry.Title);
            Assert.True(pageEntry.StartedDateTime > DateTime.UtcNow.AddMinutes(-1));
            Assert.True(pageEntry.PageTimings.OnContentLoad > 0);
            Assert.True(pageEntry.PageTimings.OnLoad > 0);
        }

        [PlaywrightTest("har.spec.ts", "should have pages in persistent context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHavePagesInPersistentContext()
        {
            using var harPath = new TempDirectory();
            using var _persistentContextDir = new TempDirectory();
            string harFilePath = Path.Combine(harPath.Path, "test.har");

            var context = await BrowserType.LaunchPersistentContextAsync(_persistentContextDir.Path, recordHarPath: harFilePath);
            var page = context.Pages.FirstOrDefault();

            await TaskUtils.WhenAll(
                page.GotoAsync("data:text/html,<title>Hello</title>"),
                page.WaitForLoadStateAsync(LoadState.DOMContentLoaded));

            await context.CloseAsync();

            var log = GetHarResult(harFilePath).Log;
            Assert.Single(log.Pages);
            var pageEntry = log.Pages.First();
            Assert.Equal("page_0", pageEntry.Id);
            Assert.Equal("Hello", pageEntry.Title);
        }

        [PlaywrightTest("har.spec.ts", "should include request")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeRequest()
        {
            await _page.GotoAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            Assert.Single(log.Entries);
            var entry = log.Entries.First();
            Assert.Equal("page_0", entry.Pageref);
            Assert.Equal(TestConstants.EmptyPage, entry.Request.Url);
            Assert.Equal(HttpMethod.Get, entry.Request.Method);
            Assert.Equal("HTTP/1.1", entry.Request.HttpVersion);
            Assert.True(entry.Request.Headers.Count() > 1);
            Assert.NotNull(entry.Request.Headers.Where(h => h.Name.ToLower() == "user-agent"));
        }

        [PlaywrightTest("har.spec.ts", "should include response")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeResponse()
        {
            await _page.GotoAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            Assert.Single(log.Entries);
            var entry = log.Entries.First();
            Assert.Equal(200, entry.Response.Status);
            Assert.Equal("OK", entry.Response.StatusText);
            Assert.Equal("HTTP/1.1", entry.Response.HttpVersion);
            Assert.True(entry.Response.Headers.Count() > 1);
            Assert.NotNull(entry.Response.Headers.Where(h => h.Name.ToLower() == "user-agent"));
        }

        [PlaywrightTest("har.spec.ts", "should include redirectURL")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeRedirecturl()
        {
            Server.SetRedirect("/foo.html", "/empty.html");
            await _page.GotoAsync(TestConstants.ServerUrl + "/foo.html");
            var log = await GetLogAsync();
            Assert.Equal(2, log.Entries.Count());
            var entry = log.Entries.First();
            Assert.Equal(302, entry.Response.Status);
            Assert.Equal(TestConstants.EmptyPage, entry.Response.RedirectURL);
        }

        [PlaywrightTest("har.spec.ts", "should include query params")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeQueryParams()
        {
            await _page.GotoAsync(TestConstants.ServerUrl + "/har.html?name=value");
            var log = await GetLogAsync();
            Assert.Equal(
                new (string Name, string Value)[] { ("name", "value") }.ToJson(),
                log.Entries.First().Request.QueryString.ToJson());
        }

        [PlaywrightTest("har.spec.ts", "should include postData")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludePostdata()
        {
            await _page.GotoAsync(TestConstants.EmptyPage);
            await _page.EvaluateAsync("() => fetch('./post', { method: 'POST', body: 'Hello' })");
            var log = await GetLogAsync();
            Assert.Equal(
                new HarPostData
                {
                    MimeType = "text/plain;charset=UTF-8",
                    Text = "Hello"
                }.ToJson(),
                log.Entries.ElementAt(1).Request.PostData.ToJson());
        }

        [PlaywrightTest("har.spec.ts", "should include binary postData")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeBinaryPostdata()
        {
            await _page.GotoAsync(TestConstants.EmptyPage);
            await _page.EvaluateAsync("() => fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(16).keys())) })");
            var log = await GetLogAsync();
            Assert.Equal(
                new HarPostData
                {
                    MimeType = "application/octet-stream",
                    Text = ""
                }.ToJson(),
                log.Entries.ElementAt(1).Request.PostData.ToJson());
        }

        [PlaywrightTest("har.spec.ts", "should include form params")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeFormParams()
        {
            await _page.GotoAsync(TestConstants.EmptyPage);
            await _page.SetContentAsync("<form method='POST' action='/post'><input type='text' name='foo' value='bar'><input type='number' name='baz' value='123'><input type='submit'></form>");
            await _page.ClickAsync("input[type=submit]");
            var log = await GetLogAsync();
            Assert.Equal(
                new HarPostData
                {
                    MimeType = "application/x-www-form-urlencoded",
                    Params = new HarPostDataParam[]
                    {
                        new HarPostDataParam
                        {
                            Name ="foo",
                            Value= "bar"
                        },
                        new HarPostDataParam
                        {
                            Name = "baz",
                            Value = "123"
                        },
                    },
                    Text = "foo=bar&baz=123",
                }.ToJson(),
                log.Entries.ElementAt(1).Request.PostData.ToJson());
        }

        [PlaywrightTest("har.spec.ts", "should include cookies")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeCookies()
        {
            await _context.AddCookiesAsync(new[]
            {
                new Cookie { Name = "name1", Value = "value1", Domain = "localhost", Path = "/", HttpOnly = true },
                new Cookie { Name = "name2", Value = "val\"ue2", Domain = "localhost", Path = "/", SameSite = SameSiteAttribute.Lax },
                new Cookie { Name = "name3", Value = "val=ue3", Domain = "localhost", Path = "/" },
                new Cookie { Name = "name4", Value = "val,ue4", Domain = "localhost", Path = "/" },
            });

            await _page.GotoAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            Assert.Equal(new[]
            {
                new HarCookie { Name = "name1", Value = "value1", },
                new HarCookie { Name = "name2", Value = "val\"ue2", },
                new HarCookie { Name = "name3", Value = "val=ue3", },
                new HarCookie { Name = "name4", Value = "val,ue4", }
            }.ToJson(),
            log.Entries.First().Request.Cookies.ToJson());
        }

        [PlaywrightTest("har.spec.ts", "should include set-cookies")]
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipOSX: true)]
        public async Task ShouldIncludeSetCookies()
        {
            Server.SetRoute("/empty.html", ctx =>
            {
                ctx.Response.Headers.Add(
                    "Set-Cookie",
                    new StringValues(new[]
                    {
                      "name1=value1; HttpOnly",
                      "name2=value2",
                      "name3=value4; Path=/; Domain=example.com; Max-Age=1500"
                    }));
                return Task.CompletedTask;
            });
            await _page.GotoAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            var cookies = log.Entries.First().Response.Cookies;
            Assert.Equal(new HarCookie { Name = "name1", Value = "value1", HttpOnly = true }.ToJson(), cookies.ElementAt(0).ToJson());
            Assert.Equal(new HarCookie { Name = "name2", Value = "value2" }.ToJson(), cookies.ElementAt(1).ToJson());
            Assert.True(cookies.ElementAt(2).Expires > DateTimeOffset.Now);
        }

        [PlaywrightTest("har.spec.ts", "should include set-cookies with comma")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeSetCookiesWithComma()
        {
            Server.SetRoute("/empty.html", ctx =>
            {
                ctx.Response.Headers.Add("Set-Cookie", new StringValues(new[] { "name1=val,ue1" }));
                return Task.CompletedTask;
            });
            await _page.GotoAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            var cookies = log.Entries.First().Response.Cookies;
            Assert.Equal(new HarCookie { Name = "name1", Value = "val,ue1" }.ToJson(), cookies.First().ToJson());
        }

        [PlaywrightTest("har.spec.ts", "should include secure set-cookies")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeSecureSetCookies()
        {
            Server.SetRoute("/empty.html", ctx =>
            {
                ctx.Response.Headers.Add("Set-Cookie", new StringValues(new[] { "name1=value1; Secure" }));
                return Task.CompletedTask;
            });
            await _page.GotoAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            var cookies = log.Entries.First().Response.Cookies;
            Assert.Equal(new HarCookie { Name = "name1", Value = "value1", Secure = true }.ToJson(), cookies.First().ToJson());
        }

        [PlaywrightTest("har.spec.ts", "should include content")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeContent()
        {
            await _page.GotoAsync(TestConstants.ServerUrl + "/har.html");
            var log = await GetLogAsync();

            var content1 = log.Entries.ElementAt(0).Response.Content;
            Assert.Equal("base64", content1.Encoding);
            Assert.Equal("text/html; charset=utf-8", content1.MimeType);
            Assert.Contains("HAR Page", System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(content1.Text)));

            var content2 = log.Entries.ElementAt(1).Response.Content;
            Assert.Equal("base64", content2.Encoding);
            Assert.Contains("text/css", content2.MimeType);
            Assert.Contains("pink", System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(content2.Text)));
        }

#if NETCOREAPP
        /// <summary>
        /// We test that the har class we have, contains all the properties in the JSON file
        /// </summary>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRepresentTheHarFile()
        {
            await _page.GotoAsync(TestConstants.ServerUrl + "/har.html");
            await _context.CloseAsync();
            var log = GetHarResult(_harPath);
            string serialized = JsonSerializer.Serialize(log, SerializerOptions);
            string curatedHar = File.ReadAllText(_harPath)
                .Replace("\"beforeRequest\": null,", string.Empty)
                .Replace("\"afterRequest\": null", string.Empty);

            var logAsDynamic = JsonDocument.Parse(curatedHar);
            var serializedAsDynamic = JsonDocument.Parse(serialized);

            Assert.True(new JsonElementComparer().Equals(logAsDynamic.RootElement, serializedAsDynamic.RootElement));
        }
#endif

        private async Task<HarLog> GetLogAsync()
        {
            await _context.CloseAsync();
            return GetHarResult(_harPath).Log;
        }

        private HarResult GetHarResult(string harPath)
            => JsonSerializer.Deserialize<HarResult>(File.ReadAllText(harPath), SerializerOptions);

        private JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,
            Converters =
            {
                new HttpMethodConverter(),
                new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase),
            },
        };
    }
}
