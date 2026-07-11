using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests;

public class ContextSupplementTests : ContextTestEx
{
    [PlaywrightTest("context-supplement.spec.ts", "exposeBinding with 0-param Action callback should work")]
    public async Task ExposeBindingWithActionNoParamShouldWork()
    {
        var called = false;
        await Context.ExposeBindingAsync("ping", (BindingSource _) =>
        {
            called = true;
        });
        var page = await Context.NewPageAsync();
        await page.EvaluateAsync("ping()");
        Assert.True(called);
    }

    [PlaywrightTest("context-supplement.spec.ts", "exposeBinding with 1-param Action callback should work")]
    public async Task ExposeBindingWithAction1ParamShouldWork()
    {
        string? received = null;
        await Context.ExposeBindingAsync<string>("echo", (BindingSource _, string value) =>
        {
            received = value;
        });
        var page = await Context.NewPageAsync();
        await page.EvaluateAsync("echo('context')");
        Assert.AreEqual("context", received);
    }

    [PlaywrightTest("context-supplement.spec.ts", "exposeBinding with Func returning TResult should work")]
    public async Task ExposeBindingWithFuncReturnShouldWork()
    {
        await Context.ExposeBindingAsync("add", (BindingSource _) => 42);
        var page = await Context.NewPageAsync();
        var result = await page.EvaluateAsync<int>("add()");
        Assert.AreEqual(42, result);
    }

    [PlaywrightTest("context-supplement.spec.ts", "exposeFunction with 1-param Func should work")]
    public async Task ExposeFunctionWith1ParamFuncShouldWork()
    {
        await Context.ExposeFunctionAsync("double", (int x) => x * 2);
        var page = await Context.NewPageAsync();
        var result = await page.EvaluateAsync<int>("async () => double(21)");
        Assert.AreEqual(42, result);
    }

    [PlaywrightTest("context-supplement.spec.ts", "exposeFunction with 3-param Func should work")]
    public async Task ExposeFunctionWith3ParamFuncShouldWork()
    {
        await Context.ExposeFunctionAsync("sum3", (int a, int b, int c) => a + b + c);
        var page = await Context.NewPageAsync();
        var result = await page.EvaluateAsync<int>("async () => sum3(10, 20, 30)");
        Assert.AreEqual(60, result);
    }

    [PlaywrightTest("context-supplement.spec.ts", "exposeFunction with 4-param Func should work")]
    public async Task ExposeFunctionWith4ParamFuncShouldWork()
    {
        await Context.ExposeFunctionAsync("sum4", (int a, int b, int c, int d) => a + b + c + d);
        var page = await Context.NewPageAsync();
        var result = await page.EvaluateAsync<int>("async () => sum4(1, 2, 3, 4)");
        Assert.AreEqual(10, result);
    }

    [PlaywrightTest("context-supplement.spec.ts", "unroute with regex and Func handler should work")]
    public async Task UnrouteWithRegexAndFuncHandlerShouldWork()
    {
        async Task handler(IRoute route)
        {
            await route.FulfillAsync(new() { Body = "intercepted", ContentType = "text/plain" });
        }

        await Context.RouteAsync(new Regex(".*empty\\.html"), handler);
        var page = await Context.NewPageAsync();
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("intercepted", await response.TextAsync());
        await Context.UnrouteAsync(new Regex(".*empty\\.html"), handler);
        response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)System.Net.HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("context-supplement.spec.ts", "unroute with predicate and Func handler should work")]
    public async Task UnrouteWithPredicateAndFuncHandlerShouldWork()
    {
        async Task handler(IRoute route)
        {
            await route.FulfillAsync(new() { Body = "bypassed", ContentType = "text/plain" });
        }

        await Context.RouteAsync((string url) => url.Contains("empty.html"), handler);
        var page = await Context.NewPageAsync();
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("bypassed", await response.TextAsync());
        await Context.UnrouteAsync((string url) => url.Contains("empty.html"), handler);
        response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)System.Net.HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("context-supplement.spec.ts", "backgroundPages should return empty list")]
    public void BackgroundPagesShouldReturnEmptyList()
    {
        Assert.IsEmpty(Context.BackgroundPages);
    }
}
