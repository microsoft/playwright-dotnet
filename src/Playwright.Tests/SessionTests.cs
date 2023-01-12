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

namespace Microsoft.Playwright.Tests.Chromium;

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
        client.On("Network.requestWillBeSent", (eventArgs) =>
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
        bool threwException = false;
        var client = await Page.Context.NewCDPSessionAsync(Page);
        await client.SendAsync("Runtime.enable");
        var evalResponse = await client.SendAsync("Runtime.evaluate", new Dictionary<string, object> { { "expression", "1 + 2" }, { "returnByValue", true } });

        // TODO: Why is the result nested in a result?
        Assert.AreEqual(3, (int)evalResponse.Value.Deserialize<JsonNode>()["result"]["result"]["value"]);

        await client.DetachAsync();

        try
        {
            await client.SendAsync("Runtime.evaluate", new Dictionary<string, object> { { "expression", "'1 + 2'" }, { "returnByValue", "true" } });
        }
        catch (PlaywrightException ex)
        {
            threwException = true;
            Assert.AreEqual("Target page, context or browser has been closed", ex.Message);
        }
        Assert.IsTrue(threwException);
    }

    [PlaywrightTest("chromium/session.spec.ts", "should throw nice errors")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldThrowNiceErrors()
    {
        var client = await Page.Context.NewCDPSessionAsync(Page);

        try
        {
            await TheSourceOfTheProblem();
        }
        catch (PlaywrightException ex)
        {
            Assert.IsTrue(ex.StackTrace.Contains("TheSourceOfTheProblem", StringComparison.InvariantCultureIgnoreCase), "StackTrace does not contain 'TheSourceOfTheProblem'");
            Assert.IsTrue(ex.Message.Contains("ThisCommand.DoesNotExist", StringComparison.InvariantCultureIgnoreCase), "Exception message does not contain 'ThisCommand.DoesNotExist'");
        }

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
        var thewException = false;
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        Assert.IsTrue(Page.Frames[0].Url.Contains("/frames/one-frame.html"));
        Assert.IsTrue(Page.Frames[1].Url.Contains("/frames/frame.html"));

        try
        {
            await Page.Context.NewCDPSessionAsync(Page.Frames[1]);
        }
        catch (PlaywrightException ex)
        {
            thewException = true;
            Assert.IsTrue(ex.Message.Contains("This frame does not have a separate CDP session, it is a part of the parent frame's session"));
        }
        Assert.IsTrue(thewException);
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

    [PlaywrightTest("chromium/session.spec.ts", "should work with newBrowserCDPSession")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldWorkWithNewBrowserCDPSession()
    {
        var session = await Browser.NewBrowserCDPSessionAsync();

        var version = await session.SendAsync("Browser.getVersion");
        Assert.NotNull(version.Value.Deserialize<JsonNode>()["result"]["userAgent"]);

        var gotEvent = false;
        session.On("Target.targetCreated", (_) => gotEvent = true);
        await session.SendAsync("Target.setDiscoverTargets", new() { { "discover", true } });
        var page = await Browser.NewPageAsync();

        Assert.IsTrue(gotEvent);

        await page.CloseAsync();
        await session.DetachAsync();
    }
}
;
