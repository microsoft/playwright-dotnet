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
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-event-console.spec.ts.</summary>
public class PageEventConsoleTests : UpstreamPageTest
{
    [UpstreamTest("CM-001 should work @smoke")]
    public async Task ShouldWork()
    {
        IConsoleMessage message = null;
        EventHandler<IConsoleMessage> once = null;
        once = (_, m) =>
        {
            Page.Console -= once;
            message = m;
        };
        Page.Console += once;
        await PromiseAll(
            Page.EvaluateAsync("() => console.log('hello', 5, { foo: 'bar' })"),
            Page.WaitForConsoleMessageAsync());
        if (BrowserName != "firefox" || IsBidi)
        {
            Assert.That(message.Text, Is.EqualTo("hello 5 {foo: bar}"));
        }
        else
        {
            Assert.That(message.Text, Is.EqualTo("hello 5 JSHandle@object"));
        }
        Assert.That(message.Type, Is.EqualTo("log"));
        Assert.That(await message.Args[0].JsonValueAsync<string>(), Is.EqualTo("hello"));
        Assert.That(await message.Args[1].JsonValueAsync<int>(), Is.EqualTo(5));
        ExpectToEqual(await message.Args[2].JsonValueAsync<object>(), new { foo = "bar" });
    }

    [UpstreamTest("CM-002 should emit same log twice")]
    public async Task ShouldEmitSameLogTwice()
    {
        var messages = new List<string>();
        Page.Console += (_, m) => messages.Add(m.Text);
        await Page.EvaluateAsync(@"() => {
    for (let i = 0; i < 2; ++i)
      console.log('hello');
  }");
        Assert.That(messages, Is.EqualTo(new[] { "hello", "hello" }));
    }

    [UpstreamTest("CM-004 should work for different console API calls")]
    public async Task ShouldWorkForDifferentConsoleAPICalls()
    {
        var messages = new List<IConsoleMessage>();
        Page.Console += (_, msg) => messages.Add(msg);
        // All console events will be reported before `page.evaluate` is finished.
        await Page.EvaluateAsync(@"() => {
    // A pair of time/timeEnd generates only one Console API call.
    console.time('calling console.time');
    console.timeEnd('calling console.time');
    console.trace('calling console.trace');
    console.dir('calling console.dir');
    console.warn('calling console.warn');
    console.error('calling console.error');
    console.info('calling console.info');
    console.debug('calling console.debug');
    console.log(Promise.resolve('should not wait until resolved!'));
  }");
        // WebKit uses console.debug() to report binding calls, make sure they don't get reported.
        await Page.ExposeBindingAsync("foobar", (BindingSource _, string value) => Page.EvaluateAsync("value => console.log(value)", value));
        await Page.EvaluateAsync("() => window['foobar']('Using bindings')");

        Assert.That(messages.Select(msg => msg.Type), Is.EqualTo(new[]
        {
            "timeEnd", "trace", "dir", "warning", "error", "info", "debug", "log", "log",
        }));
        Assert.That(messages[0].Text, Does.Contain("calling console.time"));
        Assert.That(messages.Skip(1).Select(msg => msg.Text), Is.EqualTo(new[]
        {
            "calling console.trace",
            "calling console.dir",
            "calling console.warn",
            "calling console.error",
            "calling console.info",
            "calling console.debug",
            "Promise",
            "Using bindings",
        }));
    }

    [UpstreamTest("CM-005 should format the message correctly with time/timeLog/timeEnd")]
    public async Task ShouldFormatTheMessageCorrectlyWithTimeTimeLogTimeEnd()
    {
        Info.Fixme(BrowserName == "firefox" && !IsBidi, "https://github.com/microsoft/playwright/issues/10580");
        var messages = new List<IConsoleMessage>();
        Page.Console += (_, msg) => messages.Add(msg);
        await Page.EvaluateAsync(@"async () => {
    console.time('foo time');
    await new Promise(x => window.builtins.setTimeout(x, 100));
    console.timeLog('foo time');
    await new Promise(x => window.builtins.setTimeout(x, 100));
    console.timeEnd('foo time');
  }");
        Assert.That(messages.Count, Is.EqualTo(2));
        if (BrowserName == "webkit")
        {
            Assert.That(messages[0].Type, Is.EqualTo("timeEnd"));
        }
        else if (BrowserName == "chromium")
        {
            Assert.That(messages[0].Type, Is.EqualTo("log"));
        }
        else if (BrowserName == "firefox")
        {
            Assert.That(messages[0].Type, Is.EqualTo("timeLog"));
        }
        Assert.That(messages[1].Type, Is.EqualTo("timeEnd"));

        // WebKit has no space before the unit: https://bugs.webkit.org/show_bug.cgi?id=233556
        // Firefox/Bidi has no fractional part.
        Assert.That(messages[0].Text, Does.Match(@"foo time: \d+(.\d+)? ?ms"));
        Assert.That(messages[1].Text, Does.Match(@"foo time: \d+(.\d+)? ?ms"));
    }

