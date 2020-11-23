using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
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
            _context = await Browser.NewContextAsync(
                recordHar: new RecordHarOptions { Path = _harPath },
                ignoreHTTPSErrors: true);
            _page = await _context.NewPageAsync();

            await base.InitializeAsync();
        }

        public override Task DisposeAsync()
            => Task.WhenAll(_context.CloseAsync(), base.DisposeAsync());

        public override void Dispose() => _tempDir.Dispose();

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should throw without path</playwright-it>
        [Fact(Skip = "We don't need this test.")]
        public void ShouldThrowWithoutPath()
        {
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should have version and creator</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldHaveVersionAndCreator()
        {
            await _page.GoToAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            Assert.Equal("1.2", log.Version);
            Assert.Equal("Playwright", log.Creator.Name);
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should have browser</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldHaveBrowser()
        {
            await _page.GoToAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            Assert.Equal(TestConstants.Product.ToLower(), log.Browser.Name);
            Assert.Equal(Browser.Version, log.Browser.Version);
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should have pages</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldHavePages()
        {
            await _page.GoToAsync("data:text/html,<title>Hello</title>");
            await _page.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);
            var log = await GetLogAsync();
            Assert.Single(log.Pages);
            var pageEntry = log.Pages.First();
            Assert.Equal("page_0", pageEntry.Id);
            Assert.Equal("Hello", pageEntry.Title);
            Assert.True(pageEntry.StartedDateTime > DateTime.Now.AddMinutes(-1));
            Assert.True(pageEntry.PageTimings.OnContentLoad > 0);
            Assert.True(pageEntry.PageTimings.OnLoad > 0);
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should have pages in persistent context</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldHavePagesInPersistentContext()
        {
            using var harPath = new TempDirectory();
            using var _persistentContextDir = new TempDirectory();
            string harFilePath = Path.Combine(harPath.Path, "test.har");
            var context = await BrowserType.LaunchPersistentContextAsync(
                _persistentContextDir.Path,
                recordHar: new RecordHarOptions { Path = harFilePath });
            var page = context.Pages[0];
            await page.GoToAsync("data:text/html,<title>Hello</title>");
            await page.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);
            await context.CloseAsync();

            var log = GetHarResult(harFilePath);
            Assert.Single(log.Pages);
            var pageEntry = log.Pages.First();
            Assert.Equal("page_0", pageEntry.Id);
            Assert.Equal("Hello", pageEntry.Title);
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should include request</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIncludeRequest()
        {
            await _page.GoToAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            Assert.Single(log.Entries);
            var entry = log.Entries.First();
            Assert.Equal("page_0", entry.Pageref);
            Assert.Equal(TestConstants.EmptyPage, entry.Url);
            Assert.Equal(TestConstants.EmptyPage, entry.Request.Url);
            Assert.Equal(HttpMethod.Get, entry.Request.Method);
            Assert.Equal("HTTP/1.1", entry.Request.HttpVersion);
            Assert.True(entry.Request.Headers.Count() > 1);
            Assert.NotNull(entry.Request.Headers.Where(h => h.Name.ToLower() == "user-agent"));
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should include response</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIncludeResponse()
        {
            await _page.GoToAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            Assert.Single(log.Entries);
            var entry = log.Entries.First();
            Assert.Equal(HttpStatusCode.OK, entry.Response.Status);
            Assert.Equal("OK", entry.Response.StatusText);
            Assert.Equal("HTTP/1.1", entry.Response.HttpVersion);
            Assert.True(entry.Response.Headers.Count() > 1);
            Assert.NotNull(entry.Response.Headers.Where(h => h.Name.ToLower() == "user-agent"));
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should include redirectURL</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIncludeRedirecturl()
        {
            Server.SetRedirect("/foo.html", "/empty.html");
            await _page.GoToAsync(TestConstants.ServerUrl + "/foo.html");
            var log = await GetLogAsync();
            Assert.Equal(2, log.Entries.Count());
            var entry = log.Entries.First();
            Assert.Equal(HttpStatusCode.Redirect, entry.Response.Status);
            Assert.Equal(TestConstants.EmptyPage, entry.Response.RedirectURL);
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should include query params</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIncludeQueryParams()
        {
            await _page.GoToAsync(TestConstants.ServerUrl + "/har.html?name=value");
            var log = await GetLogAsync();
            Assert.Equal(
                new[] { new HeaderEntry { Name = "name", Value = "value" } },
                log.Entries.First().Request.QueryString);
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should include postData</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIncludePostdata()
        {
            await _page.GoToAsync(TestConstants.EmptyPage);
            await _page.EvaluateAsync("(() => fetch('./post', { method: 'POST', body: 'Hello' })");
            var log = await GetLogAsync();
            Assert.Equal(
                new HarResult.HarPostData
                {
                    MimeType = "text/plain;charset=UTF-8",
                    Text = "Hello"
                }.ToJson(),
                log.Entries.ElementAt(1).Request.PostData.ToJson());
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should include binary postData</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIncludeBinaryPostdata()
        {
            await _page.GoToAsync(TestConstants.EmptyPage);
            await _page.EvaluateAsync("() => fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(16).keys())) })");
            var log = await GetLogAsync();
            Assert.Equal(
                new HarResult.HarPostData
                {
                    MimeType = "application/octet-stream",
                    Text = ""
                }.ToJson(),
                log.Entries.ElementAt(1).Request.PostData.ToJson());
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should include form params</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIncludeFormParams()
        {
            await _page.GoToAsync(TestConstants.EmptyPage);
            await _page.SetContentAsync("<form method='POST' action='/post'><input type='text' name='foo' value='bar'><input type='number' name='baz' value='123'><input type='submit'></form>");
            await _page.ClickAsync("input[type=submit]");
            var log = await GetLogAsync();
            Assert.Equal(
                new HarResult.HarPostData
                {
                    MimeType = "application/x-www-form-urlencoded",
                    Params = new[]
                    {
                        new HeaderEntry{ Name= "foo", Value ="bar" },
                        new HeaderEntry{ Name= "baz", Value ="123" },
                    },
                    Text = "foo=bar&baz=123",
                }.ToJson(),
                log.Entries.ElementAt(1).Request.PostData.ToJson());
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should include cookies</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIncludeCookies()
        {
            await _context.AddCookiesAsync(
                new SetNetworkCookieParam { Name = "name1", Value = "value1", Domain = "localhost", Path = "/", HttpOnly = true },
                new SetNetworkCookieParam { Name = "name2", Value = "val\"ue2", Domain = "localhost", Path = "/", SameSite = SameSite.Lax },
                new SetNetworkCookieParam { Name = "name3", Value = "val=ue3", Domain = "localhost", Path = "/" },
                new SetNetworkCookieParam { Name = "name4", Value = "val,ue4", Domain = "localhost", Path = "/" });

            await _page.GoToAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            Assert.Equal(new[]
            {
                new SetNetworkCookieParam { Name = "name1", Value = "value1", Domain = "localhost", Path = "/", HttpOnly = true },
                new SetNetworkCookieParam { Name = "name2", Value = "val\"ue2", Domain = "localhost", Path = "/", SameSite = SameSite.Lax },
                new SetNetworkCookieParam { Name = "name3", Value = "val=ue3", Domain = "localhost", Path = "/" },
                new SetNetworkCookieParam { Name = "name4", Value = "val,ue4", Domain = "localhost", Path = "/" }
            },
            log.Entries.First().Request.Cookies);
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should include set-cookies</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipOSX: true)]
        public async Task ShouldIncludeSetCookies()
        {
            Server.SetRoute("/empty", ctx =>
            {
                ctx.Response.Headers["Set-Cookie"] = @"
                  name1=value1; HttpOnly,
                  name2=""value2"",
                  name3=value4; Path=/; Domain=example.com; Max-Age=1500";
                return Task.CompletedTask;
            });
            await _page.GoToAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            var cookies = log.Entries.First().Response.Cookies;
            Assert.Equal(new SetNetworkCookieParam { Name = "name1", Value = "value1", HttpOnly = true }, cookies[0]);
            Assert.Equal(new SetNetworkCookieParam { Name = "name2", Value = "value2", HttpOnly = true }, cookies[1]);
            Assert.True(cookies[2].Expires > DateTimeOffset.Now.ToUnixTimeSeconds());
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should include set-cookies with comma</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIncludeSetCookiesWithComma()
        {
            Server.SetRoute("/empty", ctx =>
            {
                ctx.Response.Headers["Set-Cookie"] = @"name1=val,ue1";
                return Task.CompletedTask;
            });
            await _page.GoToAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            var cookies = log.Entries.First().Response.Cookies;
            Assert.Equal(new SetNetworkCookieParam { Name = "name1", Value = "val,ue1" }, cookies[0]);
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should include secure set-cookies</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIncludeSecureSetCookies()
        {
            Server.SetRoute("/empty", ctx =>
            {
                ctx.Response.Headers["Set-Cookie"] = @"name1=value1; Secure";
                return Task.CompletedTask;
            });
            await _page.GoToAsync(TestConstants.EmptyPage);
            var log = await GetLogAsync();
            var cookies = log.Entries.First().Response.Cookies;
            Assert.Equal(new SetNetworkCookieParam { Name = "name1", Value = "value1", Secure = true }, cookies[0]);
        }

        /// <playwright-file>har.spec.ts</playwright-file>
        /// <playwright-it>should include content</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIncludeContent()
        {
            await _page.GoToAsync(TestConstants.ServerUrl + "/har.html");
            var log = await GetLogAsync();

            var content1 = log.Entries.ElementAt(0).Response.Content;
            Assert.Equal("base64", content1.Encoding);
            Assert.Equal("text/html; charset=utf-8", content1.MimeType);
            Assert.Equal("HAR Page", System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(content1.Text)));

            var content2 = log.Entries.ElementAt(1).Response.Content;
            Assert.Equal("base64", content2.Encoding);
            Assert.Equal("text/css`; charset=utf-8", content2.MimeType);
            Assert.Equal("pink", System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(content2.Text)));
        }

        private async Task<HarResult.HarLog> GetLogAsync()
        {
            await _context.CloseAsync();
            return GetHarResult(_harPath);
        }

        private HarResult.HarLog GetHarResult(string harPath)
            => JsonSerializer.Deserialize<HarResult>(
                File.ReadAllText(harPath),
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters =
                    {
                new HttpMethodConverter(),
                new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase),
                    },
                }).Log;

        private class HarResult
        {
            public HarLog Log { get; set; }

            public class HarLog
            {
                public string Version { get; set; }
                public HarCreator Creator { get; set; }
                public HarBrowser Browser { get; set; }
                public IEnumerable<HarPage> Pages { get; set; }
                public IEnumerable<HarEntry> Entries { get; set; }

                public class HarCreator
                {
                    public string Name { get; set; }
                }

                public class HarBrowser
                {
                    public string Name { get; set; }
                    public string Version { get; set; }
                }

                public class HarPage
                {
                    public string Id { get; set; }
                    public string Title { get; set; }
                    public DateTime StartedDateTime { get; set; }
                    public HarPageTimings PageTimings { get; set; }

                    public class HarPageTimings
                    {
                        public decimal OnContentLoad { get; set; }
                        public decimal OnLoad { get; set; }
                    }
                }

                public class HarEntry
                {
                    public string Pageref { get; set; }
                    public string Url { get; set; }
                    public HarEntryRequest Request { get; set; }
                    public HarEntryResponse Response { get; internal set; }

                    public class HarEntryRequest
                    {
                        public string Url { get; set; }
                        public HttpMethod Method { get; set; }
                        public string HttpVersion { get; set; }
                        public IEnumerable<HarEntryHeader> Headers { get; set; }
                        public IEnumerable<HeaderEntry> QueryString { get; set; }
                        public HarPostData PostData { get; set; }
                        public IEnumerable<SetNetworkCookieParam> Cookies { get; set; }
                    }

                    public class HarEntryHeader
                    {
                        public string Name { get; set; }
                    }

                    public class HarEntryResponse
                    {
                        public string HttpVersion { get; set; }
                        public IEnumerable<HarEntryHeader> Headers { get; set; }
                        public HttpStatusCode Status { get; internal set; }
                        public string StatusText { get; internal set; }
                        public string RedirectURL { get; set; }
                        public SetNetworkCookieParam[] Cookies { get; set; }
                        public HarContent Content { get; set; }

                        public class HarContent
                        {
                            public string Encoding { get; set; }
                            public string MimeType { get; set; }
                            public string Text { get; set; }
                        }
                    }
                }
            }

            public class HarPostData
            {
                public string MimeType { get; set; }
                public string Text { get; set; }
                public HeaderEntry[] Params { get; set; } = new HeaderEntry[] { };
            }
        }
    }
}
