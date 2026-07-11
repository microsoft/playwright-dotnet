using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests;

public class PageSupplementTests : PageTestEx
{
    [PlaywrightTest("page-supplement.spec.ts", "playwright indexer should return browser type")]
    public void PlaywrightIndexerShouldReturnBrowserType()
    {
        var browserType = Playwright[TestConstants.BrowserName];
        Assert.NotNull(browserType);
        Assert.AreEqual(TestConstants.BrowserName, browserType.Name);
    }

    [PlaywrightTest("page-supplement.spec.ts", "exposeBinding with 0-param Action callback should work")]
    public async Task ExposeBindingWithActionNoParamShouldWork()
    {
        var called = false;
        await Page.ExposeBindingAsync("ping", (BindingSource _) =>
        {
            called = true;
        });
        await Page.EvaluateAsync("ping()");
        Assert.True(called);
    }

    [PlaywrightTest("page-supplement.spec.ts", "exposeBinding with 1-param Action callback should work")]
    public async Task ExposeBindingWithAction1ParamShouldWork()
    {
        string? received = null;
        await Page.ExposeBindingAsync<string>("echo", (BindingSource _, string value) =>
        {
            received = value;
        });
        await Page.EvaluateAsync("echo('hello')");
        Assert.AreEqual("hello", received);
    }

    [PlaywrightTest("page-supplement.spec.ts", "exposeBinding with Func returning TResult should work")]
    public async Task ExposeBindingWithFuncReturnShouldWork()
    {
        await Page.ExposeBindingAsync("add", (BindingSource _) => 42);
        var result = await Page.EvaluateAsync<int>("add()");
        Assert.AreEqual(42, result);
    }

    [PlaywrightTest("page-supplement.spec.ts", "exposeFunction with 1-param Func should work")]
    public async Task ExposeFunctionWith1ParamFuncShouldWork()
    {
        await Page.ExposeFunctionAsync("double", (int x) => x * 2);
        var result = await Page.EvaluateAsync<int>("async () => double(21)");
        Assert.AreEqual(42, result);
    }

    [PlaywrightTest("page-supplement.spec.ts", "exposeFunction with 3-param Func should work")]
    public async Task ExposeFunctionWith3ParamFuncShouldWork()
    {
        await Page.ExposeFunctionAsync("sum3", (int a, int b, int c) => a + b + c);
        var result = await Page.EvaluateAsync<int>("async () => sum3(10, 20, 30)");
        Assert.AreEqual(60, result);
    }

    [PlaywrightTest("page-supplement.spec.ts", "exposeFunction with 4-param Func should work")]
    public async Task ExposeFunctionWith4ParamFuncShouldWork()
    {
        await Page.ExposeFunctionAsync("sum4", (int a, int b, int c, int d) => a + b + c + d);
        var result = await Page.EvaluateAsync<int>("async () => sum4(1, 2, 3, 4)");
        Assert.AreEqual(10, result);
    }

