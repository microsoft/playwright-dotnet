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
public class GlobalFetchTests
{
    [Fixture]
    public static IBrowserContext Context() => throw new Exception("global fetch tests should not use context");

    [BeforeEach]
    public static void Skip(string mode) => It.Skip(mode != "default");

    [UpstreamTest("FE-001 ${method} should work @smoke")]
    [Case("fetch")]
    [Case("delete")]
    [Case("get")]
    [Case("head")]
    [Case("patch")]
    [Case("post")]
    [Case("put")]
    public async Task ShouldWork(string method, IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync();
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
        var response = await requestMethod(server.Prefix + "/simple.json");
        Assert.That(response.Url, Is.EqualTo(server.Prefix + "/simple.json"));
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.StatusText, Is.EqualTo("OK"));
        Assert.That(response.Ok, Is.True);
        Assert.That(response.Headers["content-type"], Is.EqualTo("application/json; charset=utf-8"));
        Assert.That(response.HeadersArray, Has.Some.Matches<Header>(header => DeepEquals(header, new { name = "Content-Type", value = "application/json; charset=utf-8" })));
        Assert.That(await response.TextAsync(), Is.EqualTo("head" == method ? "" : "{\"foo\": \"bar\"}\n"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-002 should dispose global request")]
    public async Task ShouldDisposeGlobalRequest(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync();
        var response = await request.GetAsync(server.Prefix + "/simple.json");
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
        await request.DisposeAsync();
        var error = await Catch(() => response.BodyAsync());
        Assert.That(error.Message, Does.Contain("Response has been disposed"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-003 should support global userAgent option")]
    public async Task ShouldSupportGlobalUserAgentOption(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync(new() { UserAgent = "My Agent" });
        var (serverRequest, response) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            request.GetAsync(server.EmptyPage));
        Assert.That(response.Ok, Is.True);
        Assert.That(response.Url, Is.EqualTo(server.EmptyPage));
        Assert.That(serverRequest.Headers["user-agent"], Is.EqualTo("My Agent"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-004 should support global timeout option")]
    [Fixme("dotnet: the message is 'Timeout 100ms exceeded.', without the 'apiRequestContext.get: ' prefix of the call")]
    public async Task ShouldSupportGlobalTimeoutOption(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync(new() { Timeout = 100 });
        server.SetRoute("/empty.html", (req, res) => { });
        var error = await Catch(() => request.GetAsync(server.EmptyPage));
        Assert.That(error.Message, Does.Contain("apiRequestContext.get: Timeout 100ms exceeded"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-005 should propagate extra http headers with redirects")]
    public async Task ShouldPropagateExtraHttpHeadersWithRedirects(IPlaywright playwright, Server server)
    {
        server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        server.SetRedirect("/b/c/redirect2", "/simple.json");
        var request = await playwright.APIRequest.NewContextAsync(new() { ExtraHTTPHeaders = new Dictionary<string, string> { ["My-Secret"] = "Value" } });
        var (req1, req2, req3, _) = await PromiseAll(
            server.WaitForRequest("/a/redirect1"),
            server.WaitForRequest("/b/c/redirect2"),
            server.WaitForRequest("/simple.json"),
            request.GetAsync($"{server.Prefix}/a/redirect1"));
        Assert.That(req1.Headers["my-secret"], Is.EqualTo("Value"));
        Assert.That(req2.Headers["my-secret"], Is.EqualTo("Value"));
        Assert.That(req3.Headers["my-secret"], Is.EqualTo("Value"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-006 should preserve authorization on same-origin redirect but strip on cross-origin")]
    public async Task ShouldPreserveAuthorizationOnSameOriginRedirectButStripOnCrossOrigin(IPlaywright playwright, Server server)
    {
        server.SetRedirect("/same/redirect", "/same/dest");
        server.SetRedirect("/cross/redirect", server.CrossProcessPrefix + "/cross/dest");
        var request = await playwright.APIRequest.NewContextAsync(new()
        {
            ExtraHTTPHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer secret" },
        });

        var (sameDestReq, _) = await PromiseAll(
            server.WaitForRequest("/same/dest"),
            request.GetAsync($"{server.Prefix}/same/redirect"));
        Assert.That(sameDestReq.Headers["authorization"], Is.EqualTo("Bearer secret"));

        var (crossDestReq, _) = await PromiseAll(
            server.WaitForRequest("/cross/dest"),
            request.GetAsync($"{server.Prefix}/cross/redirect"));
        Assert.That(crossDestReq.Headers["authorization"], Is.Null);

        await request.DisposeAsync();
    }

    [UpstreamTest("FE-007 should support global httpCredentials option")]
    public async Task ShouldSupportGlobalHttpCredentialsOption(IPlaywright playwright, Server server)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var request1 = await playwright.APIRequest.NewContextAsync();
        var response1 = await request1.GetAsync(server.EmptyPage);
        Assert.That(response1.Status, Is.EqualTo(401));
        await request1.DisposeAsync();

        var request2 = await playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass" } });
        var response2 = await request2.GetAsync(server.EmptyPage);
        Assert.That(response2.Status, Is.EqualTo(200));
        await request2.DisposeAsync();
    }

    [UpstreamTest("FE-008 should return error with wrong credentials")]
    public async Task ShouldReturnErrorWithWrongCredentials(IPlaywright playwright, Server server)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var request = await playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "wrong" } });
        var response = await request.GetAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(401));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-009 should work with correct credentials and matching origin")]
    public async Task ShouldWorkWithCorrectCredentialsAndMatchingOrigin(IPlaywright playwright, Server server)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var request = await playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass", Origin = server.Prefix } });
        var response = await request.GetAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-010 should work with correct credentials and matching origin case insensitive")]
    public async Task ShouldWorkWithCorrectCredentialsAndMatchingOriginCaseInsensitive(IPlaywright playwright, Server server)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var request = await playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass", Origin = server.Prefix.ToUpperInvariant() } });
        var response = await request.GetAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-011 should return error with correct credentials and mismatching scheme")]
    public async Task ShouldReturnErrorWithCorrectCredentialsAndMismatchingScheme(IPlaywright playwright, Server server)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var request = await playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass", Origin = ReplaceFirst(server.Prefix, "http://", "https://") } });
        var response = await request.GetAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(401));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-012 should return error with correct credentials and mismatching hostname")]
    public async Task ShouldReturnErrorWithCorrectCredentialsAndMismatchingHostname(IPlaywright playwright, Server server)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var hostname = new Uri(server.Prefix).Host;
        var origin = ReplaceFirst(server.Prefix, hostname, "mismatching-hostname");
        var request = await playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass", Origin = origin } });
        var response = await request.GetAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(401));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-013 should return error with correct credentials and mismatching port")]
    public async Task ShouldReturnErrorWithCorrectCredentialsAndMismatchingPort(IPlaywright playwright, Server server)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var origin = ReplaceFirst(server.Prefix, server.Port.ToString(CultureInfo.InvariantCulture), (server.Port + 1).ToString(CultureInfo.InvariantCulture));
        var request = await playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass", Origin = origin } });
        var response = await request.GetAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(401));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-014 should support WWW-Authenticate: Basic")]
    public async Task ShouldSupportWWWAuthenticateBasic(IPlaywright playwright, Server server)
    {
        string credentials = null;
        server.SetRoute("/empty.html", (req, res) =>
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
        var request = await playwright.APIRequest.NewContextAsync(new() { HttpCredentials = new() { Username = "user", Password = "pass" } });
        var response = await request.GetAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(credentials, Is.EqualTo("user:pass"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-015 should support HTTPCredentials.send")]
    public async Task ShouldSupportHTTPCredentialsSend(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync(new()
        {
            HttpCredentials = new() { Username = "user", Password = "pass", Origin = server.Prefix.ToUpperInvariant(), Send = HttpCredentialsSend.Always },
        });
        {
            var (serverRequest, response) = await PromiseAll(
                server.WaitForRequest("/empty.html"),
                request.GetAsync(server.EmptyPage));
            Assert.That(serverRequest.Headers["authorization"], Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"))));
            Assert.That(response.Status, Is.EqualTo(200));
        }
        {
            var (serverRequest, response) = await PromiseAll(
                server.WaitForRequest("/empty.html"),
                request.GetAsync(server.CrossProcessPrefix + "/empty.html"));
            // Not sent to another origin.
            Assert.That(serverRequest.Headers["authorization"], Is.Null);
            Assert.That(response.Status, Is.EqualTo(200));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-016 should support multiple httpCredentials")]
    public async Task ShouldSupportMultipleHttpCredentials(IPlaywright playwright, Server server)
    {
        server.SetAuth("/empty.html", "user1", "pass1");
        var request = await playwright.APIRequest.NewContextAsync(new()
        {
            HttpCredentialsList = new HttpCredentials[]
            {
                new() { Username = "user1", Password = "pass1", Origin = server.Prefix },
                new() { Username = "user2", Password = "pass2", Origin = server.CrossProcessPrefix },
            },
        });
        var response1 = await request.GetAsync(server.EmptyPage);
        Assert.That(response1.Status, Is.EqualTo(200));
        // Wrong credentials are picked for the other origin.
        var response2 = await request.GetAsync(server.CrossProcessPrefix + "/empty.html");
        Assert.That(response2.Status, Is.EqualTo(401));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-017 should support HTTPCredentials.send with multiple httpCredentials")]
    public async Task ShouldSupportHTTPCredentialsSendWithMultipleHttpCredentials(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync(new()
        {
            HttpCredentialsList = new HttpCredentials[]
            {
                new() { Username = "user1", Password = "pass1", Origin = server.Prefix, Send = HttpCredentialsSend.Always },
                new() { Username = "user2", Password = "pass2", Origin = server.CrossProcessPrefix, Send = HttpCredentialsSend.Unauthorized },
            },
        });
        {
            var (serverRequest, response) = await PromiseAll(
                server.WaitForRequest("/empty.html"),
                request.GetAsync(server.EmptyPage));
            Assert.That(serverRequest.Headers["authorization"], Is.EqualTo("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user1:pass1"))));
            Assert.That(response.Status, Is.EqualTo(200));
        }
        {
            var (serverRequest, response) = await PromiseAll(
                server.WaitForRequest("/empty.html"),
                request.GetAsync(server.CrossProcessPrefix + "/empty.html"));
            // This origin has send: 'unauthorized', so credentials are not sent proactively.
            Assert.That(serverRequest.Headers["authorization"], Is.Null);
            Assert.That(response.Status, Is.EqualTo(200));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-018 should support global ignoreHTTPSErrors option")]
    public async Task ShouldSupportGlobalIgnoreHTTPSErrorsOption(IPlaywright playwright, Server httpsServer)
    {
        var request = await playwright.APIRequest.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var response = await request.GetAsync(httpsServer.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-019 should propagate ignoreHTTPSErrors on redirects")]
    public async Task ShouldPropagateIgnoreHTTPSErrorsOnRedirects(IPlaywright playwright, Server httpsServer)
    {
        httpsServer.SetRedirect("/redir", "/empty.html");
        var request = await playwright.APIRequest.NewContextAsync();
        var response = await request.GetAsync(httpsServer.Prefix + "/redir", new() { IgnoreHTTPSErrors = true });
        Assert.That(response.Status, Is.EqualTo(200));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-020 should return server address from response")]
    public async Task ShouldReturnServerAddressFromResponse(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync();
        // The second request reuses the keep-alive socket and should report the address as well.
        for (var i = 0; i < 2; i++)
        {
            var response = await request.GetAsync(server.EmptyPage);
            var addr = await response.ServerAddrAsync();
            Assert.That(addr.IpAddress, Does.Match(@"^(127\.0\.0\.1|::1)$"));
            Assert.That(addr.Port, Is.EqualTo(server.Port));
        }
        server.SetRoute("/idle", (req, res) =>
        {
            // Keep the server socket open beyond its advertised timeout.
            res.OnFinish(() => req.Socket.SetTimeout(30_000));
            res.SetHeader("Keep-Alive", "timeout=2");
            res.End("ok");
        });
        var (serverRequest, _) = await PromiseAll(
            server.WaitForRequest("/idle"),
            request.GetAsync(server.Prefix + "/idle"));
        await Expect(() => Assert.That(serverRequest.Socket.Destroyed, Is.EqualTo(true))).ToPassAsync(new() { Timeout = 7000 });
        // .NET: 'response' is also a local of the loop above.
        var response2 = await request.GetAsync(server.EmptyPage);
        ExpectToEqual(await response2.ServerAddrAsync(), new { ipAddress = AnyString, port = server.Port });
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-021 should return security details from response")]
    [Fixme("harness: the HTTPS server of the tests speaks TLS 1.2 on macOS, where SslStream of .NET has no TLS 1.3 for a server: the protocol is 'TLSv1.2', upstream expects 'TLSv1.3'", Platforms = "darwin")]
    public async Task ShouldReturnSecurityDetailsFromResponse(IPlaywright playwright, Server httpsServer)
    {
        var request = await playwright.APIRequest.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        // The second request reuses the keep-alive socket and should report the details as well.
        for (var i = 0; i < 2; i++)
        {
            var response = await request.GetAsync(httpsServer.EmptyPage);
            ExpectToEqual(await response.SecurityDetailsAsync(), new { issuer = "playwright-test", protocol = "TLSv1.3", subjectName = "playwright-test", validFrom = 1691708270, validTo = 2007068270 });
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-022 should return security details for a resumed TLS session")]
    [Fixme("harness: the HTTPS server of the tests speaks TLS 1.2 on macOS, where SslStream of .NET has no TLS 1.3 for a server: the protocol is 'TLSv1.2', upstream expects 'TLSv1.3'", Platforms = "darwin")]
    public async Task ShouldReturnSecurityDetailsForAResumedTLSSession(IPlaywright playwright, Server httpsServer)
    {
        var expected = new { issuer = "playwright-test", protocol = "TLSv1.3", subjectName = "playwright-test", validFrom = 1691708270, validTo = 2007068270 };
        var request = await playwright.APIRequest.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        // 'Connection: close' drops the socket, so the next request opens a new one and resumes
        // the cached TLS session. A resumed session does not carry the server certificate.
        var first = await request.GetAsync(httpsServer.EmptyPage, new() { Headers = new Dictionary<string, string> { ["connection"] = "close" } });
        ExpectToEqual(await first.SecurityDetailsAsync(), expected);
        var second = await request.GetAsync(httpsServer.EmptyPage);
        ExpectToEqual(await second.SecurityDetailsAsync(), expected);
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-023 should return null security details for http response")]
    public async Task ShouldReturnNullSecurityDetailsForHttpResponse(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync();
        var response = await request.GetAsync(server.EmptyPage);
        Assert.That(await response.SecurityDetailsAsync(), Is.Null);
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-024 should return security details for certificate with multiple CN attributes")]
    public async Task ShouldReturnSecurityDetailsForCertificateWithMultipleCNAttributes(IPlaywright playwright, Func<string, string> asset)
    {
        // .NET: utils.createHttpsServer({ key, cert }, handler) that listens on the port 0 is a Server with a route.
        var server = Server.CreateHttps(asset(""), 0, null, Server.Certificate(asset("multi-value-rdn/cert.pem"), asset("multi-value-rdn/key.pem")));
        server.SetRoute("/", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "application/json" });
            res.End(JsonStringify(new { ok = true }));
        });
        try
        {
            var request = await playwright.APIRequest.NewContextAsync(new() { IgnoreHTTPSErrors = true });
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
    public async Task ShouldResolveUrlRelativeToGlobalBaseURLOption(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync(new() { BaseURL = server.Prefix });
        var response = await request.GetAsync("/empty.html");
        Assert.That(response.Url, Is.EqualTo(server.EmptyPage));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-026 should fallback to given URL if baseURL is bogus")]
    public async Task ShouldFallbackToGivenURLIfBaseURLIsBogus(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync(new() { BaseURL = "bogus" });
        var response = await request.GetAsync(server.Prefix + "/empty.html");
        Assert.That(response.Url, Is.EqualTo(server.EmptyPage));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-029 should return empty body")]
    public async Task ShouldReturnEmptyBody(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync();
        var response = await request.GetAsync(server.EmptyPage);
        var body = await response.BodyAsync();
        Assert.That(body.Length, Is.EqualTo(0));
        Assert.That(await response.TextAsync(), Is.EqualTo(""));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-030 should abort requests when context is disposed")]
    public async Task ShouldAbortRequestsWhenContextIsDisposed(IPlaywright playwright, Server server)
    {
        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/empty.html", req => req.Socket.OnClose(() => connectionClosed.TrySetResult()));
        var request = await playwright.APIRequest.NewContextAsync();
        // .NET: the last promise has no value; it gives null to fit the array of the results.
        async Task<Exception> Then()
        {
            await server.WaitForRequest("/empty.html");
            await request.DisposeAsync();
            return null;
        }
        var results = await PromiseAll(new[]
        {
            Catch(() => request.GetAsync(server.EmptyPage)),
            Catch(() => request.PostAsync(server.EmptyPage)),
            Catch(() => request.DeleteAsync(server.EmptyPage)),
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
    public async Task ShouldAbortRedirectedRequestsWhenContextIsDisposed(IPlaywright playwright, Server server)
    {
        server.SetRedirect("/redirect", "/test");
        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/test", req => req.Socket.OnClose(() => connectionClosed.TrySetResult()));
        var request = await playwright.APIRequest.NewContextAsync();
        async Task Then()
        {
            await server.WaitForRequest("/test");
            await request.DisposeAsync();
        }
        var result = await PromiseAll(
            Catch(() => request.GetAsync(server.Prefix + "/redirect")),
            Then());
        Assert.That(result is Exception, Is.True);
        Assert.That(result.Message, Does.Match("Request context disposed|Target page, context or browser has been closed"));
        await connectionClosed.Task;
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-032 should remove content-length from redirected post requests")]
    public async Task ShouldRemoveContentLengthFromRedirectedPostRequests(IPlaywright playwright, Server server)
    {
        server.SetRedirect("/redirect", "/empty.html");
        var request = await playwright.APIRequest.NewContextAsync();
        var (result, req1, req2) = await PromiseAll(
            request.PostAsync(server.Prefix + "/redirect", new()
            {
                DataObject = new
                {
                    foo = "bar",
                },
            }),
            server.WaitForRequest("/redirect"),
            server.WaitForRequest("/empty.html"));
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
    [Fixme("dotnet: FetchAsync ignores Data = \"\" (string.IsNullOrEmpty) and posts an empty body, upstream posts '\"\"'", Case = "\"string (falsey)\"")]
    [Fixme("dotnet: DataObject = null is 'no data': the API can not post the JSON value null and posts an empty body, upstream posts 'null'", Case = "\"null\"")]
    [CaseSource(nameof(Serialization))]
    public async Task ShouldJsonStringifyBodyWhenContentTypeIsApplicationJson(string type, object value, IPlaywright playwright, Server server)
    {
        var stringifiedValue = JsonStringify(value);
        var request = await playwright.APIRequest.NewContextAsync();
        foreach (var contentType in new[] { "application/json", "Application/JSON; charset=utf-8" })
        {
            // .NET: data: value is Data for a string and DataObject for any other value.
            var (req, _) = await PromiseAll(
                server.WaitForRequest("/empty.html"),
                request.PostAsync(server.EmptyPage, new()
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
    [CaseSource(nameof(Serialization))]
    public async Task ShouldNotDoubleStringifyBodyWhenContentTypeIsApplicationJson(string type, object value, IPlaywright playwright, Server server)
    {
        var stringifiedValue = JsonStringify(value);
        var request = await playwright.APIRequest.NewContextAsync();
        var (req, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            request.PostAsync(server.EmptyPage, new()
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
    public async Task ShouldAcceptAlreadySerializedDataAsBufferWhenContentTypeIsApplicationJson(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync();
        var value = JsonStringify(JsonStringify(new { foo = "bar" }));
        var (req, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            request.PostAsync(server.EmptyPage, new()
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
    public async Task ShouldHaveNiceToString(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync();
        var response = await request.PostAsync(server.EmptyPage, new()
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
    public async Task ShouldNotFailOnEmptyBodyWithEncoding(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync();
        foreach (var method in new[] { "head", "put" })
        {
            foreach (var encoding in new[] { "br", "gzip", "deflate" })
            {
                server.SetRoute("/empty.html", (req, res) =>
                {
                    res.WriteHead(200, new()
                    {
                        ["Content-Encoding"] = encoding,
                        ["Content-Type"] = "text/plain",
                    });
                    res.End();
                });
                // .NET: request[method](url)
                var response = await (method == "head" ? request.HeadAsync(server.EmptyPage) : request.PutAsync(server.EmptyPage));
                Assert.That(response.Status, Is.EqualTo(200));
                Assert.That((await response.BodyAsync()).Length, Is.EqualTo(0));
            }
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-038 should return body for failing requests")]
    public async Task ShouldReturnBodyForFailingRequests(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync();
        foreach (var method in new[] { "head", "put", "trace" })
        {
            server.SetRoute("/empty.html", (req, res) =>
            {
                res.WriteHead(404, new() { ["Content-Length"] = 10, ["Content-Type"] = "text/plain" });
                res.End("Not found.");
            });
            var response = await request.FetchAsync(server.EmptyPage, new() { Method = method });
            Assert.That(response.Status, Is.EqualTo(404));
            // HEAD response returns empty body in node http module.
            Assert.That(await response.TextAsync(), Is.EqualTo(method == "head" ? "" : "Not found."));
        }
        await request.DisposeAsync();
    }

    private static readonly string[] HTTP_METHODS = ["GET", "PUT", "POST", "OPTIONS", "HEAD", "PATCH"];

    [UpstreamTest("FE-039 should throw an error when maxRedirects is exceeded")]
    public async Task ShouldThrowAnErrorWhenMaxRedirectsIsExceeded(IPlaywright playwright, Server server)
    {
        server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        server.SetRedirect("/b/c/redirect2", "/b/c/redirect3");
        server.SetRedirect("/b/c/redirect3", "/b/c/redirect4");
        server.SetRedirect("/b/c/redirect4", "/simple.json");

        var request = await playwright.APIRequest.NewContextAsync();
        foreach (var method in HTTP_METHODS)
        {
            foreach (var maxRedirects in new[] { 1, 2, 3 })
            {
                var error = await ThrowsAsync<PlaywrightException>(() => request.FetchAsync($"{server.Prefix}/a/redirect1", new() { Method = method, MaxRedirects = maxRedirects }));
                Assert.That(error.Message, Does.Contain("Max redirect count exceeded"));
            }
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-040 should not follow redirects when maxRedirects is set to 0")]
    public async Task ShouldNotFollowRedirectsWhenMaxRedirectsIsSetTo0(IPlaywright playwright, Server server)
    {
        server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        server.SetRedirect("/b/c/redirect2", "/simple.json");

        var request = await playwright.APIRequest.NewContextAsync();
        foreach (var method in HTTP_METHODS)
        {
            var response = await request.FetchAsync($"{server.Prefix}/a/redirect1", new() { Method = method, MaxRedirects = 0 });
            Assert.That(response.Headers["location"], Is.EqualTo("/b/c/redirect2"));
            Assert.That(response.Status, Is.EqualTo(302));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-041 should throw an error when maxRedirects is less than 0")]
    [Fixme("dotnet: the message is \"'MaxRedirects' must be greater than or equal to '0'\", upstream expects \"'maxRedirects' must be greater than or equal to '0'\"")]
    public async Task ShouldThrowAnErrorWhenMaxRedirectsIsLessThan0(IPlaywright playwright, Server server)
    {
        server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        server.SetRedirect("/b/c/redirect2", "/simple.json");

        var request = await playwright.APIRequest.NewContextAsync();
        foreach (var method in HTTP_METHODS)
        {
            var error = await ThrowsAsync<PlaywrightException>(() => request.FetchAsync($"{server.Prefix}/a/redirect1", new() { Method = method, MaxRedirects = -1 }));
            Assert.That(error.Message, Does.Contain("'maxRedirects' must be greater than or equal to '0'"));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-042 should not follow redirects when maxRedirects is set to 0 in newContext")]
    public async Task ShouldNotFollowRedirectsWhenMaxRedirectsIsSetTo0InNewContext(IPlaywright playwright, Server server)
    {
        server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        server.SetRedirect("/b/c/redirect2", "/simple.json");

        var request = await playwright.APIRequest.NewContextAsync(new() { MaxRedirects = 0 });
        foreach (var method in HTTP_METHODS)
        {
            var response = await request.FetchAsync($"{server.Prefix}/a/redirect1", new() { Method = method });
            Assert.That(response.Headers["location"], Is.EqualTo("/b/c/redirect2"));
            Assert.That(response.Status, Is.EqualTo(302));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-043 should follow redirects up to maxRedirects limit set in newContext")]
    public async Task ShouldFollowRedirectsUpToMaxRedirectsLimitSetInNewContext(IPlaywright playwright, Server server)
    {
        server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        server.SetRedirect("/b/c/redirect2", "/b/c/redirect3");
        server.SetRedirect("/b/c/redirect3", "/b/c/redirect4");
        server.SetRedirect("/b/c/redirect4", "/simple.json");

        foreach (var maxRedirects in new[] { 1, 2, 3, 4 })
        {
            var request = await playwright.APIRequest.NewContextAsync(new() { MaxRedirects = maxRedirects });
            foreach (var method in HTTP_METHODS)
            {
                if (maxRedirects < 4)
                {
                    var error = await ThrowsAsync<PlaywrightException>(() => request.FetchAsync($"{server.Prefix}/a/redirect1", new() { Method = method }));
                    Assert.That(error.Message, Does.Contain("Max redirect count exceeded"));
                }
                else
                {
                    var response = await request.FetchAsync($"{server.Prefix}/a/redirect1", new() { Method = method });
                    Assert.That(response.Status, Is.EqualTo(200));
                }
            }
            await request.DisposeAsync();
        }
    }

    [UpstreamTest("FE-044 should use maxRedirects from fetch when provided, overriding newContext")]
    public async Task ShouldUseMaxRedirectsFromFetchWhenProvidedOverridingNewContext(IPlaywright playwright, Server server)
    {
        server.SetRedirect("/a/redirect1", "/b/c/redirect2");
        server.SetRedirect("/b/c/redirect2", "/b/c/redirect3");
        server.SetRedirect("/b/c/redirect3", "/b/c/redirect4");
        server.SetRedirect("/b/c/redirect4", "/simple.json");

        var request = await playwright.APIRequest.NewContextAsync(new() { MaxRedirects = 1 });
        foreach (var method in HTTP_METHODS)
        {
            var response = await request.FetchAsync($"{server.Prefix}/a/redirect1", new() { Method = method, MaxRedirects = 4 });
            Assert.That(response.Status, Is.EqualTo(200));
        }
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-045 should keep headers capitalization")]
    public async Task ShouldKeepHeadersCapitalization(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync();
        var (serverRequest, response) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            request.GetAsync(server.EmptyPage, new()
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
    public async Task ShouldThrowAfterDispose(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync();
        await request.DisposeAsync();
        var error = await ThrowsAsync<PlaywrightException>(() => request.GetAsync(server.EmptyPage));
        Assert.That(error.Message, Does.Contain("Target page, context or browser has been closed"));
    }

    [UpstreamTest("FE-048 should retry ECONNRESET")]
    public async Task ShouldRetryECONNRESET(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync();
        var requestCount = 0;
        server.SetRoute("/test", (req, res) =>
        {
            if (requestCount++ < 3)
            {
                req.Socket.Destroy();
                return;
            }
            res.WriteHead(200, new() { ["content-type"] = "text/plain" });
            res.End("Hello!");
        });
        var response = await request.FetchAsync(server.Prefix + "/test", new() { MaxRetries = 3 });
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await response.TextAsync(), Is.EqualTo("Hello!"));
        Assert.That(requestCount, Is.EqualTo(4));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-049 should expose node error code on network errors")]
    public async Task ShouldExposeNodeErrorCodeOnNetworkErrors(IPlaywright playwright, Server server)
    {
        // Reset the connection so that node reports a system error instead of a plain 'socket hang up'.
        server.SetRoute("/reset", req => req.Socket.ResetAndDestroy());
        var request = await playwright.APIRequest.NewContextAsync();
        var error = await Catch(() => request.GetAsync(server.Prefix + "/reset", new() { MaxRetries = 0 }));
        Assert.That(error.Message, Does.Contain("ECONNRESET"));
        // .NET: expect(error.code).toBe('ECONNRESET'), expect(error.syscall).toBe(undefined) and expect(error.errno).toBe(undefined) are left out: PlaywrightException has no code, syscall and errno.
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-050 should append node error code to the message")]
    public async Task ShouldAppendNodeErrorCodeToTheMessage(IPlaywright playwright, Server server)
    {
        // Node reports this one as a plain 'socket hang up' that does not mention the code.
        server.SetRoute("/hangup", req => req.Socket.Destroy());
        var request = await playwright.APIRequest.NewContextAsync();
        var error = await Catch(() => request.GetAsync(server.Prefix + "/hangup", new() { MaxRetries = 0 }));
        Assert.That(error.Message, Does.Contain("socket hang up (ECONNRESET)"));
        // .NET: expect(error.code).toBe('ECONNRESET') is left out: PlaywrightException has no code.
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-052 should throw when failOnStatusCode is set to true inside APIRequest context options")]
    public async Task ShouldThrowWhenFailOnStatusCodeIsSetToTrueInsideAPIRequestContextOptions(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync(new() { FailOnStatusCode = true });
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.WriteHead(404, new() { ["Content-Length"] = 10, ["Content-Type"] = "text/plain" });
            res.End("Not found.");
        });
        var error = await Catch(() => request.FetchAsync(server.EmptyPage));
        Assert.That(error.Message, Does.Contain("404 Not Found"));
        await request.DisposeAsync();
    }

    [UpstreamTest("FE-053 should not throw when failOnStatusCode is set to false inside APIRequest context options")]
    public async Task ShouldNotThrowWhenFailOnStatusCodeIsSetToFalseInsideAPIRequestContextOptions(IPlaywright playwright, Server server)
    {
        var request = await playwright.APIRequest.NewContextAsync(new() { FailOnStatusCode = false });
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.WriteHead(404, new() { ["Content-Length"] = 10, ["Content-Type"] = "text/plain" });
            res.End("Not found.");
        });
        var response = await request.FetchAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(404));
        await request.DisposeAsync();
    }
}
