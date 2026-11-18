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
public class PermissionsTests : UpstreamBrowserTest
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

    public class Permissions : UpstreamBrowserTest
    {
        [SetUp]
        public void Fixme() => Info.Fixme(BrowserName == "webkit" && IsWindows, "Permissions API is disabled on Windows WebKit");

        [UpstreamTest("PE-001 should be prompt by default")]
        public async Task ShouldBePromptByDefault()
        {
            var page = await PageFixture();
            await page.GotoAsync(Server.EmptyPage);
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
        }

        [UpstreamTest("PE-002 should deny permission when not listed")]
        public async Task ShouldDenyPermissionWhenNotListed()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            Info.Skip(IsFrozenWebkit);

            await page.GotoAsync(Server.EmptyPage);
            await context.GrantPermissionsAsync([], new() { Origin = Server.EmptyPage });
            if (BrowserName == "webkit")
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
                // Since https://github.com/WebKit/WebKit/pull/45470 WebKit only returns actual
                // permission value, if the API has been accessed.
                await page.EvaluateAsync("() => navigator.geolocation.getCurrentPosition(() => { })");
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("denied"));
            }
            else if (IsBidi)
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
            }
            else
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("denied"));
            }
        }

        [UpstreamTest("PE-003 should fail when bad permission is given")]
        public async Task ShouldFailWhenBadPermissionIsGiven()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            await page.GotoAsync(Server.EmptyPage);
            Exception error = null;
            error = await Catch(() => context.GrantPermissionsAsync(["foo"], new() { Origin = Server.EmptyPage }));
            if (IsBidi)
            {
                Assert.That(error.Message, Does.Contain("Protocol error (permissions.setPermission): invalid argument"));
            }
            else
            {
                Assert.That(error.Message, Does.Contain("Unknown permission: foo"));
            }
        }

        [UpstreamTest("PE-004 should grant geolocation permission when origin is listed")]
        public async Task ShouldGrantGeolocationPermissionWhenOriginIsListed()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            await page.GotoAsync(Server.EmptyPage);
            await context.GrantPermissionsAsync(["geolocation"], new() { Origin = Server.EmptyPage });
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("granted"));
        }

        [UpstreamTest("PE-005 should prompt for geolocation permission when origin is not listed")]
        public async Task ShouldPromptForGeolocationPermissionWhenOriginIsNotListed()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            await page.GotoAsync(Server.EmptyPage);
            await context.GrantPermissionsAsync(["geolocation"], new() { Origin = Server.EmptyPage });
            await page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
        }

        [UpstreamTest("PE-006 should grant notifications permission when listed")]
        public async Task ShouldGrantNotificationsPermissionWhenListed()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            await page.GotoAsync(Server.EmptyPage);
            await context.GrantPermissionsAsync(["notifications"], new() { Origin = Server.EmptyPage });
            Assert.That(await GetPermission(page, "notifications"), Is.EqualTo("granted"));
        }

        [UpstreamTest("PE-007 should accumulate when adding")]
        public async Task ShouldAccumulateWhenAdding()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            await page.GotoAsync(Server.EmptyPage);
            await context.GrantPermissionsAsync(["geolocation"]);
            await context.GrantPermissionsAsync(["notifications"]);
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("granted"));
            Assert.That(await GetPermission(page, "notifications"), Is.EqualTo("granted"));
        }

        [UpstreamTest("PE-008 should clear permissions")]
        public async Task ShouldClearPermissions()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            await page.GotoAsync(Server.EmptyPage);
            await context.GrantPermissionsAsync(["geolocation"]);
            await context.ClearPermissionsAsync();
            await context.GrantPermissionsAsync(["notifications"]);
            Assert.That(await GetPermission(page, "geolocation"), Is.Not.EqualTo("granted"));
            Assert.That(await GetPermission(page, "notifications"), Is.EqualTo("granted"));
        }

        [UpstreamTest("PE-009 should grant permission when listed for all domains")]
        public async Task ShouldGrantPermissionWhenListedForAllDomains()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            await page.GotoAsync(Server.EmptyPage);
            await context.GrantPermissionsAsync(["geolocation"]);
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("granted"));
        }

        [UpstreamTest("PE-010 should grant permission when creating context")]
        public async Task ShouldGrantPermissionWhenCreatingContext()
        {
            var context = await Browser.NewContextAsync(new(ContextOptions()) { Permissions = ["geolocation"] });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("granted"));
            await context.CloseAsync();
        }

        [UpstreamTest("PE-011 should reset permissions")]
        public async Task ShouldResetPermissions()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            await page.GotoAsync(Server.EmptyPage);
            await context.GrantPermissionsAsync(["geolocation"], new() { Origin = Server.EmptyPage });
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("granted"));
            await context.ClearPermissionsAsync();
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
        }

        [UpstreamTest("PE-012 should trigger permission onchange")]
        public async Task ShouldTriggerPermissionOnchange()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            Info.Fixme(BrowserName == "webkit");

            await page.GotoAsync(Server.EmptyPage);
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
            await context.GrantPermissionsAsync([], new() { Origin = Server.EmptyPage });
            if (!IsBidi)
            {
                expectedEvents.Add("denied");
            }
            Assert.That(await page.EvaluateAsync<string[]>("() => window['events']"), Is.EqualTo(expectedEvents));
            await context.GrantPermissionsAsync(["geolocation"], new() { Origin = Server.EmptyPage });
            expectedEvents.Add("granted");
            Assert.That(await page.EvaluateAsync<string[]>("() => window['events']"), Is.EqualTo(expectedEvents));
            await context.ClearPermissionsAsync();
            expectedEvents.Add("prompt");
            Assert.That(await page.EvaluateAsync<string[]>("() => window['events']"), Is.EqualTo(expectedEvents));
        }

        [UpstreamTest("PE-013 should isolate permissions between browser contexts")]
        public async Task ShouldIsolatePermissionsBetweenBrowserContexts()
        {
            Info.Skip(IsFrozenWebkit);

            var context = await Browser.NewContextAsync(ContextOptions());
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            var otherContext = await Browser.NewContextAsync(ContextOptions());
            var otherPage = await otherContext.NewPageAsync();
            await otherPage.GotoAsync(Server.EmptyPage);
            Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
            Assert.That(await GetPermission(otherPage, "geolocation"), Is.EqualTo("prompt"));

            await context.GrantPermissionsAsync([], new() { Origin = Server.EmptyPage });
            await otherContext.GrantPermissionsAsync(["geolocation"], new() { Origin = Server.EmptyPage });
            if (BrowserName == "webkit")
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
                // Since https://github.com/WebKit/WebKit/pull/45470 WebKit only returns actual
                // permission value, if the API has been accessed.
                await page.EvaluateAsync("() => navigator.geolocation.getCurrentPosition(() => { })");
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("denied"));
            }
            else if (IsBidi)
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("prompt"));
            }
            else
            {
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("denied"));
            }
            Assert.That(await GetPermission(otherPage, "geolocation"), Is.EqualTo("granted"));

            await context.ClearPermissionsAsync();
            if (BrowserName == "webkit")
            {
                // Since https://github.com/WebKit/WebKit/pull/45470 WebKit returns the cached
                // permission value, if the geolocation API has been accessed.
                // TODO: We can probably reset the cached state in the Web Process when resetting
                // permissions.
                Assert.That(await GetPermission(page, "geolocation"), Is.EqualTo("denied"));

                // Geolocation API in the new page in the same context has not been accessed yet,
                // so the permission status should be prompt.
                var page2 = await context.NewPageAsync();
                await page2.GotoAsync(Server.EmptyPage);
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
    public async Task ShouldSupportClipboardRead()
    {
        var page = await PageFixture();
        var context = await ContextFixture();
        Info.Fixme(BrowserName == "firefox", "No such permissions (requires flag) in Firefox");

        await page.GotoAsync(Server.EmptyPage);
        // There is no 'clipboard-read' permission in WebKit Web API.
        if (BrowserName != "webkit")
        {
            Assert.That(await GetPermission(page, "clipboard-read"), Is.EqualTo("prompt"));
        }

        if (IsHeadlessShell)
        {
            // Chromium (but not headless-shell) shows a dialog and does not resolve the promise.
            var error = await Catch(() => page.EvaluateAsync("() => navigator.clipboard.readText()"));
            Assert.That(error.ToString(), Does.Contain("denied"));
        }

        await context.GrantPermissionsAsync(["clipboard-read"]);
        if (BrowserName != "webkit")
        {
            Assert.That(await GetPermission(page, "clipboard-read"), Is.EqualTo("granted"));
        }
        // There is no 'clipboard-write' permission in WebKit Web API.
        if (BrowserName == "chromium")
        {
            await context.GrantPermissionsAsync(["clipboard-write"]);
        }
        await page.EvaluateAsync("() => navigator.clipboard.writeText('test content')");
        Assert.That(await page.EvaluateAsync<string>("() => navigator.clipboard.readText()"), Is.EqualTo("test content"));
    }

    [UpstreamTest("PE-015 should isolate the headless clipboard from the operating system")]
    public async Task ShouldIsolateTheHeadlessClipboardFromTheOperatingSystem()
    {
        Info.Skip(IsFrozenWebkit, "needs recent webkit");

        // Each headless browser gets its own clipboard, so neither can see the clipboard of the operating system nor of another browser.
        var browser1 = await BrowserType.LaunchAsync(new(LaunchOptions()) { Headless = true });
        var browser2 = await BrowserType.LaunchAsync(new(LaunchOptions()) { Headless = true });
        // .NET: the results of PromiseAll are an array.
        var pages = await PromiseAll(new[] { browser1, browser2 }.Select(async browser =>
        {
            var context = await browser.NewContextAsync(ContextOptions());
            // There is no 'clipboard-write' permission in WebKit Web API and no clipboard permission at all in Firefox.
            if (BrowserName == "chromium")
            {
                await context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
            }
            else if (BrowserName == "webkit")
            {
                await context.GrantPermissionsAsync(["clipboard-read"]);
            }
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
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
    public async Task StorageAccess()
    {
        var page = await PageFixture();
        var context = await ContextFixture();
        Info.Skip(BrowserName != "chromium", "chromium-only api");

        await context.GrantPermissionsAsync(["storage-access"]);
        Assert.That(await GetPermission(page, "storage-access"), Is.EqualTo("granted"));
        Server.SetRoute("/set-cookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "name=value; Path=/; SameSite=Strict; Secure");
            res.End();
        });
        Server.SetRoute("/my-frame.html", (req, res) =>
        {
            res.SetHeader("Content-type", "text/html");
            res.End($"<iframe src=\"{Server.CrossProcessPrefix + "/empty.html"}\"></iframe>");
        });

        // Navigate once to the domain as top level.
        await page.GotoAsync(Server.CrossProcessPrefix + "/set-cookie.html");
        await page.GotoAsync(Server.Prefix + "/my-frame.html");

        var frame = page.Frames[1];
        Assert.That(await GetPermission(frame, "storage-access"), Is.EqualTo("granted"));
        var access = await frame.EvaluateAsync<bool>("() => document.requestStorageAccess().then(() => true, () => false)");
        Assert.That(access, Is.EqualTo(true));
        Assert.That(await frame.EvaluateAsync<bool>("() => document.hasStorageAccess()"), Is.EqualTo(true));
    }

    public class Describe : UpstreamBrowserTest
    {
        // Secure context
        // it.use({ ignoreHTTPSErrors: true, });
        public override BrowserNewContextOptions ContextOptions() => new(base.ContextOptions()) { IgnoreHTTPSErrors = true };

        [UpstreamTest("PE-017 should be able to use the local-fonts API")]
        public async Task ShouldBeAbleToUseTheLocalFontsAPI()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            Info.Skip(BrowserName != "chromium", "chromium-only api");
            Info.Skip(!string.IsNullOrEmpty(Channel) && Channel.StartsWith("msedge", StringComparison.Ordinal), "always times out in edge");
            Info.Fixme(!Headless, "times out in headed");

            await page.GotoAsync(HttpsServer.EmptyPage);
            Assert.That(await GetPermission(page, "local-fonts"), Is.EqualTo("prompt"));
            await context.GrantPermissionsAsync(["local-fonts"]);
            Assert.That(await GetPermission(page, "local-fonts"), Is.EqualTo("granted"));
            Assert.That(await page.EvaluateAsync<bool>("async () => (await window.queryLocalFonts()).length > 0"), Is.EqualTo(true));
        }
    }

    [UpstreamTest("PE-018 local network request is allowed from public origin")]
    public async Task LocalNetworkRequestIsAllowedFromPublicOrigin()
    {
        var page = await PageFixture();
        var context = await ContextFixture();
        Info.Skip(BrowserName == "webkit");
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 145, "local-network-access permission support has changed between versions");

        if (BrowserName == "chromium" || BrowserName == "firefox")
        {
            await context.GrantPermissionsAsync(["local-network-access"]);
        }
        var serverRequests = new List<string>();
        Server.SetRoute("/cors", (req, res) =>
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
  }", Server.CrossProcessPrefix + "/cors");
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
            $"POST {Server.CrossProcessPrefix}/cors",
        }));
    }

    [UpstreamTest("PE-019 can request screen-wake-lock")]
    public async Task CanRequestScreenWakeLock()
    {
        var page = await PageFixture();
        var context = await ContextFixture();
        await context.GrantPermissionsAsync(["screen-wake-lock"]);
        await page.RouteAsync("**/*", route => route.FulfillAsync(new() { Status = 200, Body = "<div>Hello there!</div>", ContentType = "text/html" }));
        await page.GotoAsync("https://example.com");
        await page.EvaluateAsync("() => navigator.wakeLock.request('screen')");
    }

    public class CameraAndMicrophone : UpstreamBrowserTest
    {
        // WebKit exposes deterministic mock capture devices; capture is gated on the
        // 'camera'/'microphone' permissions. Chromium needs --use-fake-device-for-media-stream
        // and Firefox has no camera/microphone permission mapping, so scope to WebKit.
        [SetUp]
        public void Skip() => Info.Skip(BrowserName != "webkit", "WebKit mock-capture-device based test");

        // Native Windows WebKit is built without a media-capture backend, so getUserMedia is
        // unavailable there; WSL runs the Linux build, which does have mock capture.
        [SetUp]
        public void Skip2() => Info.Skip(IsWindows && Channel != "webkit-wsl", "WebKit has no media capture on Windows");

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
        public async Task ShouldCaptureWhenCameraAndMicrophoneAreGranted()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            Info.Skip(IsFrozenWebkit, "Mock capture device support requires a newer WebKit build");

            await context.GrantPermissionsAsync(["camera", "microphone"], new() { Origin = Server.Prefix });
            await page.GotoAsync(Server.EmptyPage);
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
        public async Task ShouldRejectWhenNoPermissionIsGranted()
        {
            var page = await PageFixture();
            Info.Skip(IsFrozenWebkit, "Mock capture device support requires a newer WebKit build");

            await page.GotoAsync(Server.EmptyPage);
            ExpectToEqual(await GetUserMedia(page, new { video = true, audio = true }), new { error = "NotAllowedError" });
        }

        [UpstreamTest("PE-022 should gate audio and video independently")]
        public async Task ShouldGateAudioAndVideoIndependently()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            Info.Skip(IsFrozenWebkit, "Mock capture device support requires a newer WebKit build");

            await context.GrantPermissionsAsync(["camera"], new() { Origin = Server.Prefix });
            await page.GotoAsync(Server.EmptyPage);
            // Camera-only request is allowed.
            ExpectToEqual(await GetUserMedia(page, new { video = true }), new { tracks = new[] { new { kind = "video", live = true } } });
            // Microphone request is rejected because it was not granted.
            ExpectToEqual(await GetUserMedia(page, new { audio = true }), new { error = "NotAllowedError" });
        }

        [UpstreamTest("PE-023 should stop capturing after permissions are cleared")]
        public async Task ShouldStopCapturingAfterPermissionsAreCleared()
        {
            var page = await PageFixture();
            var context = await ContextFixture();
            Info.Skip(IsFrozenWebkit, "Mock capture device support requires a newer WebKit build");

            await context.GrantPermissionsAsync(["camera"], new() { Origin = Server.Prefix });
            await page.GotoAsync(Server.EmptyPage);
            ExpectToEqual(await GetUserMedia(page, new { video = true }), new { tracks = new[] { new { kind = "video", live = true } } });
            await context.ClearPermissionsAsync();
            ExpectToEqual(await GetUserMedia(page, new { video = true }), new { error = "NotAllowedError" });
        }
    }
}
