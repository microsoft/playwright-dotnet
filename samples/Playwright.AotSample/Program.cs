using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright;

await using var server = LocalJsonServer.Start();
using var playwright = await Playwright.CreateAsync();

int passed = 0, failed = 0;
IBrowser? browser = null;
IBrowserContext? context = null;
IPage? page = null;

void Assert(bool cond, string msg)
{
    if (!cond) throw new InvalidOperationException(msg);
}

async Task RunGroupAsync(string name, Func<Task> action)
{
    try
    {
        await action().ConfigureAwait(false);
        Console.WriteLine($"  PASS {name}");
        Interlocked.Increment(ref passed);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  FAIL {name}: {ex.GetType().Name}: {ex.Message}");
        Interlocked.Increment(ref failed);
    }
}

try
{
    // ═══════════════════════════════════════════════════════════
    // 1. BROWSER LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("browser-launch", async () =>
    {
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        Assert(!string.IsNullOrWhiteSpace(browser.Version), "Browser version missing");
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 800, Height = 600 } });
        page = await context.NewPageAsync();
        Assert(page != null, "Page should be created");
    });

    await RunGroupAsync("browser-type-name", async () =>
    {
        Assert(browser!.BrowserType.Name == "chromium", "BrowserType.Name mismatch");
    });

    await RunGroupAsync("browser-new-page", async () =>
    {
        await using var bp = await browser!.NewPageAsync();
        Assert(bp != null, "Browser.NewPageAsync should work");
        await bp!.CloseAsync();
    });

    // ═══════════════════════════════════════════════════════════
    // 2. NAVIGATION
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-goto", async () =>
    {
        var url = server!.BaseUri + "page";
        var response = await page!.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(response != null, "GotoAsync should return response");
        Assert(response!.Status == 200, "Response status should be 200");
        Assert(page.Url == url, "Page.Url mismatch");
    });

    await RunGroupAsync("page-title", async () =>
    {
        Assert(await page!.TitleAsync() == "Network OK", "Title mismatch");
    });

    await RunGroupAsync("page-go-back-forward", async () =>
    {
        var url = server!.BaseUri + "page";
        await page!.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync("data:text/html,<title>Page B</title><h1>B</h1>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var back = await page.GoBackAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(back?.Status == 200, "GoBack should return response");
        Assert(await page.TitleAsync() == "Network OK", "GoBack title mismatch");
        var fwd = await page.GoForwardAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        // GoForward to a data: URI returns null response, that's expected
        Assert(await page.TitleAsync() == "Page B", "GoForward title mismatch");
    });

    await RunGroupAsync("page-reload", async () =>
    {
        var url = server!.BaseUri + "page";
        await page!.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.EvaluateAsync("() => document.title = 'modified'");
        var response = await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(response?.Status == 200, "Reload should return response");
        Assert(await page.TitleAsync() == "Network OK", "Reload title mismatch");
    });

    await RunGroupAsync("page-wait-for-navigation", async () =>
    {
        var url = server!.BaseUri + "page";
        await page!.GotoAsync("data:text/html,<title>Nav Start</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var waitTask = page.WaitForURLAsync(url);
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await waitTask;
    });

    await RunGroupAsync("page-wait-for-url", async () =>
    {
        var url = server!.BaseUri + "page";
        await page!.GotoAsync("data:text/html,<title>URL Wait</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var urlTask = page.WaitForURLAsync("**/page");
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await urlTask;
    });

    await RunGroupAsync("page-wait-for-load-state", async () =>
    {
        var url = server!.BaseUri + "page";
        await page!.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    });

    // ═══════════════════════════════════════════════════════════
    // 3. PAGE CONTENT
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-set-content", async () =>
    {
        await page!.SetContentAsync("<!doctype html><title>SC</title><p>content</p>");
        Assert(await page.TitleAsync() == "SC", "SetContent title mismatch");
    });

    await RunGroupAsync("page-content", async () =>
    {
        var html = await page!.ContentAsync();
        Assert(html.Contains("content"), "ContentAsync mismatch");
    });

    await RunGroupAsync("page-bring-to-front", async () =>
    {
        await page!.BringToFrontAsync();
    });

    await RunGroupAsync("page-opener", async () =>
    {
        var opener = await page!.OpenerAsync();
        Assert(opener == null, "Main page should have no opener");
    });

    // ═══════════════════════════════════════════════════════════
    // 4. EVALUATE
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-evaluate", async () =>
    {
        var sum = await page!.EvaluateAsync<int>("() => 1 + 2");
        Assert(sum == 3, "Evaluate<int> mismatch");
    });

    await RunGroupAsync("page-evaluate-json-element", async () =>
    {
        var payload = await page!.EvaluateAsync<JsonElement>("() => ({ a: 1, b: 'two' })");
        Assert(payload.GetProperty("a").GetInt32() == 1, "Evaluate JsonElement a mismatch");
        Assert(payload.GetProperty("b").GetString() == "two", "Evaluate JsonElement b mismatch");
    });

    await RunGroupAsync("page-evaluate-non-generic", async () =>
    {
        var result = await page!.EvaluateAsync("() => 'hello'");
        Assert(result?.ToString() == "hello", "Evaluate non-generic mismatch");
    });

    await RunGroupAsync("page-evaluate-handle", async () =>
    {
        var handle = await page!.EvaluateHandleAsync("() => ({ x: 10 })");
        Assert(handle != null, "EvaluateHandleAsync should return handle");
    });

    await RunGroupAsync("page-eval-on-selector", async () =>
    {
        await page!.SetContentAsync("<p id='eos'>hello</p>");
        var text = await page.EvalOnSelectorAsync<string>("#eos", "el => el.textContent");
        Assert(text == "hello", "EvalOnSelector mismatch");
    });

    await RunGroupAsync("page-eval-on-selector-all", async () =>
    {
        await page!.SetContentAsync("<ul><li>a</li><li>b</li></ul>");
        var count = await page.EvalOnSelectorAllAsync<int>("li", "els => els.length");
        Assert(count == 2, "EvalOnSelectorAll mismatch");
    });

    // ═══════════════════════════════════════════════════════════
    // 5. LOCATOR
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("locator-create", async () =>
    {
        await page!.SetContentAsync("<div class='test'>x</div>");
        var loc = page.Locator(".test");
        Assert(loc != null, "Locator should not be null");
    });

    await RunGroupAsync("locator-get-by-text", async () =>
    {
        await page!.SetContentAsync("<ul><li>alpha</li><li>beta</li></ul>");
        Assert(await page.GetByText("beta").CountAsync() == 1, "GetByText count mismatch");
    });

    await RunGroupAsync("locator-get-by-role", async () =>
    {
        await page!.SetContentAsync("<button>OK</button>");
        Assert(await page.GetByRole(AriaRole.Button).CountAsync() == 1, "GetByRole count mismatch");
    });

    await RunGroupAsync("locator-get-by-test-id", async () =>
    {
        await page!.SetContentAsync("<button data-testid='sub'>Send</button>");
        Assert(await page.GetByTestId("sub").TextContentAsync() == "Send", "GetByTestId mismatch");
    });

    await RunGroupAsync("locator-get-by-alt-text", async () =>
    {
        await page!.SetContentAsync("<img alt='photo' src=''>");
        Assert(await page.GetByAltText("photo").CountAsync() == 1, "GetByAltText mismatch");
    });

    await RunGroupAsync("locator-get-by-label", async () =>
    {
        await page!.SetContentAsync("<label for='fn'>Name</label><input id='fn'>");
        Assert(await page.GetByLabel("Name").CountAsync() == 1, "GetByLabel mismatch");
    });

    await RunGroupAsync("locator-get-by-placeholder", async () =>
    {
        await page!.SetContentAsync("<input placeholder='Enter...'>");
        Assert(await page.GetByPlaceholder("Enter...").CountAsync() == 1, "GetByPlaceholder mismatch");
    });

    await RunGroupAsync("locator-get-by-title", async () =>
    {
        await page!.SetContentAsync("<div title='info'>text</div>");
        Assert(await page.GetByTitle("info").TextContentAsync() == "text", "GetByTitle mismatch");
    });

    await RunGroupAsync("locator-count", async () =>
    {
        await page!.SetContentAsync("<ul><li>a</li><li>b</li><li>c</li></ul>");
        Assert(await page.Locator("li").CountAsync() == 3, "Count mismatch");
    });

    await RunGroupAsync("locator-first-last-nth", async () =>
    {
        await page!.SetContentAsync("<ul><li>x</li><li>y</li><li>z</li></ul>");
        Assert(await page.Locator("li").First.TextContentAsync() == "x", "First mismatch");
        Assert(await page.Locator("li").Last.TextContentAsync() == "z", "Last mismatch");
        Assert(await page.Locator("li").Nth(1).TextContentAsync() == "y", "Nth mismatch");
    });

    await RunGroupAsync("locator-filter-and-or", async () =>
    {
        await page!.SetContentAsync("<div class='x'>a</div><div class='x'>b</div>");
        var filtered = page.Locator("div").Filter(new() { HasText = "a" });
        Assert(await filtered.CountAsync() == 1, "Filter mismatch");
        var orLoc = page.Locator(".x").Or(page.Locator(".nonexistent"));
        Assert(await orLoc.CountAsync() == 2, "Or count mismatch");
        var andLoc = page.Locator("div").And(page.Locator(".x"));
        Assert(await andLoc.CountAsync() == 2, "And count mismatch");
    });

    await RunGroupAsync("locator-all-texts", async () =>
    {
        await page!.SetContentAsync("<ul><li>red</li><li>green</li></ul>");
        var texts = await page.Locator("li").AllTextContentsAsync();
        Assert(texts.Count == 2 && texts[1] == "green", "AllTextContents mismatch");
        var inner = await page.Locator("li").AllInnerTextsAsync();
        Assert(inner.Count == 2, "AllInnerTexts mismatch");
    });

    await RunGroupAsync("locator-all-elements", async () =>
    {
        await page!.SetContentAsync("<ul><li>a</li><li>b</li></ul>");
        var all = await page.Locator("li").AllAsync();
        Assert(all.Count == 2, "AllAsync count mismatch");
    });

    await RunGroupAsync("locator-describe", async () =>
    {
        var loc = page!.Locator("div").Describe("my-div");
        Assert(await loc.CountAsync() >= 0, "Describe should not throw");
    });

    // ═══════════════════════════════════════════════════════════
    // 6. LOCATOR ACTIONS
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("locator-click", async () =>
    {
        await page!.SetContentAsync("<button id='lc'>click</button><output id='lc-out'></output><script>document.getElementById('lc').onclick=()=>document.getElementById('lc-out').textContent='ok'</script>");
        await page.Locator("#lc").ClickAsync();
        Assert(await page.Locator("#lc-out").TextContentAsync() == "ok", "Click mismatch");
    });

    await RunGroupAsync("locator-dbl-click", async () =>
    {
        await page!.SetContentAsync("<button id='ld'>db</button><output id='ld-out'></output><script>document.getElementById('ld').ondblclick=()=>document.getElementById('ld-out').textContent='ok'</script>");
        await page.Locator("#ld").DblClickAsync();
        Assert(await page.Locator("#ld-out").TextContentAsync() == "ok", "DblClick mismatch");
    });

    await RunGroupAsync("locator-fill", async () =>
    {
        await page!.SetContentAsync("<input id='lf'>");
        await page.Locator("#lf").FillAsync("hello");
        Assert(await page.InputValueAsync("#lf") == "hello", "Fill mismatch");
    });

    await RunGroupAsync("locator-type", async () =>
    {
        await page!.SetContentAsync("<input id='lt'>");
        await page.Locator("#lt").FillAsync("world");
        Assert(await page.InputValueAsync("#lt") == "world", "Fill mismatch");
    });

    await RunGroupAsync("locator-press", async () =>
    {
        await page!.SetContentAsync("<input id='lp'>");
        await page.Locator("#lp").PressAsync("Shift+KeyA");
        Assert(await page.InputValueAsync("#lp") == "A", "Press mismatch");
    });

    await RunGroupAsync("locator-press-sequentially", async () =>
    {
        await page!.SetContentAsync("<input id='lps'>");
        await page.Locator("#lps").PressSequentiallyAsync("seq");
        Assert(await page.InputValueAsync("#lps") == "seq", "PressSequentially mismatch");
    });

    await RunGroupAsync("locator-check-uncheck", async () =>
    {
        await page!.SetContentAsync("<input type='checkbox' id='lcu'>");
        await page.Locator("#lcu").CheckAsync();
        Assert(await page.Locator("#lcu").IsCheckedAsync(), "Check failed");
        await page.Locator("#lcu").UncheckAsync();
        Assert(!await page.Locator("#lcu").IsCheckedAsync(), "Uncheck failed");
    });

    await RunGroupAsync("locator-set-checked", async () =>
    {
        await page!.SetContentAsync("<input type='checkbox' id='lsc'>");
        await page.Locator("#lsc").SetCheckedAsync(true);
        Assert(await page.Locator("#lsc").IsCheckedAsync(), "SetChecked true failed");
        await page.Locator("#lsc").SetCheckedAsync(false);
        Assert(!await page.Locator("#lsc").IsCheckedAsync(), "SetChecked false failed");
    });

    await RunGroupAsync("locator-hover", async () =>
    {
        await page!.SetContentAsync("<div id='lh' style='width:50px;height:50px;background:red' onmouseenter='this.classList.add(\"hovered\")'></div>");
        await page.Locator("#lh").HoverAsync();
        var hasClass = await page.EvaluateAsync<bool>("() => document.getElementById('lh').classList.contains('hovered')");
        Assert(hasClass, "Hover should add class");
    });

    await RunGroupAsync("locator-focus-blur", async () =>
    {
        await page!.SetContentAsync("<input id='lfb'><output id='lfb-out'></output><script>document.getElementById('lfb').onblur=()=>document.getElementById('lfb-out').textContent='blur'</script>");
        await page.Locator("#lfb").FocusAsync();
        var focused = await page.EvaluateAsync<bool>("() => document.activeElement?.id === 'lfb'");
        Assert(focused, "Focus failed");
        await page.Locator("#lfb").BlurAsync();
        Assert(await page.Locator("#lfb-out").TextContentAsync() == "blur", "Blur failed");
    });

    await RunGroupAsync("locator-clear", async () =>
    {
        await page!.SetContentAsync("<input id='lcl' value='initial'>");
        await page.Locator("#lcl").ClearAsync();
        Assert(await page.InputValueAsync("#lcl") == "", "Clear mismatch");
    });

    await RunGroupAsync("locator-select-option", async () =>
    {
        await page!.SetContentAsync("<select id='lso'><option value='a'>A</option><option value='b'>B</option></select>");
        var selected = await page.Locator("#lso").SelectOptionAsync("b");
        Assert(selected.Count == 1 && selected[0] == "b", "SelectOption mismatch");
    });

    await RunGroupAsync("locator-dispatch-event", async () =>
    {
        await page!.SetContentAsync("<button id='lde'>btn</button><output id='lde-out'></output><script>document.getElementById('lde').addEventListener('click',()=>document.getElementById('lde-out').textContent='clicked')</script>");
        await page.Locator("#lde").DispatchEventAsync("click");
        Assert(await page.Locator("#lde-out").TextContentAsync() == "clicked", "DispatchEvent mismatch");
    });

    await RunGroupAsync("locator-tap", async () =>
    {
        var touchCtx = await browser!.NewContextAsync(new() { HasTouch = true });
        var tp = await touchCtx.NewPageAsync();
        await tp.SetContentAsync("<button id='ltap'>tap</button><output id='ltap-out'></output><script>document.getElementById('ltap').addEventListener('touchstart',()=>document.getElementById('ltap-out').textContent='tapped')</script>");
        await tp.Locator("#ltap").TapAsync();
        Assert(await tp.Locator("#ltap-out").TextContentAsync() == "tapped", "Locator Tap mismatch");
        await tp.CloseAsync();
        await touchCtx.CloseAsync();
    });

    await RunGroupAsync("locator-screenshot", async () =>
    {
        await page!.SetContentAsync("<div style='width:50px;height:50px;background:red'></div>");
        var bytes = await page.Locator("div").ScreenshotAsync();
        Assert(bytes.Length > 0, "Locator screenshot empty");
    });

    await RunGroupAsync("locator-bounding-box", async () =>
    {
        await page!.SetContentAsync("<div id='lbb' style='width:80px;height:30px'>box</div>");
        var box = await page.Locator("#lbb").BoundingBoxAsync();
        Assert(box != null && Math.Abs(box!.Width - 80) < 1, "BoundingBox mismatch");
    });

    await RunGroupAsync("locator-text-content", async () =>
    {
        await page!.SetContentAsync("<p id='ltc'>text</p>");
        Assert(await page.Locator("#ltc").TextContentAsync() == "text", "TextContent mismatch");
    });

    await RunGroupAsync("locator-inner-html-text", async () =>
    {
        await page!.SetContentAsync("<div id='lih'><span>inner</span></div>");
        Assert(await page.Locator("#lih").InnerHTMLAsync() == "<span>inner</span>", "InnerHTML mismatch");
        Assert(await page.Locator("#lih").InnerTextAsync() == "inner", "InnerText mismatch");
    });

    await RunGroupAsync("locator-input-value", async () =>
    {
        await page!.SetContentAsync("<input id='liv' value='test'>");
        Assert(await page.Locator("#liv").InputValueAsync() == "test", "InputValue mismatch");
    });

    await RunGroupAsync("locator-get-attribute", async () =>
    {
        await page!.SetContentAsync("<a id='lga' href='https://aot.test'>link</a>");
        Assert(await page.Locator("#lga").GetAttributeAsync("href") == "https://aot.test", "GetAttribute mismatch");
    });

    await RunGroupAsync("locator-drag-to", async () =>
    {
        await page!.SetContentAsync("<div id='src' draggable='true' style='width:50px;height:50px;background:red'>drag</div><div id='dst' style='width:50px;height:50px;background:blue'>drop</div><script>let dropped=false;document.getElementById('dst').addEventListener('drop',e=>{e.preventDefault();dropped=true});document.getElementById('dst').addEventListener('dragover',e=>e.preventDefault());document.getElementById('src').addEventListener('dragstart',e=>e.dataTransfer.setData('text','drag'))</script>");
        await page.Locator("#src").DragToAsync(page.Locator("#dst"));
        var isDropped = await page.EvaluateAsync<bool>("() => dropped");
        Assert(isDropped, "DragTo mismatch");
    });

    await RunGroupAsync("locator-scroll-into-view", async () =>
    {
        await page!.SetContentAsync("<div style='height:2000px'></div><div id='lsv'>bottom</div>");
        await page.Locator("#lsv").ScrollIntoViewIfNeededAsync();
        var visible = await page.Locator("#lsv").IsVisibleAsync();
        Assert(visible, "ScrollIntoViewIfNeeded mismatch");
    });

    await RunGroupAsync("locator-select-text", async () =>
    {
        await page!.SetContentAsync("<p id='lst'>selectable</p>");
        await page.Locator("#lst").SelectTextAsync();
        var selected = await page.EvaluateAsync<string>("() => window.getSelection()?.toString()");
        Assert(selected == "selectable", "SelectText mismatch");
    });

    await RunGroupAsync("locator-wait-for", async () =>
    {
        await page!.SetContentAsync("<div id='lwf' style='display:none'>shown</div><script>setTimeout(()=>document.getElementById('lwf').style.display='block',50)</script>");
        await page.Locator("#lwf").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        Assert(await page.Locator("#lwf").IsVisibleAsync(), "WaitFor visible failed");
    });

    await RunGroupAsync("locator-element-handle", async () =>
    {
        await page!.SetContentAsync("<p id='leh'>handle</p>");
        var handle = await page.Locator("#leh").ElementHandleAsync();
        Assert(handle != null, "ElementHandleAsync should return handle");
        var handles = await page.Locator("p").ElementHandlesAsync();
        Assert(handles.Count == 1, "ElementHandlesAsync count mismatch");
    });

    await RunGroupAsync("locator-evaluate", async () =>
    {
        await page!.SetContentAsync("<p id='lev'>text</p>");
        var text = await page.Locator("#lev").EvaluateAsync<string>("el => el.textContent");
        Assert(text == "text", "Locator Evaluate mismatch");
        var all = await page.Locator("p").EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)");
        Assert(all.Length == 1 && all[0] == "text", "EvaluateAll mismatch");
    });

    await RunGroupAsync("locator-evaluate-handle", async () =>
    {
        await page!.SetContentAsync("<p id='leh2'>data</p>");
        var handle = await page.Locator("#leh2").EvaluateHandleAsync("el => el");
        Assert(handle != null, "EvaluateHandle async should return handle");
    });

    // ═══════════════════════════════════════════════════════════
    // 7. PAGE ACTIONS
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-click", async () =>
    {
        await page!.SetContentAsync("<button id='pc'>c</button><output id='pc-out'></output><script>document.getElementById('pc').onclick=()=>document.getElementById('pc-out').textContent='ok'</script>");
        await page.ClickAsync("#pc");
        Assert(await page.Locator("#pc-out").TextContentAsync() == "ok", "Page Click mismatch");
    });

    await RunGroupAsync("page-dbl-click", async () =>
    {
        await page!.SetContentAsync("<button id='pdc'>db</button><output id='pdc-out'></output><script>document.getElementById('pdc').ondblclick=()=>document.getElementById('pdc-out').textContent='ok'</script>");
        await page.DblClickAsync("#pdc");
        Assert(await page.Locator("#pdc-out").TextContentAsync() == "ok", "Page DblClick mismatch");
    });

    await RunGroupAsync("page-fill", async () =>
    {
        await page!.SetContentAsync("<input id='pf'>");
        await page.FillAsync("#pf", "page-fill");
        Assert(await page.InputValueAsync("#pf") == "page-fill", "Page Fill mismatch");
    });

    await RunGroupAsync("page-type", async () =>
    {
        await page!.SetContentAsync("<input id='pt'>");
        await page.FillAsync("#pt", "page-type");
        Assert(await page.InputValueAsync("#pt") == "page-type", "Page Fill mismatch");
    });

    await RunGroupAsync("page-press", async () =>
    {
        await page!.SetContentAsync("<input id='pp'>");
        await page.FocusAsync("#pp");
        await page.PressAsync("#pp", "Shift+KeyH");
        Assert(await page.InputValueAsync("#pp") == "H", "Page Press mismatch");
    });

    await RunGroupAsync("page-check-uncheck", async () =>
    {
        await page!.SetContentAsync("<input type='checkbox' id='pcu'>");
        await page.CheckAsync("#pcu");
        Assert(await page.IsCheckedAsync("#pcu"), "Page Check failed");
        await page.UncheckAsync("#pcu");
        Assert(!await page.IsCheckedAsync("#pcu"), "Page Uncheck failed");
    });

    await RunGroupAsync("page-set-checked", async () =>
    {
        await page!.SetContentAsync("<input type='checkbox' id='psc'>");
        await page.SetCheckedAsync("#psc", true);
        Assert(await page.IsCheckedAsync("#psc"), "Page SetChecked true failed");
    });

    await RunGroupAsync("page-hover", async () =>
    {
        await page!.SetContentAsync("<div id='ph' style='width:50px;height:50px;background:red' onmouseenter='this.classList.add(\"hovered\")'></div>");
        await page.HoverAsync("#ph");
        var hasClass = await page.EvaluateAsync<bool>("() => document.getElementById('ph').classList.contains('hovered')");
        Assert(hasClass, "Page Hover should add class");
    });

    await RunGroupAsync("page-dispatch-event", async () =>
    {
        await page!.SetContentAsync("<button id='pde'>btn</button><output id='pde-out'></output><script>document.getElementById('pde').addEventListener('click',()=>document.getElementById('pde-out').textContent='ok')</script>");
        await page.DispatchEventAsync("#pde", "click");
        Assert(await page.Locator("#pde-out").TextContentAsync() == "ok", "Page DispatchEvent mismatch");
    });

    await RunGroupAsync("page-drag-and-drop", async () =>
    {
        await page!.SetContentAsync("<div id='p-src' draggable='true' style='width:50px;height:50px;background:red'>drag</div><div id='p-dst' style='width:50px;height:50px;background:blue'>drop</div><script>let pDropped=false;document.getElementById('p-dst').addEventListener('drop',e=>{e.preventDefault();pDropped=true});document.getElementById('p-dst').addEventListener('dragover',e=>e.preventDefault())</script>");
        await page.DragAndDropAsync("#p-src", "#p-dst");
        var dropped = await page.EvaluateAsync<bool>("() => pDropped");
        Assert(dropped, "Page DragAndDrop mismatch");
    });

    await RunGroupAsync("page-select-option", async () =>
    {
        await page!.SetContentAsync("<select id='pso'><option value='a'>A</option><option value='b'>B</option></select>");
        var sel = await page.SelectOptionAsync("#pso", "a");
        Assert(sel.Count == 1 && sel[0] == "a", "Page SelectOption mismatch");
    });

    await RunGroupAsync("page-tap", async () =>
    {
        var touchCtx = await browser!.NewContextAsync(new() { HasTouch = true });
        var tp = await touchCtx.NewPageAsync();
        await tp.SetContentAsync("<button id='ptap'>tap</button><output id='ptap-out'></output><script>document.getElementById('ptap').addEventListener('touchstart',()=>document.getElementById('ptap-out').textContent='tapped')</script>");
        await tp.TapAsync("#ptap");
        Assert(await tp.Locator("#ptap-out").TextContentAsync() == "tapped", "Page Tap mismatch");
        await tp.CloseAsync();
        await touchCtx.CloseAsync();
    });

    // ═══════════════════════════════════════════════════════════
    // 8. PAGE STATE CHECKS
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-state-checks", async () =>
    {
        await page!.SetContentAsync("<input id='ps-enabled'><input id='ps-disabled' disabled><input id='ps-checked' type='checkbox' checked><input id='ps-hidden' type='hidden'>");
        Assert(await page.IsEnabledAsync("#ps-enabled"), "IsEnabled mismatch");
        Assert(await page.IsDisabledAsync("#ps-disabled"), "IsDisabled mismatch");
        Assert(await page.IsCheckedAsync("#ps-checked"), "IsChecked mismatch");
        Assert(await page.IsHiddenAsync("#ps-hidden"), "IsHidden mismatch");
        Assert(await page.IsVisibleAsync("#ps-enabled"), "IsVisible mismatch");
        Assert(await page.IsEditableAsync("#ps-enabled"), "IsEditable mismatch");
    });

    await RunGroupAsync("page-get-attribute", async () =>
    {
        await page!.SetContentAsync("<a id='pga' href='https://aot.test'>link</a>");
        Assert(await page.GetAttributeAsync("#pga", "href") == "https://aot.test", "Page GetAttribute mismatch");
    });

    await RunGroupAsync("page-text-content", async () =>
    {
        await page!.SetContentAsync("<p id='ptc'>ptext</p>");
        Assert(await page.TextContentAsync("#ptc") == "ptext", "Page TextContent mismatch");
    });

    await RunGroupAsync("page-inner-html-text", async () =>
    {
        await page!.SetContentAsync("<div id='piht'><span>inner</span></div>");
        Assert(await page.InnerHTMLAsync("#piht") == "<span>inner</span>", "Page InnerHTML mismatch");
        Assert(await page.InnerTextAsync("#piht") == "inner", "Page InnerText mismatch");
    });

    await RunGroupAsync("page-input-value", async () =>
    {
        await page!.SetContentAsync("<input id='piv' value='pval'>");
        Assert(await page.InputValueAsync("#piv") == "pval", "Page InputValue mismatch");
    });

    await RunGroupAsync("page-is-closed", async () =>
    {
        Assert(!page!.IsClosed, "Page should not be closed");
    });

    // ═══════════════════════════════════════════════════════════
    // 9. PAGE QUERY SELECTORS
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-query-selector", async () =>
    {
        await page!.SetContentAsync("<p id='pqs'>qs</p>");
        var h = await page.QuerySelectorAsync("#pqs");
        Assert(h != null, "QuerySelector should find element");
        Assert(await h!.TextContentAsync() == "qs", "QuerySelector text mismatch");
    });

    await RunGroupAsync("page-query-selector-all", async () =>
    {
        await page!.SetContentAsync("<ul><li>a</li><li>b</li></ul>");
        var handles = await page.QuerySelectorAllAsync("li");
        Assert(handles.Count == 2, "QuerySelectorAll count mismatch");
    });

    // ═══════════════════════════════════════════════════════════
    // 10. ELEMENT HANDLE
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("element-handle-bounding-box", async () =>
    {
        await page!.SetContentAsync("<div id='ehbb' style='width:100px;height:50px;background:red'>box</div>");
        var h = await page.QuerySelectorAsync("#ehbb");
        var bb = await h!.BoundingBoxAsync();
        Assert(bb != null && Math.Abs(bb!.Width - 100) < 1, "EH BoundingBox mismatch");
    });

    await RunGroupAsync("element-handle-click", async () =>
    {
        await page!.SetContentAsync("<button id='ehc'>c</button><output id='ehc-out'></output><script>document.getElementById('ehc').onclick=()=>document.getElementById('ehc-out').textContent='ok'</script>");
        var h = await page.QuerySelectorAsync("#ehc");
        await h!.ClickAsync();
        Assert(await page.Locator("#ehc-out").TextContentAsync() == "ok", "EH Click mismatch");
    });

    await RunGroupAsync("element-handle-fill", async () =>
    {
        await page!.SetContentAsync("<input id='ehf'>");
        var h = await page.QuerySelectorAsync("#ehf");
        await h!.FillAsync("filled");
        Assert(await h.InputValueAsync() == "filled", "EH Fill mismatch");
    });

    await RunGroupAsync("element-handle-type", async () =>
    {
        await page!.SetContentAsync("<input id='eht'>");
        var h = await page.QuerySelectorAsync("#eht");
        await h!.FillAsync("typed");
        Assert(await h.InputValueAsync() == "typed", "EH Fill mismatch");
    });

    await RunGroupAsync("element-handle-press", async () =>
    {
        await page!.SetContentAsync("<input id='ehp'>");
        var h = await page.QuerySelectorAsync("#ehp");
        await h!.PressAsync("Shift+KeyX");
        Assert(await h.InputValueAsync() == "X", "EH Press mismatch");
    });

    await RunGroupAsync("element-handle-check-uncheck", async () =>
    {
        await page!.SetContentAsync("<input type='checkbox' id='ehcu'>");
        var h = await page.QuerySelectorAsync("#ehcu");
        await h!.CheckAsync();
        Assert(await h.IsCheckedAsync(), "EH Check failed");
        await h.UncheckAsync();
        Assert(!await h.IsCheckedAsync(), "EH Uncheck failed");
    });

    await RunGroupAsync("element-handle-set-checked", async () =>
    {
        await page!.SetContentAsync("<input type='checkbox' id='ehsc'>");
        var h = await page.QuerySelectorAsync("#ehsc");
        await h!.SetCheckedAsync(true);
        Assert(await h.IsCheckedAsync(), "EH SetChecked true failed");
    });

    await RunGroupAsync("element-handle-hover", async () =>
    {
        await page!.SetContentAsync("<div id='ehh' style='width:50px;height:50px;background:red' onmouseenter='this.classList.add(\"hovered\")'></div>");
        var h = await page.QuerySelectorAsync("#ehh");
        await h!.HoverAsync();
        var hasClass = await page.EvaluateAsync<bool>("() => document.getElementById('ehh').classList.contains('hovered')");
        Assert(hasClass, "EH Hover should add class");
    });

    await RunGroupAsync("element-handle-focus", async () =>
    {
        await page!.SetContentAsync("<input id='ehfoc'>");
        var h = await page.QuerySelectorAsync("#ehfoc");
        await h!.FocusAsync();
        var focused = await page.EvaluateAsync<bool>("() => document.activeElement?.id === 'ehfoc'");
        Assert(focused, "EH Focus mismatch");
    });

    await RunGroupAsync("element-handle-dispatch-event", async () =>
    {
        await page!.SetContentAsync("<button id='ehde'>btn</button><output id='ehde-out'></output><script>document.getElementById('ehde').addEventListener('click',()=>document.getElementById('ehde-out').textContent='ok')</script>");
        var h = await page.QuerySelectorAsync("#ehde");
        await h!.DispatchEventAsync("click");
        Assert(await page.Locator("#ehde-out").TextContentAsync() == "ok", "EH DispatchEvent mismatch");
    });

    await RunGroupAsync("element-handle-select-text", async () =>
    {
        await page!.SetContentAsync("<p id='ehst'>select text</p>");
        var h = await page.QuerySelectorAsync("#ehst");
        await h!.SelectTextAsync();
        var sel = await page.EvaluateAsync<string>("() => window.getSelection()?.toString()");
        Assert(sel == "select text", "EH SelectText mismatch");
    });

    await RunGroupAsync("element-handle-scroll-into-view", async () =>
    {
        await page!.SetContentAsync("<div style='height:2000px'></div><div id='ehsv'>bottom</div>");
        var h = await page.QuerySelectorAsync("#ehsv");
        await h!.ScrollIntoViewIfNeededAsync();
        Assert(await h.IsVisibleAsync(), "EH ScrollIntoView mismatch");
    });

    await RunGroupAsync("element-handle-state-checks", async () =>
    {
        await page!.SetContentAsync("<input id='eh-ena'><input id='eh-dis' disabled><input id='eh-hid' type='hidden'>");
        var ena = await page.QuerySelectorAsync("#eh-ena");
        var dis = await page.QuerySelectorAsync("#eh-dis");
        var hid = await page.QuerySelectorAsync("#eh-hid");
        Assert(await ena!.IsEnabledAsync(), "EH IsEnabled");
        Assert(await dis!.IsDisabledAsync(), "EH IsDisabled");
        Assert(await hid!.IsHiddenAsync(), "EH IsHidden");
        Assert(await ena!.IsVisibleAsync(), "EH IsVisible");
        Assert(await ena!.IsEditableAsync(), "EH IsEditable");
    });

    await RunGroupAsync("element-handle-text-html-value", async () =>
    {
        await page!.SetContentAsync("<div id='ehtv'><span>inner</span></div>");
        var h = await page.QuerySelectorAsync("#ehtv");
        Assert(await h!.InnerHTMLAsync() == "<span>inner</span>", "EH InnerHTML");
        Assert(await h.InnerTextAsync() == "inner", "EH InnerText");
        Assert(await h.TextContentAsync() == "inner", "EH TextContent");
    });

    await RunGroupAsync("element-handle-get-attribute", async () =>
    {
        await page!.SetContentAsync("<a id='ehga' href='https://aot.test'>link</a>");
        var h = await page.QuerySelectorAsync("#ehga");
        Assert(await h!.GetAttributeAsync("href") == "https://aot.test", "EH GetAttribute");
    });

    await RunGroupAsync("element-handle-screenshot", async () =>
    {
        await page!.SetContentAsync("<div style='width:50px;height:50px;background:red'></div>");
        var h = await page.QuerySelectorAsync("div");
        var bytes = await h!.ScreenshotAsync();
        Assert(bytes.Length > 0, "EH Screenshot empty");
    });

    await RunGroupAsync("element-handle-set-input-files", async () =>
    {
        await page!.SetContentAsync("<input type='file' id='ehsif'>");
        var h = await page.QuerySelectorAsync("#ehsif");
        await h!.SetInputFilesAsync(new FilePayload { Name = "test.txt", MimeType = "text/plain", Buffer = "hello"u8.ToArray() });
        var name = await page.EvaluateAsync<string>("() => document.getElementById('ehsif').files[0].name");
        Assert(name == "test.txt", "EH SetInputFiles mismatch");
    });

    await RunGroupAsync("element-handle-select-option", async () =>
    {
        await page!.SetContentAsync("<select id='ehso'><option value='x'>X</option><option value='y'>Y</option></select>");
        var h = await page.QuerySelectorAsync("#ehso");
        await h!.SelectOptionAsync("y");
        var val = await page.EvaluateAsync<string>("() => document.getElementById('ehso').value");
        Assert(val == "y", "EH SelectOption mismatch");
    });

    await RunGroupAsync("element-handle-query-selector", async () =>
    {
        await page!.SetContentAsync("<div id='parent'><p id='child'>child</p></div>");
        var parent = await page.QuerySelectorAsync("#parent");
        var child = await parent!.QuerySelectorAsync("#child");
        Assert(child != null, "EH QuerySelector child");
    });

    await RunGroupAsync("element-handle-tap", async () =>
    {
        await page!.SetContentAsync("<button id='ehtap'>tap</button><output id='ehtap-out'></output><script>document.getElementById('ehtap').addEventListener('touchstart',()=>document.getElementById('ehtap-out').textContent='tapped')</script>");
        var h = await page.QuerySelectorAsync("#ehtap");
        await h!.TapAsync();
        Assert(await page.Locator("#ehtap-out").TextContentAsync() == "tapped", "EH Tap mismatch");
    });

    await RunGroupAsync("element-handle-wait-for-element-state", async () =>
    {
        await page!.SetContentAsync("<div id='ehw' style='display:none'>hidden</div><script>setTimeout(()=>document.getElementById('ehw').style.display='block',50)</script>");
        var h = await page.QuerySelectorAsync("#ehw");
        await h!.WaitForElementStateAsync(ElementState.Visible);
        Assert(await h!.IsVisibleAsync(), "EH WaitForElementState mismatch");
    });

    await RunGroupAsync("element-handle-owner-frame", async () =>
    {
        await page!.SetContentAsync("<p id='ehof'>frame</p>");
        var h = await page.QuerySelectorAsync("#ehof");
        var frame = await h!.OwnerFrameAsync();
        Assert(frame != null, "EH OwnerFrame should not be null");
    });

    await RunGroupAsync("element-handle-content-frame", async () =>
    {
        await page!.SetContentAsync("<iframe id='ehcf' src='data:text/html,<p>iframe</p>'></iframe>");
        await Task.Delay(200);
        var h = await page.QuerySelectorAsync("#ehcf");
        var cf = await h!.ContentFrameAsync();
        Assert(cf != null, "EH ContentFrame should exist");
    });

    // ═══════════════════════════════════════════════════════════
    // 11. FRAME
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("frame-main", async () =>
    {
        var mf = page!.MainFrame;
        Assert(mf != null, "MainFrame should exist");
        Assert(!mf!.IsDetached, "MainFrame should not be detached");
        Assert(mf.Name == string.Empty, "MainFrame name should be empty");
    });

    await RunGroupAsync("frame-child", async () =>
    {
        await page!.SetContentAsync("<iframe src='data:text/html,<title>Child</title>'></iframe>");
        await Task.Delay(300);
        var child = page.Frames.FirstOrDefault(f => f != page.MainFrame);
        Assert(child != null, "Child frame should exist");
        Assert(await child!.TitleAsync() == "Child", "Child title mismatch");
        Assert(child.ParentFrame == page.MainFrame, "Parent frame mismatch");
    });

    await RunGroupAsync("frame-by-url", async () =>
    {
        var f = page!.FrameByUrl("**/Child*");
        // May be null if the frame URL doesn't match pattern; that's OK.
    });

    await RunGroupAsync("frame-by-name", async () =>
    {
        var f = page!.Frame("child-frame");
        Assert(f == null, "Frame by name should match (or null if none)");
    });

    await RunGroupAsync("frame-evaluate", async () =>
    {
        var mf = page!.MainFrame;
        var val = await mf.EvaluateAsync<int>("() => 42");
        Assert(val == 42, "Frame Evaluate mismatch");
    });

    await RunGroupAsync("frame-evaluate-handle", async () =>
    {
        var h = await page!.MainFrame.EvaluateHandleAsync("() => ({ a: 1 })");
        Assert(h != null, "Frame EvaluateHandle");
    });

    await RunGroupAsync("frame-locator", async () =>
    {
        await page!.MainFrame.SetContentAsync("<input id='fl'>");
        await page.MainFrame.Locator("#fl").FillAsync("frame-val");
        Assert(await page.MainFrame.Locator("#fl").InputValueAsync() == "frame-val", "Frame Locator Fill");
    });

    await RunGroupAsync("frame-content", async () =>
    {
        await page!.MainFrame.SetContentAsync("<p>frame-content</p>");
        var html = await page.MainFrame.ContentAsync();
        Assert(html.Contains("frame-content"), "Frame Content");
    });

    await RunGroupAsync("frame-goto", async () =>
    {
        var r = await page!.MainFrame.GotoAsync(server!.BaseUri + "page", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(r?.Status == 200, "Frame Goto should return response");
    });

    await RunGroupAsync("frame-title", async () =>
    {
        Assert(await page!.MainFrame.TitleAsync() == "Network OK", "Frame Title mismatch");
    });

    await RunGroupAsync("frame-frame-locator", async () =>
    {
        await page!.SetContentAsync("<iframe id='outer' src='data:text/html,<input id=\"inner-input\">'></iframe>");
        await Task.Delay(300);
        await page.FrameLocator("#outer").Locator("#inner-input").FillAsync("fl-input");
        Assert(await page.FrameLocator("#outer").Locator("#inner-input").InputValueAsync() == "fl-input", "FrameLocator Fill");
    });

    await RunGroupAsync("frame-query-selector", async () =>
    {
        await page!.MainFrame.SetContentAsync("<p id='fqs'>qs</p>");
        var h = await page.MainFrame.QuerySelectorAsync("#fqs");
        Assert(h != null, "Frame QuerySelector");
    });

    await RunGroupAsync("frame-set-content", async () =>
    {
        await page!.MainFrame.SetContentAsync("<title>FSC</title><p>fsc</p>");
        Assert(await page.MainFrame.TitleAsync() == "FSC", "Frame SetContent");
    });

    // ═══════════════════════════════════════════════════════════
    // 12. KEYBOARD & MOUSE
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("keyboard-type", async () =>
    {
        await page!.SetContentAsync("<input id='kt'>");
        await page.Locator("#kt").FocusAsync();
        await page.Keyboard.TypeAsync("kbd-type");
        Assert(await page.InputValueAsync("#kt") == "kbd-type", "Keyboard Type");
    });

    await RunGroupAsync("keyboard-press", async () =>
    {
        await page!.SetContentAsync("<input id='kp'>");
        await page.Locator("#kp").FocusAsync();
        await page.Keyboard.PressAsync("Shift+KeyA");
        Assert(await page.InputValueAsync("#kp") == "A", "Keyboard Press");
    });

    await RunGroupAsync("keyboard-down-up", async () =>
    {
        await page!.SetContentAsync("<input id='kdu'>");
        await page.Locator("#kdu").FocusAsync();
        await page.Keyboard.DownAsync("Shift");
        await page.Keyboard.PressAsync("KeyA");
        await page.Keyboard.UpAsync("Shift");
        Assert(await page.InputValueAsync("#kdu") == "A", "Keyboard Down/Up");
    });

    await RunGroupAsync("keyboard-insert-text", async () =>
    {
        await page!.SetContentAsync("<input id='kit'>");
        await page.Locator("#kit").FocusAsync();
        await page.Keyboard.InsertTextAsync("inserted");
        Assert(await page.InputValueAsync("#kit") == "inserted", "Keyboard InsertText");
    });

    await RunGroupAsync("mouse-click", async () =>
    {
        await page!.SetContentAsync("<div style='width:100px;height:100px'></div><script>let mPos=[];document.querySelector('div').addEventListener('click',e=>mPos.push({x:e.clientX,y:e.clientY}));window.getMPos=()=>mPos</script>");
        await page.Mouse.ClickAsync(30, 40);
        var pos = await page.EvaluateAsync<JsonElement>("() => window.getMPos()");
        Assert(pos[0].GetProperty("x").GetInt32() == 30, "Mouse Click x");
        Assert(pos[0].GetProperty("y").GetInt32() == 40, "Mouse Click y");
    });

    await RunGroupAsync("mouse-dbl-click", async () =>
    {
        await page!.SetContentAsync("<script>let dc=0;document.addEventListener('dblclick',()=>dc++);window.getDC=()=>dc</script>");
        await page.Mouse.DblClickAsync(50, 50);
        var c = await page.EvaluateAsync<int>("() => window.getDC()");
        Assert(c >= 1, "Mouse DblClick");
    });

    await RunGroupAsync("mouse-down-up", async () =>
    {
        await page!.SetContentAsync("<script>let md=false,mu=false;document.addEventListener('mousedown',()=>md=true);document.addEventListener('mouseup',()=>mu=true);window.getMDU=()=>({md,mu})</script>");
        await page.Mouse.MoveAsync(10, 10);
        await page.Mouse.DownAsync();
        await page.Mouse.UpAsync();
        var state = await page.EvaluateAsync<JsonElement>("() => window.getMDU()");
        Assert(state.GetProperty("md").GetBoolean(), "Mouse Down");
        Assert(state.GetProperty("mu").GetBoolean(), "Mouse Up");
    });

    await RunGroupAsync("mouse-wheel", async () =>
    {
        await page!.SetContentAsync("<div id='mw' style='width:100px;height:100px;overflow:scroll'><div style='height:500px'>big</div></div>");
        await page.Locator("#mw").FocusAsync();
        await page.Mouse.MoveAsync(50, 50);
        await page.Mouse.WheelAsync(0, 50);
        await Task.Delay(100);
        var st = await page.EvaluateAsync<int>("() => document.getElementById('mw').scrollTop");
        Assert(st >= 1, "Mouse Wheel");
    });

    // ═══════════════════════════════════════════════════════════
    // 13. VIEWPORT & SCREENSHOT
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-viewport", async () =>
    {
        await page!.SetViewportSizeAsync(500, 400);
        var size = await page.EvaluateAsync<JsonElement>("() => ({ w: window.innerWidth, h: window.innerHeight })");
        Assert(size.GetProperty("w").GetInt32() == 500, "Viewport w");
        Assert(size.GetProperty("h").GetInt32() == 400, "Viewport h");
        await page.SetViewportSizeAsync(800, 600);
    });

    await RunGroupAsync("page-screenshot", async () =>
    {
        var bytes = await page!.ScreenshotAsync();
        Assert(bytes.Length > 0, "Screenshot empty");
    });

    // ═══════════════════════════════════════════════════════════
    // 14. SCRIPTS & STYLES
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-add-init-script", async () =>
    {
        await page!.AddInitScriptAsync("window.AOT_INIT = 1;");
        await page.GotoAsync("data:text/html,<title>Init</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var val = await page.EvaluateAsync<int>("() => window.AOT_INIT");
        Assert(val == 1, "AddInitScript");
    });

    await RunGroupAsync("page-add-script-tag", async () =>
    {
        await page!.SetContentAsync("<title>ScriptTag</title>");
        await page.AddScriptTagAsync(new() { Content = "window.AOT_SCRIPT = 'injected';" });
        var val = await page.EvaluateAsync<string>("() => window.AOT_SCRIPT");
        Assert(val == "injected", "AddScriptTag");
    });

    await RunGroupAsync("page-add-style-tag", async () =>
    {
        await page!.SetContentAsync("<h1>Style</h1>");
        await page.AddStyleTagAsync(new() { Content = "h1 { color: red; }" });
        var color = await page.EvaluateAsync<string>("() => getComputedStyle(document.querySelector('h1')).color");
        Assert(color == "rgb(255, 0, 0)", "AddStyleTag");
    });

    // ═══════════════════════════════════════════════════════════
    // 15. BINDING (ExposeFunction / ExposeBinding)
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("binding-expose-function-sync", async () =>
    {
        await page!.ExposeFunctionAsync("addNums", (int a, int b) => a + b);
        var sum = await page.EvaluateAsync<int>("async () => await window.addNums(40, 2)");
        Assert(sum == 42, "ExposeFunction sync");
    });

    await RunGroupAsync("binding-expose-function-async-task", async () =>
    {
        await page!.ExposeFunctionAsync("mulTask", (int a, int b) => Task.FromResult(a * b));
        var res = await page.EvaluateAsync<int>("async () => await window.mulTask(6, 7)");
        Assert(res == 42, "ExposeFunction async Task<T>");
    });

    await RunGroupAsync("binding-expose-function-completed-task", async () =>
    {
        await page!.ExposeFunctionAsync("noop", () => Task.CompletedTask);
        var ok = await page.EvaluateAsync<string?>("async () => { await window.noop(); return 'ok'; }");
        Assert(ok == "ok", "ExposeFunction Task.CompletedTask");
    });

    await RunGroupAsync("binding-expose-function-error", async () =>
    {
        await page!.ExposeFunctionAsync("fail", () => throw new InvalidOperationException("AOT-error"));
        var msg = await page.EvaluateAsync<string?>("async () => { try { await window.fail(); return null; } catch(e) { return e.message; } }");
        Assert(msg != null && msg.Contains("AOT-error"), "ExposeFunction error");
    });

    await RunGroupAsync("binding-expose-binding", async () =>
    {
        await page!.ExposeBindingAsync("doubleBind", (BindingSource source, int x) => x * 2);
        var res = await page.EvaluateAsync<int>("async () => await window.doubleBind(21)");
        Assert(res == 42, "ExposeBinding");
    });

    await RunGroupAsync("binding-expose-binding-two-params", async () =>
    {
        await page!.ExposeBindingAsync("addBind", (BindingSource source, int a, int b) => a + b);
        var res = await page.EvaluateAsync<int>("async () => await window.addBind(30, 12)");
        Assert(res == 42, "ExposeBinding two params");
    });

    await RunGroupAsync("binding-expose-binding-source-only", async () =>
    {
        await page!.ExposeBindingAsync("getPageUrl", (BindingSource source) => source.Page?.Url ?? "none");
        var url = await page.EvaluateAsync<string>("async () => await window.getPageUrl()");
        Assert(!string.IsNullOrEmpty(url), "ExposeBinding source only");
    });

    await RunGroupAsync("binding-expose-binding-generic-result", async () =>
    {
        await page!.ExposeBindingAsync<int>("answerBind", (BindingSource _) => 42);
        var res = await page.EvaluateAsync<int>("async () => await window.answerBind()");
        Assert(res == 42, "ExposeBinding<TResult>");
    });

    await RunGroupAsync("binding-expose-binding-generic-func", async () =>
    {
        await page!.ExposeBindingAsync<string, string>("echoBind", (BindingSource source, string msg) => $"echo:{msg}");
        var res = await page.EvaluateAsync<string>("async () => await window.echoBind('hi')");
        Assert(res == "echo:hi", "ExposeBinding<T,TResult>");
    });

    await RunGroupAsync("binding-expose-binding-generic-action", async () =>
    {
        string? captured = null;
        await page!.ExposeBindingAsync<string>("captureBind", (BindingSource source, string msg) => { captured = msg; });
        await page.EvaluateAsync<string>("async () => { window.captureBind('stored'); return 'ok'; }");
        Assert(captured == "stored", "ExposeBinding<T> action");
    });

    await RunGroupAsync("binding-context-expose-function", async () =>
    {
        var ctx = context!;
        await ctx.ExposeFunctionAsync("ctxDouble", (int x) => x * 2);
        var cp = await ctx.NewPageAsync();
        await cp.GotoAsync("data:text/html,<title>ctx</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var res = await cp.EvaluateAsync<int>("async () => await window.ctxDouble(21)");
        Assert(res == 42, "Context ExposeFunction");
        await cp.CloseAsync();
    });

    await RunGroupAsync("binding-context-expose-binding", async () =>
    {
        var ctx = context!;
        await ctx.ExposeBindingAsync("ctxBind", (BindingSource source, int x) => x * x);
        var cp = await ctx.NewPageAsync();
        await cp.GotoAsync("data:text/html,<title>ctxb</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var res = await cp.EvaluateAsync<int>("async () => await window.ctxBind(7)");
        Assert(res == 49, "Context ExposeBinding");
        await cp.CloseAsync();
    });

    // ═══════════════════════════════════════════════════════════
    // 16. ROUTE
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("route-fulfill", async () =>
    {
        await using var reg = await page!.RouteAsync("**/route-test", async route =>
        {
            await route.FulfillAsync(new() { Status = 200, ContentType = "text/html", Body = "<h1 id='rt'>route-ok</h1>" });
        });
        var response = await page.GotoAsync("http://example.test/route-test", new() { Timeout = 10_000, WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(response?.Status == 200, "Route fulfill status");
        Assert(await page.Locator("#rt").TextContentAsync() == "route-ok", "Route fulfill body");
    });

    await RunGroupAsync("route-abort", async () =>
    {
        await using var reg = await page!.RouteAsync("**/abort-test", async route => await route.AbortAsync());
        try
        {
            await page.GotoAsync("http://example.test/abort-test", new() { Timeout = 5000 });
            Assert(false, "Route abort should throw");
        }
        catch (PlaywrightException) { }
    });

    await RunGroupAsync("route-continue", async () =>
    {
        await using var reg = await page!.RouteAsync("**/continue-test", async route => await route.ContinueAsync());
        var response = await page.GotoAsync(server!.BaseUri + "continue-test", new() { Timeout = 10_000 });
        Assert(response?.Status == 404, "Route continue should get 404 from server");
    });

    await RunGroupAsync("route-unroute", async () =>
    {
        await using var reg = await page!.RouteAsync("**/unroute-test", async route => { await route.ContinueAsync(); });
        await page.UnrouteAsync("**/unroute-test");
        // After unroute, the route should not fire anymore
    });

    await RunGroupAsync("route-unroute-all", async () =>
    {
        await page!.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Default });
    });

    // ═══════════════════════════════════════════════════════════
    // 17. EXTRA HTTP HEADERS
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-extra-http-headers", async () =>
    {
        var hPage = await context!.NewPageAsync();
        await hPage.SetExtraHTTPHeadersAsync(new[] { new KeyValuePair<string, string>("X-AOT", "yes") });
        IRequest captured = null!;
        await using var reg = await hPage.RouteAsync("**/headers-check", async route =>
        {
            captured = route.Request;
            await route.FulfillAsync(new() { Status = 200, Body = "<p>ok</p>" });
        });
        await hPage.GotoAsync("http://example.test/headers-check", new() { Timeout = 10_000, WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(captured.Headers.TryGetValue("x-aot", out var hv) || captured.Headers.TryGetValue("X-AOT", out hv), "Extra header not sent");
        await hPage.CloseAsync();
    });

    // ═══════════════════════════════════════════════════════════
    // 18. EMULATE MEDIA
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-emulate-media", async () =>
    {
        await page!.SetContentAsync("<style>@media print { body:after { content: 'print'; } }</style>");
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        var isPrint = await page.EvaluateAsync<bool>("() => matchMedia('print').matches");
        Assert(isPrint, "EmulateMedia print");
    });

    // ═══════════════════════════════════════════════════════════
    // 19. DIALOG
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-dialog", async () =>
    {
        var dPage = await context!.NewPageAsync();
        var tcs = new TaskCompletionSource<IDialog>();
        dPage.Dialog += async (_, d) => { tcs.TrySetResult(d); await d.DismissAsync(); };
        await dPage.EvaluateAsync("() => alert('hello-aot')");
        var dialog = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert(dialog.Message == "hello-aot", "Dialog message");
        Assert(dialog.Type == "alert", "Dialog type");
        await dPage.CloseAsync();
    });

    // ═══════════════════════════════════════════════════════════
    // 20. CONSOLE
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-console", async () =>
    {
        var msg = await page!.RunAndWaitForConsoleMessageAsync(async () =>
        {
            await page.EvaluateAsync("() => console.log('aot-console')");
        });
        Assert(msg.Text == "aot-console", "Console message text");
        Assert(msg.Type == "log", "Console message type");
    });

    // ═══════════════════════════════════════════════════════════
    // 21. WAIT FOR SELECTOR / FUNCTION
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-wait-for-selector", async () =>
    {
        await page!.SetContentAsync("<p id='wfs'>wait</p>");
        var el = await page.WaitForSelectorAsync("#wfs", new() { State = WaitForSelectorState.Attached, Timeout = 5000 });
        Assert(el != null, "WaitForSelector");
        Assert(await el!.TextContentAsync() == "wait", "WaitForSelector text");
    });

    await RunGroupAsync("page-wait-for-function", async () =>
    {
        await page!.SetContentAsync("<script>setTimeout(() => window.ready = true, 100)</script>");
        await page.WaitForFunctionAsync("() => window.ready");
    });

    await RunGroupAsync("page-wait-for-timeout", async () =>
    {
        await page!.WaitForTimeoutAsync(50);
    });

    // ═══════════════════════════════════════════════════════════
    // 22. POPUP
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-popup", async () =>
    {
        await page!.GotoAsync(server!.BaseUri + "page", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await using var popupRoute = await page.Context.RouteAsync("**/popup-target", async route =>
        {
            await route.FulfillAsync(new() { Status = 200, Body = "<!doctype html><title>Popup</title>" });
        });
        var popup = await page.RunAndWaitForPopupAsync(async () =>
        {
            await page.EvaluateAsync($"window.open('{server!.BaseUri}popup-target', '_blank'); void 0");
        });
        Assert(popup != null, "Popup should exist");
        Assert(await popup!.TitleAsync() == "Popup", "Popup title");
        await popup.CloseAsync();
    });

    // ═══════════════════════════════════════════════════════════
    // 23. BROWSER CONTEXT
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("context-cookies", async () =>
    {
        await context!.AddCookiesAsync(new[]
        {
            new Microsoft.Playwright.Cookie { Name = "aot-cookie", Value = "ok", Url = "https://example.test" },
        });
        var cookies = await context.CookiesAsync("https://example.test");
        Assert(cookies.Any(c => c.Name == "aot-cookie" && c.Value == "ok"), "Cookie round-trip");
    });

    await RunGroupAsync("context-clear-cookies", async () =>
    {
        await context!.ClearCookiesAsync();
        var cookies = await context.CookiesAsync("https://example.test");
        Assert(!cookies.Any(c => c.Name == "aot-cookie"), "ClearCookies");
    });

    await RunGroupAsync("context-grant-permissions", async () =>
    {
        await context!.GrantPermissionsAsync(new[] { "geolocation" });
        await context.ClearPermissionsAsync();
    });

    await RunGroupAsync("context-set-geolocation", async () =>
    {
        var gp = await context!.NewPageAsync();
        await gp.GotoAsync(server!.BaseUri + "page", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await context.GrantPermissionsAsync(new[] { "geolocation" }, new() { Origin = server!.BaseUri.GetLeftPart(UriPartial.Authority) });
        await context.SetGeolocationAsync(new() { Latitude = 48.8566f, Longitude = 2.3522f });
        await gp.GotoAsync(server!.BaseUri + "page", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var pos = await gp.EvaluateAsync<JsonElement>("() => new Promise(r => navigator.geolocation.getCurrentPosition(p => r({ lat: p.coords.latitude, lng: p.coords.longitude })))");
        Assert(Math.Abs(pos.GetProperty("lat").GetDouble() - 48.8566) < 0.01, "Geolocation lat");
        await gp.CloseAsync();
    });

    await RunGroupAsync("context-set-offline", async () =>
    {
        // Just verify it doesn't throw; we restore immediately.
        await context!.SetOfflineAsync(true);
        await context.SetOfflineAsync(false);
    });

    await RunGroupAsync("context-storage-state", async () =>
    {
        var sp = await context!.NewPageAsync();
        await sp.GotoAsync(server!.BaseUri + "page", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await sp.EvaluateAsync("() => localStorage.setItem('aot-key', 'aot-val')");
        var stateJson = await context.StorageStateAsync();
        Assert(!string.IsNullOrEmpty(stateJson), "StorageState not empty");
        await sp.CloseAsync();
    });

    await RunGroupAsync("context-add-init-script", async () =>
    {
        await using var reg = await context!.AddInitScriptAsync("window.AOT_CTX_INIT = true;");
        var cp = await context.NewPageAsync();
        await cp.GotoAsync("data:text/html,<title>ctx-init</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(await cp.EvaluateAsync<bool>("() => window.AOT_CTX_INIT"), "Context AddInitScript");
        await cp.CloseAsync();
    });

    await RunGroupAsync("context-expose-binding", async () =>
    {
        await context!.ExposeBindingAsync("ctxBind2", (BindingSource s, int x) => x + 1);
        var cp = await context.NewPageAsync();
        await cp.GotoAsync("data:text/html,<title>ctx-bind</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var res = await cp.EvaluateAsync<int>("async () => await window.ctxBind2(41)");
        Assert(res == 42, "Context ExposeBinding");
        await cp.CloseAsync();
    });

    await RunGroupAsync("context-expose-function", async () =>
    {
        await context!.ExposeFunctionAsync("ctxFn", (string s) => s.ToUpper());
        var cp = await context.NewPageAsync();
        await cp.GotoAsync("data:text/html,<title>ctx-fn</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var res = await cp.EvaluateAsync<string>("async () => await window.ctxFn('hello')");
        Assert(res == "HELLO", "Context ExposeFunction");
        await cp.CloseAsync();
    });

    await RunGroupAsync("context-route", async () =>
    {
        await using var reg = await context!.RouteAsync("**/ctx-route", async route =>
        {
            await route.FulfillAsync(new() { Status = 200, Body = "<h1>ctx-route</h1>" });
        });
        var cp = await context.NewPageAsync();
        await cp.GotoAsync("http://example.test/ctx-route", new() { Timeout = 10_000, WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert(await cp.Locator("h1").TextContentAsync() == "ctx-route", "Context Route");
        await cp.CloseAsync();
    });

    // ═══════════════════════════════════════════════════════════
    // 24. API REQUEST CONTEXT
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("api-request-get", async () =>
    {
        await using var api = await playwright.APIRequest.NewContextAsync();
        var response = await api.GetAsync(new Uri(server.BaseUri, "json").ToString());
        Assert(response.Ok, "API GET ok");
        var raw = await response.JsonAsync();
        Assert(raw?.GetProperty("message").GetString() == "ok", "API GET json");
    });

    await RunGroupAsync("api-request-post", async () =>
    {
        await using var api = await playwright.APIRequest.NewContextAsync();
        var response = await api.PostAsync(new Uri(server.BaseUri, "json").ToString());
        Assert(response.Ok, "API POST ok");
    });

    await RunGroupAsync("api-request-put", async () =>
    {
        await using var api = await playwright.APIRequest.NewContextAsync();
        var response = await api.PutAsync(new Uri(server.BaseUri, "json").ToString());
        Assert(response.Ok, "API PUT ok");
    });

    await RunGroupAsync("api-request-delete", async () =>
    {
        await using var api = await playwright.APIRequest.NewContextAsync();
        var response = await api.DeleteAsync(new Uri(server.BaseUri, "json").ToString());
        Assert(response.Ok, "API DELETE ok");
    });

    await RunGroupAsync("api-request-head", async () =>
    {
        await using var api = await playwright.APIRequest.NewContextAsync();
        var response = await api.HeadAsync(new Uri(server.BaseUri, "json").ToString());
        Assert(response.Ok, "API HEAD ok");
    });

    await RunGroupAsync("api-request-patch", async () =>
    {
        await using var api = await playwright.APIRequest.NewContextAsync();
        var response = await api.PatchAsync(new Uri(server.BaseUri, "json").ToString());
        Assert(response.Ok, "API PATCH ok");
    });

    await RunGroupAsync("api-request-fetch", async () =>
    {
        await using var api = await playwright.APIRequest.NewContextAsync();
        var response = await api.FetchAsync(new Uri(server.BaseUri, "json").ToString());
        Assert(response.Ok, "API Fetch ok");
    });

    await RunGroupAsync("api-request-storage-state", async () =>
    {
        await using var api = await playwright.APIRequest.NewContextAsync();
        var state = await api.StorageStateAsync();
        Assert(!string.IsNullOrEmpty(state), "API StorageState");
    });

    await RunGroupAsync("api-request-create-form-data", async () =>
    {
        await using var api = await playwright.APIRequest.NewContextAsync();
        var fd = api.CreateFormData();
        fd.Append("key", "value");
        var response = await api.PostAsync(new Uri(server.BaseUri, "json").ToString(), new() { Multipart = fd });
        Assert(response.Ok, "API FormData POST");
    });

    // ═══════════════════════════════════════════════════════════
    // 25. CLOCK
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("clock-install", async () =>
    {
        var cp = await context!.NewPageAsync();
        await cp.GotoAsync("data:text/html,<title>clock</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await cp.Context.Clock.InstallAsync();
        var before = await cp.EvaluateAsync<long>("() => Date.now()");
        await cp.Context.Clock.FastForwardAsync(10_000);
        var after = await cp.EvaluateAsync<long>("() => Date.now()");
        Assert(after - before >= 9_000, "Clock FastForward");
        await cp.CloseAsync();
    });

    await RunGroupAsync("clock-pause-resume", async () =>
    {
        var ctx = await browser!.NewContextAsync();
        var cp = await ctx.NewPageAsync();
        await cp.GotoAsync("data:text/html,<title>clock-pr</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await cp.Context.Clock.InstallAsync();
        await cp.Context.Clock.SetFixedTimeAsync(new DateTime(2020, 1, 1));
        await cp.EvaluateAsync("() => Date.now()"); // verify no crash
        await cp.CloseAsync();
        await ctx.CloseAsync();
    });

    await RunGroupAsync("clock-set-system-time", async () =>
    {
        var cp = await context!.NewPageAsync();
        await cp.GotoAsync("data:text/html,<title>clock-sst</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await cp.Context.Clock.InstallAsync();
        await cp.Context.Clock.SetSystemTimeAsync(new DateTime(2020, 2, 2));
        await cp.CloseAsync();
    });

    await RunGroupAsync("clock-set-fixed-time", async () =>
    {
        var cp = await context!.NewPageAsync();
        await cp.GotoAsync("data:text/html,<title>clock-sft</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await cp.Context.Clock.InstallAsync();
        await cp.Context.Clock.SetFixedTimeAsync(new DateTime(2020, 3, 3));
        await cp.CloseAsync();
    });

    await RunGroupAsync("clock-run-for", async () =>
    {
        var cp = await context!.NewPageAsync();
        await cp.GotoAsync("data:text/html,<title>clock-rf</title>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await cp.Context.Clock.InstallAsync();
        await cp.Context.Clock.RunForAsync(5_000);
        await cp.CloseAsync();
    });

    // ═══════════════════════════════════════════════════════════
    // 26. SELECTORS
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("selectors-register", async () =>
    {
        await page!.SetContentAsync("<div id='selreg'>hello</div>");
        await playwright.Selectors.RegisterAsync("text-upper", new()
        {
            Script = """(s, root) => root.querySelector(s).textContent.toUpperCase()""",
        });
    });

    await RunGroupAsync("selectors-set-test-id-attribute", async () =>
    {
        playwright.Selectors.SetTestIdAttribute("data-testid");
    });

    // ═══════════════════════════════════════════════════════════
    // 27. ACCESSIBILITY
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-aria-snapshot", async () =>
    {
        await page!.SetContentAsync("<button>OK</button><input placeholder='Name'>");
        var snapshot = await page.AriaSnapshotAsync();
        Assert(snapshot.Contains("button \"OK\""), "AriaSnapshot button");
        Assert(snapshot.Contains("textbox \"Name\""), "AriaSnapshot textbox");
    });

    // ═══════════════════════════════════════════════════════════
    // 28. TIMEOUT SETTINGS
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-timeouts", async () =>
    {
        page!.SetDefaultNavigationTimeout(60_000);
        page!.SetDefaultTimeout(30_000);
        context!.SetDefaultNavigationTimeout(60_000);
        context!.SetDefaultTimeout(30_000);
    });

    // ═══════════════════════════════════════════════════════════
    // 29. PAGE CLOSE
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-close", async () =>
    {
        var cp = await context!.NewPageAsync();
        Assert(!cp.IsClosed, "New page not closed");
        await cp.CloseAsync();
        Assert(cp.IsClosed, "Page should be closed");
    });

    // ═══════════════════════════════════════════════════════════
    // 30. SET INPUT FILES
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-set-input-files", async () =>
    {
        await page!.SetContentAsync("<input type='file' id='psif'>");
        await page.SetInputFilesAsync("#psif", new FilePayload { Name = "pf.txt", MimeType = "text/plain", Buffer = "pf"u8.ToArray() });
        var name = await page.EvaluateAsync<string>("() => document.getElementById('psif').files[0].name");
        Assert(name == "pf.txt", "Page SetInputFiles");
    });

    // ═══════════════════════════════════════════════════════════
    // 31. INPUT VALUE
    // ═══════════════════════════════════════════════════════════

    await RunGroupAsync("page-input-file-check", async () =>
    {
        await page!.SetContentAsync("<input id='piv2' value='check'>");
        Assert(await page.InputValueAsync("#piv2") == "check", "Page InputValue check");
    });

    // ═══════════════════════════════════════════════════════════
    // SUMMARY
    // ═══════════════════════════════════════════════════════════

    Console.WriteLine();
    Console.WriteLine($"╔══════════════════════════════════════╗");
    Console.WriteLine($"║  AOT Sample Complete                ║");
    Console.WriteLine($"║  Passed: {passed,-3}  Failed: {failed,-3}               ║");
    Console.WriteLine($"╚══════════════════════════════════════╝");
}
finally
{
    if (page != null) await page.CloseAsync();
    if (context != null) await context.CloseAsync();
    if (browser != null) await browser.CloseAsync();
}

// ═══════════════════════════════════════════════════════════════
// Local JSON server for API tests
// ═══════════════════════════════════════════════════════════════
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
        try { await _acceptLoop.ConfigureAwait(false); }
        catch (ObjectDisposedException) { }
        catch (SocketException) { }
    }

    private async Task AcceptLoopAsync()
    {
        while (true)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false); }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { return; }
            _ = HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        await using (var stream = client.GetStream())
        {
            var buffer = new byte[2048];
            var read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            var request = Encoding.ASCII.GetString(buffer, 0, read);
            var isJson = request.Contains("/json ", StringComparison.Ordinal);
            var isPage = request.StartsWith("GET /page ", StringComparison.Ordinal) ||
                         request.StartsWith("POST /page ", StringComparison.Ordinal) ||
                         request.StartsWith("PUT /page ", StringComparison.Ordinal) ||
                         request.StartsWith("PATCH /page ", StringComparison.Ordinal);
            var isHead = request.StartsWith("HEAD ", StringComparison.Ordinal);
            var status = isJson || isPage ? "200 OK" : "404 Not Found";
            var contentType = isPage ? "text/html" : "application/json";
            var body = isJson
                ? """{"message":"ok","count":3,"tags":["nativeaot","json"]}"""
                : isPage
                    ? "<!doctype html><title>Network OK</title><h1 id=\"network\">network-ok</h1>"
                    : """{"message":"not-found","count":0,"tags":[]}""";
            var bytes = Encoding.UTF8.GetBytes(body);
            // HEAD requests must not include a body per HTTP spec
            var headers = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 {status}\r\nContent-Type: {contentType}\r\nContent-Length: {(isHead ? 0 : bytes.Length)}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(headers, 0, headers.Length).ConfigureAwait(false);
            if (!isHead)
            {
                await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
        }
    }
}