    [UpstreamTest("CM-006 should not fail for window object")]
    public async Task ShouldNotFailForWindowObject()
    {
        IConsoleMessage message = null;
        EventHandler<IConsoleMessage> once = null;
        once = (_, msg) =>
        {
            Page.Console -= once;
            message = msg;
        };
        Page.Console += once;
        await PromiseAll(
            Page.EvaluateAsync("() => console.error(window)"),
            Page.WaitForConsoleMessageAsync());
        if (BrowserName != "firefox" || IsBidi)
        {
            Assert.That(message.Text, Is.EqualTo("Window"));
        }
        else
        {
            Assert.That(message.Text, Is.EqualTo("JSHandle@object"));
        }
    }

    [UpstreamTest("CM-007 should not fail for object that can't be coerced to a primitive")]
    public async Task ShouldNotFailForObjectThatCanTBeCoercedToAPrimitive()
    {
        IConsoleMessage message = null;
        EventHandler<IConsoleMessage> once = null;
        once = (_, msg) =>
        {
            Page.Console -= once;
            message = msg;
        };
        Page.Console += once;
        await PromiseAll(
            Page.EvaluateAsync("() => console.log({ toString() { return {}; }, valueOf() { return {}; } })"),
            Page.WaitForConsoleMessageAsync());
        if (BrowserName != "firefox" || IsBidi)
        {
            Assert.That(message.Text, Does.Match(@"\{toString: (Function)?, valueOf: (Function)?\}"));
        }
        else
        {
            Assert.That(message.Text, Is.EqualTo("JSHandle@object"));
        }
    }

    [UpstreamTest("CM-008 should trigger correct Log")]
    public async Task ShouldTriggerCorrectLog()
    {
        Info.Skip(BrowserName == "webkit" && IsWindows, "Upstream issue https://bugs.webkit.org/show_bug.cgi?id=229515");
        await Page.GotoAsync("about:blank");
        var (message, _) = await PromiseAll(
            Page.WaitForConsoleMessageAsync(),
            Page.EvaluateAsync("async url => fetch(url).catch(e => {})", Server.EmptyPage));
        Assert.That(message.Text, Does.Match("Access-Control-Allow-Origin|CORS"));
        Assert.That(message.Type, Is.EqualTo("error"));
    }

