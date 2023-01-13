/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Playwright.Tests;

///<playwright-file>chromium/session.spec.ts</playwright-file>
public class SessionTests : PageTestEx
{
    [PlaywrightTest("chromium/session.spec.ts", "should work")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldWork()
    {
        var client = await Page.Context.NewCDPSessionAsync(Page);
        await client.SendAsync("Runtime.enable");
        await client.SendAsync("Runtime.evaluate", new Dictionary<string, object> { { "expression", "window.foo = 'bar'" } });

        var foo = await Page.EvaluateAsync<string>("window['foo']");
        Assert.AreEqual("bar", foo);
    }

    [PlaywrightTest("chromium/session.spec.ts", "should send events")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldSendEvents()
    {
        var client = await Page.Context.NewCDPSessionAsync(Page);
        await client.SendAsync("Network.enable");

        var events = new List<object>();
        client.AddEventListener("Network.requestWillBeSent", (eventArgs) =>
        {
            events.Add(eventArgs);
        });
        
        await Page.GotoAsync(Server.EmptyPage);

        Assert.AreEqual(1, events.Count);
    }

    [PlaywrightTest("chromium/session.spec.ts", "should be able to detach session")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldDetachSession()
    {
        var client = await Page.Context.NewCDPSessionAsync(Page);
        await client.SendAsync("Runtime.enable");
        var evalResponse = await client.SendAsync("Runtime.evaluate", new Dictionary<string, object> { { "expression", "1 + 2" }, { "returnByValue", true } });

        Assert.AreEqual(3, (int)evalResponse.Value.Deserialize<JsonNode>()["result"]["value"]);

        await client.DetachAsync();


        var exceptions = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(
            () => client.SendAsync("Runtime.evaluate", new Dictionary<string, object> { { "expression", "'1 + 2'" }, { "returnByValue", "true" } }));
        StringAssert.Contains("Target page, context or browser has been closed", exceptions.Message);
    }

    [PlaywrightTest("chromium/session.spec.ts", "should throw nice errors")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldThrowNiceErrors()
    {
        var client = await Page.Context.NewCDPSessionAsync(Page);

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => TheSourceOfTheProblem());
        StringAssert.Contains("TheSourceOfTheProblem", exception.StackTrace);
        StringAssert.Contains("ThisCommand.DoesNotExist", exception.Message);

        async Task TheSourceOfTheProblem()
        {
            await client.SendAsync("ThisCommand.DoesNotExist");
        }
    }

    [PlaywrightTest("chromium/session.spec.ts", "should work with main frame")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldWorkWithMainFrame()
    {
        var client = await Page.Context.NewCDPSessionAsync(Page.MainFrame);
        await client.SendAsync("Runtime.enable");
        await client.SendAsync("Runtime.evaluate", new Dictionary<string, object> { { "expression", "window.foo = 'bar'" } });

        var foo = await Page.EvaluateAsync<string>("window['foo']");
        Assert.AreEqual("bar", foo);
    }

    [PlaywrightTest("chromium/session.spec.ts", "should throw if target is part of main")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldThrowIfTargetIsPartOfMain()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        StringAssert.Contains("/frames/one-frame.html", Page.Frames[0].Url);
        StringAssert.Contains("/frames/frame.html", Page.Frames[1].Url);

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Context.NewCDPSessionAsync(Page.Frames[1]));
        StringAssert.Contains("This frame does not have a separate CDP session, it is a part of the parent frame's session", exception.Message);
    }

    [PlaywrightTest("chromium/session.spec.ts", "should not break page.close()")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldNotBreakPageClose()
    {
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var session = await page.Context.NewCDPSessionAsync(page);
        await session.DetachAsync();
        await page.CloseAsync();
        await context.CloseAsync();
    }

    [PlaywrightTest("chromium/session.spec.ts", "should detach when page closes")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldDetachWhenPageCloses()
    {
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var session = await page.Context.NewCDPSessionAsync(page);
        await page.CloseAsync();

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => session.DetachAsync());
        StringAssert.Contains("Target page, context or browser has been closed", exception.Message);
        await context.CloseAsync();
    }

    [PlaywrightTest("chromium/session.spec.ts", "should work with newBrowserCDPSession")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldWorkWithNewBrowserCDPSession()
    {
        var session = await Browser.NewBrowserCDPSessionAsync();

        var version = await session.SendAsync("Browser.getVersion");
        Assert.NotNull(version.Value.Deserialize<JsonNode>()["userAgent"]);

        var gotEvent = false;
        session.AddEventListener("Target.targetCreated", (_) => gotEvent = true);
        await session.SendAsync("Target.setDiscoverTargets", new() { { "discover", true } });
        var page = await Browser.NewPageAsync();

        Assert.IsTrue(gotEvent);

        await page.CloseAsync();
        await session.DetachAsync();
    }

    [PlaywrightTest]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldAddMultipleEventListeners()
    {
        var client = await Page.Context.NewCDPSessionAsync(Page);
        await client.SendAsync("Network.enable");

        var events = new List<object>();
        client.AddEventListener("Network.requestWillBeSent", eventHandler);
        client.AddEventListener("Network.requestWillBeSent", eventHandler);

        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(2, events.Count);

        void eventHandler(JsonElement? eventArgs) => events.Add(eventArgs); 
    }

    [PlaywrightTest]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldRemoveEventListeners()
    {
        var client = await Page.Context.NewCDPSessionAsync(Page);
        await client.SendAsync("Network.enable");

        var events = new List<object>();
        client.AddEventListener("Network.requestWillBeSent", eventHandler);
        client.AddEventListener("Network.requestWillBeSent", eventHandler);

        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(2, events.Count);

        client.RemoveEventListener("Network.requestWillBeSent", eventHandler);
        events.Clear();

        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(1, events.Count);

        void eventHandler(JsonElement? eventArgs) => events.Add(eventArgs);
    }
}
