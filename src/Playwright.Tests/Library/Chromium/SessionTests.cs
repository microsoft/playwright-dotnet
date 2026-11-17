/**
 * Copyright 2018 Google Inc. All rights reserved.
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

using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library.Chromium;

/// <summary>Port of tests/library/chromium/session.spec.ts.</summary>
public class SessionTests
{
    // .NET: upstream runs the files of tests/library/chromium only in the Chromium project.
    [BeforeEach]
    public static void Skip(string browserName) => Info.Skip(browserName != "chromium", "chromium only");

    [UpstreamTest("CD-037 should work")]
    public async Task ShouldWork(IPage page)
    {
        var client = await page.Context.NewCDPSessionAsync(page);

        await PromiseAll(
            client.SendAsync("Runtime.enable"),
            client.SendAsync("Runtime.evaluate", new() { ["expression"] = "window.foo = \"bar\"" }));
        var foo = await page.EvaluateAsync<string>("() => window['foo']");
        Assert.That(foo, Is.EqualTo("bar"));
    }

    [UpstreamTest("CD-038 should respect timeout and signal options")]
    public async Task ShouldRespectTimeoutAndSignalOptions(IPage page, IPlaywright playwright)
    {
        var client = await page.Context.NewCDPSessionAsync(page);
        var evalResponse = await client.SendAsync("Runtime.evaluate", new() { ["expression"] = "1 + 2", ["returnByValue"] = true }, new() { Timeout = 5000 });
        Assert.That(evalResponse.Value.GetProperty("result").GetProperty("value").GetInt32(), Is.EqualTo(3));
        var error = await Catch(() => client.SendAsync("Runtime.evaluate", new() { ["expression"] = "new Promise(() => {})", ["awaitPromise"] = true }, new() { Timeout = 500 }));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
        Assert.That(error.Message, Does.Contain("Timeout 500ms exceeded"));

        // .NET: the signal option of cdpSession.send() is not in the .NET API, so the rest of the test is left out:
        // const controller = new AbortController();
        // const promise = client.send('Runtime.evaluate', { expression: 'new Promise(() => {})', awaitPromise: true }, { signal: controller.signal }).catch(e => e);
        // const reason = new Error('Aborted by user');
        // controller.abort(reason);
        // const abortError = await promise;
        // expect(abortError.name).toBe('AbortError');
        // expect(abortError.cause).toBe(reason);
    }

    [UpstreamTest("CD-039 should send events")]
    public async Task ShouldSendEvents(IPage page, Server server)
    {
        var client = await page.Context.NewCDPSessionAsync(page);
        await client.SendAsync("Network.enable");
        var events = new List<JsonElement?>();
        client.Event("Network.requestWillBeSent").OnEvent += (_, @event) => events.Add(@event);
        await page.GotoAsync(server.EmptyPage);
        Assert.That(events.Count, Is.EqualTo(1));
    }

    [UpstreamTest("CD-042 should be able to detach session")]
    public async Task ShouldBeAbleToDetachSession(IPage page)
    {
        var client = await page.Context.NewCDPSessionAsync(page);
        await client.SendAsync("Runtime.enable");
        var evalResponse = await client.SendAsync("Runtime.evaluate", new() { ["expression"] = "1 + 2", ["returnByValue"] = true });
        Assert.That(evalResponse.Value.GetProperty("result").GetProperty("value").GetInt32(), Is.EqualTo(3));
        await client.DetachAsync();
        Exception error = null;
        try
        {
            await client.SendAsync("Runtime.evaluate", new() { ["expression"] = "3 + 1", ["returnByValue"] = true });
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error.Message, Does.Contain("Target page, context or browser has been closed"));
    }

    [UpstreamTest("CD-043 should throw nice errors")]
    public async Task ShouldThrowNiceErrors(IPage page)
    {
        var client = await page.Context.NewCDPSessionAsync(page);
        var error = await Catch(() => theSourceOfTheProblems());
        Assert.That(error.ToString(), Does.Contain("theSourceOfTheProblems"));
        Assert.That(error.Message, Does.Contain("ThisCommand.DoesNotExist"));

        async Task theSourceOfTheProblems()
        {
            await client.SendAsync("ThisCommand.DoesNotExist");
        }
    }

    [UpstreamTest("CD-044 should work with main frame")]
    public async Task ShouldWorkWithMainFrame(IPage page)
    {
        var client = await page.Context.NewCDPSessionAsync(page.MainFrame);

        await PromiseAll(
            client.SendAsync("Runtime.enable"),
            client.SendAsync("Runtime.evaluate", new() { ["expression"] = "window.foo = \"bar\"" }));
        var foo = await page.EvaluateAsync<string>("() => window['foo']");
        Assert.That(foo, Is.EqualTo("bar"));
    }

    [UpstreamTest("CD-045 should throw if target is part of main")]
    public async Task ShouldThrowIfTargetIsPartOfMain(Server server, IPage page)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        Assert.That(page.Frames[0].Url, Does.Contain("/frames/one-frame.html"));
        Assert.That(page.Frames[1].Url, Does.Contain("/frames/frame.html"));

        var error = await Catch(() => page.Context.NewCDPSessionAsync(page.Frames[1]));
        Assert.That(error.Message, Does.Contain("This frame does not have a separate CDP session, it is a part of the parent frame's session"));
    }

    [UpstreamTest("CD-046 should not break page.close()")]
    public async Task ShouldNotBreakPageClose(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        var session = await page.Context.NewCDPSessionAsync(page);
        await session.DetachAsync();
        await page.CloseAsync();
        await context.CloseAsync();
    }

    [UpstreamTest("CD-047 should detach when page closes")]
    public async Task ShouldDetachWhenPageCloses(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        var session = await context.NewCDPSessionAsync(page);
        await page.CloseAsync();
        Exception error = null;
        error = await Catch(() => session.DetachAsync());
        Assert.That(error, Is.Not.Null);
        await context.CloseAsync();
    }

    [UpstreamTest("CD-048 should reject protocol calls when page closes")]
    public async Task ShouldRejectProtocolCallsWhenPageCloses(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        var session = await context.NewCDPSessionAsync(page);
        var promise = Catch(() => session.SendAsync("Runtime.evaluate", new() { ["expression"] = "new Promise(() => {})", ["awaitPromise"] = true }));
        await page.CloseAsync();
        var error1 = await promise;
        Assert.That(error1.Message, Does.Contain(TargetClosedErrorMessage));
        var error2 = await Catch(() => session.SendAsync("Runtime.evaluate", new() { ["expression"] = "new Promise(() => {})", ["awaitPromise"] = true }));
        Assert.That(error2.Message, Does.Contain("Target page, context or browser has been closed"));
        await context.CloseAsync();
    }

    [UpstreamTest("CD-050 should emit close event when session is detached")]
    public async Task ShouldEmitCloseEventWhenSessionIsDetached(IPage page)
    {
        var client = await page.Context.NewCDPSessionAsync(page);
        ICDPSession closedSession = null;
        client.Close += (_, session) => closedSession = session;
        await client.DetachAsync();
        Assert.That(closedSession, Is.SameAs(client));
    }

    [UpstreamTest("CD-051 should emit close event when page closes")]
    public async Task ShouldEmitCloseEventWhenPageCloses(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(contextOptions);
        var page = await context.NewPageAsync();
        var session = await context.NewCDPSessionAsync(page);
        var closePromise = Once<ICDPSession>(h => session.Close += h, h => session.Close -= h);
        await page.CloseAsync();
        var closedSession = await closePromise;
        Assert.That(closedSession, Is.SameAs(session));
        await context.CloseAsync();
    }

    [UpstreamTest("CD-052 should work with newBrowserCDPSession")]
    public async Task ShouldWorkWithNewBrowserCDPSession(IBrowser browser, BrowserNewPageOptions newPageOptions)
    {
        var session = await browser.NewBrowserCDPSessionAsync();

        var version = await session.SendAsync("Browser.getVersion");
        Assert.That(version.Value.GetProperty("userAgent").GetString(), Is.Not.Empty);

        var gotEvent = false;
        session.Event("Target.targetCreated").OnEvent += (_, _) => gotEvent = true;
        await session.SendAsync("Target.setDiscoverTargets", new() { ["discover"] = true });
        var page = await browser.NewPageAsync(newPageOptions);
        Assert.That(gotEvent, Is.EqualTo(true));
        await page.CloseAsync();

        await session.DetachAsync();
    }
}
