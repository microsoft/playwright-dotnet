/**
 * Copyright Microsoft Corporation. All rights reserved.
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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/capabilities.spec.ts.</summary>
public class CapabilitiesTests
{
    [UpstreamTest("CA-001 SharedArrayBuffer should work @smoke")]
    public async Task SharedArrayBufferShouldWork(ContextFactory contextFactory, Server httpsServer)
    {
        var context = await contextFactory(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        httpsServer.SetRoute("/sharedarraybuffer", (req, res) =>
        {
            res.SetHeader("Cross-Origin-Opener-Policy", "same-origin");
            res.SetHeader("Cross-Origin-Embedder-Policy", "require-corp");
            // Note: without 'onload', Firefox sometimes does not fire the load event
            // over the protocol. The reason is unclear.
            res.End(@"
      <div>Hello there!</div>
      <script>window.onload = () => console.log('onload')</script>
    ");
        });
        await page.GotoAsync(httpsServer.Prefix + "/sharedarraybuffer");
        Assert.That(await page.EvaluateAsync<string>("() => typeof SharedArrayBuffer"), Is.EqualTo("function"));
    }

    [UpstreamTest("CA-002 Web Assembly should work @smoke")]
    public async Task WebAssemblyShouldWork(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/wasm/table2.html");
        Assert.That(await page.EvaluateAsync<string>("loadTable()"), Is.EqualTo("42, 83"));
    }

    [UpstreamTest("CA-003 WebSocket should work @smoke")]
    public async Task WebSocketShouldWork(IPage page, Server server)
    {
        server.SendOnWebSocketConnection("incoming");
        var value = await page.EvaluateAsync<string>(@"host => {
    let cb;
    const result = new Promise(f => cb = f);
    const ws = new WebSocket('ws://' + host + '/ws');
    ws.addEventListener('message', data => { ws.close(); cb(data.data); });
    ws.addEventListener('error', error => cb('Error'));
    return result;
  }", server.Host);
        Assert.That(value, Is.EqualTo("incoming"));
    }

    [UpstreamTest("CA-004 should respect CSP @smoke")]
    public async Task ShouldRespectCSP(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Content-Security-Policy", "script-src 'unsafe-inline';");
            res.End(@"
      <script>
        window.testStatus = 'SUCCESS';
        window.testStatus = eval(""'FAILED'"");
      </script>");
        });

        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.EvaluateAsync<string>("() => window['testStatus']"), Is.EqualTo("SUCCESS"));
    }

    [UpstreamTest("CA-005 should play video @smoke")]
    public async Task ShouldPlayVideo(IPage page, Func<string, string> asset, string browserName, bool isWindows, bool isLinux, string mode)
    {
        It.Skip(browserName == "webkit" && isWindows, "passes locally but fails on GitHub Action bot, apparently due to a Media Pack issue in the Windows Server");

        // Safari only plays mp4 so we test WebKit with an .mp4 clip.
        var fileName = browserName == "webkit" ? "video_mp4.html" : "video.html";
        var absolutePath = asset(fileName);
        // Our test server doesn't support range requests required to play on Mac,
        // so we load the page using a file url.
        await page.GotoAsync(new Uri(absolutePath).AbsoluteUri);
        await page.EvalOnSelectorAsync("video", "v => v.play()");
        await page.EvalOnSelectorAsync("video", "v => v.pause()");
    }

    [UpstreamTest("CA-006 should play webm video @smoke")]
    public async Task ShouldPlayWebmVideo(IPage page, Func<string, string> asset, string browserName, string platform, int macVersion, string mode)
    {
        It.Skip(browserName == "webkit" && platform == "win32", "not supported");

        var absolutePath = asset("video_webm.html");
        // Our test server doesn't support range requests required to play on Mac,
        // so we load the page using a file url.
        await page.GotoAsync(new Uri(absolutePath).AbsoluteUri);
        await page.EvalOnSelectorAsync("video", "v => v.play()");
        await page.EvalOnSelectorAsync("video", "v => v.pause()");
    }

    [UpstreamTest("CA-007 should play audio @smoke")]
    public async Task ShouldPlayAudio(IPage page, Server server, string browserName, string platform, int macVersion)
    {
        It.Fixme(browserName == "webkit" && platform == "win32", "https://github.com/microsoft/playwright/issues/10892");
        It.Skip(browserName == "webkit" && platform == "darwin" && macVersion == 15, "audio output is unreliable on hosted macOS 15 runners");

        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync($"<audio src=\"{server.Prefix}/example.mp3\"></audio>");
        await page.EvalOnSelectorAsync("audio", "e => e.play()");
        await page.WaitForTimeoutAsync(3000);
        await page.EvalOnSelectorAsync("audio", "e => e.pause()");
        Assert.That(await page.EvalOnSelectorAsync<double>("audio", "e => e.currentTime"), Is.GreaterThan(0.1));
    }

    [UpstreamTest("CA-008 should support webgl @smoke")]
    public async Task ShouldSupportWebgl(IPage page)
    {
        var hasWebGL = await page.EvaluateAsync<bool>(@"() => {
    const canvas = document.createElement('canvas');
    return !!canvas.getContext('webgl');
  }");
        Assert.That(hasWebGL, Is.EqualTo(true));
    }

    [UpstreamTest("CA-009 should support webgl 2 @smoke")]
    public async Task ShouldSupportWebgl2(IPage page, string browserName, bool isWindows)
    {
        It.Fixme(browserName == "firefox" && isWindows);

        var hasWebGL2 = await page.EvaluateAsync<bool>(@"() => {
    const canvas = document.createElement('canvas');
    return !!canvas.getContext('webgl2');
  }");
        Assert.That(hasWebGL2, Is.EqualTo(true));
    }

    [UpstreamTest("CA-010 should not crash on page with mp4 @smoke")]
    public async Task ShouldNotCrashOnPageWithMp4(IPage page, Server server, string platform, string browserName)
    {
        It.Fixme(browserName == "webkit" && platform == "win32", "https://github.com/microsoft/playwright/issues/11009, times out in setContent");
        await page.SetContentAsync($"<video><source src=\"{server.Prefix}/movie.mp4\"/></video>");
        await page.WaitForTimeoutAsync(1000);
    }

    [UpstreamTest("CA-011 should not crash on showDirectoryPicker")]
    public async Task ShouldNotCrashOnShowDirectoryPicker(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Skip(browserName != "chromium", "showDirectoryPicker is only available in Chromium");
        await page.GotoAsync(server.EmptyPage);
        // "User activation is required to show a file picker." - so we click first.
        await page.Locator("body").ClickAsync();
        await page.EvaluateAsync(@"() => {
    window.showDirectoryPicker().catch(e => { window.__pickerError = String(e); });
  }");
        // The dialog will not be accepted, so we just wait for some time to
        // to give the browser a chance to crash.
        await page.WaitForTimeoutAsync(3_000);
        // In headless it throws (aborted), in headed it stalls and waits for the picker to be accepted.
        var error = await page.EvaluateAsync<string>("() => window.__pickerError ?? 'stalled'");
        Assert.That(error, Does.Match("((DOMException|AbortError): .*The user aborted a request|stalled)"));
    }

    [UpstreamTest("CA-012 should not crash on storage.getDirectory()")]
    public async Task ShouldNotCrashOnStorageGetDirectory(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.EmptyPage);
        // .NET: error is the value of the call or its exception, as upstream's .catch(e => e) gives.
        object error;
        try
        {
            error = await page.EvaluateAsync<string>(@"async () => {
    const dir = await navigator.storage.getDirectory();
    return dir.name;
  }");
        }
        catch (Exception e)
        {
            error = e;
        }
        if (browserName == "webkit")
        {
            Assert.That(((Exception)error).Message, Does.Contain("UnknownError: The operation failed for an unknown transient reason"));
        }
        else
        {
            Assert.That(error, Is.Null.Or.Empty);
        }
    }

    [UpstreamTest("CA-013 navigator.clipboard should be present")]
    public async Task NavigatorClipboardShouldBePresent(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.EvaluateAsync<object>("() => navigator.clipboard"), Is.Not.Null);
    }

    [UpstreamTest("CA-014 should set CloseEvent.wasClean to false when the server terminates a WebSocket connection")]
    public async Task ShouldSetCloseEventWasCleanToFalseWhenTheServerTerminatesAWebSocketConnection(IPage page, Server server)
    {
        server.OnceWebSocketConnection(socket =>
        {
            socket.Terminate();
        });
        var wasClean = await page.EvaluateAsync<bool>(@"port => new Promise(resolve => {
    const ws = new WebSocket('ws://localhost:' + port + '/ws');
    ws.addEventListener('close', error => resolve(error.wasClean));
  })", server.Port);
        Assert.That(wasClean, Is.EqualTo(false));
    }

    [UpstreamTest("CA-015 serviceWorker should intercept document request")]
    public async Task ServiceWorkerShouldInterceptDocumentRequest(IPage page, Server server)
    {
        server.SetRoute("/sw.js", (req, res) =>
        {
            res.SetHeader("Content-Type", "application/javascript");
            res.End(@"
      self.addEventListener('fetch', event => {
        event.respondWith(new Response('intercepted'));
      });
      self.addEventListener('activate', event => {
        event.waitUntil(clients.claim());
      });
    ");
        });
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"async () => {
    await navigator.serviceWorker.register('/sw.js');
    await new Promise(resolve => navigator.serviceWorker.oncontrollerchange = resolve);
  }");
        await page.ReloadAsync();
        Assert.That(await page.TextContentAsync("body"), Is.EqualTo("intercepted"));
    }

    [UpstreamTest("CA-016 webkit should define window.safari")]
    public async Task WebkitShouldDefineWindowSafari(IPage page, Server server, string browserName)
    {
        It.Skip(browserName != "webkit");
        await page.GotoAsync(server.EmptyPage);
        var defined = await page.EvaluateAsync<bool>("() => !!window.safari");
        Assert.That(defined, Is.True);
        Assert.That(await page.EvaluateAsync<string>("() => typeof window.safari.pushNotification"), Is.EqualTo("object"));
        Assert.That(await page.EvaluateAsync<string>("() => window.safari.pushNotification.toString()"), Is.EqualTo("[object SafariRemoteNotification]"));
    }

    [UpstreamTest("CA-017 make sure that XMLHttpRequest upload events are emitted correctly")]
    public async Task MakeSureThatXMLHttpRequestUploadEventsAreEmittedCorrectly(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var events = await page.EvaluateAsync<string[]>(@"async () => {
    const events = [];
    const xhr = new XMLHttpRequest();
    xhr.upload.addEventListener('loadstart', () => events.push('loadstart'));
    xhr.upload.addEventListener('progress', () => events.push('progress'));
    xhr.upload.addEventListener('load', () => events.push('load'));
    xhr.upload.addEventListener('loadend', () => events.push('loadend'));
    xhr.open('POST', '/simple.json');
    xhr.send('hello');
    await new Promise(f => xhr.onload = f);
    return events;
  }");
        Assert.That(events, Is.EqualTo(new[] { "loadstart", "progress", "load", "loadend" }));
    }

    [UpstreamTest("CA-018 loading in HTMLImageElement.prototype")]
    public async Task LoadingInHTMLImageElementPrototype(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var defined = await page.EvaluateAsync<bool>("() => 'loading' in HTMLImageElement.prototype");
        Assert.That(defined, Is.True);
    }

    [UpstreamTest("CA-019 window.GestureEvent in WebKit")]
    public async Task WindowGestureEventInWebKit(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.EmptyPage);
        var defined = await page.EvaluateAsync<bool>("() => 'GestureEvent' in window");
        Assert.That(defined, Is.EqualTo(browserName == "webkit"));
        var type = await page.EvaluateAsync<string>("() => typeof window.GestureEvent");
        Assert.That(type, Is.EqualTo(browserName == "webkit" ? "function" : "undefined"));
    }

    [UpstreamTest("CA-020 requestFullscreen")]
    public async Task RequestFullscreen(IPage page, Server server, string browserName, bool headless)
    {
        It.Fixme(browserName == "firefox" && !headless, "Error: Request for fullscreen was denied because requesting element is not in the currently focused tab");
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"() => {
    const result = new Promise(resolve => document.addEventListener('fullscreenchange', resolve));
    void document.documentElement.requestFullscreen().then(() => console.log('success')).catch(e => console.log(e));
    return result;
  }");
        Assert.That(await page.EvaluateAsync<bool>("() => document.fullscreenElement === document.documentElement"), Is.True);
        await page.EvaluateAsync(@"() => {
    const result = new Promise(resolve => document.addEventListener('fullscreenchange', resolve));
    void document.exitFullscreen().then(() => console.log('success')).catch(e => console.log(e));
    return result;
  }");
        Assert.That(await page.EvaluateAsync<bool>("() => !!document.fullscreenElement"), Is.False);
    }

    [UpstreamTest("CA-021 should send no Content-Length header for GET requests with a Content-Type")]
    public async Task ShouldSendNoContentLengthHeaderForGETRequestsWithAContentType(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (request, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.EvaluateAsync(@"() => fetch('/empty.html', {
      'headers': { 'Content-Type': 'application/json' },
      'method': 'GET'
    })"));
        Assert.That(request.Headers["content-length"], Is.Null);
    }

    [UpstreamTest("CA-022 Intl.ListFormat should work")]
    public async Task IntlListFormatShouldWork(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.EmptyPage);
        var formatted = await page.EvaluateAsync<string>(@"() => {
    const data = ['first', 'second', 'third'];
    const listFormat = new Intl.ListFormat('en', {
      type: 'disjunction',
      style: 'short',
    });
    return listFormat.format(data);
  }");
        Assert.That(formatted, Is.EqualTo("first, second, or third"));
    }

    [UpstreamTest("CA-023 service worker should cover the iframe")]
    public async Task ServiceWorkerShouldCoverTheIframe(IPage page, Server server)
    {
        server.SetRoute("/sw.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End(@"
      <script>
        window.registrationPromise = navigator.serviceWorker.register('sw.js');
        window.activationPromise = new Promise(resolve => navigator.serviceWorker.oncontrollerchange = resolve);
      </script>
    ");
        });

        server.SetRoute("/iframe.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End("<div>from the server</div>");
        });

        server.SetRoute("/sw.js", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "application/javascript" });
            res.End(@"
      const kIframeHtml = ""<div>from the service worker</div>"";

      self.addEventListener('fetch', event => {
        if (event.request.url.endsWith('iframe.html')) {
          const blob = new Blob([kIframeHtml], { type: 'text/html' });
          const response = new Response(blob, { status: 200 , statusText: 'OK' });
          event.respondWith(response);
          return;
        }
        event.respondWith(fetch(event.request));
      });

      self.addEventListener('activate', event => {
        event.waitUntil(clients.claim());
      });
    ");
        });

        await page.GotoAsync(server.Prefix + "/sw.html");
        await page.EvaluateAsync("() => window['activationPromise']");

        await page.EvaluateAsync(@"() => {
    const iframe = document.createElement('iframe');
    iframe.src = '/iframe.html';
    document.body.appendChild(iframe);
  }");

        await Expect(page.FrameLocator("iframe").Locator("div")).ToHaveTextAsync("from the service worker");
    }

    [UpstreamTest("CA-024 service worker should register in an iframe")]
    public async Task ServiceWorkerShouldRegisterInAnIframe(IPage page, Server server)
    {
        server.SetRoute("/main.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End(@"
      <iframe src='/dir/iframe.html'></iframe>
    ");
        });

        server.SetRoute("/dir/iframe.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End(@"
      <script>
        window.registrationPromise = navigator.serviceWorker.register('sw.js');
        window.activationPromise = new Promise(resolve => navigator.serviceWorker.oncontrollerchange = resolve);
      </script>
    ");
        });

        server.SetRoute("/dir/sw.js", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "application/javascript" });
            res.End(@"
      const kIframeHtml = ""<div>from the service worker</div>"";

      self.addEventListener('fetch', event => {
        if (event.request.url.endsWith('html')) {
          event.respondWith(fetch(event.request));
          return;
        }
        const blob = new Blob(['responseFromServiceWorker'], { type: 'text/plain' });
        const response = new Response(blob, { status: 200 , statusText: 'OK' });
        event.respondWith(response);
      });

      self.addEventListener('activate', event => {
        event.waitUntil(clients.claim());
      });
    ");
        });

        await page.GotoAsync(server.Prefix + "/main.html");
        var iframe = page.Frames[1];
        await iframe.EvaluateAsync("() => window['activationPromise']");

        var response = await iframe.EvaluateAsync<string>(@"async () => {
    const response = await fetch('foo.txt');
    return response.text();
  }");
        Assert.That(response, Is.EqualTo("responseFromServiceWorker"));
    }

    [UpstreamTest("CA-025 should be able to render avif images")]
    public async Task ShouldBeAbleToRenderAvifImages(IPage page, Server server, string browserName, string platform, bool isFrozenWebkit)
    {
        It.Skip(browserName == "webkit" && platform == "win32");
        It.Skip(isFrozenWebkit);
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync($"<img src=\"{server.Prefix}/rgb.avif\" onerror=\"window.error = true\">");
        await Expect(async () => ExpectToEqual(await page.Locator("img").BoundingBoxAsync(), ObjectContaining(new
        {
            width = 128,
            height = 128,
        }))).ToPassAsync();
        Assert.That(await page.EvaluateAsync<object>("() => window.error"), Is.Null);
    }

    [UpstreamTest("CA-026 should not crash when clicking a label with a <input type=\"file\"/>")]
    public async Task ShouldNotCrashWhenClickingALabelWithAInputTypeFile(IPage page)
    {
        await page.SetContentAsync(@"
    <form>
      <label>
        A second file
        <input type=""file"" />
      </label>
    </form>
  ");
        var fileChooserPromise = page.WaitForFileChooserAsync();
        await page.GetByText("A second file").ClickAsync();
        var fileChooser = await fileChooserPromise;
        Assert.That(fileChooser.Page, Is.SameAs(page));
    }

    [UpstreamTest("CA-027 should not crash when clicking a color input")]
    public async Task ShouldNotCrashWhenClickingAColorInput(IPage page)
    {
        await page.SetContentAsync("<input type=\"color\">");
        var input = page.Locator("input");

        await Expect(input).ToBeVisibleAsync();
        await input.ClickAsync();
        await Expect(input).ToBeVisibleAsync();
    }

    [UpstreamTest("CA-028 should not auto play audio")]
    public async Task ShouldNotAutoPlayAudio(IPage page, string browserName, bool isWindows)
    {
        It.Skip(browserName == "webkit" && isWindows, "audio does not play at all");
        It.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen", "no way to inject real setTimeout");
        await page.RouteAsync("**/*", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/html",
                Body = @"
      <script>
        async function onLoad() {
          const log = document.getElementById('log');
          const audioContext = new AudioContext();
          const gainNode = new GainNode(audioContext);
          gainNode.connect(audioContext.destination);
          gainNode.gain.value = 0.025;
          const sineNode = new OscillatorNode(audioContext);
          sineNode.connect(gainNode);
          sineNode.start();
          await new Promise((resolve) => setTimeout(resolve, 1000));
          log.innerHTML = 'State: ' + audioContext.state;
        }
      </script>
      <body onload=""onLoad()"">
      <div id=""log""></div>
      </body>",
            });
        });
        await page.GotoAsync("http://127.0.0.1/audio.html");
        if (browserName == "webkit")
        {
            await Expect(page.Locator("#log")).ToHaveTextAsync(new Regex("State: (interrupted|suspended)"));
        }
        else
        {
            await Expect(page.Locator("#log")).ToHaveTextAsync("State: suspended");
        }
    }

    [UpstreamTest("CA-029 should not crash on feature detection for PublicKeyCredential")]
    public async Task ShouldNotCrashOnFeatureDetectionForPublicKeyCredential(IPage page, Server server, IBrowserContext context)
    {
        await page.GotoAsync(server.EmptyPage);
        Assert.That(await page.EvaluateAsync<string>("() => typeof PublicKeyCredential"), Is.EqualTo("function"));
        Assert.That(await page.EvaluateAsync<string>("() => PublicKeyCredential.name"), Is.EqualTo("PublicKeyCredential"));
        Assert.That(await page.EvaluateAsync<bool>("() => ({}) instanceof PublicKeyCredential"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>(@"() => {
    try {
      new PublicKeyCredential();
      return false;
    } catch (e) {
      return e instanceof TypeError;
    }
  }"), Is.EqualTo(true));
        await page.EvaluateAsync(@"async () => {
    await PublicKeyCredential.getClientCapabilities();
    await PublicKeyCredential.isConditionalMediationAvailable();
    await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
  }");
        await context.Credentials.InstallAsync();
        Assert.That(await page.EvaluateAsync<string>("() => typeof PublicKeyCredential"), Is.EqualTo("function"));
    }
}
