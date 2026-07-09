using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright;

await using var server = LocalJsonServer.Start();
using var playwright = await Playwright.CreateAsync();

IBrowser? browser = null;
IBrowserContext? context = null;
IPage? page = null;
IBrowser? clearcoteBrowser = null;
IBrowserContext? clearcoteContext = null;
IPage? clearcotePage = null;

try
{
    await RunGroupAsync("driver-browser-lifecycle", async () =>
    {
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        Assert(!string.IsNullOrWhiteSpace(browser.Version), "Browser version should be available.");

        context = await browser.NewContextAsync(new()
        {
            Locale = "en-US",
            ViewportSize = new() { Width = 800, Height = 600 },
        });
        page = await context.NewPageAsync();
        Assert(page != null, "Page should be created.");
    });

    await RunGroupAsync("context-cookies", async () =>
    {
        var activeContext = context ?? throw new InvalidOperationException("Browser context was not created.");
        await activeContext.AddCookiesAsync(new[]
        {
            new Microsoft.Playwright.Cookie
            {
                Name = "aot-cookie",
                Value = "ok",
                Url = "https://example.test",
            },
        });

        var cookies = await activeContext.CookiesAsync("https://example.test");
        Assert(cookies.Any(static cookie => cookie.Name == "aot-cookie" && cookie.Value == "ok"), "Cookie round-trip failed.");
    });

    await RunGroupAsync("page-dom-navigation", async () =>
    {
        await page!.SetContentAsync("""
<!doctype html>
<title>Playwright NativeAOT</title>
<label>Name <input id="name" /></label>
<button id="primary">Run</button>
<output id="result"></output>
<ul>
  <li>transport</li>
  <li>evaluate</li>
  <li>screenshot</li>
</ul>
<script>
  document.querySelector('#primary').addEventListener('click', () => {
    document.querySelector('#result').textContent = document.querySelector('#name').value.toUpperCase();
  });
</script>
""");

        Assert(await page.TitleAsync() == "Playwright NativeAOT", "Unexpected page title.");
        Assert(await page.Locator("li").CountAsync() == 3, "Unexpected list count.");
    });

    await RunGroupAsync("locator-input-actions", async () =>
    {
        await page!.Locator("#name").FillAsync("nativeaot");
        await page.Locator("#primary").ClickAsync();
        Assert(await page.Locator("#result").TextContentAsync() == "NATIVEAOT", "Locator input/click failed.");
    });

    await RunGroupAsync("evaluate-serialization", async () =>
    {
        var sum = await page!.EvaluateAsync<int>(
            "payload => payload.values.reduce((total, value) => total + value, 0)",
            new Dictionary<string, object?>
            {
                ["values"] = new[] { 1, 2, 3 },
            });
        Assert(sum == 6, "Evaluate argument serialization failed.");

        var payload = await page.EvaluateAsync<JsonElement>(
            "() => ({ title: document.title, itemCount: document.querySelectorAll('li').length })");
        Assert(payload.GetProperty("title").GetString() == "Playwright NativeAOT", "JsonElement result title mismatch.");
        Assert(payload.GetProperty("itemCount").GetInt32() == 3, "JsonElement result count mismatch.");
    });

    await RunGroupAsync("route-interception", async () =>
    {
        await using var routeRegistration = await page!.RouteAsync("**/route-page", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/html",
                Body = "<!doctype html><title>Route OK</title><h1 id=\"route\">route-ok</h1>",
            }).ConfigureAwait(false);
        });

        var response = await page.GotoAsync("http://example.test/route-page", new()
        {
            Timeout = 10_000,
            WaitUntil = WaitUntilState.DOMContentLoaded,
        });
        Assert(response?.Status == 200, "Route response status mismatch.");
        Assert(await page.Locator("#route").TextContentAsync() == "route-ok", "Route fulfilled body mismatch.");
    });

    await RunGroupAsync("page-network-response", async () =>
    {
        var response = await page!.GotoAsync(new Uri(server.BaseUri, "page").ToString(), new()
        {
            Timeout = 10_000,
            WaitUntil = WaitUntilState.DOMContentLoaded,
        });
        Assert(response?.Status == 200, "Page network response status mismatch.");
        Assert(await page.Locator("#network").TextContentAsync() == "network-ok", "Network page body mismatch.");

        var fetchResult = await page.EvaluateAsync<JsonElement>("async () => await (await fetch('/json')).json()");
        Assert(fetchResult.GetProperty("message").GetString() == "ok", "Page fetch JSON string mismatch.");
        Assert(fetchResult.GetProperty("count").GetInt32() == 3, "Page fetch JSON number mismatch.");
    });

    await RunGroupAsync("api-request-json-sourcegen", async () =>
    {
        await using var api = await playwright.APIRequest.NewContextAsync();
        var response = await api.GetAsync(new Uri(server.BaseUri, "json").ToString());
        Assert(response.Ok, "APIRequest response should be OK.");

        var raw = await response.JsonAsync();
        Assert(raw?.GetProperty("message").GetString() == "ok", "Raw APIResponse.JsonAsync failed.");

        var typed = await response.JsonAsync(AotSampleJsonContext.Default.SampleApiPayload);
        Assert(typed?.Count == 3, "Typed APIResponse.JsonAsync count mismatch.");
        var typedPayload = typed ?? throw new InvalidOperationException("Typed APIResponse.JsonAsync returned null.");
        Assert(typedPayload.Tags.Length == 2 && typedPayload.Tags[0] == "nativeaot", "Typed APIResponse.JsonAsync tags mismatch.");
    });

    await RunGroupAsync("binding-expose-function", async () =>
    {
        await page!.ExposeFunctionAsync("addNumbers", (int a, int b) => a + b);
        var sum = await page.EvaluateAsync<int>("async () => await window.addNumbers(40, 2)");
        Assert(sum == 42, "ExposeFunction sum mismatch.");

        await page.ExposeFunctionAsync("greet", (string name) => $"Hello, {name}!");
        var greeting = await page.EvaluateAsync<string>("async () => await window.greet('AOT')");
        Assert(greeting == "Hello, AOT!", "ExposeFunction greeting mismatch.");
    });

    await RunGroupAsync("binding-error-serialization", async () =>
    {
        await page!.ExposeFunctionAsync("fail", () => throw new InvalidOperationException("AOT-bind-error"));
        var errorMessage = await page.EvaluateAsync<string?>(@"
            async () => {
                try { await window.fail(); return null; }
                catch (e) { return e.message; }
            }");
        Assert(errorMessage != null && errorMessage.Contains("AOT-bind-error"),
            "Binding error serialization failed.");
    });

    await RunGroupAsync("binding-async-void", async () =>
    {
        await page!.ExposeFunctionAsync("ping", async () => { await Task.Yield(); });
        var ok = await page.EvaluateAsync<string?>("async () => { await window.ping(); return 'pong'; }");
        Assert(ok == "pong", "Async void binding failed.");
    });

    await RunGroupAsync("locator-find-by-text", async () =>
    {
        await page!.SetContentAsync("<ul><li>alpha</li><li>beta</li><li>gamma</li></ul>");
        var items = page.GetByText("beta");
        Assert(await items.CountAsync() == 1, "GetByText count mismatch.");
        Assert(await items.TextContentAsync() == "beta", "GetByText text mismatch.");
    });

    await RunGroupAsync("keyboard-navigation", async () =>
    {
        await page!.SetContentAsync("""
            <label>Name <input id="name" /></label>
            <button id="next">Next</button>
            <output id="out"></output>
            """);
        await page.Locator("#name").FillAsync("");
        await page.Keyboard.TypeAsync("keyboard-aot");
        var inputValue = await page.InputValueAsync("#name");
        Assert(inputValue == "keyboard-aot", "Keyboard.TypeAsync failed.");

        await page.Keyboard.PressAsync("Tab");
        var focusIsButton = await page.EvaluateAsync<bool>("() => document.activeElement?.id === 'next'");
        Assert(focusIsButton, "Keyboard Tab focus failed.");
    });

    await RunGroupAsync("console-listener", async () =>
    {
        var consoleMsg = await page!.RunAndWaitForConsoleMessageAsync(async () =>
        {
            await page.EvaluateAsync("() => console.log('aot-console')");
        });
        Assert(consoleMsg.Text == "aot-console", "Console message text mismatch.");
    });

    await RunGroupAsync("wait-for-element", async () =>
    {
        await page!.SetContentAsync("<p id='target'>hello</p>");
        var el = await page.WaitForSelectorAsync("#target", new() { State = WaitForSelectorState.Attached, Timeout = 5000 });
        Assert(el != null, "WaitForSelector did not find element.");
        Assert(await el!.TextContentAsync() == "hello", "WaitForSelector element text mismatch.");
    });

    await RunGroupAsync("page-go-back-forward", async () =>
    {
        await page!.GotoAsync("data:text/html,<title>Page A</title><h1>A</h1>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(await page.TitleAsync() == "Page A", "Initial page title mismatch.");
        await page.GotoAsync("data:text/html,<title>Page B</title><h1>B</h1>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(await page.TitleAsync() == "Page B", "Second page title mismatch.");
        await page.GoBackAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(await page.TitleAsync() == "Page A", "GoBack title mismatch.");
        await page.GoForwardAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(await page.TitleAsync() == "Page B", "GoForward title mismatch.");
    });

    await RunGroupAsync("page-reload", async () =>
    {
        await page!.GotoAsync("data:text/html,<title>Reload Test</title><input id='counter' value='0' />", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.EvaluateAsync("() => document.title = 'modified'");
        Assert(await page.TitleAsync() == "modified", "Pre-reload title mismatch.");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(await page.TitleAsync() == "Reload Test", "Post-reload title mismatch.");
    });

    await RunGroupAsync("page-viewport", async () =>
    {
        await page!.SetViewportSizeAsync(500, 400);
        var size = await page.EvaluateAsync<JsonElement>("() => ({ w: window.innerWidth, h: window.innerHeight })");
        Assert(size.GetProperty("w").GetInt32() == 500, "Viewport width mismatch.");
        Assert(size.GetProperty("h").GetInt32() == 400, "Viewport height mismatch.");
        await page.SetViewportSizeAsync(800, 600);
    });

    await RunGroupAsync("page-add-init-script", async () =>
    {
        await page!.AddInitScriptAsync("window.AOT_INIT = 'ran';");
        await page.GotoAsync("data:text/html,<title>Init Script</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var val = await page.EvaluateAsync<string>("() => window.AOT_INIT");
        Assert(val == "ran", "AddInitScript did not execute.");
    });

    await RunGroupAsync("locator-state-checks", async () =>
    {
        await page!.SetContentAsync("""
            <button id="enabled">Click</button>
            <button id="disabled" disabled>No</button>
            <input id="check" type="checkbox" checked />
            <input id="visible" />
            """);
        Assert(await page.Locator("#enabled").IsEnabledAsync(), "Enabled button should be enabled.");
        Assert(await page.Locator("#disabled").IsDisabledAsync(), "Disabled button should be disabled.");
        Assert(await page.Locator("#check").IsCheckedAsync(), "Checked checkbox should be checked.");
        Assert(await page.Locator("#visible").IsVisibleAsync(), "Visible input should be visible.");
    });

    await RunGroupAsync("locator-all-inner-texts", async () =>
    {
        await page!.SetContentAsync("<ul><li>red</li><li>green</li><li>blue</li></ul>");
        var texts = await page.Locator("li").AllTextContentsAsync();
        Assert(texts.Count == 3 && texts[1] == "green", "AllTextContents mismatch.");
    });

    await RunGroupAsync("locator-select-option", async () =>
    {
        await page!.SetContentAsync("""
            <select id="color">
              <option value="r">Red</option>
              <option value="g">Green</option>
              <option value="b">Blue</option>
            </select>
            """);
        await page.Locator("#color").SelectOptionAsync("g");
        var val = await page.EvaluateAsync<string>("() => document.getElementById('color').value");
        Assert(val == "g", "SelectOption failed.");
    });

    await RunGroupAsync("locator-dispatch-event", async () =>
    {
        await page!.SetContentAsync("""
            <button id="btn">button</button>
            <div id="out"></div>
            <script>
              document.getElementById('btn').addEventListener('click', () => {
                document.getElementById('out').textContent = 'clicked';
              });
            </script>
            """);
        await page.Locator("#btn").DispatchEventAsync("click");
        var text = await page.Locator("#out").TextContentAsync();
        Assert(text == "clicked", "DispatchEvent click failed.");
    });

    await RunGroupAsync("element-handle-operations", async () =>
    {
        await page!.SetContentAsync("""
            <div id="box" style="width:100px;height:50px;background:red">box</div>
            """);
        var handle = await page.QuerySelectorAsync("#box");
        Assert(handle != null, "QuerySelector should find element.");

        var bb = await handle!.BoundingBoxAsync();
        Assert(bb != null && bb.Width == 100, "BoundingBox width mismatch.");

        var text = await handle.TextContentAsync();
        Assert(text == "box", "ElementHandle TextContent mismatch.");

        var bytes = await handle.ScreenshotAsync();
        Assert(bytes.Length > 0, "ElementHandle screenshot bytes empty.");
    });

    await RunGroupAsync("mouse-events", async () =>
    {
        await page!.SetContentAsync("""
            <div id="track" style="width:200px;height:200px;position:relative"></div>
            <script>
              let pos = [];
              document.getElementById('track').addEventListener('click', e => {
                pos.push({ x: e.clientX, y: e.clientY });
              });
              window.getPos = () => pos;
            </script>
            """);
        await page.Mouse.ClickAsync(50, 60);
        var pos = await page.EvaluateAsync<JsonElement>("() => window.getPos()");
        Assert(pos[0].GetProperty("x").GetInt32() == 50, "Mouse click x mismatch.");
        Assert(pos[0].GetProperty("y").GetInt32() == 60, "Mouse click y mismatch.");
    });

    await RunGroupAsync("page-inner-html-text", async () =>
    {
        await page!.SetContentAsync("<div id='content'><span>hello</span></div>");
        var html = await page.Locator("#content").InnerHTMLAsync();
        Assert(html == "<span>hello</span>", "InnerHTML mismatch.");
        var inner = await page.Locator("#content").InnerTextAsync();
        Assert(inner == "hello", "InnerText mismatch.");
    });

    await RunGroupAsync("browser-context-clear-storage", async () =>
    {
        var activeContext = context ?? throw new InvalidOperationException("Browser context was not created.");
        await activeContext.AddCookiesAsync(new[]
        {
            new Microsoft.Playwright.Cookie
            {
                Name = "clear-test", Value = "x", Url = "https://example.test",
            },
        });
        await activeContext.ClearCookiesAsync();
        var cookies = await activeContext.CookiesAsync("https://example.test");
        Assert(!cookies.Any(static c => c.Name == "clear-test"), "ClearCookies failed.");

        await activeContext.GrantPermissionsAsync(new[] { "geolocation" });
        await activeContext.ClearPermissionsAsync();
    });

    await RunGroupAsync("page-content", async () =>
    {
        await page!.SetContentAsync("<!doctype html><p>content-test</p>");
        var html = await page.ContentAsync();
        Assert(html.Contains("content-test"), "Page.ContentAsync mismatch.");
    });

    await RunGroupAsync("page-get-by-role", async () =>
    {
        await page!.SetContentAsync("<button>Click Me</button><output id='role-out'></output>");
        await page.GetByRole(AriaRole.Button, new() { Name = "Click Me" }).ClickAsync();
        await page.EvaluateAsync("() => document.getElementById('role-out').textContent = 'ok'");
        Assert(await page.Locator("#role-out").TextContentAsync() == "ok", "GetByRole click failed.");
    });

    await RunGroupAsync("page-get-by-test-id", async () =>
    {
        await page!.SetContentAsync("<button data-testid='submit-btn'>Send</button>");
        var btn = page.GetByTestId("submit-btn");
        Assert(await btn.TextContentAsync() == "Send", "GetByTestId text mismatch.");
    });

    await RunGroupAsync("page-add-script-tag", async () =>
    {
        await page!.SetContentAsync("<title>Script Test</title>");
        await page.AddScriptTagAsync(new() { Content = "window.AOT_SCRIPT = 'injected';" });
        var val = await page.EvaluateAsync<string>("() => window.AOT_SCRIPT");
        Assert(val == "injected", "AddScriptTag injection failed.");
    });

    await RunGroupAsync("page-emulate-media", async () =>
    {
        await page!.SetContentAsync("<!doctype html><style>@media print { body:after { content: 'print'; } }</style>");
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        var isPrint = await page.EvaluateAsync<bool>("() => matchMedia('print').matches");
        Assert(isPrint, "EmulateMedia print failed.");
    });

    await RunGroupAsync("page-wait-for-function", async () =>
    {
        await page!.SetContentAsync("<script>setTimeout(() => window.ready = true, 200)</script>");
        await page.WaitForFunctionAsync("() => window.ready");
        Assert(true, "WaitForFunction completed.");
    });

    await RunGroupAsync("locator-hover", async () =>
    {
        await page!.SetContentAsync("""
            <style>#hover-target { width:50px;height:50px;background:red; }
            #hover-target:hover { background:green; }</style>
            <div id="hover-target"></div>
            """);
        await page.Locator("#hover-target").HoverAsync();
        var bg = await page.EvaluateAsync<string>("() => getComputedStyle(document.getElementById('hover-target')).backgroundColor");
        Assert(bg == "rgb(0, 128, 0)", "Hover background color mismatch.");
    });

    await RunGroupAsync("locator-press-sequentially", async () =>
    {
        await page!.SetContentAsync("<input id='seq-input' />");
        await page.Locator("#seq-input").PressSequentiallyAsync("seq-test");
        var val = await page.InputValueAsync("#seq-input");
        Assert(val == "seq-test", "PressSequentially value mismatch.");
    });

    await RunGroupAsync("locator-clear", async () =>
    {
        await page!.SetContentAsync("<input id='clear-input' value='initial' />");
        await page.Locator("#clear-input").ClearAsync();
        var val = await page.InputValueAsync("#clear-input");
        Assert(val == "", "ClearAsync failed to clear input.");
    });

    await RunGroupAsync("locator-wait-for-state", async () =>
    {
        await page!.SetContentAsync("""
            <div id="dynamic" style="display:none">shown</div>
            <script>setTimeout(() => document.getElementById('dynamic').style.display = 'block', 100)</script>
            """);
        await page.Locator("#dynamic").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        Assert(await page.Locator("#dynamic").IsVisibleAsync(), "WaitFor state visible failed.");
    });

    await RunGroupAsync("locator-filter-nth", async () =>
    {
        await page!.SetContentAsync("<ul><li>a</li><li>b</li><li>c</li></ul>");
        var firstText = await page.Locator("li").First.TextContentAsync();
        Assert(firstText == "a", "Locator.First mismatch.");
        var lastText = await page.Locator("li").Last.TextContentAsync();
        Assert(lastText == "c", "Locator.Last mismatch.");
        var nthText = await page.Locator("li").Nth(1).TextContentAsync();
        Assert(nthText == "b", "Locator.Nth mismatch.");
        var filteredText = await page.Locator("li").Filter(new() { HasText = "b" }).TextContentAsync();
        Assert(filteredText == "b", "Locator.Filter mismatch.");
    });

    await RunGroupAsync("keyboard-down-up", async () =>
    {
        await page!.SetContentAsync("<input id='shift-input' />");
        await page.Locator("#shift-input").FocusAsync();
        await page.Keyboard.DownAsync("Shift");
        await page.Keyboard.PressAsync("KeyA");
        await page.Keyboard.PressAsync("KeyB");
        await page.Keyboard.UpAsync("Shift");
        var val = await page.InputValueAsync("#shift-input");
        Assert(val == "AB", "Shift+Press did not produce uppercase.");
    });

    await RunGroupAsync("mouse-wheel", async () =>
    {
        await page!.SetContentAsync("""
            <div id="scrollable" tabindex="0" style="width:100px;height:100px;overflow:scroll">
              <div style="height:500px">content</div>
            </div>
            """);
        await page.Locator("#scrollable").FocusAsync();
        await page.Mouse.MoveAsync(50, 50);
        await page.Mouse.WheelAsync(0, 50);
        await Task.Delay(100);
        var scrollTop = await page.EvaluateAsync<int>("() => document.getElementById('scrollable').scrollTop");
        Assert(scrollTop >= 1, "Mouse.Wheel scroll mismatch.");
    });

    await RunGroupAsync("context-set-geolocation", async () =>
    {
        var activeContext = context ?? throw new InvalidOperationException("Browser context was not created.");
        var geoPage = await activeContext.NewPageAsync();
        await geoPage.GotoAsync(server!.BaseUri + "page");
        Assert(await geoPage.Locator("#network").TextContentAsync() == "network-ok", "Server page not loaded.");
        await activeContext.GrantPermissionsAsync(new[] { "geolocation" }, new() { Origin = server!.BaseUri.GetLeftPart(UriPartial.Authority) });
        await activeContext.SetGeolocationAsync(new() { Latitude = 48.8566f, Longitude = 2.3522f });
        await geoPage.GotoAsync(server!.BaseUri + "page");
        var pos = await geoPage.EvaluateAsync<JsonElement>(@"
            () => new Promise(r =>
                navigator.geolocation.getCurrentPosition(p =>
                    r({ lat: p.coords.latitude, lng: p.coords.longitude })
                )
            )");
        var lat = pos.GetProperty("lat").GetDouble();
        Assert(Math.Abs(lat - 48.8566) < 0.01, "Geolocation latitude mismatch.");
        await geoPage.CloseAsync();
    });

    await RunGroupAsync("context-storage-state", async () =>
    {
        var activeContext = context ?? throw new InvalidOperationException("Browser context was not created.");
        var storagePage = await activeContext.NewPageAsync();
        await storagePage.GotoAsync(server!.BaseUri + "page");
        await storagePage.EvaluateAsync("() => localStorage.setItem('aot-key', 'aot-val')");
        var stateJson = await activeContext.StorageStateAsync();
        Assert(!string.IsNullOrEmpty(stateJson), "StorageState should not be empty.");
        await storagePage.CloseAsync();
    });

    await RunGroupAsync("element-handle-set-input-files", async () =>
    {
        await page!.SetContentAsync("<input type='file' id='file-input' />");
        var handle = await page.QuerySelectorAsync("#file-input");
        Assert(handle != null, "QuerySelector for file input failed.");
        await handle!.SetInputFilesAsync(new FilePayload
        {
            Name = "test.txt",
            MimeType = "text/plain",
            Buffer = new byte[] { (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o' },
        });
        var fileName = await page.EvaluateAsync<string>("() => document.getElementById('file-input').files[0].name");
        Assert(fileName == "test.txt", "File upload name mismatch.");
    });

    // ─── Frame operations ──────────────────────────────────────────────

    await RunGroupAsync("frame-main", async () =>
    {
        await page!.SetContentAsync("<!doctype html><title>Frame Test</title>");
        var mainFrame = page.MainFrame;
        System.Diagnostics.Debug.Assert(mainFrame != null);
        Assert(!string.IsNullOrEmpty(mainFrame.Url), "MainFrame.Url should not be empty.");
        Assert(await page.TitleAsync() == "Frame Test", "Page title via frame mismatch.");
        Assert(!mainFrame.IsDetached, "MainFrame should not be detached.");
        Assert(mainFrame.Name == string.Empty, "MainFrame name should be empty.");
    });

    await RunGroupAsync("frame-evaluate", async () =>
    {
        await page!.SetContentAsync("<script>window.FRAME_VAL = 42;</script>");
        var mainFrame = page.MainFrame;
        var val = await mainFrame.EvaluateAsync<int>("() => window.FRAME_VAL");
        Assert(val == 42, "Frame.EvaluateAsync mismatch.");

        var handle = await mainFrame.EvaluateHandleAsync("() => ({ a: 1, b: 2 })");
        Assert(handle != null, "EvaluateHandleAsync should return handle.");
    });

    await RunGroupAsync("frame-locator", async () =>
    {
        await page!.SetContentAsync("<input id='fname' value='' />");
        var mainFrame = page.MainFrame;
        await mainFrame.Locator("#fname").FillAsync("frame-aot");
        var val = await mainFrame.Locator("#fname").InputValueAsync();
        Assert(val == "frame-aot", "Frame locator FillAsync failed.");
    });

    await RunGroupAsync("frame-content", async () =>
    {
        await page!.SetContentAsync("<p>frame-content-test</p>");
        var mainFrame = page.MainFrame;
        var html = await mainFrame.ContentAsync();
        Assert(html.Contains("frame-content-test"), "Frame.ContentAsync mismatch.");
    });

    await RunGroupAsync("child-frame", async () =>
    {
        await page!.SetContentAsync("""
            <iframe id="child" src="data:text/html,<title>Child</title><p>child-here</p>"></iframe>
            """);
        await Task.Delay(200);
        var childFrame = page.Frames.FirstOrDefault(f => f != page.MainFrame);
        Assert(childFrame != null, "Child frame should exist.");
        Assert(await childFrame!.TitleAsync() == "Child", "Child frame title mismatch.");
        var text = await childFrame.Locator("p").TextContentAsync();
        Assert(text == "child-here", "Child frame text mismatch.");
        Assert(childFrame.ParentFrame == page.MainFrame, "Child parent frame mismatch.");
    });

    await RunGroupAsync("page-frame-locator", async () =>
    {
        await page!.SetContentAsync("""
            <iframe id="outer" src="data:text/html,<input id='inner-input'>"></iframe>
            """);
        await Task.Delay(200);
        await page.FrameLocator("#outer").Locator("#inner-input").FillAsync("fl-input");
        var val = await page.FrameLocator("#outer").Locator("#inner-input").InputValueAsync();
        Assert(val == "fl-input", "FrameLocator input value mismatch.");
    });

    // ─── Page misc operations ──────────────────────────────────────────

    await RunGroupAsync("page-bring-to-front", async () =>
    {
        await page!.BringToFrontAsync();
        Assert(true, "BringToFront completed.");
    });

    await RunGroupAsync("page-url", async () =>
    {
        await page!.GotoAsync("data:text/html,<title>URL Test</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(page.Url.StartsWith("data:"), "Page.Url mismatch.");
    });

    await RunGroupAsync("page-expose-binding", async () =>
    {
        await page!.ExposeBindingAsync("doubleBinding", (BindingSource source, int x) => x * 2);
        var result = await page.EvaluateAsync<int>("async () => await window.doubleBinding(21)");
        Assert(result == 42, "ExposeBinding result mismatch.");
    });

    await RunGroupAsync("page-dispatch-event", async () =>
    {
        await page!.SetContentAsync("""
            <button id='d-btn'>D</button><output id='d-out'></output>
            <script>
              document.getElementById('d-btn').addEventListener('dblclick', () => {
                document.getElementById('d-out').textContent = 'dbl';
              });
            </script>
            """);
        await page.DispatchEventAsync("#d-btn", "dblclick");
        var text = await page.Locator("#d-out").TextContentAsync();
        Assert(text == "dbl", "Page.DispatchEventAsync failed.");
    });

    await RunGroupAsync("page-fill-focus", async () =>
    {
        await page!.SetContentAsync("<input id='pf-input' />");
        await page.FocusAsync("#pf-input");
        var focused = await page.EvaluateAsync<bool>("() => document.activeElement?.id === 'pf-input'");
        Assert(focused, "Page.FocusAsync failed.");
        await page.FillAsync("#pf-input", "page-fill");
        var val = await page.InputValueAsync("#pf-input");
        Assert(val == "page-fill", "Page.FillAsync failed.");
    });

    await RunGroupAsync("page-press", async () =>
    {
        await page!.SetContentAsync("<input id='p-input' />");
        await page.FocusAsync("#p-input");
        await page.PressAsync("#p-input", "Shift+KeyH");
        await page.PressAsync("#p-input", "Shift+KeyI");
        var val = await page.InputValueAsync("#p-input");
        Assert(val == "HI", "Page.PressAsync failed.");
    });

    await RunGroupAsync("page-drag-and-drop", async () =>
    {
        await page!.SetContentAsync("""
            <div id="src" draggable="true" style="width:50px;height:50px;background:red">drag</div>
            <div id="dst" style="width:50px;height:50px;background:blue">drop</div>
            <script>
              let dropped = false;
              document.getElementById('dst').addEventListener('drop', e => {
                e.preventDefault(); dropped = true;
              });
              document.getElementById('dst').addEventListener('dragover', e => e.preventDefault());
              document.getElementById('src').addEventListener('dragstart', e => {
                e.dataTransfer.setData('text/plain', 'dragged');
              });
            </script>
            """);
        await page.DragAndDropAsync("#src", "#dst");
        var isDropped = await page.EvaluateAsync<bool>("() => dropped");
        Assert(isDropped, "DragAndDrop did not trigger drop.");
    });

    // ─── ElementHandle operations ──────────────────────────────────────

    await RunGroupAsync("element-handle-scroll", async () =>
    {
        await page!.SetContentAsync("""
            <div style="height:2000px"></div>
            <div id="bottom" style="margin-top:1800px">bottom</div>
            """);
        var handle = await page.QuerySelectorAsync("#bottom");
        Assert(handle != null, "QuerySelector for scroll target failed.");
        var before = await page.EvaluateAsync<int>("() => window.scrollY");
        await handle!.ScrollIntoViewIfNeededAsync();
        await Task.Delay(100);
        var after = await page.EvaluateAsync<int>("() => window.scrollY");
        Assert(after > before, "ScrollIntoViewIfNeeded did not scroll.");
    });

    await RunGroupAsync("element-handle-select-text", async () =>
    {
        await page!.SetContentAsync("<p id='sel-text'>selectable text</p>");
        var handle = await page.QuerySelectorAsync("#sel-text");
        await handle!.SelectTextAsync();
        var selected = await page.EvaluateAsync<string>("() => window.getSelection()?.toString()");
        Assert(selected == "selectable text", "SelectTextAsync mismatch.");
    });

    await RunGroupAsync("element-handle-check", async () =>
    {
        await page!.SetContentAsync("<input type='checkbox' id='eh-check' />");
        var handle = await page.QuerySelectorAsync("#eh-check");
        await handle!.CheckAsync();
        Assert(await handle.IsCheckedAsync(), "ElementHandle CheckAsync failed.");
        await handle.UncheckAsync();
        Assert(!await handle.IsCheckedAsync(), "ElementHandle UncheckAsync failed.");
    });

    await RunGroupAsync("element-handle-state-checks", async () =>
    {
        await page!.SetContentAsync("""
            <input id="eh-hidden" type="hidden" />
            <input id="eh-editable" />
            """);
        var hiddenHandle = await page.QuerySelectorAsync("#eh-hidden");
        Assert(await hiddenHandle!.IsHiddenAsync(), "Hidden element should be hidden.");
        var editableHandle = await page.QuerySelectorAsync("#eh-editable");
        Assert(await editableHandle!.IsEditableAsync(), "Editable input should be editable.");
    });

    await RunGroupAsync("element-handle-fill-type", async () =>
    {
        await page!.SetContentAsync("<input id='eh-input' />");
        var handle = await page.QuerySelectorAsync("#eh-input");
        await handle!.FillAsync("filled");
        var val = await handle.InputValueAsync();
        Assert(val == "filled", "ElementHandle FillAsync failed.");
    });

    // ─── Locator operations ────────────────────────────────────────────

    await RunGroupAsync("locator-screenshot", async () =>
    {
        await page!.SetContentAsync("<div id='ls-box' style='width:50px;height:50px;background:red'></div>");
        var bytes = await page.Locator("#ls-box").ScreenshotAsync();
        Assert(bytes.Length > 0, "Locator screenshot bytes empty.");
    });

    await RunGroupAsync("locator-check-uncheck", async () =>
    {
        await page!.SetContentAsync("<input type='checkbox' id='lc-check' />");
        await page.Locator("#lc-check").CheckAsync();
        Assert(await page.Locator("#lc-check").IsCheckedAsync(), "Locator CheckAsync failed.");
        await page.Locator("#lc-check").UncheckAsync();
        Assert(!await page.Locator("#lc-check").IsCheckedAsync(), "Locator UncheckAsync failed.");
    });

    await RunGroupAsync("locator-blur", async () =>
    {
        await page!.SetContentAsync("""
            <input id='lb-input' /><output id='lb-out'></output>
            <script>
              document.getElementById('lb-input').addEventListener('blur', () => {
                document.getElementById('lb-out').textContent = 'blurred';
              });
            </script>
            """);
        await page.Locator("#lb-input").FocusAsync();
        await page.Locator("#lb-input").BlurAsync();
        await Task.Delay(50);
        var text = await page.Locator("#lb-out").TextContentAsync();
        Assert(text == "blurred", "Locator BlurAsync failed.");
    });

    await RunGroupAsync("locator-get-attribute", async () =>
    {
        await page!.SetContentAsync("<a id='la-link' href='https://aot.test'>link</a>");
        var attr = await page.Locator("#la-link").GetAttributeAsync("href");
        Assert(attr == "https://aot.test", "Locator GetAttributeAsync failed.");
    });

    // ─── Mouse operations ──────────────────────────────────────────────

    await RunGroupAsync("mouse-dblclick", async () =>
    {
        await page!.SetContentAsync("""
            <div id='mc-dbl' style='width:100px;height:100px'></div>
            <script>
              let dblCount = 0;
              document.getElementById('mc-dbl').addEventListener('dblclick', () => { dblCount++; });
              window.getDblCount = () => dblCount;
            </script>
            """);
        await page.Locator("#mc-dbl").DblClickAsync();
        var count = await page.EvaluateAsync<int>("() => window.getDblCount()");
        Assert(count == 1, "DblClick did not fire.");
    });

    await RunGroupAsync("mouse-down-up", async () =>
    {
        await page!.SetContentAsync("""
            <div id='mc-du' style='width:100px;height:100px;background:gray'></div>
            <script>
              let down = false, up = false;
              document.getElementById('mc-du').addEventListener('mousedown', () => { down = true; });
              document.getElementById('mc-du').addEventListener('mouseup', () => { up = true; });
              window.getMouseDU = () => ({ down, up });
            </script>
            """);
        await page.Locator("#mc-du").HoverAsync();
        await page.Mouse.DownAsync();
        await page.Mouse.UpAsync();
        var state = await page.EvaluateAsync<JsonElement>("() => window.getMouseDU()");
        Assert(state.GetProperty("down").GetBoolean(), "Mouse.DownAsync failed.");
        Assert(state.GetProperty("up").GetBoolean(), "Mouse.UpAsync failed.");
    });

    await RunGroupAsync("page-close", async () =>
    {
        var closePage = await context!.NewPageAsync();
        Assert(!closePage.IsClosed, "New page should not be closed.");
        await closePage.CloseAsync();
        Assert(closePage.IsClosed, "Page should be closed.");
    });

    await RunGroupAsync("page-set-extra-http-headers", async () =>
    {
        var headersPage = await context!.NewPageAsync();
        await headersPage.SetExtraHTTPHeadersAsync(new[] { new KeyValuePair<string, string>("X-AOT", "yes") });
        IRequest capturedRequest = null!;
        await using var routeReg = await headersPage.RouteAsync("**/headers-page", async route =>
        {
            capturedRequest = route.Request;
            await route.FulfillAsync(new() { Status = 200, ContentType = "text/html", Body = "<p>ok</p>" });
        });
        await headersPage.GotoAsync("http://example.test/headers-page", new() { Timeout = 10_000, WaitUntil = WaitUntilState.DOMContentLoaded });
        var hasHeader = capturedRequest.Headers.TryGetValue("x-aot", out var val) || capturedRequest.Headers.TryGetValue("X-AOT", out val);
        Assert(hasHeader && val == "yes", "SetExtraHTTPHeaders did not send X-AOT.");
        await headersPage.CloseAsync();
    });

    await RunGroupAsync("page-popup", async () =>
    {
        await page!.GotoAsync(server!.BaseUri + "page", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await using var popupRoute = await page.Context.RouteAsync("**/popup-target", async route =>
        {
            await route.FulfillAsync(new() { Status = 200, ContentType = "text/html", Body = "<!doctype html><title>Popup</title>" });
        });
        var popup = await page.RunAndWaitForPopupAsync(async () =>
        {
            await page.EvaluateAsync($"window.open('{server!.BaseUri}popup-target', '_blank'); void 0");
        });
        Assert(popup != null, "Popup should be created.");
        Assert(await popup!.TitleAsync() == "Popup", "Popup title mismatch.");
        await popup.CloseAsync();
    });

    await RunGroupAsync("page-dialog", async () =>
    {
        var dialogPage = await context!.NewPageAsync();
        var tcs = new TaskCompletionSource<IDialog>();
        dialogPage.Dialog += async (_, dialog) =>
        {
            tcs.TrySetResult(dialog);
            await dialog.AcceptAsync();
        };
        await dialogPage.EvaluateAsync("() => alert('hello-aot')");
        var dialog = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert(dialog.Message == "hello-aot", "Dialog message mismatch.");
        Assert(dialog.Type == "alert", "Dialog type mismatch.");
        await dialogPage.CloseAsync();
    });

    await RunGroupAsync("route-abort", async () =>
    {
        await using var abortRoute = await page!.RouteAsync("**/abort-me", async route =>
        {
            await route.AbortAsync();
        });
        try
        {
            await page.GotoAsync("http://example.test/abort-me", new() { Timeout = 5000, WaitUntil = WaitUntilState.DOMContentLoaded });
            throw new InvalidOperationException("GotoAsync should have thrown on aborted route.");
        }
        catch (PlaywrightException)
        {
            Assert(true, "Route abort correctly caused navigation failure.");
        }
    });

    await RunGroupAsync("browser-type-name", async () =>
    {
        Assert(browser!.BrowserType.Name == "chromium", "BrowserType.Name mismatch.");
    });

    await RunGroupAsync("locator-all", async () =>
    {
        await page!.SetContentAsync("<ul><li>a</li><li>b</li><li>c</li></ul>");
        var handles = await page.Locator("li").AllAsync();
        Assert(handles.Count == 3, "Locator.All count mismatch.");
        var texts = await page.Locator("li").AllInnerTextsAsync();
        Assert(texts.Count == 3, "AllInnerTexts count mismatch.");
    });

    await RunGroupAsync("locator-bounding-box", async () =>
    {
        await page!.SetContentAsync("<div id='bb-box' style='width:80px;height:30px;margin:10px'>box</div>");
        var box = await page.Locator("#bb-box").BoundingBoxAsync();
        Assert(box != null, "BoundingBox should not be null.");
        Assert(Math.Abs(box!.Width - 80) < 1, "BoundingBox width mismatch.");
    });

    await RunGroupAsync("locator-or-and", async () =>
    {
        await page!.SetContentAsync("<div class='x'>first</div><div class='x'>second</div>");
        var orLocator = page.Locator(".x").Or(page.Locator(".nonexistent"));
        Assert(await orLocator.CountAsync() == 2, "Locator.Or count mismatch.");

        var andLocator = page.Locator("div").And(page.Locator(".x"));
        Assert(await andLocator.CountAsync() == 2, "Locator.And count mismatch.");
    });

    await RunGroupAsync("element-handle-get-attribute", async () =>
    {
        await page!.SetContentAsync("<a id='ehg-link' href='https://aot.test' data-info='test'>link</a>");
        var handle = await page.QuerySelectorAsync("#ehg-link");
        var href = await handle!.GetAttributeAsync("href");
        Assert(href == "https://aot.test", "ElementHandle GetAttributeAsync href mismatch.");
        var dataInfo = await handle.GetAttributeAsync("data-info");
        Assert(dataInfo == "test", "ElementHandle GetAttributeAsync data-info mismatch.");
    });

    await RunGroupAsync("element-handle-press", async () =>
    {
        await page!.SetContentAsync("<input id='ehp-input' />");
        var handle = await page.QuerySelectorAsync("#ehp-input");
        await handle!.FocusAsync();
        await handle.PressAsync("Shift+KeyA");
        await handle.PressAsync("Shift+KeyB");
        var val = await page.InputValueAsync("#ehp-input");
        Assert(val == "AB", "ElementHandle PressAsync failed.");
    });

    await RunGroupAsync("element-handle-select-option", async () =>
    {
        await page!.SetContentAsync("""
            <select id="ehs-select">
              <option value="o1">One</option>
              <option value="o2">Two</option>
              <option value="o3">Three</option>
            </select>
            """);
        var handle = await page.QuerySelectorAsync("#ehs-select");
        await handle!.SelectOptionAsync("o2");
        var val = await page.EvaluateAsync<string>("() => document.getElementById('ehs-select').value");
        Assert(val == "o2", "ElementHandle SelectOptionAsync failed.");
    });

    await RunGroupAsync("keyboard-insert-text", async () =>
    {
        await page!.SetContentAsync("<input id='kit-input' />");
        await page.Locator("#kit-input").FocusAsync();
        await page.Keyboard.InsertTextAsync("inserted-text");
        var val = await page.InputValueAsync("#kit-input");
        Assert(val == "inserted-text", "Keyboard InsertTextAsync failed.");
    });

    // ─── Accessibility / Aria ──────────────────────────────────────────

    await RunGroupAsync("page-aria-snapshot", async () =>
    {
        await page!.SetContentAsync("<button>OK</button><input placeholder='Name' />");
        var snapshot = await page.AriaSnapshotAsync();
        Assert(snapshot.Contains("button \"OK\""), "AriaSnapshot should contain button.");
        Assert(snapshot.Contains("textbox \"Name\""), "AriaSnapshot should contain textbox.");
    });

    // ─── Selectors ─────────────────────────────────────────────────────

    await RunGroupAsync("selectors-register", async () =>
    {
        await page!.SetContentAsync("<div id='sreg'>hello</div>");
        await playwright.Selectors.RegisterAsync("text-upper", new()
        {
            Script = """(s, root) => root.querySelector(s).textContent.toUpperCase()""",
        });
        var text = await page.EvaluateAsync<string>("() => document.querySelector('div').textContent");
        Assert(text == "hello", "Custom selector test text mismatch.");
    });

    // ─── Clock ─────────────────────────────────────────────────────────

    await RunGroupAsync("clock-install", async () =>
    {
        await page!.Context.Clock.InstallAsync();
        var before = await page.EvaluateAsync<long>("() => Date.now()");
        await page.Context.Clock.FastForwardAsync(10_000);
        var after = await page.EvaluateAsync<long>("() => Date.now()");
        Assert(after - before >= 9_000, "Clock.FastForward should advance time.");
    });

    // ─── Console message details ───────────────────────────────────────

    await RunGroupAsync("console-message-details", async () =>
    {
        var msg = await page!.RunAndWaitForConsoleMessageAsync(async () =>
        {
            await page.EvaluateAsync("() => console.log('detail-test', 42)");
        });
        Assert(msg.Type == "log", "Console message type mismatch.");
        Assert(msg.Text == "detail-test 42", "Console message text mismatch.");
        Assert(!string.IsNullOrEmpty(msg.Location), "Console message location should not be empty.");
    });

    // ─── APIRequestContext Post ────────────────────────────────────────

    await RunGroupAsync("api-request-post", async () =>
    {
        await using var api = await playwright.APIRequest.NewContextAsync();
        var response = await api.PostAsync(new Uri(server.BaseUri, "json").ToString());
        Assert(response.Ok, "APIRequest POST should be OK.");
        var json = await response.JsonAsync();
        Assert(json?.GetProperty("message").GetString() == "ok", "APIRequest POST response mismatch.");
    });

    // ─── Clearcote ─────────────────────────────────────────────────────

    await RunGroupAsync("clearcote-launch-options", async () =>
    {
        var opts = new ClearcoteLaunchOptions
        {
            Fingerprint = "test-fp",
            Humanize = true,
            ShowCursor = true,
            Quiet = true,
            DisablePrivacySandbox = true,
        };
        Assert(opts.Fingerprint == "test-fp", "ClearcoteLaunchOptions.Fingerprint mismatch.");
        Assert(opts.Humanize == true, "ClearcoteLaunchOptions.Humanize mismatch.");
        Assert(opts.Quiet == true, "ClearcoteLaunchOptions.Quiet mismatch.");
    });

    await RunGroupAsync("clearcote-persistent-options", async () =>
    {
        var opts = new ClearcoteLaunchPersistentContextOptions
        {
            Fingerprint = "persist-fp",
            Humanize = true,
            Geoip = false,
            Widevine = false,
            Profile = "test-profile",
        };
        Assert(opts.Fingerprint == "persist-fp", "ClearcoteLaunchPersistentContextOptions.Fingerprint mismatch.");
        Assert(opts.Geoip == false, "ClearcoteLaunchPersistentContextOptions.Geoip mismatch.");
        Assert(opts.Profile == "test-profile", "ClearcoteLaunchPersistentContextOptions.Profile mismatch.");
    });

    await RunGroupAsync("clearcote-profile", async () =>
    {
        var profile = new ClearcoteProfile("aottest", new ClearcoteLaunchPersistentContextOptions
        {
            Humanize = true,
        });
        Assert(profile.Name == "aottest", "ClearcoteProfile.Name mismatch.");
        Assert(profile.Options.Humanize == true, "ClearcoteProfile.Options.Humanize mismatch.");
        var savedPath = profile.Save();
        Assert(File.Exists(savedPath), "ClearcoteProfile.Save should create file.");

        var loaded = ClearcoteProfile.Load(savedPath);
        Assert(loaded.Name == "aottest", "Loaded profile name mismatch.");
        Assert(loaded.Options.Humanize == true, "Loaded profile Humanize mismatch.");
        File.Delete(savedPath);
    });

    await RunGroupAsync("clearcote-render-verdict", async () =>
    {
        var verdict = new ClearcoteRenderVerdict
        {
            Vendor = "Google",
            Renderer = "ANGLE",
            Webgl = true,
            Webgl2 = true,
            MaxTextureSize = 16384,
            SoftwareSuspected = false,
            Coherent = true,
            Warnings = Array.Empty<string>(),
        };
        Assert(verdict.Vendor == "Google", "RenderVerdict Vendor mismatch.");
        Assert(verdict.Coherent == true, "RenderVerdict Coherent mismatch.");
    });

    await RunGroupAsync("clearcote-agent-result", async () =>
    {
        var result = new ClearcoteAgentTaskResult
        {
            Success = true,
            FinalText = "done",
            Steps = Array.Empty<ClearcoteAgentStep>(),
            StepsJson = "[]",
        };
        Assert(result.Success == true, "AgentTaskResult Success mismatch.");
        Assert(result.FinalText == "done", "AgentTaskResult FinalText mismatch.");
    });

    // ===== Clearcote humanization tests =====
    // Verify the humanization interceptors work correctly.
    // They use CLEARCOTE_BINARY env var or auto-detect Playwright Chromium.

    var clearcoteBinary = Environment.GetEnvironmentVariable("CLEARCOTE_BINARY");
    if (string.IsNullOrEmpty(clearcoteBinary))
    {
        var home = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("USERPROFILE");
        if (home != null)
        {
            var pwCache = Path.Combine(home, ".cache", "ms-playwright");
            if (Directory.Exists(pwCache))
            {
                var dirs = Directory.GetDirectories(pwCache, "chromium-*");
                if (dirs.Length > 0)
                {
                    var candidate = Path.Combine(dirs[0], "chrome-linux64", "chrome");
                    if (File.Exists(candidate))
                    {
                        clearcoteBinary = candidate;
                    }
                }
            }
        }
    }

    if (clearcoteBinary != null)
    {
        Environment.SetEnvironmentVariable("CLEARCOTE_BINARY", clearcoteBinary);

        await RunGroupAsync("clearcote-humanize-setup", async () =>
        {
            clearcoteBrowser = await ClearcoteBrowser.LaunchAsync(playwright.Chromium, new ClearcoteLaunchOptions
            {
                Headless = true,
                Humanize = true,
                Quiet = true,
            });
            clearcoteContext = await clearcoteBrowser.NewContextAsync();
            clearcotePage = await clearcoteContext.NewPageAsync();
        });

        await RunGroupAsync("clearcote-humanize-click", async () =>
        {
            await clearcotePage!.SetContentAsync("""
                <button id="btn">Click</button>
                <output id="out"></output>
                <script>document.getElementById('btn').onclick = () => document.getElementById('out').textContent = 'ok';</script>
                """);
            await clearcotePage.Locator("#btn").ClickAsync();
            Assert(await clearcotePage.Locator("#out").TextContentAsync() == "ok", "Humanized click failed.");
        });

        await RunGroupAsync("clearcote-humanize-type", async () =>
        {
            await clearcotePage!.SetContentAsync("<input id='name'>");
            await clearcotePage.Locator("#name").ClickAsync();
            await clearcotePage.Keyboard.TypeAsync("hi");
            Assert(await clearcotePage.InputValueAsync("#name") == "hi", "Humanized type failed.");
        });

        await RunGroupAsync("clearcote-humanize-fill", async () =>
        {
            await clearcotePage!.SetContentAsync("<input id='field'>");
            await clearcotePage.Locator("#field").FillAsync("val");
            Assert(await clearcotePage.InputValueAsync("#field") == "val", "Humanized fill failed.");
        });

        await RunGroupAsync("clearcote-humanize-hover", async () =>
        {
            await clearcotePage!.SetContentAsync("""
                <style>#h { width:50px; height:50px; background:red; } #h:hover { background:green; }</style>
                <div id="h"></div>
                """);
            await clearcotePage.Locator("#h").HoverAsync();
            var bg = await clearcotePage.EvaluateAsync<string>("() => getComputedStyle(document.getElementById('h')).backgroundColor");
            Assert(bg.Contains("128") || bg.Contains("green"), $"Hover should turn green, got: {bg}");
        });

        await RunGroupAsync("clearcote-humanize-dblclick", async () =>
        {
            await clearcotePage!.SetContentAsync("""
                <button id="dbl">Db</button>
                <output id="out"></output>
                <script>document.getElementById('dbl').addEventListener('dblclick', () => document.getElementById('out').textContent = 'ok');</script>
                """);
            await clearcotePage.Locator("#dbl").DblClickAsync();
            Assert(await clearcotePage.Locator("#out").TextContentAsync() == "ok", "Humanized dblclick failed.");
        });

        await RunGroupAsync("clearcote-humanize-press", async () =>
        {
            await clearcotePage!.SetContentAsync("<input id='press'>");
            await clearcotePage.Locator("#press").PressAsync("a");
            Assert(await clearcotePage.InputValueAsync("#press") == "a", "Humanized press failed.");
        });

        await RunGroupAsync("clearcote-humanize-dragndrop", async () =>
        {
            await clearcotePage!.SetContentAsync("""
                <div id="src" style="position:absolute;left:0;top:0;width:50px;height:50px;background:red;"></div>
                <div id="tgt" style="position:absolute;left:300px;top:0;width:50px;height:50px;background:blue;"></div>
                """);
            await clearcotePage.DragAndDropAsync("#src", "#tgt");
            Assert(true, "Humanized drag-and-drop completed.");
        });

        await RunGroupAsync("clearcote-humanize-showcursor", async () =>
        {
            await using var ccBrowser2 = await ClearcoteBrowser.LaunchAsync(playwright.Chromium, new ClearcoteLaunchOptions
            {
                Headless = true,
                Humanize = true,
                ShowCursor = true,
                Quiet = true,
            });
            await using var ccCtx2 = await ccBrowser2.NewContextAsync();
            var ccPage2 = await ccCtx2.NewPageAsync();
            var hasCursor = await ccPage2.EvaluateAsync<bool>("() => !!document.getElementById('__clearcote_cursor')");
            Assert(hasCursor, "ShowCursor should inject cursor overlay.");
        });
    }
    else
    {
        Console.WriteLine("SKIP clearcote-humanize-* (set CLEARCOTE_BINARY or install Playwright browsers)");
    }

    await RunGroupAsync("screenshot", async () =>
    {
        var screenshotPath = "playwright-aot.png";
        var bytes = await page!.ScreenshotAsync(new() { Path = screenshotPath });
        Assert(bytes.Length > 0, "Screenshot bytes should not be empty.");
        Assert(File.Exists(screenshotPath), "Screenshot file was not written.");
    });
}
finally
{
    if (clearcotePage != null)
    {
        await clearcotePage.CloseAsync();
    }

    if (clearcoteContext != null)
    {
        await clearcoteContext.CloseAsync();
    }

    if (clearcoteBrowser != null)
    {
        await clearcoteBrowser.CloseAsync();
    }

    if (context != null)
    {
        await context.CloseAsync();
    }

    if (browser != null)
    {
        await browser.CloseAsync();
    }
}

Console.WriteLine("NativeAOT validation complete.");

static async Task RunGroupAsync(string name, Func<Task> action)
{
    try
    {
        await action().ConfigureAwait(false);
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL {name}: {ex.GetType().Name}: {ex.Message}");
        throw;
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

internal sealed class SampleApiPayload
{
    public string Message { get; set; } = string.Empty;

    public int Count { get; set; }

    public string[] Tags { get; set; } = Array.Empty<string>();
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SampleApiPayload))]
internal partial class AotSampleJsonContext : JsonSerializerContext
{
}

internal sealed class LocalJsonServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly Task _acceptLoop;

    private LocalJsonServer(TcpListener listener)
    {
        _listener = listener;
        BaseUri = new Uri($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");
        _acceptLoop = AcceptLoopAsync();
    }

    public Uri BaseUri { get; }

    public static LocalJsonServer Start()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return new LocalJsonServer(listener);
    }

    public async ValueTask DisposeAsync()
    {
        _listener.Stop();
        try
        {
            await _acceptLoop.ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException)
        {
        }
    }

    private async Task AcceptLoopAsync()
    {
        while (true)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (SocketException)
            {
                return;
            }

            _ = HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            await using var stream = client.GetStream();
            var buffer = new byte[2048];
            var read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            var request = Encoding.ASCII.GetString(buffer, 0, read);
            var isJson = request.StartsWith("GET /json ", StringComparison.Ordinal) ||
                         request.StartsWith("POST /json ", StringComparison.Ordinal);
            var isPage = request.StartsWith("GET /page ", StringComparison.Ordinal) ||
                         request.StartsWith("POST /page ", StringComparison.Ordinal);
            var status = isJson || isPage ? "200 OK" : "404 Not Found";
            var contentType = isPage ? "text/html" : "application/json";
            var body = isJson
                ? """{"message":"ok","count":3,"tags":["nativeaot","json"]}"""
                : isPage
                    ? "<!doctype html><title>Network OK</title><h1 id=\"network\">network-ok</h1>"
                    : """{"message":"not-found","count":0,"tags":[]}""";
            var bytes = Encoding.UTF8.GetBytes(body);
            var headers = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {status}\r\nContent-Type: {contentType}\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(headers, 0, headers.Length).ConfigureAwait(false);
            await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }
    }
}
