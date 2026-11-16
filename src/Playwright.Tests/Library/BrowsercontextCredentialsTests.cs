/**
 * Copyright 2018 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-credentials.spec.ts.</summary>
public class BrowsercontextCredentialsTests
{
    [Fixture]
    public static bool FailsOn401(string browserName, bool isHeadlessShell) => browserName == "chromium" && !isHeadlessShell;

    [UpstreamTest("AU-001 should fail without credentials")]
    public async Task ShouldFailWithoutCredentials(IBrowser browser, Server server, bool failsOn401, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        // .NET: responseOrError is an object, cast to the response or to the error where upstream reads a member.
        object responseOrError;
        try
        {
            responseOrError = await page.GotoAsync(server.EmptyPage);
        }
        catch (Exception e)
        {
            responseOrError = e;
        }
        if (failsOn401)
        {
            Assert.That(((Exception)responseOrError).Message, Does.Contain("net::ERR_INVALID_AUTH_CREDENTIALS"));
        }
        else
        {
            Assert.That(((IResponse)responseOrError).Status, Is.EqualTo(401));
        }
    }

    [UpstreamTest("AU-004 should work with correct credentials @smoke")]
    public async Task ShouldWorkWithCorrectCredentials(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentials = new() { Username = "user", Password = "pass" }
        });
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        await context.CloseAsync();
    }

    [UpstreamTest("AU-005 should fail with wrong credentials")]
    public async Task ShouldFailWithWrongCredentials(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentials = new() { Username = "foo", Password = "bar" }
        });
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(401));
        await context.CloseAsync();
    }

    [UpstreamTest("AU-006 should return resource body")]
    public async Task ShouldReturnResourceBody(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/playground.html", "user", "pass");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentials = new() { Username = "user", Password = "pass" }
        });
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(server.Prefix + "/playground.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await page.TitleAsync(), Is.EqualTo("Playground"));
        Assert.That(Encoding.UTF8.GetString(await response.BodyAsync()), Does.Contain("Playground"));
        await context.CloseAsync();
    }

    [UpstreamTest("AU-007 should work with a single credential in an array")]
    public async Task ShouldWorkWithASingleCredentialInAnArray(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentialsList = [new() { Username = "user", Password = "pass" }]
        });
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        await context.CloseAsync();
    }

    [UpstreamTest("AU-008 should work with multiple credentials for different origins")]
    public async Task ShouldWorkWithMultipleCredentialsForDifferentOrigins(IBrowser browser, Server server, string browserName, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user1", "pass1");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentialsList =
            [
                new() { Username = "user1", Password = "pass1", Origin = server.Prefix },
                new() { Username = "user2", Password = "pass2", Origin = server.CrossProcessPrefix },
            ]
        });
        var page = await context.NewPageAsync();
        var response1 = await page.GotoAsync(server.EmptyPage);
        Assert.That(response1.Status, Is.EqualTo(200));
        // Wrong credentials are picked for the other origin.
        var response2 = await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
        Assert.That(response2.Status, Is.EqualTo(401));
        await context.CloseAsync();
    }

    [UpstreamTest("AU-009 should fall back to credentials without origin")]
    public async Task ShouldFallBackToCredentialsWithoutOrigin(IBrowser browser, Server server, string browserName, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentialsList =
            [
                new() { Username = "user2", Password = "pass2", Origin = server.CrossProcessPrefix },
                new() { Username = "user", Password = "pass" },
            ]
        });
        var page = await context.NewPageAsync();
        var response1 = await page.GotoAsync(server.EmptyPage);
        Assert.That(response1.Status, Is.EqualTo(200));
        // First matching entry has wrong credentials for this origin.
        var response2 = await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
        Assert.That(response2.Status, Is.EqualTo(401));
        await context.CloseAsync();
    }

    [UpstreamTest("AU-010 should use the first matching credential")]
    public async Task ShouldUseTheFirstMatchingCredential(IBrowser browser, Server server, string browserName, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentialsList =
            [
                new() { Username = "wrong", Password = "wrong" },
                new() { Username = "user", Password = "pass", Origin = server.Prefix },
            ]
        });
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(401));
        await context.CloseAsync();
    }

    [UpstreamTest("AU-011 should work with correct credentials and matching origin")]
    public async Task ShouldWorkWithCorrectCredentialsAndMatchingOrigin(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentials = new() { Username = "user", Password = "pass", Origin = server.Prefix }
        });
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        await context.CloseAsync();
    }

    [UpstreamTest("AU-012 should work with correct credentials and matching origin case insensitive")]
    public async Task ShouldWorkWithCorrectCredentialsAndMatchingOriginCaseInsensitive(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentials = new() { Username = "user", Password = "pass", Origin = server.Prefix.ToUpperInvariant() }
        });
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        await context.CloseAsync();
    }

    [UpstreamTest("AU-013 should fail with correct credentials and mismatching scheme")]
    public async Task ShouldFailWithCorrectCredentialsAndMismatchingScheme(IBrowser browser, Server server, bool failsOn401, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentials = new() { Username = "user", Password = "pass", Origin = ReplaceFirst(server.Prefix, "http://", "https://") }
        });
        var page = await context.NewPageAsync();
        // .NET: responseOrError is an object, cast to the response or to the error where upstream reads a member.
        object responseOrError;
        try
        {
            responseOrError = await page.GotoAsync(server.EmptyPage);
        }
        catch (Exception e)
        {
            responseOrError = e;
        }
        if (failsOn401)
        {
            Assert.That(((Exception)responseOrError).Message, Does.Contain("net::ERR_INVALID_AUTH_CREDENTIALS"));
        }
        else
        {
            Assert.That(((IResponse)responseOrError).Status, Is.EqualTo(401));
        }
        await context.CloseAsync();
    }

    [UpstreamTest("AU-014 should fail with correct credentials and mismatching hostname")]
    public async Task ShouldFailWithCorrectCredentialsAndMismatchingHostname(IBrowser browser, Server server, bool failsOn401, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var hostname = new Uri(server.Prefix).Host;
        var origin = ReplaceFirst(server.Prefix, hostname, "mismatching-hostname");
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentials = new() { Username = "user", Password = "pass", Origin = origin }
        });
        var page = await context.NewPageAsync();
        // .NET: responseOrError is an object, cast to the response or to the error where upstream reads a member.
        object responseOrError;
        try
        {
            responseOrError = await page.GotoAsync(server.EmptyPage);
        }
        catch (Exception e)
        {
            responseOrError = e;
        }
        if (failsOn401)
        {
            Assert.That(((Exception)responseOrError).Message, Does.Contain("net::ERR_INVALID_AUTH_CREDENTIALS"));
        }
        else
        {
            Assert.That(((IResponse)responseOrError).Status, Is.EqualTo(401));
        }
        await context.CloseAsync();
    }

    [UpstreamTest("AU-015 should fail with correct credentials and mismatching port")]
    public async Task ShouldFailWithCorrectCredentialsAndMismatchingPort(IBrowser browser, Server server, bool failsOn401, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user", "pass");
        var origin = ReplaceFirst(server.Prefix, server.Port.ToString(CultureInfo.InvariantCulture), (server.Port + 1).ToString(CultureInfo.InvariantCulture));
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentials = new() { Username = "user", Password = "pass", Origin = origin }
        });
        var page = await context.NewPageAsync();
        // .NET: responseOrError is an object, cast to the response or to the error where upstream reads a member.
        object responseOrError;
        try
        {
            responseOrError = await page.GotoAsync(server.EmptyPage);
        }
        catch (Exception e)
        {
            responseOrError = e;
        }
        if (failsOn401)
        {
            Assert.That(((Exception)responseOrError).Message, Does.Contain("net::ERR_INVALID_AUTH_CREDENTIALS"));
        }
        else
        {
            Assert.That(((IResponse)responseOrError).Status, Is.EqualTo(401));
        }
        await context.CloseAsync();
    }

    [UpstreamTest("AU-016 should not override Authorization header set by the page")]
    public async Task ShouldNotOverrideAuthorizationHeaderSetByThePage(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        server.SetAuth("/empty.html", "user", "pass");
        server.SetRoute("/echo-auth", (req, res) =>
        {
            res.End(req.Headers["authorization"] ?? "<none>");
        });
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            HttpCredentials = new() { Username = "user", Password = "pass" }
        });
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        var received = await page.EvaluateAsync<string>(@"async () => {
    const response = await fetch('/echo-auth', { headers: { 'Authorization': 'Bearer my-own-app-token' } });
    return await response.text();
  }");
        Assert.That(received, Is.EqualTo("Bearer my-own-app-token"));
        await context.CloseAsync();
    }
}
