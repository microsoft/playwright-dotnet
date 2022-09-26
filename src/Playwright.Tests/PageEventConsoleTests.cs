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

namespace Microsoft.Playwright.Tests;


public class PageEventConsoleTests2 : PageTestEx
{
    [PlaywrightTest("page-event-console.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        IConsoleMessage message = null;
        void EventHandler(object sender, IConsoleMessage e)
        {
            message = e;
            Page.Console -= EventHandler;
        }
        Page.Console += EventHandler;
        await TaskUtils.WhenAll(
            Page.WaitForConsoleMessageAsync(),
            Page.EvaluateAsync("() => console.log('hello', 5, { foo: 'bar'})"));

        if (TestConstants.IsFirefox)
        {
            Assert.AreEqual("hello 5 JSHandle@object", message.Text);
        }
        else
        {
            Assert.AreEqual("hello 5 {foo: bar}", message.Text);
        }
        Assert.AreEqual("log", message.Type);
        Assert.AreEqual("hello", await message.Args.ElementAt(0).JsonValueAsync<string>());
        Assert.AreEqual(5, await message.Args.ElementAt(1).JsonValueAsync<int>());
        Assert.AreEqual("bar", (await message.Args.ElementAt(2).JsonValueAsync<JsonElement>()).GetProperty("foo").GetString());
    }

    [PlaywrightTest("page-event-console.spec.ts", "should emit same log twice")]
    public async Task ShouldEmitSameLogTwice()
    {
        var messages = new List<string>();

        Page.Console += (_, e) => messages.Add(e.Text);
        await Page.EvaluateAsync("() => { for (let i = 0; i < 2; ++i ) console.log('hello'); } ");

        CollectionAssert.AreEqual(new[] { "hello", "hello" }, messages.ToArray());
    }

    [PlaywrightTest("page-event-console.spec.ts", "should work for different console API calls")]
    public async Task ShouldWorkForDifferentConsoleAPICalls()
    {
        var messages = new List<IConsoleMessage>();
        Page.Console += (_, e) => messages.Add(e);
        // All console events will be reported before `Page.evaluate` is finished.
        await Page.EvaluateAsync(@"() => {
                // A pair of time/timeEnd generates only one Console API call.
                console.time('calling console.time');
                console.timeEnd('calling console.time');
                console.trace('calling console.trace');
                console.dir('calling console.dir');
                console.warn('calling console.warn');
                console.error('calling console.error');
                console.log(Promise.resolve('should not wait until resolved!'));
            }");
        CollectionAssert.AreEqual(new[] { "timeEnd", "trace", "dir", "warning", "error", "log" }, messages.Select(msg => msg.Type).ToArray());
        StringAssert.Contains("calling console.time", messages[0].Text);
        CollectionAssert.AreEqual(new[]
        {
                "calling console.trace",
                "calling console.dir",
                "calling console.warn",
                "calling console.error",
                "Promise"
            }, messages.Skip(1).Select(msg => msg.Text).ToArray());
    }

    [PlaywrightTest("page-event-console.spec.ts", "should not fail for window object")]
    public async Task ShouldNotFailForWindowObject()
    {
        var message = await Page.RunAndWaitForConsoleMessageAsync(() => Page.EvaluateAsync("() => console.error(window)"));
        if (TestConstants.IsFirefox)
        {
            Assert.AreEqual("JSHandle@object", message.Text);
        }
        else
        {
            Assert.AreEqual("Window", message.Text);
        }
    }

    [PlaywrightTest("page-event-console.spec.ts", "should trigger correct Log")]
    public async Task ShouldTriggerCorrectLog()
    {
        await Page.GotoAsync("about:blank");
        var (messageEvent, _) = await TaskUtils.WhenAll(
            Page.WaitForConsoleMessageAsync(),
            Page.EvaluateAsync("async url => fetch(url).catch (e => { })", Server.EmptyPage)
        );
        StringAssert.Contains("Access-Control-Allow-Origin", messageEvent.Text);
        Assert.AreEqual("error", messageEvent.Type);
    }

    [PlaywrightTest("page-event-console.spec.ts", "should have location for console API calls")]
    public async Task ShouldHaveLocationForConsoleAPICalls()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var messageEvent = await Page.RunAndWaitForConsoleMessageAsync(async () =>
        {
            await Page.GotoAsync(Server.Prefix + "/consolelog.html");
        });
        Assert.AreEqual("yellow", messageEvent.Text);
        Assert.AreEqual("log", messageEvent.Type);
        string location = messageEvent.Location;
    }

    [PlaywrightTest("page-event-console.spec.ts", "should not throw when there are console messages in detached iframes")]
    public async Task ShouldNotThrowWhenThereAreConsoleMessagesInDetachedIframes()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync<bool>(@"async () =>
                {
                    // 1. Create a popup that Playwright is not connected to.
                    const win = window.open('');
                    window._popup = win;
                    if (window.document.readyState !== 'complete')
                      await new Promise(f => window.addEventListener('load', f));
                    // 2. In this popup, create an iframe that console.logs a message.
                    win.document.body.innerHTML = `<iframe src='/consolelog.html'></iframe>`;
                    const frame = win.document.querySelector('iframe');
                    if (!frame.contentDocument || frame.contentDocument.readyState !== 'complete')
                      await new Promise(f => frame.addEventListener('load', f));
                    // 3. After that, remove the iframe.
                    frame.remove();
                }"));
        // 4. Connect to the popup and make sure it doesn't throw.
        Assert.AreEqual(2, await popup.EvaluateAsync<int>("1 + 1"));
    }
}