    [UpstreamTest("CM-010 should not throw when there are console messages in detached iframes")]
    public async Task ShouldNotThrowWhenThereAreConsoleMessagesInDetachedIframes()
    {
        // @see https://github.com/GoogleChrome/puppeteer/issues/3865
        await Page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync(@"async () => {
      // 1. Create a popup that Playwright is not connected to.
      const win = window.open('');
      window['_popup'] = win;
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
        Assert.That(await popup.EvaluateAsync<int>("1 + 1"), Is.EqualTo(2));
    }

    [UpstreamTest("CM-011 should use object previews for arrays and objects")]
    public async Task ShouldUseObjectPreviewsForArraysAndObjects()
    {
        string text = null;
        Page.Console += (_, message) =>
        {
            text = message.Text;
        };
        await Page.EvaluateAsync("() => console.log([1, 2, 3], { a: 1 }, window)");

        if (BrowserName != "firefox" || IsBidi)
        {
            Assert.That(text, Is.EqualTo("[1, 2, 3] {a: 1} Window"));
        }
        else
        {
            Assert.That(text, Is.EqualTo("Array JSHandle@object JSHandle@object"));
        }
    }

    [UpstreamTest("CM-012 should use object previews for errors")]
    public async Task ShouldUseObjectPreviewsForErrors()
    {
        string text = null;
        Page.Console += (_, message) =>
        {
            text = message.Text;
        };
        await Page.EvaluateAsync("() => console.log(new Error('Exception'))");
        if (IsBidi || BrowserName == "firefox")
        {
            Assert.That(text, Is.EqualTo("Error"));
        }
        else if (BrowserName == "chromium")
        {
            Assert.That(text, Does.Contain(".evaluate"));
        }
        else if (BrowserName == "webkit")
        {
            Assert.That(text, Is.EqualTo("Error: Exception"));
        }
    }

    [UpstreamTest("CM-013 do not update console count on unhandled rejections")]
    public async Task DoNotUpdateConsoleCountOnUnhandledRejections()
    {
        var messages = new List<string>();
        EventHandler<IConsoleMessage> consoleEventListener = (_, m) => messages.Add(m.Text);
        Page.Console += consoleEventListener;

        await Page.EvaluateAsync(@"() => {
    const fail = async () => Promise.reject(new Error('error'));
    console.log('begin');
    void fail();
    void fail();
    fail().catch(() => {
      console.log('end');
    });
  }");

        await Expect(() => Assert.That(messages, Is.EqualTo(new[] { "begin", "end" }))).ToPassAsync();
    }

    [UpstreamTest("CM-014 should have timestamp")]
    [LocalFixme("dotnet: ConsoleMessage.Timestamp is a float, which holds the milliseconds since the epoch in steps of 131072 only: the value is up to 65 seconds away from the time of the message, and the test passes only when the rounded value falls in its window by chance")]
    public async Task ShouldHaveTimestamp()
    {
        Info.Skip(IsAndroid, "there is a time difference between android emulator and host machine");

        // Generous slack to absorb host wall-clock resolution (e.g. ~15.6ms on Windows)
        // vs sub-millisecond browser timestamps.
        // .NET: a number of JavaScript is a double. NUnit compares a float with a long as two floats.
        double before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 100;
        var (message, _) = await PromiseAll(
            Page.WaitForConsoleMessageAsync(),
            Page.EvaluateAsync("() => console.log('timestamp test')"));
        double after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 100;
        Assert.That(message.Timestamp, Is.GreaterThanOrEqualTo(before));
        Assert.That(message.Timestamp, Is.LessThanOrEqualTo(after));
    }

    [UpstreamTest("CM-015 should have increasing timestamps")]
    public async Task ShouldHaveIncreasingTimestamps()
    {
        var messages = new List<IConsoleMessage>();
        Page.Console += (_, msg) => messages.Add(msg);
        await Page.EvaluateAsync(@"() => {
    console.log('first');
    console.log('second');
    console.log('third');
  }");
        Assert.That(messages.Count, Is.EqualTo(3));
        for (var i = 1; i < messages.Count; i++)
        {
            Assert.That(messages[i].Timestamp, Is.GreaterThanOrEqualTo(messages[i - 1].Timestamp));
        }
    }

    [UpstreamTest("CM-016 should have timestamp in consoleMessages")]
    [LocalFixme("dotnet: ConsoleMessage.Timestamp is a float, which holds the milliseconds since the epoch in steps of 131072 only: the value is up to 65 seconds away from the time of the message, and the test passes only when the rounded value falls in its window by chance")]
    public async Task ShouldHaveTimestampInConsoleMessages()
    {
        Info.Skip(IsAndroid, "there is a time difference between android emulator and host machine");

        // Generous slack to absorb host wall-clock resolution (e.g. ~15.6ms on Windows)
        // vs sub-millisecond browser timestamps.
        // .NET: a number of JavaScript is a double. NUnit compares a float with a long as two floats.
        double before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 100;
        await Page.EvaluateAsync("() => console.log('stored message')");
        double after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 100;
        var messages = await Page.ConsoleMessagesAsync();
        Assert.That(messages.Count, Is.GreaterThanOrEqualTo(1));
        var last = messages[messages.Count - 1];
        Assert.That(last.Text, Is.EqualTo("stored message"));
        Assert.That(last.Timestamp, Is.GreaterThanOrEqualTo(before));
        Assert.That(last.Timestamp, Is.LessThanOrEqualTo(after));
    }

    [UpstreamTest("CM-017 consoleMessages should work")]
    public async Task ConsoleMessagesShouldWork()
    {
        await Page.EvaluateAsync(@"() => {
    for (let i = 0; i < 301; i++)
      console.log('message' + i);
  }");

        var messages = await Page.ConsoleMessagesAsync();
        var objects = messages.Select(m => new { text = m.Text, type = m.Type, page = m.Page }).ToList();

        var expected = new List<object>();
        for (var i = 201; i < 301; i++)
        {
            expected.Add(ObjectContaining(new { text = "message" + i, type = "log", page = Page }));
        }

        Assert.That(objects.Count, Is.GreaterThanOrEqualTo(100), "should be at least 100 messages");
        // .NET: ExpectToEqual takes no message, 'should return last messages'.
        ExpectToEqual(objects.Skip(objects.Count - expected.Count), expected);
    }

    [UpstreamTest("CM-018 clearConsoleMessages should work")]
    public async Task ClearConsoleMessagesShouldWork()
    {
        await Page.EvaluateAsync(@"() => {
    console.log('message1');
    console.log('message2');
  }");

        var messages = await Page.ConsoleMessagesAsync();
        Assert.That(messages.Select(m => m.Text), Does.Contain("message1"));
        Assert.That(messages.Select(m => m.Text), Does.Contain("message2"));

        await Page.ClearConsoleMessagesAsync();

        messages = await Page.ConsoleMessagesAsync();
        Assert.That(messages, Is.EqualTo(Array.Empty<IConsoleMessage>()));

        await Page.EvaluateAsync("() => console.log('message3')");
        messages = await Page.ConsoleMessagesAsync();
        Assert.That(messages.Count, Is.EqualTo(1));
        Assert.That(messages[0].Text, Is.EqualTo("message3"));
    }

    [UpstreamTest("CM-019 consoleMessages since-navigation filter should work")]
    public async Task ConsoleMessagesSinceNavigationFilterShouldWork()
    {
        await Page.EvaluateAsync("() => console.log('before navigation')");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync("() => console.log('after navigation')");

        var all = await Page.ConsoleMessagesAsync(new() { Filter = ConsoleMessagesFilter.All });
        Assert.That(all.Select(m => m.Text), Does.Contain("before navigation"));
        Assert.That(all.Select(m => m.Text), Does.Contain("after navigation"));

        // since-navigation is the default
        var sinceNav = await Page.ConsoleMessagesAsync();
        Assert.That(sinceNav.Select(m => m.Text), Does.Not.Contain("before navigation"));
        Assert.That(sinceNav.Select(m => m.Text), Does.Contain("after navigation"));
    }
}
