/**
 * Copyright 2018 Google Inc. All rights reserved.
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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-wait-for-function.spec.ts.</summary>
public class PageWaitForFunctionTests : UpstreamPageTest
{
    [UpstreamTest("WA-001 should timeout")]
    public async Task ShouldTimeout()
    {
        var startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var timeout = 42;
        await Page.WaitForTimeoutAsync(timeout);
        Assert.That(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startTime, Is.Not.LessThan(timeout / 2));
    }

    [UpstreamTest("WA-002 should accept a string")]
    public async Task ShouldAcceptAString()
    {
        var watchdog = Page.WaitForFunctionAsync("window.__FOO === 1");
        await Page.EvaluateAsync("() => window['__FOO'] = 1");
        await watchdog;
    }

    [UpstreamTest("WA-003 should work when resolved right before execution context disposal")]
    public async Task ShouldWorkWhenResolvedRightBeforeExecutionContextDisposal()
    {
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await Page.AddInitScriptAsync("(() => window['__RELOADED'] = true)()");
        await Page.WaitForFunctionAsync(@"() => {
    if (!window['__RELOADED'])
      window.location.reload();
    return true;
  }");
    }

    [UpstreamTest("WA-004 should poll on interval")]
    public async Task ShouldPollOnInterval()
    {
        var polling = 100;
        var timeDelta = await Page.WaitForFunctionAsync(@"() => {
    if (!window['__startTime']) {
      window['__startTime'] = window.builtins.Date.now();
      return false;
    }
    return window.builtins.Date.now() - window['__startTime'];
  }", new { }, new() { PollingInterval = polling });
        Assert.That(await timeDelta.JsonValueAsync<double>(), Is.Not.LessThan(polling));
    }

    [UpstreamTest("WA-005 should avoid side effects after timeout")]

    public async Task ShouldAvoidSideEffectsAfterTimeout()
    {
        var counter = 0;
        Page.Console += (_, _) => ++counter;

        var error = await Catch(() => Page.WaitForFunctionAsync(@"() => {
    window['counter'] = (window['counter'] || 0) + 1;
    console.log(window['counter']);
  }", new { }, new() { PollingInterval = 1, Timeout = 1000 }));

        var savedCounter = counter;
        await Page.WaitForTimeoutAsync(2000); // Give it some time to produce more logs.

        Assert.That(error.Message, Does.Contain("Timeout 1000ms exceeded"));
        Assert.That(counter, Is.EqualTo(savedCounter));
    }

    [UpstreamTest("WA-008 should fail with predicate throwing on first call")]
    public async Task ShouldFailWithPredicateThrowingOnFirstCall()
    {
        var error = await Catch(() => Page.WaitForFunctionAsync("() => { throw new Error('oh my'); }"));
        Assert.That(error.Message, Does.Contain("oh my"));
    }

    [UpstreamTest("WA-009 should fail with predicate throwing sometimes")]
    public async Task ShouldFailWithPredicateThrowingSometimes()
    {
        var error = await Catch(() => Page.WaitForFunctionAsync(@"() => {
    window['counter'] = (window['counter'] || 0) + 1;
    if (window['counter'] === 3)
      throw new Error('Bad counter!');
    return window['counter'] === 5 ? 'result' : false;
  }"));
        Assert.That(error.Message, Does.Contain("Bad counter!"));
    }

    [UpstreamTest("WA-010 should fail with ReferenceError on wrong page")]
    public async Task ShouldFailWithReferenceErrorOnWrongPage()
    {
        var error = await Catch(() => Page.WaitForFunctionAsync("() => globalVar === 123"));
        Assert.That(error.Message, Does.Contain("globalVar"));
    }

    [UpstreamTest("WA-011 should work with strict CSP policy")]
    public async Task ShouldWorkWithStrictCSPPolicy()
    {
        Server.SetCSP("/empty.html", "script-src " + Server.Prefix);
        await Page.GotoAsync(Server.EmptyPage);
        Exception error = null;
        // .NET: waitForFunction(...).catch(e => error = e) is a local function that is called in its place.
        // .NET: { polling: 'raf' } is left out: the .NET option takes an interval only, and 'raf' is the default.
        async Task WaitForFunction()
        {
            error = await Catch(() => Page.WaitForFunctionAsync("() => window['__FOO'] === 'hit'", new { }));
        }
        var p = WaitForFunction();
        await Page.WaitForTimeoutAsync(1000);
        await Page.EvaluateAsync("() => window['__FOO'] = 'hit'");
        await p;
        Assert.That(error, Is.Null);
    }

    [UpstreamTest("WA-013 should throw negative polling interval")]
    public async Task ShouldThrowNegativePollingInterval()
    {
        Exception error = null;
        try
        {
            await Page.WaitForFunctionAsync("() => !!document.body", new { }, new() { PollingInterval = -10 });
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("Cannot poll with non-positive interval"));
    }

    [UpstreamTest("WA-014 should return the success value as a JSHandle")]
    public async Task ShouldReturnTheSuccessValueAsAJSHandle()
    {
        Assert.That(await (await Page.WaitForFunctionAsync("() => 5")).JsonValueAsync<int>(), Is.EqualTo(5));
    }

    [UpstreamTest("WA-015 should return the window as a success value")]
    public async Task ShouldReturnTheWindowAsASuccessValue()
    {
        Assert.That(await Page.WaitForFunctionAsync("() => window"), Is.Not.Null);
    }

    [UpstreamTest("WA-016 should accept ElementHandle arguments")]
    public async Task ShouldAcceptElementHandleArguments()
    {
        await Page.SetContentAsync("<div></div>");
        var div = await Page.QuerySelectorAsync("div");
        var resolved = false;
        // .NET: waitForFunction(...).then(() => resolved = true) is a local function that is called in its place.
        async Task WaitForFunction()
        {
            await Page.WaitForFunctionAsync("element => !element.parentElement", div);
            resolved = true;
        }
        var waitForFunction = WaitForFunction();
        Assert.That(resolved, Is.EqualTo(false));
        await Page.EvaluateAsync("element => element.remove()", div);
        await waitForFunction;
    }

    [UpstreamTest("WA-017 should respect timeout")]

    public async Task ShouldRespectTimeout()
    {
        Exception error = null;
        error = await Catch(() => Page.WaitForFunctionAsync("false", new { }, new() { Timeout = 10 }));
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("Timeout 10ms exceeded"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("WA-018 should respect default timeout")]

    public async Task ShouldRespectDefaultTimeout()
    {
        Page.SetDefaultTimeout(1);
        Exception error = null;
        error = await Catch(() => Page.WaitForFunctionAsync("false"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
        Assert.That(error.Message, Does.Contain("Timeout 1ms exceeded"));
    }

    [UpstreamTest("WA-019 should disable timeout when its set to 0")]
    public async Task ShouldDisableTimeoutWhenItsSetTo0()
    {
        var watchdog = Page.WaitForFunctionAsync(@"() => {
    window['__counter'] = (window['__counter'] || 0) + 1;
    return window['__injected'];
  }", new { }, new() { Timeout = 0, PollingInterval = 10 });
        await Page.WaitForFunctionAsync("() => window['__counter'] > 10");
        await Page.EvaluateAsync("() => window['__injected'] = true");
        await watchdog;
    }

    [UpstreamTest("WA-020 should survive cross-process navigation")]
    public async Task ShouldSurviveCrossProcessNavigation()
    {
        var fooFound = false;
        // .NET: waitForFunction(...).then(() => fooFound = true) is a local function that is called in its place.
        async Task WaitForFunction()
        {
            await Page.WaitForFunctionAsync("window.__FOO === 1");
            fooFound = true;
        }
        var waitForFunction = WaitForFunction();
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(fooFound, Is.EqualTo(false));
        await Page.ReloadAsync();
        Assert.That(fooFound, Is.EqualTo(false));
        await Page.GotoAsync(Server.CrossProcessPrefix + "/grid.html");
        Assert.That(fooFound, Is.EqualTo(false));
        await Page.EvaluateAsync("() => window['__FOO'] = 1");
        await waitForFunction;
        Assert.That(fooFound, Is.EqualTo(true));
    }

    [UpstreamTest("WA-021 should survive navigations")]
    public async Task ShouldSurviveNavigations()
    {
        var watchdog = Page.WaitForFunctionAsync("() => window['__done']");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.GotoAsync(Server.Prefix + "/consolelog.html");
        await Page.EvaluateAsync("() => window['__done'] = true");
        await watchdog;
    }

    [UpstreamTest("WA-022 should work with multiline body")]
    public async Task ShouldWorkWithMultilineBody()
    {
        var result = await Page.WaitForFunctionAsync(@"
    (() => true)()
  ");
        Assert.That(await result.JsonValueAsync<bool>(), Is.EqualTo(true));
    }

    [UpstreamTest("WA-023 should wait for predicate with arguments")]
    public async Task ShouldWaitForPredicateWithArguments()
    {
        await Page.WaitForFunctionAsync("({ arg1, arg2 }) => arg1 + arg2 === 3", new { arg1 = 1, arg2 = 2 });
    }

    [UpstreamTest("WA-024 should not be called after finishing successfully")]
    public async Task ShouldNotBeCalledAfterFinishingSuccessfully()
    {
        await Page.GotoAsync(Server.EmptyPage);

        var messages = new List<string>();
        Page.Console += (_, msg) =>
        {
            if (msg.Text.StartsWith("waitForFunction", StringComparison.Ordinal))
            {
                messages.Add(msg.Text);
            }
        };

        await Page.WaitForFunctionAsync(@"() => {
    console.log('waitForFunction1');
    return true;
  }");
        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync(@"() => {
    console.log('waitForFunction2');
    return true;
  }");
        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync(@"() => {
    console.log('waitForFunction3');
    return true;
  }");

        Assert.That(string.Join("|", messages), Is.EqualTo("waitForFunction1|waitForFunction2|waitForFunction3"));
    }

    [UpstreamTest("WA-025 should not be called after finishing unsuccessfully")]
    public async Task ShouldNotBeCalledAfterFinishingUnsuccessfully()
    {
        await Page.GotoAsync(Server.EmptyPage);

        var messages = new List<string>();
        Page.Console += (_, msg) =>
        {
            if (msg.Text.StartsWith("waitForFunction", StringComparison.Ordinal))
            {
                messages.Add(msg.Text);
            }
        };

        await Catch(() => Page.WaitForFunctionAsync(@"() => {
    console.log('waitForFunction1');
    throw new Error('waitForFunction1');
  }"));
        await Page.ReloadAsync();
        await Catch(() => Page.WaitForFunctionAsync(@"() => {
    console.log('waitForFunction2');
    throw new Error('waitForFunction2');
  }"));
        await Page.ReloadAsync();
        await Catch(() => Page.WaitForFunctionAsync(@"() => {
    console.log('waitForFunction3');
    throw new Error('waitForFunction3');
  }"));

        Assert.That(string.Join("|", messages), Is.EqualTo("waitForFunction1|waitForFunction2|waitForFunction3"));
    }

    [UpstreamTest("WA-026 should throw when frame is detached")]

    public async Task ShouldThrowWhenFrameIsDetached()
    {
        await AttachFrame(Page, "frame1", Server.EmptyPage);
        var frame = Page.Frames[1];
        var promise = Catch(() => frame.WaitForFunctionAsync("() => false"));
        await DetachFrame(Page, "frame1");
        var error = await promise;
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Match("(Frame was detached|Execution context was destroyed)"));
    }
}