    [PlaywrightTest("page-supplement.spec.ts", "unroute with regex and Func handler should work")]
    public async Task UnrouteWithRegexAndFuncHandlerShouldWork()
    {
        async Task handler(IRoute route)
        {
            await route.FulfillAsync(new() { Body = "intercepted", ContentType = "text/plain" });
        }

        await Page.RouteAsync(new Regex(".*empty\\.html"), handler);
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("intercepted", await response.TextAsync());
        await Page.UnrouteAsync(new Regex(".*empty\\.html"), handler);
        response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("page-supplement.spec.ts", "unroute with predicate and Func handler should work")]
    public async Task UnrouteWithPredicateAndFuncHandlerShouldWork()
    {
        async Task handler(IRoute route)
        {
            await route.FulfillAsync(new() { Body = "bypassed", ContentType = "text/plain" });
        }

        await Page.RouteAsync((string url) => url.Contains("empty.html"), handler);
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("bypassed", await response.TextAsync());
        await Page.UnrouteAsync((string url) => url.Contains("empty.html"), handler);
        response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("page-supplement.spec.ts", "frameByUrl with Func predicate should find frame")]
    public async Task FrameByUrlWithFuncPredicateShouldFindFrame()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/nested-frames.html");
        var frame = Page.FrameByUrl(url => url.Contains("frame.html"));
        Assert.NotNull(frame);
        StringAssert.Contains("frame.html", frame.Url);
    }

    [PlaywrightTest("page-supplement.spec.ts", "frameByUrl with string should find frame")]
    public async Task FrameByUrlWithStringShouldFindFrame()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/nested-frames.html");
        var frame = Page.FrameByUrl(url => url.Contains("frame.html"));
        Assert.NotNull(frame);
        var frameByUrl = Page.FrameByUrl(frame.Url);
        Assert.NotNull(frameByUrl);
        Assert.AreEqual(frame.Url, frameByUrl.Url);
    }

    [PlaywrightTest("page-supplement.spec.ts", "frameByUrl with Regex should find frame")]
    public async Task FrameByUrlWithRegexShouldFindFrame()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/nested-frames.html");
        var frame = Page.FrameByUrl(new System.Text.RegularExpressions.Regex("frame\\.html"));
        Assert.NotNull(frame);
        StringAssert.Contains("frame.html", frame.Url);
    }

    [PlaywrightTest("page-supplement.spec.ts", "response.jsonAsync with JsonTypeInfo should work")]
    public async Task ResponseJsonAsyncWithJsonTypeInfoShouldWork()
    {
        Server.SetRoute("/test.json", async ctx =>
        {
            ctx.Response.Headers["Content-Type"] = "application/json";
            await ctx.Response.WriteAsync("{\"message\":\"hello from SourceGen\"}");
            await ctx.Response.CompleteAsync();
        });

        var response = await Page.GotoAsync(Server.Prefix + "/test.json");
        Assert.NotNull(response);
        var result = await response.JsonAsync(SourceGenJsonContext.Default.ResponsePayload);
        Assert.AreEqual("hello from SourceGen", result.Message);
    }

    [PlaywrightTest("page-supplement.spec.ts", "clearPageErrors should work")]
    public async Task ClearPageErrorsShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Page.Console += (_, _) => { };
        await Page.EvaluateAsync("() => { setTimeout(() => { throw new Error('test error'); }, 0); }");
        await Task.Delay(500);
        var errors = await Page.PageErrorsAsync();
        Assert.IsNotEmpty(errors);
        await Page.ClearPageErrorsAsync();
        errors = await Page.PageErrorsAsync();
        Assert.IsEmpty(errors);
    }

    [PlaywrightTest("page-supplement.spec.ts", "pickLocator and cancelPickLocator should work")]
    public async Task PickLocatorAndCancelShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var pickTask = Page.PickLocatorAsync();
        await Page.CancelPickLocatorAsync();
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => pickTask);
        Assert.NotNull(exception);
    }

    [PlaywrightTest("page-supplement.spec.ts", "runAndWaitForPopup with Func action should work")]
    public async Task RunAndWaitForPopupWithFuncActionShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/popup/popup.html\">link</a>");
        var popup = await Page.RunAndWaitForPopupAsync(async () =>
        {
            await Page.ClickAsync("a");
        });
        Assert.NotNull(popup);
        Assert.AreEqual(2, Page.Context.Pages.Count);
        await popup.CloseAsync();
    }

    [PlaywrightTest("page-supplement.spec.ts", "runAndWaitForWebSocket with Func action should work")]
    public async Task RunAndWaitForWebSocketWithFuncActionShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var webSocket = await Page.RunAndWaitForWebSocketAsync(async () =>
        {
            await Page.EvaluateAsync("() => { window.ws = new WebSocket('ws://localhost:' + window.location.port + '/ws'); }");
        });
        Assert.NotNull(webSocket);
        await Page.EvaluateAsync("() => window.ws.close()");
    }

    [PlaywrightTest("page-supplement.spec.ts", "runAndWaitForWorker with Func action should work")]
    public async Task RunAndWaitForWorkerWithFuncActionShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var worker = await Page.RunAndWaitForWorkerAsync(async () =>
        {
            await Page.EvaluateHandleAsync("() => new Worker(URL.createObjectURL(new Blob(['1'], {type: 'application/javascript'})))");
        });
        Assert.NotNull(worker);
    }

    public record ResponsePayload(string Message);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PageSupplementTests.ResponsePayload))]
internal partial class SourceGenJsonContext : JsonSerializerContext
{
}
