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
using System.Text;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/global-fetch.spec.ts.</summary>
public class GlobalFetchTests : UpstreamBrowserTest
{
    // .NET: it.use({ context: async ({}, use) => { throw new Error('global fetch tests should not use context'); } })
    // is left out: a test of this class has no context unless it asks for one.

    [SetUp]
    public void Skip() => Info.Skip(Mode != "default");

    [UpstreamTest("FE-001 ${method} should work @smoke")]
    [TestCase("fetch")]
    [TestCase("delete")]
    [TestCase("get")]
    [TestCase("head")]
    [TestCase("patch")]
    [TestCase("post")]
    [TestCase("put")]
    public async Task ShouldWork(string method)
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        // .NET: request[method](url)
        Task<IAPIResponse> requestMethod(string url) => method switch
        {
            "fetch" => request.FetchAsync(url),
            "delete" => request.DeleteAsync(url),
            "get" => request.GetAsync(url),
            "head" => request.HeadAsync(url),
            "patch" => request.PatchAsync(url),
            "post" => request.PostAsync(url),
            "put" => request.PutAsync(url),
            _ => throw new ArgumentException(method, nameof(method)),
        };
        var response = await requestMethod(Server.Prefix + "/simple.json");
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/simple.json"));
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.StatusText, Is.EqualTo("OK"));
        Assert.That(response.Ok, Is.True);
        Assert.That(response.Headers["content-type"], Is.EqualTo("application/json; charset=utf-8"));
        Assert.That(response.HeadersArray, Has.Some.Matches<Header>(header => DeepEquals(header, new { name = "Content-Type", value = "application/json; charset=utf-8" })));
        Assert.That(await response.TextAsync(), Is.EqualTo("head" == method ? "" : "{\"foo\": \"bar\"}\n"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-002 should dispose global request")]
    public async Task ShouldDisposeGlobalRequest()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        var response = await request.GetAsync(Server.Prefix + "/simple.json");
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
        await request.DisposeAsync();
        var error = await Catch(() => response.BodyAsync());
        Assert.That(error.Message, Does.Contain("Response has been disposed"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-003 should support global userAgent option")]
    public async Task ShouldSupportGlobalUserAgentOption()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new() { UserAgent = "My Agent" });
        var (serverRequest, response) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage));
        Assert.That(response.Ok, Is.True);
        Assert.That(response.Url, Is.EqualTo(Server.EmptyPage));
        Assert.That(serverRequest.Headers["user-agent"], Is.EqualTo("My Agent"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-004 should support global timeout option")]
    public async Task ShouldSupportGlobalTimeoutOption()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new() { Timeout = 100 });
        Server.SetRoute("/empty.html", (req, res) => { });
        var error = await Catch(() => request.GetAsync(Server.EmptyPage));
        Assert.That(error.Message, Does.Contain("Timeout 100ms exceeded"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-005 should propagate extra http headers with redirects")]
    public async Task ShouldPropagateExtraHttpHeadersWithRedirects()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/simple.json");
        var request = await Playwright.APIRequest.NewContextAsync(new() { ExtraHTTPHeaders = new Dictionary<string, string> { ["My-Secret"] = "Value" } });
        var (req1, req2, req3, _) = await PromiseAll(
            Server.WaitForRequest("/a/redirect1"),
            Server.WaitForRequest("/b/c/redirect2"),
            Server.WaitForRequest("/simple.json"),
            request.GetAsync($"{Server.Prefix}/a/redirect1"));
        Assert.That(req1.Headers["my-secret"], Is.EqualTo("Value"));
        Assert.That(req2.Headers["my-secret"], Is.EqualTo("Value"));
        Assert.That(req3.Headers["my-secret"], Is.EqualTo("Value"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-006 should preserve authorization on same-origin redirect but strip on cross-origin")]
    public async Task ShouldPreserveAuthorizationOnSameOriginRedirectButStripOnCrossOrigin()
    {
        Server.SetRedirect("/same/redirect", "/same/dest");
        Server.SetRedirect("/cross/redirect", Server.CrossProcessPrefix + "/cross/dest");
        var request = await Playwright.APIRequest.NewContextAsync(new()
        {
            ExtraHTTPHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer secret" },
        });

        var (sameDestReq, _) = await PromiseAll(
            Server.WaitForRequest("/same/dest"),
            request.GetAsync($"{Server.Prefix}/same/redirect"));
        Assert.That(sameDestReq.Headers["authorization"], Is.EqualTo("Bearer secret"));

        var (crossDestReq, _) = await PromiseAll(
            Server.WaitForRequest("/cross/dest"),
            request.GetAsync($"{Server.Prefix}/cross/redirect"));
        Assert.That(crossDestReq.Headers["authorization"], Is.Null);

        await request.DisposeAsync();
    }

    [UpstreamTest("FE-007 should support global httpCredentials option")]
    public async Task ShouldSupportGlobalHttpCredentialsOption()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        var request1 = await Playwright.APIRequest.NewContextAsync();
        var response1 = await request1.GetAsync(Server.EmptyPage);
        Assert.That(response1.Status, Is.EqualTo(401));
        await request1.DisposeAsync();

        var request2 = await Playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass" } });
        var response2 = await request2.GetAsync(Server.EmptyPage);
        Assert.That(response2.Status, Is.EqualTo(200));
        await request2.DisposeAsync();
    }

    [UpstreamTest("FE-008 should return error with wrong credentials")]
    public async Task ShouldReturnErrorWithWrongCredentials()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        var request = await Playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "wrong" } });
        var response = await request.GetAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(401));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-009 should work with correct credentials and matching origin")]
    public async Task ShouldWorkWithCorrectCredentialsAndMatchingOrigin()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        var request = await Playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass", Origin = Server.Prefix } });
        var response = await request.GetAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-010 should work with correct credentials and matching origin case insensitive")]
    public async Task ShouldWorkWithCorrectCredentialsAndMatchingOriginCaseInsensitive()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        var request = await Playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass", Origin = Server.Prefix.ToUpperInvariant() } });
        var response = await request.GetAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-011 should return error with correct credentials and mismatching scheme")]
    public async Task ShouldReturnErrorWithCorrectCredentialsAndMismatchingScheme()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        var request = await Playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass", Origin = ReplaceFirst(Server.Prefix, "http://", "https://") } });
        var response = await request.GetAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(401));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-012 should return error with correct credentials and mismatching hostname")]
    public async Task ShouldReturnErrorWithCorrectCredentialsAndMismatchingHostname()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        var hostname = new Uri(Server.Prefix).Host;
        var origin = ReplaceFirst(Server.Prefix, hostname, "mismatching-hostname");
        var request = await Playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass", Origin = origin } });
        var response = await request.GetAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(401));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-013 should return error with correct credentials and mismatching port")]
    public async Task ShouldReturnErrorWithCorrectCredentialsAndMismatchingPort()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        var origin = ReplaceFirst(Server.Prefix, Server.Port.ToString(CultureInfo.InvariantCulture), (Server.Port + 1).ToString(CultureInfo.InvariantCulture));
        var request = await Playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass", Origin = origin } });
        var response = await request.GetAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(401));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-014 should support WWW-Authenticate: Basic")]
    public async Task ShouldSupportWWWAuthenticateBasic()
    {
        string credentials = null;
        Server.SetRoute("/empty.html", (req, res) =>
        {
            if (string.IsNullOrEmpty(req.Headers["authorization"]))
            {
                res.WriteHead(401, new() { ["WWW-Authenticate"] = "Basic" });
                res.End("HTTP Error 401 Unauthorized: Access is denied");
                return;
            }
            credentials = Encoding.UTF8.GetString(Convert.FromBase64String(req.Headers["authorization"].Split(' ').ElementAtOrDefault(1) ?? ""));
            res.WriteHead(200, new() { ["content-type"] = "text/plain" });
            res.End();
        });
        var request = await Playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass" } });
        var response = await request.GetAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(credentials, Is.EqualTo("user:pass"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-015 should support HTTPCredentials.send")]
    public async Task ShouldSupportHTTPCredentialsSend()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new()
        {
            HttpCredentials = new() { Username = "user", Password = "pass", Origin = Server.Prefix.ToUpperInvariant(), Send = HttpCredentialsSend.Always },
        });
        {
            var (serverRequest, response) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                request.GetAsync(Server.EmptyPage));
            Assert.That(serverRequest.Headers["authorization"], Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"))));
            Assert.That(response.Status, Is.EqualTo(200));
        }
        {
            var (serverRequest, response) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                request.GetAsync(Server.CrossProcessPrefix + "/empty.html"));
            // Not sent to another origin.
            Assert.That(serverRequest.Headers["authorization"], Is.Null);
            Assert.That(response.Status, Is.EqualTo(200));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-016 should support multiple httpCredentials")]
    public async Task ShouldSupportMultipleHttpCredentials()
    {
        Server.SetAuth("/empty.html", "user1", "pass1");
        var request = await Playwright.APIRequest.NewContextAsync(new()
        {
            HttpCredentialsList = new HttpCredentials[]
            {
                new() { Username = "user1", Password = "pass1", Origin = Server.Prefix },
                new() { Username = "user2", Password = "pass2", Origin = Server.CrossProcessPrefix },
            },
        });
        var response1 = await request.GetAsync(Server.EmptyPage);
        Assert.That(response1.Status, Is.EqualTo(200));
        // Wrong credentials are picked for the other origin.
        var response2 = await request.GetAsync(Server.CrossProcessPrefix + "/empty.html");
        Assert.That(response2.Status, Is.EqualTo(401));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-017 should support HTTPCredentials.send with multiple httpCredentials")]
    public async Task ShouldSupportHTTPCredentialsSendWithMultipleHttpCredentials()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new()
        {
            HttpCredentialsList = new HttpCredentials[]
            {
                new() { Username = "user1", Password = "pass1", Origin = Server.Prefix, Send = HttpCredentialsSend.Always },
                new() { Username = "user2", Password = "pass2", Origin = Server.CrossProcessPrefix, Send = HttpCredentialsSend.Unauthorized },
            },
        });
        {
            var (serverRequest, response) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                request.GetAsync(Server.EmptyPage));
            Assert.That(serverRequest.Headers["authorization"], Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user1:pass1"))));
            Assert.That(response.Status, Is.EqualTo(200));
        }
        {
            var (serverRequest, response) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                request.GetAsync(Server.CrossProcessPrefix + "/empty.html"));
            // This origin has send: 'unauthorized', so credentials are not sent proactively.
            Assert.That(serverRequest.Headers["authorization"], Is.Null);
            Assert.That(response.Status, Is.EqualTo(200));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-018 should support global ignoreHTTPSErrors option")]
    public async Task ShouldSupportGlobalIgnoreHTTPSErrorsOption()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var response = await request.GetAsync(HttpsServer.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-019 should propagate ignoreHTTPSErrors on redirects")]
    public async Task ShouldPropagateIgnoreHTTPSErrorsOnRedirects()
    {
        HttpsServer.SetRedirect("/redir", "/empty.html");
        var request = await Playwright.APIRequest.NewContextAsync();
        var response = await request.GetAsync(HttpsServer.Prefix + "/redir", new() { IgnoreHTTPSErrors = true });
        Assert.That(response.Status, Is.EqualTo(200));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-020 should return server address from response")]
    public async Task ShouldReturnServerAddressFromResponse()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        // The second request reuses the keep-alive socket and should report the address as well.
        for (var i = 0; i < 2; i++)
        {
            var response = await request.GetAsync(Server.EmptyPage);
            var addr = await response.ServerAddrAsync();
            Assert.That(addr.IpAddress, Does.Match(@"^(127\.0\.0\.1|::1)$"));
            Assert.That(addr.Port, Is.EqualTo(Server.Port));
        }
        Server.SetRoute("/idle", (req, res) =>
        {
            // Keep the server socket open beyond its advertised timeout.
            res.OnFinish(() => req.Socket.SetTimeout(30_000));
            res.SetHeader("Keep-Alive", "timeout=2");
            res.End("ok");
        });
        var (serverRequest, _) = await PromiseAll(
            Server.WaitForRequest("/idle"),
            request.GetAsync(Server.Prefix + "/idle"));
        await Expect(() => Assert.That(serverRequest.Socket.Destroyed, Is.EqualTo(true))).ToPassAsync(new() { Timeout = 7000 });
        // .NET: 'response' is also a local of the loop above.
        var response2 = await request.GetAsync(Server.EmptyPage);
        ExpectToEqual(await response2.ServerAddrAsync(), new { ipAddress = AnyString, port = Server.Port });
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-021 should return security details from response")]
    [LocalFixme("harness: the HTTPS server of the tests speaks TLS 1.2 on macOS, where SslStream of .NET has no TLS 1.3 for a server: the protocol is 'TLSv1.2', upstream expects 'TLSv1.3' (only on darwin)")]
    public async Task ShouldReturnSecurityDetailsFromResponse()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        // The second request reuses the keep-alive socket and should report the details as well.
        for (var i = 0; i < 2; i++)
        {
            var response = await request.GetAsync(HttpsServer.EmptyPage);
            ExpectToEqual(await response.SecurityDetailsAsync(), new { issuer = "playwright-test", protocol = "TLSv1.3", subjectName = "playwright-test", validFrom = 1691708270, validTo = 2007068270 });
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-022 should return security details for a resumed TLS session")]
    [LocalFixme("harness: the HTTPS server of the tests speaks TLS 1.2 on macOS, where SslStream of .NET has no TLS 1.3 for a server: the protocol is 'TLSv1.2', upstream expects 'TLSv1.3' (only on darwin)")]
    public async Task ShouldReturnSecurityDetailsForAResumedTLSSession()
    {
        var expected = new { issuer = "playwright-test", protocol = "TLSv1.3", subjectName = "playwright-test", validFrom = 1691708270, validTo = 2007068270 };
        var request = await Playwright.APIRequest.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        // 'Connection: close' drops the socket, so the next request opens a new one and resumes
        // the cached TLS session. A resumed session does not carry the server certificate.
        var first = await request.GetAsync(HttpsServer.EmptyPage, new() { Headers = new Dictionary<string, string> { ["connection"] = "close" } });
        ExpectToEqual(await first.SecurityDetailsAsync(), expected);
        var second = await request.GetAsync(HttpsServer.EmptyPage);
        ExpectToEqual(await second.SecurityDetailsAsync(), expected);
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-023 should return null security details for http response")]
    public async Task ShouldReturnNullSecurityDetailsForHttpResponse()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        var response = await request.GetAsync(Server.EmptyPage);
        Assert.That(await response.SecurityDetailsAsync(), Is.Null);
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-024 should return security details for certificate with multiple CN attributes")]
    public async Task ShouldReturnSecurityDetailsForCertificateWithMultipleCNAttributes()
    {
        // .NET: utils.createHttpsServer({ key, cert }, handler) that listens on the port 0 is a Server with a route.
        var server = Server.CreateHttps(Asset(""), 0, null, Server.Certificate(Asset("multi-value-rdn/cert.pem"), Asset("multi-value-rdn/key.pem")));
        server.SetRoute("/", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "application/json" });
            res.End(JsonStringify(new { ok = true }));
        });
        try
        {
            var request = await Playwright.APIRequest.NewContextAsync(new() { IgnoreHTTPSErrors = true });
            var response = await request.GetAsync($"https://localhost:{server.Port}/");
            Assert.That(response.Status, Is.EqualTo(200));
            var securityDetails = await response.SecurityDetailsAsync();
            Assert.That(securityDetails.SubjectName, Is.EqualTo("localhost"));
            Assert.That(securityDetails.Issuer, Is.EqualTo("localhost"));
            await request.DisposeAsync();
        }
        finally
        {
            await server.StopAsync();
        }
    }

    [UpstreamTest("FE-025 should resolve url relative to global baseURL option")]
    public async Task ShouldResolveUrlRelativeToGlobalBaseURLOption()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new() { BaseURL = Server.Prefix });
        var response = await request.GetAsync("/empty.html");
        Assert.That(response.Url, Is.EqualTo(Server.EmptyPage));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-026 should fallback to given URL if baseURL is bogus")]
    public async Task ShouldFallbackToGivenURLIfBaseURLIsBogus()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new() { BaseURL = "bogus" });
        var response = await request.GetAsync(Server.Prefix + "/empty.html");
        Assert.That(response.Url, Is.EqualTo(Server.EmptyPage));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-029 should return empty body")]
    public async Task ShouldReturnEmptyBody()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        var response = await request.GetAsync(Server.EmptyPage);
        var body = await response.BodyAsync();
        Assert.That(body.Length, Is.EqualTo(0));
        Assert.That(await response.TextAsync(), Is.EqualTo(""));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-030 should abort requests when context is disposed")]
    public async Task ShouldAbortRequestsWhenContextIsDisposed()
    {
        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Server.SetRoute("/empty.html", req => req.Socket.OnClose(() => connectionClosed.TrySetResult()));
        var request = await Playwright.APIRequest.NewContextAsync();
        // .NET: the last promise has no value; it gives null to fit the array of the results.
        async Task<Exception> Then()
        {
            await Server.WaitForRequest("/empty.html");
            await request.DisposeAsync();
            return null;
        }
        var results = await PromiseAll(new[]
        {
            Catch(() => request.GetAsync(Server.EmptyPage)),
            Catch(() => request.PostAsync(Server.EmptyPage)),
            Catch(() => request.DeleteAsync(Server.EmptyPage)),
            Then(),
        });
        foreach (var result in results[..^1])
        {
            Assert.That(result is Exception, Is.True);
            Assert.That(result.Message, Does.Contain("Request context disposed."));
        }
        await connectionClosed.Task;
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-031 should abort redirected requests when context is disposed")]
    public async Task ShouldAbortRedirectedRequestsWhenContextIsDisposed()
    {
        Server.SetRedirect("/redirect", "/test");
        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Server.SetRoute("/test", req => req.Socket.OnClose(() => connectionClosed.TrySetResult()));
        var request = await Playwright.APIRequest.NewContextAsync();
        async Task Then()
        {
            await Server.WaitForRequest("/test");
            await request.DisposeAsync();
        }
        var result = await PromiseAll(
            Catch(() => request.GetAsync(Server.Prefix + "/redirect")),
            Then());
        Assert.That(result is Exception, Is.True);
        Assert.That(result.Message, Does.Match("Request context disposed|Target page, context or browser has been closed"));
        await connectionClosed.Task;
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-032 should remove content-length from redirected post requests")]
    public async Task ShouldRemoveContentLengthFromRedirectedPostRequests()
    {
        Server.SetRedirect("/redirect", "/empty.html");
        var request = await Playwright.APIRequest.NewContextAsync();
        var (result, req1, req2) = await PromiseAll(
            request.PostAsync(Server.Prefix + "/redirect", new()
            {
                DataObject = new
                {
                    foo = "bar",
                },
            }),
            Server.WaitForRequest("/redirect"),
            Server.WaitForRequest("/empty.html"));
        Assert.That(result.Status, Is.EqualTo(200));
        Assert.That(req1.Headers["content-length"], Is.EqualTo("13"));
        Assert.That(req2.Headers["content-length"], Is.Null);
        await request.DisposeAsync();
    }

    private static readonly object[][] Serialization =
    [
        ["object", new { foo = "bar" }],
        ["array", new object[] { "foo", "bar", 2021 }],
        ["string", "foo"],
        ["string (falsey)", ""],
        ["bool", true],
        ["bool (false)", false],
        ["number", 2021],
        ["number (falsey)", 0],
        ["null", null],
        ["literal string undefined", "undefined"],
    ];

    [UpstreamTest("FE-033 should json stringify ${type} body when content-type is application/json")]
    [LocalFixme("dotnet: FetchAsync ignores Data = \"\" (string.IsNullOrEmpty) and posts an empty body, upstream posts '\"\"' (only for \"string (falsey)\"); dotnet: DataObject = null is 'no data': the API can not post the JSON value null and posts an empty body, upstream posts 'null' (only for \"null\")")]
    [TestCaseSource(nameof(Serialization))]
    public async Task ShouldJsonStringifyBodyWhenContentTypeIsApplicationJson(string type, object value)
    {
        var stringifiedValue = JsonStringify(value);
        var request = await Playwright.APIRequest.NewContextAsync();
        foreach (var contentType in new[] { "application/json", "Application/JSON; charset=utf-8" })
        {
            // .NET: data: value is Data for a string and DataObject for any other value.
            var (req, _) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                request.PostAsync(Server.EmptyPage, new()
                {
                    Headers = new Dictionary<string, string>
                    {
                        ["content-type"] = contentType,
                    },
                    Data = value as string,
                    DataObject = value is string ? null : value,
                }));
            var body = await req.PostBody;
            Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo(stringifiedValue));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-034 should not double stringify ${type} body when content-type is application/json")]
    [TestCaseSource(nameof(Serialization))]
    public async Task ShouldNotDoubleStringifyBodyWhenContentTypeIsApplicationJson(string type, object value)
    {
        var stringifiedValue = JsonStringify(value);
        var request = await Playwright.APIRequest.NewContextAsync();
        var (req, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.PostAsync(Server.EmptyPage, new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["content-type"] = "application/json",
                },
                Data = stringifiedValue,
            }));
        var body = await req.PostBody;
        Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo(stringifiedValue));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-035 should accept already serialized data as Buffer when content-type is application/json")]
    public async Task ShouldAcceptAlreadySerializedDataAsBufferWhenContentTypeIsApplicationJson()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        var value = JsonStringify(JsonStringify(new { foo = "bar" }));
        var (req, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.PostAsync(Server.EmptyPage, new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["content-type"] = "application/json",
                },
                DataByte = Encoding.UTF8.GetBytes(value),
            }));
        var body = await req.PostBody;
        Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo(value));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-036 should have nice toString")]
    public async Task ShouldHaveNiceToString()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        var response = await request.PostAsync(Server.EmptyPage, new()
        {
            Headers = new Dictionary<string, string>
            {
                ["content-type"] = "application/json",
            },
            Data = "My post data",
        });
        // .NET: response[util.inspect.custom]()
        var str = response.ToString();
        Assert.That(str, Does.Contain("APIResponse: 200 OK"));
        foreach (var header in response.HeadersArray)
        {
            Assert.That(str, Does.Contain($"  {header.Name}: {header.Value}"));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-037 should not fail on empty body with encoding")]
    public async Task ShouldNotFailOnEmptyBodyWithEncoding()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        foreach (var method in new[] { "head", "put" })
        {
            foreach (var encoding in new[] { "br", "gzip", "deflate" })
            {
                Server.SetRoute("/empty.html", (req, res) =>
                {
                    res.WriteHead(200, new()
                    {
                        ["Content-Encoding"] = encoding,
                        ["Content-Type"] = "text/plain",
                    });
                    res.End();
                });
                // .NET: request[method](url)
                var response = await (method == "head" ? request.HeadAsync(Server.EmptyPage) : request.PutAsync(Server.EmptyPage));
                Assert.That(response.Status, Is.EqualTo(200));
                Assert.That((await response.BodyAsync()).Length, Is.EqualTo(0));
            }
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-038 should return body for failing requests")]
    public async Task ShouldReturnBodyForFailingRequests()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        foreach (var method in new[] { "head", "put", "trace" })
        {
            Server.SetRoute("/empty.html", (req, res) =>
            {
                res.WriteHead(404, new() { ["Content-Length"] = 10, ["Content-Type"] = "text/plain" });
                res.End("Not found.");
            });
            var response = await request.FetchAsync(Server.EmptyPage, new() { Method = method });
            Assert.That(response.Status, Is.EqualTo(404));
            // HEAD response returns empty body in node http module.
            Assert.That(await response.TextAsync(), Is.EqualTo(method == "head" ? "" : "Not found."));
        }
        await request.DisposeAsync();
    }

    private static readonly string[] HTTP_METHODS = ["GET", "PUT", "POST", "OPTIONS", "HEAD", "PATCH"];

    [UpstreamTest("FE-039 should throw an error when maxRedirects is exceeded")]
    public async Task ShouldThrowAnErrorWhenMaxRedirectsIsExceeded()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/b/c/redirect3");
        Server.SetRedirect("/b/c/redirect3", "/b/c/redirect4");
        Server.SetRedirect("/b/c/redirect4", "/simple.json");

        var request = await Playwright.APIRequest.NewContextAsync();
        foreach (var method in HTTP_METHODS)
        {
            foreach (var maxRedirects in new[] { 1, 2, 3 })
            {
                var error = await ThrowsAsync<PlaywrightException>(() => request.FetchAsync($"{Server.Prefix}/a/redirect1", new() { Method = method, MaxRedirects = maxRedirects }));
                Assert.That(error.Message, Does.Contain("Max redirect count exceeded"));
            }
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-040 should not follow redirects when maxRedirects is set to 0")]
    public async Task ShouldNotFollowRedirectsWhenMaxRedirectsIsSetTo0()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/simple.json");

        var request = await Playwright.APIRequest.NewContextAsync();
        foreach (var method in HTTP_METHODS)
        {
            var response = await request.FetchAsync($"{Server.Prefix}/a/redirect1", new() { Method = method, MaxRedirects = 0 });
            Assert.That(response.Headers["location"], Is.EqualTo("/b/c/redirect2"));
            Assert.That(response.Status, Is.EqualTo(302));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-041 should throw an error when maxRedirects is less than 0")]
    [LocalFixme("dotnet: the message is \"'MaxRedirects' must be greater than or equal to '0'\", upstream expects \"'maxRedirects' must be greater than or equal to '0'\"")]
    public async Task ShouldThrowAnErrorWhenMaxRedirectsIsLessThan0()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/simple.json");

        var request = await Playwright.APIRequest.NewContextAsync();
        foreach (var method in HTTP_METHODS)
        {
            var error = await ThrowsAsync<PlaywrightException>(() => request.FetchAsync($"{Server.Prefix}/a/redirect1", new() { Method = method, MaxRedirects = -1 }));
            Assert.That(error.Message, Does.Contain("'maxRedirects' must be greater than or equal to '0'"));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-042 should not follow redirects when maxRedirects is set to 0 in newContext")]
    public async Task ShouldNotFollowRedirectsWhenMaxRedirectsIsSetTo0InNewContext()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/simple.json");

        var request = await Playwright.APIRequest.NewContextAsync(new() { MaxRedirects = 0 });
        foreach (var method in HTTP_METHODS)
        {
            var response = await request.FetchAsync($"{Server.Prefix}/a/redirect1", new() { Method = method });
            Assert.That(response.Headers["location"], Is.EqualTo("/b/c/redirect2"));
            Assert.That(response.Status, Is.EqualTo(302));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-043 should follow redirects up to maxRedirects limit set in newContext")]
    public async Task ShouldFollowRedirectsUpToMaxRedirectsLimitSetInNewContext()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/b/c/redirect3");
        Server.SetRedirect("/b/c/redirect3", "/b/c/redirect4");
        Server.SetRedirect("/b/c/redirect4", "/simple.json");

        foreach (var maxRedirects in new[] { 1, 2, 3, 4 })
        {
            var request = await Playwright.APIRequest.NewContextAsync(new() { MaxRedirects = maxRedirects });
            foreach (var method in HTTP_METHODS)
            {
                if (maxRedirects < 4)
                {
                    var error = await ThrowsAsync<PlaywrightException>(() => request.FetchAsync($"{Server.Prefix}/a/redirect1", new() { Method = method }));
                    Assert.That(error.Message, Does.Contain("Max redirect count exceeded"));
                }
                else
                {
                    var response = await request.FetchAsync($"{Server.Prefix}/a/redirect1", new() { Method = method });
                    Assert.That(response.Status, Is.EqualTo(200));
                }
            }
            await request.DisposeAsync();
        }
    }

    [UpstreamTest("FE-044 should use maxRedirects from fetch when provided, overriding newContext")]
    public async Task ShouldUseMaxRedirectsFromFetchWhenProvidedOverridingNewContext()
    {
        Server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        Server.SetRedirect("/b/c/redirect2", "/b/c/redirect3");
        Server.SetRedirect("/b/c/redirect3", "/b/c/redirect4");
        Server.SetRedirect("/b/c/redirect4", "/simple.json");

        var request = await Playwright.APIRequest.NewContextAsync(new() { MaxRedirects = 1 });
        foreach (var method in HTTP_METHODS)
        {
            var response = await request.FetchAsync($"{Server.Prefix}/a/redirect1", new() { Method = method, MaxRedirects = 4 });
            Assert.That(response.Status, Is.EqualTo(200));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-045 should keep headers capitalization")]
    public async Task ShouldKeepHeadersCapitalization()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        var (serverRequest, response) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            request.GetAsync(Server.EmptyPage, new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["X-fOo"] = "vaLUE",
                },
            }));
        Assert.That(response.Ok, Is.True);
        Assert.That(serverRequest.RawHeaders, Does.Contain("X-fOo"));
        Assert.That(serverRequest.RawHeaders, Does.Contain("vaLUE"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-047 should throw after dispose")]
    public async Task ShouldThrowAfterDispose()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        await request.DisposeAsync();
        var error = await ThrowsAsync<PlaywrightException>(() => request.GetAsync(Server.EmptyPage));
        Assert.That(error.Message, Does.Contain("Target page, context or browser has been closed"));
    }

    [UpstreamTest("FE-048 should retry ECONNRESET")]
    public async Task ShouldRetryECONNRESET()
    {
        var request = await Playwright.APIRequest.NewContextAsync();
        var requestCount = 0;
        Server.SetRoute("/test", (req, res) =>
        {
            if (requestCount++ < 3)
            {
                req.Socket.Destroy();
                return;
            }
            res.WriteHead(200, new() { ["content-type"] = "text/plain" });
            res.End("Hello!");
        });
        var response = await request.FetchAsync(Server.Prefix + "/test", new() { MaxRetries = 3 });
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await response.TextAsync(), Is.EqualTo("Hello!"));
        Assert.That(requestCount, Is.EqualTo(4));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-049 should expose node error code on network errors")]
    public async Task ShouldExposeNodeErrorCodeOnNetworkErrors()
    {
        // Reset the connection so that node reports a system error instead of a plain 'socket hang up'.
        Server.SetRoute("/reset", req => req.Socket.ResetAndDestroy());
        var request = await Playwright.APIRequest.NewContextAsync();
        var error = await Catch(() => request.GetAsync(Server.Prefix + "/reset", new() { MaxRetries = 0 }));
        Assert.That(error.Message, Does.Contain("ECONNRESET"));
        // .NET: expect(error.code).toBe('ECONNRESET'), expect(error.syscall).toBe(undefined) and expect(error.errno).toBe(undefined) are left out: PlaywrightException has no code, syscall and errno.
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-050 should append node error code to the message")]
    public async Task ShouldAppendNodeErrorCodeToTheMessage()
    {
        // Node reports this one as a plain 'socket hang up' that does not mention the code.
        Server.SetRoute("/hangup", req => req.Socket.Destroy());
        var request = await Playwright.APIRequest.NewContextAsync();
        var error = await Catch(() => request.GetAsync(Server.Prefix + "/hangup", new() { MaxRetries = 0 }));
        Assert.That(error.Message, Does.Contain("socket hang up (ECONNRESET)"));
        // .NET: expect(error.code).toBe('ECONNRESET') is left out: PlaywrightException has no code.
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-052 should throw when failOnStatusCode is set to true inside APIRequest context options")]
    public async Task ShouldThrowWhenFailOnStatusCodeIsSetToTrueInsideAPIRequestContextOptions()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new() { FailOnStatusCode = true });
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.WriteHead(404, new() { ["Content-Length"] = 10, ["Content-Type"] = "text/plain" });
            res.End("Not found.");
        });
        var error = await Catch(() => request.FetchAsync(Server.EmptyPage));
        Assert.That(error.Message, Does.Contain("404 Not Found"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-053 should not throw when failOnStatusCode is set to false inside APIRequest context options")]
    public async Task ShouldNotThrowWhenFailOnStatusCodeIsSetToFalseInsideAPIRequestContextOptions()
    {
        var request = await Playwright.APIRequest.NewContextAsync(new() { FailOnStatusCode = false });
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.WriteHead(404, new() { ["Content-Length"] = 10, ["Content-Type"] = "text/plain" });
            res.End("Not found.");
        });
        var response = await request.FetchAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(404));
        await request.DisposeAsync();
    }
}
