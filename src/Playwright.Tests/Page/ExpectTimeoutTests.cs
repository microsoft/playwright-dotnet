/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
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

// .NET: the message of a failed Expect() has the layout of upstream and names the .NET call:
// "expect(locator).not.toHaveText(expected) failed" is "Expect(locator).Not.ToHaveTextAsync(expected) failed",
// and 'Expect "not toHaveText"' in the call log is 'Expect "Not.ToHaveTextAsync"'.
/// <summary>Port of tests/page/expect-timeout.spec.ts.</summary>
public class ExpectTimeoutTests : UpstreamPageTest
{
    [UpstreamTest("EX-244 should print element not found")]
    public async Task ShouldPrintElementNotFound()
    {
        await Page.SetContentAsync("<div id=node>Text content</div>");
        var error = await Catch(() => Expect(Page.Locator("no-such-thing")).ToHaveTextAsync("hey", new() { Timeout = 1000 }));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveTextAsync(expected) failed

Locator: Locator(""no-such-thing"")
Expected: ""hey""
Timeout: 1000ms
Error: element(s) not found

Call log:
"));
    }

    [UpstreamTest("EX-245 should print timed out error message when value does not match")]
    public async Task ShouldPrintTimedOutErrorMessageWhenValueDoesNotMatch()
    {
        await Page.SetContentAsync("<div id=node>Text content</div>");
        var error = await Catch(() => Expect(Page.Locator("div")).ToHaveTextAsync("hey", new() { Timeout = 1000 }));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveTextAsync(expected) failed

Locator:  Locator(""div"")
Expected: ""hey""
Received: ""Text content""
Timeout:  1000ms

Call log:
"));
    }

    [UpstreamTest("EX-246 should print timed out error message with impossible timeout")]
    public async Task ShouldPrintTimedOutErrorMessageWithImpossibleTimeout()
    {
        await Page.SetContentAsync("<div id=node>Text content</div>");
        var error = await Catch(() => Expect(Page.Locator("no-such-thing")).ToHaveTextAsync("hey", new() { Timeout = 1 }));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveTextAsync(expected) failed

Locator: Locator(""no-such-thing"")
Expected: ""hey""
Timeout: 1ms
Error: element(s) not found

Call log:"));
    }

    [UpstreamTest("EX-247 should print timed out error message when value does not match with impossible timeout")]
    public async Task ShouldPrintTimedOutErrorMessageWhenValueDoesNotMatchWithImpossibleTimeout()
    {
        await Page.SetContentAsync("<div id=node>Text content</div>");
        var error = await Catch(() => Expect(Page.Locator("div")).ToHaveTextAsync("hey", new() { Timeout = 1 }));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveTextAsync(expected) failed

Locator:  Locator(""div"")
Expected: ""hey""
Received: ""Text content""
Timeout:  1ms

Call log:
"));
    }

    [UpstreamTest("EX-248 should have timeout error name")]
    public async Task ShouldHaveTimeoutErrorName()
    {
        var error = await Catch(() => Page.WaitForSelectorAsync("#not-found", new() { Timeout = 1 }));
        // .NET: expect(error.name).toBe('TimeoutError'): an exception has a class, not a name.
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("EX-249 should not throw when navigating during one-shot check")]
    public async Task ShouldNotThrowWhenNavigatingDuringOneShotCheck()
    {
        await Page.SetContentAsync("<div>hello</div>");
        var promise = Expect(Page.Locator("div")).ToHaveTextAsync("bye");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<div>bye</div>");
        await promise;
    }

    [UpstreamTest("EX-250 should not throw when navigating during first locator handler check")]
    public async Task ShouldNotThrowWhenNavigatingDuringFirstLocatorHandlerCheck()
    {
        await Page.AddLocatorHandlerAsync(Page.Locator("span"), async locator => { });
        await Page.SetContentAsync("<div>hello</div>");
        var promise = Expect(Page.Locator("div")).ToHaveTextAsync("bye");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<div>bye</div>");
        await promise;
    }

    [UpstreamTest("EX-251 should timeout during first locator handler check")]
    public async Task ShouldTimeoutDuringFirstLocatorHandlerCheck()
    {
        await Page.AddLocatorHandlerAsync(Page.Locator("div"), async locator => { });
        await Page.SetContentAsync("<div>hello</div><span>bye</span>");
        var error = await Catch(() => Expect(Page.Locator("span")).ToHaveTextAsync("bye", new() { Timeout = 3000 }));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveTextAsync(expected) failed

Locator:  Locator(""span"")
Expected: ""bye""
Received: """"
Timeout:  3000ms

Call log:
"));
        Assert.That(error.Message, Does.Contain("locator handler has finished, waiting for Locator(\"div\") to be hidden"));
        Assert.That(error.Message, Does.Contain("locator resolved to visible <div>hello</div>"));
    }

    [UpstreamTest("EX-252 should not miss element that appears between retries before the deadline")]
    public async Task ShouldNotMissElementThatAppearsBetweenRetriesBeforeTheDeadline()
    {
        await Page.SetContentAsync("<div id=\"target\" style=\"display:none\">content</div>");
        await Page.EvaluateAsync(@"() => {
    window.builtins.setTimeout(() => {
      document.getElementById('target').style.display = 'block';
    }, 1500);
  }");
        await Expect(Page.Locator("#target")).ToBeVisibleAsync(new() { Timeout = 1800 });
    }
}
