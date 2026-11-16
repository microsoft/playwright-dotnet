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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library.Chromium;

/// <summary>Port of tests/library/chromium/chromium.spec.ts.</summary>
public class ChromiumTests
{
    // .NET: upstream runs the files of tests/library/chromium only in the Chromium project.
    [BeforeEach]
    public static void Skip(string browserName) => It.Skip(browserName != "chromium", "chromium only");

    [UpstreamTest("CR-009 should pass args with spaces")]
    public async Task ShouldPassArgsWithSpaces(IBrowserType browserType, CreateUserDataDir createUserDataDir, TestInfo testInfo, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
    {
        var browser = await browserType.LaunchPersistentContextAsync(await createUserDataDir(), new(launchPersistentContextOptions)
        {
            Args = ["--user-agent=I am Foo"],
        });
        var page = await browser.NewPageAsync();
        var userAgent = await page.EvaluateAsync<string>("() => navigator.userAgent");
        await browser.CloseAsync();
        Assert.That(userAgent, Is.EqualTo("I am Foo"));
    }

    [UpstreamTest("CR-012 should report failure (due to content-type) of main service worker request")]
    public async Task ShouldReportFailureDueToContentTypeOfMainServiceWorkerRequest(Server server, IPage page, IBrowserContext context, int browserMajorVersion)
    {
        It.Skip(browserMajorVersion < 104, "Requires http://crrev.com/1012503 or later.");
        server.SetRoute("/serviceworkers/fetch/sw.js", (req, res) =>
        {
            res.WriteHead(200, "OK", new() { ["Content-Type"] = "text/html" });
            res.Write("console.log('hi from sw');");
            res.End();
        });
        var (_, main, _) = await PromiseAll(
            server.WaitForRequest("/serviceworkers/fetch/sw.js"),
            Once<IRequest>(h => context.Request += h, h => context.Request -= h, r => r.Url.EndsWith("sw.js", StringComparison.Ordinal)),
            page.GotoAsync(server.Prefix + "/serviceworkers/fetch/sw.html"));
        // This will timeout today
        await main.ResponseAsync();
    }

    [UpstreamTest("CR-013 should report failure (due to redirect) of main service worker request")]
    public async Task ShouldReportFailureDueToRedirectOfMainServiceWorkerRequest(Server server, IPage page, IBrowserContext context, int browserMajorVersion)
    {
        It.Skip(browserMajorVersion < 104, "Requires http://crrev.com/1012503 or later.");
        server.SetRedirect("/serviceworkers/empty/sw.js", "/dev/null");
        var (_, main, _) = await PromiseAll(
            server.WaitForRequest("/serviceworkers/empty/sw.js"),
            Once<IRequest>(h => context.Request += h, h => context.Request -= h, r => r.Url.EndsWith("sw.js", StringComparison.Ordinal)),
            page.GotoAsync(server.Prefix + "/serviceworkers/empty/sw.html"));
        // This will timeout today
        var resp = await main.ResponseAsync();
        Assert.That(resp.Status, Is.EqualTo(302));
    }

    [UpstreamTest("CR-022 should throw when connecting twice to an already running persistent context (--remote-debugging-port)")]
    public async Task ShouldThrowWhenConnectingTwiceToAnAlreadyRunningPersistentContextRemoteDebuggingPort(IBrowserType browserType, CreateUserDataDir createUserDataDir, string platform, bool isHeadlessShell, string channel, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
    {
        It.Skip(isHeadlessShell, "Headless shell does not create a ProcessSingleton");
        It.Fixme(platform == "win32", "Windows does not print something to the console when the profile is already in use by another instance of Chromium.");
        It.Skip(channel?.StartsWith("msedge", StringComparison.Ordinal) == true, "Edge does not print the ProcessSingleton line");
        var userDataDir = await createUserDataDir();
        var browser = await browserType.LaunchPersistentContextAsync(userDataDir, new(launchPersistentContextOptions)
        {
            Args = ["--remote-debugging-port=9222"],
        });
        try
        {
            var error = await Catch(() => browserType.LaunchPersistentContextAsync(userDataDir, new(launchPersistentContextOptions)
            {
                Args = ["--remote-debugging-port=9223"],
            }));
            Assert.That(error.Message, Does.Contain("This usually means that the profile is already in use by another instance of Chromium."));
        }
        finally
        {
            await browser.CloseAsync();
        }
    }

    [UpstreamTest("CR-023 should throw when connecting twice to an already running persistent context (--remote-debugging-pipe)")]
    public async Task ShouldThrowWhenConnectingTwiceToAnAlreadyRunningPersistentContextRemoteDebuggingPipe(IBrowserType browserType, CreateUserDataDir createUserDataDir, string platform, bool isHeadlessShell, string channel, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
    {
        It.Skip(isHeadlessShell, "Headless shell does not create a ProcessSingleton");
        It.Fixme(platform == "win32", "Windows does not print something to the console when the profile is already in use by another instance of Chromium.");
        It.Skip(channel?.StartsWith("msedge", StringComparison.Ordinal) == true, "Edge does not print the ProcessSingleton line");
        var userDataDir = await createUserDataDir();
        var browser = await browserType.LaunchPersistentContextAsync(userDataDir, launchPersistentContextOptions);
        try
        {
            var error = await Catch(() => browserType.LaunchPersistentContextAsync(userDataDir, launchPersistentContextOptions));
            Assert.That(error.Message, Does.Contain("This usually means that the profile is already in use by another instance of Chromium."));
        }
        finally
        {
            await browser.CloseAsync();
        }
    }

    public class WebUINavigation
    {
        private static bool IsEdge(string channel) => channel?.StartsWith("msedge", StringComparison.Ordinal) == true;

        private static async Task<string> GotoError(IPage page, string url)
        {
            try
            {
                await page.GotoAsync(url);
                return "";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        [UpstreamTest("CR-027 should refuse WebUI pages that crash the browser in an isolated context")]
        public async Task ShouldRefuseWebUIPagesThatCrashTheBrowserInAnIsolatedContext(IBrowser browser, IPage page, string channel)
        {
            It.Skip(IsEdge(channel), "Edge only disallows chrome://history");

            var hosts = new[] { "apps", "extensions", "help", "history", "password-manager", "settings" };
            foreach (var url in (string[])[.. hosts.Select(host => $"chrome://{host}"), "chrome://extensions/", "chrome://settings/help", "chrome://SETTINGS", "chrome:settings", "chrome:///settings", "view-source:chrome://settings"])
            {
                Assert.That(await GotoError(page, url), Does.Contain($"Cannot navigate to \"{url}\""));
            }
            // The refusal only matters because it keeps the browser process alive.
            Assert.That(browser.IsConnected, Is.EqualTo(true));
            Assert.That(await page.EvaluateAsync<int>("() => 1 + 1"), Is.EqualTo(2));
        }

        [UpstreamTest("CR-028 should refuse WebUI pages that crash Edge in an isolated context")]
        public async Task ShouldRefuseWebUIPagesThatCrashEdgeInAnIsolatedContext(IBrowser browser, IPage page, string channel)
        {
            It.Skip(!IsEdge(channel), "Edge has its own list of pages disallowed in InPrivate");

            foreach (var url in new[] { "edge://history", "chrome://history", "view-source:edge://history" })
            {
                Assert.That(await GotoError(page, url), Does.Contain($"Cannot navigate to \"{url}\""));
            }
            // Edge does allow these in InPrivate, so they must not be refused.
            foreach (var url in new[] { "edge://settings", "edge://extensions" })
            {
                Assert.That(await GotoError(page, url), Does.Not.Contain("Cannot navigate to"));
            }
            Assert.That(browser.IsConnected, Is.EqualTo(true));
        }

        [UpstreamTest("CR-029 should navigate to WebUI pages that work in an isolated context")]
        public async Task ShouldNavigateToWebUIPagesThatWorkInAnIsolatedContext(IPage page, bool headless)
        {
            It.Skip(headless, "WebUI pages are not available in headless");

            var response = await page.GotoAsync("chrome://version");
            Assert.That(response.Status, Is.EqualTo(200));
        }

        [UpstreamTest("CR-030 should navigate to any WebUI page in a persistent context")]
        public async Task ShouldNavigateToAnyWebUIPageInAPersistentContext(IBrowserType browserType, CreateUserDataDir createUserDataDir, bool headless, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
        {
            It.Skip(headless, "WebUI pages are not available in headless");

            var context = await browserType.LaunchPersistentContextAsync(await createUserDataDir(), launchPersistentContextOptions);
            try
            {
                var page = await context.NewPageAsync();
                // Committing the navigation is all this asserts - waiting for the WebUI to load is slow and beside the point.
                var response = await page.GotoAsync("chrome://extensions", new() { WaitUntil = WaitUntilState.Commit });
                Assert.That(response.Status, Is.EqualTo(200));
            }
            finally
            {
                await context.CloseAsync();
            }
        }
    }

    [UpstreamTest("CR-031 should fire dialogclosed event when dialog is closed out of band")]
    public async Task ShouldFireDialogclosedEventWhenDialogIsClosedOutOfBand(IPage page)
    {
        // Establish the CDP session up front: creating one while a dialog is blocking the page hangs.
        var client = await page.Context.NewCDPSessionAsync(page);
        await client.SendAsync("Page.enable");
        var dialogPromise = Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h);
        var closedPromise = Once<IDialog>(h => page.DialogClosed += h, h => page.DialogClosed -= h);
        var evaluatePromise = page.EvaluateAsync("() => alert('yo')");
        var dialog = await dialogPromise;
        // Handle the dialog out of band, similar to the user closing it in the headed browser.
        await client.SendAsync("Page.handleJavaScriptDialog", new Dictionary<string, object> { ["accept"] = true });
        Assert.That(await closedPromise, Is.SameAs(dialog));
        await evaluatePromise;
    }
}
