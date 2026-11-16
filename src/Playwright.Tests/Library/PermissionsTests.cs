/**
 * Copyright 2017 Google Inc. All rights reserved.
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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/permissions.spec.ts.</summary>
public class PermissionsTests
{
    private static Task<string> GetPermission(IPage page, string name)
    {
        return page.EvaluateAsync<string>("name => navigator.permissions.query({ name }).then(result => result.state)", name);
    }

    // .NET: upstream passes a frame to the same function.
    private static Task<string> GetPermission(IFrame page, string name)
    {
        return page.EvaluateAsync<string>("name => navigator.permissions.query({ name }).then(result => result.state)", name);
    }

    public class Permissions
    {
        [BeforeEach]
        public static void Fixme(string browserName, bool isWindows) => It.Fixme(browserName == "webkit" && isWindows, "Permissions API is disabled on Windows WebKit");

        [UpstreamTest("PE-001 should be prompt by default")]
        public async Task ShouldBePromptByDefault(IPage page, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
        }

        [UpstreamTest("PE-002 should deny permission when not listed")]
        public async Task ShouldDenyPermissionWhenNotListed(IPage page, IBrowserContext context, Server server, string browserName, bool isBidi, bool isFrozenWebkit)
        {
            It.Skip(isFrozenWebkit);

            await page.GotoAsync(server.EmptyPage);
            await context.GrantPermissionsAsync([], new() { Origin = server.EmptyPage });
            if (browserName == "webkit")
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
                // Since https://github.com/WebKit/WebKit/pull/45470 WebKit only returns actual
                // permission value, if the API has been accessed.
                await page.EvaluateAsync("() => navigator.geolocation.getCurrentPosition(() => { })");
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("denied"));
            }
            else if (isBidi)
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
            }
            else
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("denied"));
            }
        }

        [UpstreamTest("PE-003 should fail when bad permission is given")]
        public async Task ShouldFailWhenBadPermissionIsGiven(IPage page, IBrowserContext context, Server server, bool isBidi)
        {
            await page.GotoAsync(server.EmptyPage);
            Exception error = null;
            error = await Catch(() => context.GrantPermissionsAsync(["foo"], new() { Origin = server.EmptyPage }));
            if (isBidi)
            {
                Assert.That(error.Message, Does.Contain("Protocol error (permissions.setPermission): invalid argument"));
            }
            else
            {
                Assert.That(error.Message, Does.Contain("Unknown permission: foo"));
            }
        }

        [UpstreamTest("PE-004 should grant geolocation permission when origin is listed")]
        public async Task ShouldGrantGeolocationPermissionWhenOriginIsListed(IPage page, IBrowserContext context, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            await context.GrantPermissionsAsync(["geolocation"], new() { Origin = server.EmptyPage });
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("granted"));
        }

        [UpstreamTest("PE-005 should prompt for geolocation permission when origin is not listed")]
        public async Task ShouldPromptForGeolocationPermissionWhenOriginIsNotListed(IPage page, IBrowserContext context, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            await context.GrantPermissionsAsync(["geolocation"], new() { Origin = server.EmptyPage });
            await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
        }

        [UpstreamTest("PE-006 should grant notifications permission when listed")]
        public async Task ShouldGrantNotificationsPermissionWhenListed(IPage page, IBrowserContext context, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            await context.GrantPermissionsAsync(["notifications"], new() { Origin = server.EmptyPage });
            Assert.That(await GetPermission(page, "notifications"), Is.EqualTo("granted"));
        }

        [UpstreamTest("PE-007 should accumulate when adding")]
        public async Task ShouldAccumulateWhenAdding(IPage page, IBrowserContext context, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            await context.GrantPermissionsAsync(["geolocation"]);
            await context.GrantPermissionsAsync(["notifications"]);
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("granted"));
            Assert.That(await GetPermission(page, "notifications"), Is.EqualTo("granted"));
        }

        [UpstreamTest("PE-008 should clear permissions")]
        public async Task ShouldClearPermissions(IPage page, IBrowserContext context, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            await context.GrantPermissionsAsync(["geolocation"]);
            await context.ClearPermissionsAsync();
            await context.GrantPermissionsAsync(["notifications"]);
            Assert.That(await GetPermission(page, "geolocation"), Is.Not.EqualTo("granted"));
            Assert.That(await GetPermission(page, "notifications"), Is.EqualTo("granted"));
        }

        [UpstreamTest("PE-009 should grant permission when listed for all domains")]
        public async Task ShouldGrantPermissionWhenListedForAllDomains(IPage page, IBrowserContext context, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            await context.GrantPermissionsAsync(["geolocation"]);
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("granted"));
        }

        [UpstreamTest("PE-010 should grant permission when creating context")]
        public async Task ShouldGrantPermissionWhenCreatingContext(Server server, IBrowser browser, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { Permissions = ["geolocation"] });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("granted"));
            await context.CloseAsync();
        }

        [UpstreamTest("PE-011 should reset permissions")]
        public async Task ShouldResetPermissions(IPage page, IBrowserContext context, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            await context.GrantPermissionsAsync(["geolocation"], new() { Origin = server.EmptyPage });
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("granted"));
            await context.ClearPermissionsAsync();
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
        }

        [UpstreamTest("PE-012 should trigger permission onchange")]
        public async Task ShouldTriggerPermissionOnchange(IPage page, IBrowserContext context, Server server, string browserName, bool isBidi)
        {
            It.Fail(browserName == "webkit");

            await page.GotoAsync(server.EmptyPage);
            await page.EvaluateAsync(@"() => {
      window['events'] = [];
      return navigator.permissions.query({ name: 'geolocation' }).then(function(result) {
        window['events'].push(result.state);
        result.onchange = function() {
          window['events'].push(result.state);
        };
      });
    }");
            var expectedEvents = new List<string> { "prompt" };
            Assert.That(await page.EvaluateAsync<string[]>("() => window['events']"), Is.EqualTo(expectedEvents));
            await context.GrantPermissionsAsync([], new() { Origin = server.EmptyPage });
            if (!isBidi)
            {
                expectedEvents.Add("denied");
            }
            Assert.That(await page.EvaluateAsync<string[]>("() => window['events']"), Is.EqualTo(expectedEvents));
            await context.GrantPermissionsAsync(["geolocation"], new() { Origin = server.EmptyPage });
            expectedEvents.Add("granted");
            Assert.That(await page.EvaluateAsync<string[]>("() => window['events']"), Is.EqualTo(expectedEvents));
            await context.ClearPermissionsAsync();
            expectedEvents.Add("prompt");
            Assert.That(await page.EvaluateAsync<string[]>("() => window['events']"), Is.EqualTo(expectedEvents));
        }

        [UpstreamTest("PE-013 should isolate permissions between browser contexts")]
        public async Task ShouldIsolatePermissionsBetweenBrowserContexts(Server server, IBrowser browser, string browserName, bool isBidi, bool isFrozenWebkit, BrowserNewContextOptions contextOptions)
        {
            It.Skip(isFrozenWebkit);

            var context = await browser.NewContextAsync(contextOptions);
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            var otherContext = await browser.NewContextAsync(contextOptions);
            var otherPage = await otherContext.NewPageAsync();
            await otherPage.GotoAsync(server.EmptyPage);
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
            Assert.That(await GetPermission(otherPage, "geolocation"), Is.EqualTo("prompt"));

            await context.GrantPermissionsAsync([], new() { Origin = server.EmptyPage });
            await otherContext.GrantPermissionsAsync(["geolocation"], new() { Origin = server.EmptyPage });
            if (browserName == "webkit")
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
                // Since https://github.com/WebKit/WebKit/pull/45470 WebKit only returns actual
                // permission value, if the API has been accessed.
                await page.EvaluateAsync("() => navigator.geolocation.getCurrentPosition(() => { })");
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("denied"));
            }
            else if (isBidi)
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
            }
            else
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("denied"));
            }
            Assert.That(await GetPermission(otherPage, "geolocation"), Is.EqualTo("granted"));

            await context.ClearPermissionsAsync();
            if (browserName == "webkit")
            {
                // Since https://github.com/WebKit/WebKit/pull/45470 WebKit returns the cached
                // permission value, if the geolocation API has been accessed.
                // TODO: We can probably reset the cached state in the Web Process when resetting
                // permissions.
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("denied"));

                // Geolocation API in the new page in the same context has not been accessed yet,
                // so the permission status should be prompt.
                var page2 = await context.NewPageAsync();
                await page2.GotoAsync(server.EmptyPage);
                Assert.That(await GetPermission(page2, "geolocation"), Is.EqualTo("prompt"));
                await page2.CloseAsync();
            }
            else
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
            }
            Assert.That(await GetPermission(otherPage, "geolocation"), Is.EqualTo("granted"));
            await otherContext.CloseAsync();
            await context.CloseAsync();
        }
    }

    [UpstreamTest("PE-014 should support clipboard read")]
    public async Task ShouldSupportClipboardRead(IPage page, IBrowserContext context, Server server, string browserName, bool isWindows, bool isHeadlessShell)
    {
        It.Fail(browserName == "firefox", "No such permissions (requires flag) in Firefox");

        await page.GotoAsync(server.EmptyPage);
        // There is no 'clipboard-read' permission in WebKit Web API.
        if (browserName != "webkit")
        {
            Assert.That(await GetPermission(page, "clipboard-read"), Is.EqualTo("prompt"));
        }

        if (isHeadlessShell)
        {
            // Chromium (but not headless-shell) shows a dialog and does not resolve the promise.
            var error = await Catch(() => page.EvaluateAsync("() => navigator.clipboard.readText()"));
            Assert.That(error.ToString(), Does.Contain("denied"));
        }

        await context.GrantPermissionsAsync(["clipboard-read"]);
        if (browserName != "webkit")
        {
            Assert.That(await GetPermission(page, "clipboard-read"), Is.EqualTo("granted"));
        }
        // There is no 'clipboard-write' permission in WebKit Web API.
        if (browserName == "chromium")
        {
            await context.GrantPermissionsAsync(["clipboard-write"]);
        }
        await page.EvaluateAsync("() => navigator.clipboard.writeText('test content')");
        Assert.That(await page.EvaluateAsync<string>("() => navigator.clipboard.readText()"), Is.EqualTo("test content"));
    }

    [UpstreamTest("PE-015 should isolate the headless clipboard from the operating system")]
    public async Task ShouldIsolateTheHeadlessClipboardFromTheOperatingSystem(IBrowserType browserType, Server server, string browserName, bool isFrozenWebkit, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        It.Skip(isFrozenWebkit, "needs recent webkit");

        // Each headless browser gets its own clipboard, so neither can see the clipboard of the operating system nor of another browser.
        var browser1 = await browserType.LaunchAsync(new(launchOptions) { Headless = true });
        var browser2 = await browserType.LaunchAsync(new(launchOptions) { Headless = true });
        // .NET: the results of PromiseAll are an array.
        var pages = await PromiseAll(new[] { browser1, browser2 }.Select(async browser =>
        {
            var context = await browser.NewContextAsync(contextOptions);
            // There is no 'clipboard-write' permission in WebKit Web API and no clipboard permission at all in Firefox.
            if (browserName == "chromium")
            {
                await context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
            }
            else if (browserName == "webkit")
            {
                await context.GrantPermissionsAsync(["clipboard-read"]);
            }
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            return page;
        }));
        var (page1, page2) = (pages[0], pages[1]);

        await page1.EvaluateAsync("() => navigator.clipboard.writeText('first')");
        Assert.That(await page1.EvaluateAsync<string>("() => navigator.clipboard.readText()"), Is.EqualTo("first"));

        await page2.EvaluateAsync("() => navigator.clipboard.writeText('second')");
        Assert.That(await page2.EvaluateAsync<string>("() => navigator.clipboard.readText()"), Is.EqualTo("second"));

        Assert.That(await page1.EvaluateAsync<string>("() => navigator.clipboard.readText()"), Is.EqualTo("first"));

        await PromiseAll(browser1.CloseAsync(), browser2.CloseAsync());
    }

    [UpstreamTest("PE-016 storage access")]
    public async Task StorageAccess(IPage page, IBrowserContext context, Server server, string browserName)
    {
        It.Skip(browserName != "chromium", "chromium-only api");

        await context.GrantPermissionsAsync(["storage-access"]);
        Assert.That(await GetPermission(page, "storage-access"), Is.EqualTo("granted"));
        server.SetRoute("/set-cookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "name=value; Path=/; SameSite=Strict; Secure");
            res.End();
        });
        server.SetRoute("/my-frame.html", (req, res) =>
        {
            res.SetHeader("Content-type", "text/html");
            res.End($"<iframe src=\"{server.CrossProcessPrefix + "/empty.html"}\"></iframe>");
        });

        // Navigate once to the domain as top level.
        await page.GotoAsync(server.CrossProcessPrefix + "/set-cookie.html");
        await page.GotoAsync(server.Prefix + "/my-frame.html");

        var frame = page.Frames[1];
        Assert.That(await GetPermission(frame, "storage-access"), Is.EqualTo("granted"));
        var access = await frame.EvaluateAsync<bool>("() => document.requestStorageAccess().then(() => true, () => false)");
        Assert.That(access, Is.EqualTo(true));
        Assert.That(await frame.EvaluateAsync<bool>("() => document.hasStorageAccess()"), Is.EqualTo(true));
    }

    public class Describe
    {
        // Secure context
        // it.use({ ignoreHTTPSErrors: true, });
        [Fixture]
        public static BrowserNewContextOptions ContextOptions(BrowserNewContextOptions contextOptions) => new(contextOptions) { IgnoreHTTPSErrors = true };

        [UpstreamTest("PE-017 should be able to use the local-fonts API")]
        public async Task ShouldBeAbleToUseTheLocalFontsAPI(IPage page, IBrowserContext context, Server httpsServer, string browserName, string channel, bool headless)
        {
            It.Skip(browserName != "chromium", "chromium-only api");
            It.Skip(!string.IsNullOrEmpty(channel) && channel.StartsWith("msedge", StringComparison.Ordinal), "always times out in edge");
            It.Fixme(!headless, "times out in headed");

            await page.GotoAsync(httpsServer.EmptyPage);
            Assert.That(await GetPermission(page, "local-fonts"), Is.EqualTo("prompt"));
            await context.GrantPermissionsAsync(["local-fonts"]);
            Assert.That(await GetPermission(page, "local-fonts"), Is.EqualTo("granted"));
            Assert.That(await page.EvaluateAsync<bool>("async () => (await window.queryLocalFonts()).length > 0"), Is.EqualTo(true));
        }
    }

    [UpstreamTest("PE-018 local network request is allowed from public origin")]
    public async Task LocalNetworkRequestIsAllowedFromPublicOrigin(IPage page, IBrowserContext context, Server server, string browserName, string channel, int browserMajorVersion)
    {
        It.Skip(browserName == "webkit");
        It.Skip(browserName == "chromium" && browserMajorVersion < 145, "local-network-access permission support has changed between versions");

        if (browserName == "chromium" || browserName == "firefox")
        {
            await context.GrantPermissionsAsync(["local-network-access"]);
        }
        var serverRequests = new List<string>();
        server.SetRoute("/cors", (req, res) =>
        {
            serverRequests.Add($"{req.Method} {req.Url}");
            if (req.Method == "OPTIONS")
            {
                res.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Access-Control-Allow-Methods"] = "GET, POST, PUT, OPTIONS",
                    ["Access-Control-Allow-Headers"] = "*",
                });
                res.End();
                return;
            }
            res.WriteHead(200, new() { ["Content-type"] = "text/plain", ["Access-Control-Allow-Origin"] = "*" });
            res.End("Hello there!");
        });
        var clientRequests = new List<string>();
        // Has to be a public origin.
        await page.GotoAsync("https://demo.playwright.dev/todomvc/");
        page.Request += (_, request) =>
        {
            clientRequests.Add($"{request.Method} {request.Url}");
        };
        string response;
        try
        {
            response = await page.EvaluateAsync<string>(@"async url => {
    const response = await fetch(url, {
      method: 'POST',
      body: '',
      headers: {
        'Content-Type': 'application/json',
        'X-Custom-Header': 'test-value'
      }
    });
    return await response.text();
  }", server.CrossProcessPrefix + "/cors");
        }
        catch (Exception e)
        {
            response = e.Message;
        }
        Assert.That(response, Is.EqualTo("Hello there!"));
        Assert.That(serverRequests, Is.EqualTo(new[]
        {
            "OPTIONS /cors",
            "POST /cors",
        }));
        Assert.That(clientRequests, Is.EqualTo(new[]
        {
            $"POST {server.CrossProcessPrefix}/cors",
        }));
    }

    [UpstreamTest("PE-019 can request screen-wake-lock")]
    public async Task CanRequestScreenWakeLock(IPage page, IBrowserContext context)
    {
        await context.GrantPermissionsAsync(["screen-wake-lock"]);
        await page.RouteAsync("**/*", route => route.FulfillAsync(new() { Status = 200, Body = "<div>Hello there!</div>", ContentType = "text/html" }));
        await page.GotoAsync("https://example.com");
        await page.EvaluateAsync("() => navigator.wakeLock.request('screen')");
    }

    public class CameraAndMicrophone
    {
        // WebKit exposes deterministic mock capture devices; capture is gated on the
        // 'camera'/'microphone' permissions. Chromium needs --use-fake-device-for-media-stream
        // and Firefox has no camera/microphone permission mapping, so scope to WebKit.
        [BeforeEach]
        public static void Skip(string browserName) => It.Skip(browserName != "webkit", "WebKit mock-capture-device based test");

        // Native Windows WebKit is built without a media-capture backend, so getUserMedia is
        // unavailable there; WSL runs the Linux build, which does have mock capture.
        [BeforeEach]
        public static void Skip2(bool isWindows, string channel) => It.Skip(isWindows && channel != "webkit-wsl", "WebKit has no media capture on Windows");

        private static async Task<object> GetUserMedia(IPage page, object constraints)
        {
            return await page.EvaluateAsync<object>(@"async constraints => {
      try {
        const stream = await navigator.mediaDevices.getUserMedia(constraints);
        const tracks = stream.getTracks().map(track => ({ kind: track.kind, live: track.readyState === 'live' }));
        stream.getTracks().forEach(track => track.stop());
        tracks.sort((a, b) => a.kind.localeCompare(b.kind));
        return { tracks };
      } catch (error) {
        return { error: error.name };
      }
    }", constraints);
        }

        [UpstreamTest("PE-020 should capture when camera and microphone are granted")]
        public async Task ShouldCaptureWhenCameraAndMicrophoneAreGranted(IPage page, IBrowserContext context, Server server, bool isFrozenWebkit)
        {
            It.Skip(isFrozenWebkit, "Mock capture device support requires a newer WebKit build");

            await context.GrantPermissionsAsync(["camera", "microphone"], new() { Origin = server.Prefix });
            await page.GotoAsync(server.EmptyPage);
            ExpectToEqual(await GetUserMedia(page, new { video = true, audio = true }), new
            {
                tracks = new[]
                {
                    new { kind = "audio", live = true },
                    new { kind = "video", live = true },
                },
            });
        }

        [UpstreamTest("PE-021 should reject when no permission is granted")]
        public async Task ShouldRejectWhenNoPermissionIsGranted(IPage page, Server server, bool isFrozenWebkit)
        {
            It.Skip(isFrozenWebkit, "Mock capture device support requires a newer WebKit build");

            await page.GotoAsync(server.EmptyPage);
            ExpectToEqual(await GetUserMedia(page, new { video = true, audio = true }), new { error = "NotAllowedError" });
        }

        [UpstreamTest("PE-022 should gate audio and video independently")]
        public async Task ShouldGateAudioAndVideoIndependently(IPage page, IBrowserContext context, Server server, bool isFrozenWebkit)
        {
            It.Skip(isFrozenWebkit, "Mock capture device support requires a newer WebKit build");

            await context.GrantPermissionsAsync(["camera"], new() { Origin = server.Prefix });
            await page.GotoAsync(server.EmptyPage);
            // Camera-only request is allowed.
            ExpectToEqual(await GetUserMedia(page, new { video = true }), new { tracks = new[] { new { kind = "video", live = true } } });
            // Microphone request is rejected because it was not granted.
            ExpectToEqual(await GetUserMedia(page, new { audio = true }), new { error = "NotAllowedError" });
        }

        [UpstreamTest("PE-023 should stop capturing after permissions are cleared")]
        public async Task ShouldStopCapturingAfterPermissionsAreCleared(IPage page, IBrowserContext context, Server server, bool isFrozenWebkit)
        {
            It.Skip(isFrozenWebkit, "Mock capture device support requires a newer WebKit build");

            await context.GrantPermissionsAsync(["camera"], new() { Origin = server.Prefix });
            await page.GotoAsync(server.EmptyPage);
            ExpectToEqual(await GetUserMedia(page, new { video = true }), new { tracks = new[] { new { kind = "video", live = true } } });
            await context.ClearPermissionsAsync();
            ExpectToEqual(await GetUserMedia(page, new { video = true }), new { error = "NotAllowedError" });
        }
    }
}
