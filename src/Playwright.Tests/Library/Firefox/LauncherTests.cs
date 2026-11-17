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
public class LauncherTests
{
    // .NET: the upstream config runs the files of library/firefox/ in the Firefox project only.
    [BeforeEach]
    public static void Skip(string browserName) => Info.Skip(browserName != "firefox", "firefox only");

    [UpstreamTest("FF-001 should pass firefox user preferences")]
    public async Task ShouldPassFirefoxUserPreferences(IBrowserType browserType, string mode, BrowserTypeLaunchOptions launchOptions, BrowserNewPageOptions newPageOptions)
    {
        var browser = await browserType.LaunchAsync(new(launchOptions)
        {
            FirefoxUserPrefs = new Dictionary<string, object>
            {
                ["network.proxy.type"] = 1,
                ["network.proxy.ssl"] = "127.0.0.1",
                ["network.proxy.ssl_port"] = 3333,
            },
        });
        var page = await browser.NewPageAsync(newPageOptions);
        var error = await Catch(() => page.GotoAsync("https://example.com"));
        Assert.That(error.Message, Does.Contain("NS_ERROR_PROXY_CONNECTION_REFUSED"));
        await browser.CloseAsync();
    }

    [UpstreamTest("FF-002 should pass firefox user preferences in persistent")]
    public async Task ShouldPassFirefoxUserPreferencesInPersistent(string mode, LaunchPersistent launchPersistent)
    {
        var (_, page) = await launchPersistent(new()
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
    public async Task ShouldSupportCustomFirefoxPolicies(IBrowserType browserType, string mode, Func<string, string> asset, string loopback, TestInfo testInfo, BrowserTypeLaunchOptions launchOptions, BrowserNewPageOptions newPageOptions)
    {
        var policies = new
        {
            policies = new
            {
                Certificates = new
                {
                    Install = new[] { asset("client-certificates/server/server_cert.pem") },
                },
            },
        };
        var policiesPath = testInfo.OutputPath("policies.json");
        await File.WriteAllTextAsync(policiesPath, JsonStringify(policies));

        var port = 13122;
        var server = Server.CreateHttps(asset(""), port, loopback, Server.Certificate(asset("client-certificates/client/localhost/localhost.pem"), asset("client-certificates/client/localhost/localhost.key")));
        // .NET: await server.waitUntilReady() is left out: the server listens when it is created.

        var browser = await browserType.LaunchAsync(new(launchOptions)
        {
            Env = TestChildProcess.InheritAndCleanEnv(new Dictionary<string, string> { ["PLAYWRIGHT_FIREFOX_POLICIES_JSON"] = policiesPath }),
        });

        var page = await browser.NewPageAsync(newPageOptions);
        await page.GotoAsync(server.Prefix + "/frames/frame.html");
        await Expect(page.Locator("body")).ToHaveTextAsync("Hi, I'm frame");
        await browser.CloseAsync();
        await server.StopAsync();
    }

    [UpstreamTest("FF-004 should resize viewport with fractional device scale factor")]
    public async Task ShouldResizeViewportWithFractionalDeviceScaleFactor(IBrowserType browserType, BrowserTypeLaunchOptions launchOptions, BrowserNewPageOptions newPageOptions)
    {
        // A fractional device scale factor makes the viewport size fractional in CSS pixels,
        // e.g. 400.27x799.9 instead of 400x800. Web content still sees rounded values.
        var browser = await browserType.LaunchAsync(new(launchOptions) { FirefoxUserPrefs = new Dictionary<string, object> { ["layout.css.devPixelsPerPx"] = "1.1" } });
        var page = await browser.NewPageAsync(newPageOptions);
        await page.SetViewportSizeAsync(400, 800);
        Assert.That(await page.EvaluateAsync<int[]>("() => [window.innerWidth, window.innerHeight]"), Is.EqualTo(new[] { 400, 800 }));
        await browser.CloseAsync();
    }
}
