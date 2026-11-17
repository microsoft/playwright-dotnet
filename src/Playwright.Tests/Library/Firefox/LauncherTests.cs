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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library.Firefox;

/// <summary>Port of tests/library/firefox/launcher.spec.ts.</summary>
public class LauncherTests : UpstreamBrowserTest
{
    // .NET: the upstream config runs the files of library/firefox/ in the Firefox project only.
    [SetUp]
    public void Skip() => Info.Skip(BrowserName != "firefox", "firefox only");

    [UpstreamTest("FF-001 should pass firefox user preferences")]
    public async Task ShouldPassFirefoxUserPreferences()
    {
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            FirefoxUserPrefs = new Dictionary<string, object>
            {
                ["network.proxy.type"] = 1,
                ["network.proxy.ssl"] = "127.0.0.1",
                ["network.proxy.ssl_port"] = 3333,
            },
        });
        var page = await browser.NewPageAsync(NewPageOptions());
        var error = await Catch(() => page.GotoAsync("https://example.com"));
        Assert.That(error.Message, Does.Contain("NS_ERROR_PROXY_CONNECTION_REFUSED"));
        await browser.CloseAsync();
    }

    [UpstreamTest("FF-002 should pass firefox user preferences in persistent")]
    public async Task ShouldPassFirefoxUserPreferencesInPersistent()
    {
        var (_, page) = await LaunchPersistent(new()
        {
            FirefoxUserPrefs = new Dictionary<string, object>
            {
                ["network.proxy.type"] = 1,
                ["network.proxy.ssl"] = "127.0.0.1",
                ["network.proxy.ssl_port"] = 3333,
            },
        });
        var error = await Catch(() => page.GotoAsync("https://example.com"));
        Assert.That(error.Message, Does.Contain("NS_ERROR_PROXY_CONNECTION_REFUSED"));
    }

    [UpstreamTest("FF-003 should support custom firefox policies")]
    public async Task ShouldSupportCustomFirefoxPolicies()
    {
        var policies = new
        {
            policies = new
            {
                Certificates = new
                {
                    Install = new[] { Asset("client-certificates/server/server_cert.pem") },
                },
            },
        };
        var policiesPath = Info.OutputPath("policies.json");
        await File.WriteAllTextAsync(policiesPath, JsonStringify(policies));

        var port = 13122;
        var server = Server.CreateHttps(Asset(""), port, Loopback, Server.Certificate(Asset("client-certificates/client/localhost/localhost.pem"), Asset("client-certificates/client/localhost/localhost.key")));
        // .NET: await server.waitUntilReady() is left out: the server listens when it is created.

        var browser = await BrowserType.LaunchAsync(new(LaunchOptions())
        {
            Env = TestChildProcess.InheritAndCleanEnv(new Dictionary<string, string> { ["PLAYWRIGHT_FIREFOX_POLICIES_JSON"] = policiesPath }),
        });

        var page = await browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync(server.Prefix + "/frames/frame.html");
        await Expect(page.Locator("body")).ToHaveTextAsync("Hi, I'm frame");
        await browser.CloseAsync();
        await server.StopAsync();
    }

    [UpstreamTest("FF-004 should resize viewport with fractional device scale factor")]
    public async Task ShouldResizeViewportWithFractionalDeviceScaleFactor()
    {
        // A fractional device scale factor makes the viewport size fractional in CSS pixels,
        // e.g. 400.27x799.9 instead of 400x800. Web content still sees rounded values.
        var browser = await BrowserType.LaunchAsync(new(LaunchOptions()) { FirefoxUserPrefs = new Dictionary<string, object> { ["layout.css.devPixelsPerPx"] = "1.1" } });
        var page = await browser.NewPageAsync(NewPageOptions());
        await page.SetViewportSizeAsync(400, 800);
        Assert.That(await page.EvaluateAsync<int[]>("() => [window.innerWidth, window.innerHeight]"), Is.EqualTo(new[] { 400, 800 }));
        await browser.CloseAsync();
    }
}
