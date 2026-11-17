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

/// <summary>Port of tests/page/expect-timeout.spec.ts.</summary>
public class ExpectTimeoutTests
{
    [UpstreamTest("EX-244 should print element not found")]
    [LocalFixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text\\nError: element(s) not found 'hey'\\nBut was: 'null' \\nCall log: ...\"; upstream expects \"expect(locator).toHaveText(expected) failed\" with the Locator, Expected and Timeout lines before the error")]
    public async Task ShouldPrintElementNotFound(IPage page)
    {
        await page.SetContentAsync("<div id=node>Text content</div>");
        var error = await Catch(() => Expect(page.Locator("no-such-thing")).ToHaveTextAsync("hey", new() { Timeout = 1000 }));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveText(expected) failed

Locator: locator('no-such-thing')
Expected: ""hey""
Timeout: 1000ms
Error: element(s) not found

Call log:
"));
    }

    [UpstreamTest("EX-245 should print timed out error message when value does not match")]
    [LocalFixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text 'hey'\\nBut was: 'Text content' \\nCall log: ...\"; upstream expects \"expect(locator).toHaveText(expected) failed\" with the Locator, Expected, Received and Timeout lines")]
    public async Task ShouldPrintTimedOutErrorMessageWhenValueDoesNotMatch(IPage page)
    {
        await page.SetContentAsync("<div id=node>Text content</div>");
        var error = await Catch(() => Expect(page.Locator("div")).ToHaveTextAsync("hey", new() { Timeout = 1000 }));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveText(expected) failed

Locator:  locator('div')
Expected: ""hey""
Received: ""Text content""
Timeout:  1000ms

Call log:
"));
    }

    [UpstreamTest("EX-246 should print timed out error message with impossible timeout")]
    [LocalFixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text\\nError: element(s) not found 'hey'\\nBut was: 'null' \\nCall log: ...\"; upstream expects \"expect(locator).toHaveText(expected) failed\" with the Locator, Expected and Timeout lines before the error")]
    public async Task ShouldPrintTimedOutErrorMessageWithImpossibleTimeout(IPage page)
    {
        await page.SetContentAsync("<div id=node>Text content</div>");
        var error = await Catch(() => Expect(page.Locator("no-such-thing")).ToHaveTextAsync("hey", new() { Timeout = 1 }));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveText(expected) failed

Locator: locator('no-such-thing')
Expected: ""hey""
Timeout: 1ms
Error: element(s) not found

Call log:"));
    }

    [UpstreamTest("EX-247 should print timed out error message when value does not match with impossible timeout")]
    [LocalFixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text 'hey'\\nBut was: 'Text content' \\nCall log: ...\"; upstream expects \"expect(locator).toHaveText(expected) failed\" with the Locator, Expected, Received and Timeout lines")]
    public async Task ShouldPrintTimedOutErrorMessageWhenValueDoesNotMatchWithImpossibleTimeout(IPage page)
    {
        await page.SetContentAsync("<div id=node>Text content</div>");
        var error = await Catch(() => Expect(page.Locator("div")).ToHaveTextAsync("hey", new() { Timeout = 1 }));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveText(expected) failed

Locator:  locator('div')
Expected: ""hey""
Received: ""Text content""
Timeout:  1ms

Call log:
"));
    }

    [UpstreamTest("EX-248 should have timeout error name")]
    public async Task ShouldHaveTimeoutErrorName(IPage page)
    {
        var error = await Catch(() => page.WaitForSelectorAsync("#not-found", new() { Timeout = 1 }));
        // .NET: expect(error.name).toBe('TimeoutError'): an exception has a class, not a name.
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("EX-249 should not throw when navigating during one-shot check")]
    public async Task ShouldNotThrowWhenNavigatingDuringOneShotCheck(IPage page, Server server)
    {
        await page.SetContentAsync("<div>hello</div>");
        var promise = Expect(page.Locator("div")).ToHaveTextAsync("bye");
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<div>bye</div>");
        await promise;
    }

    [UpstreamTest("EX-250 should not throw when navigating during first locator handler check")]
    public async Task ShouldNotThrowWhenNavigatingDuringFirstLocatorHandlerCheck(IPage page, Server server)
    {
        await page.AddLocatorHandlerAsync(page.Locator("span"), async locator => { });
        await page.SetContentAsync("<div>hello</div>");
        var promise = Expect(page.Locator("div")).ToHaveTextAsync("bye");
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<div>bye</div>");
        await promise;
    }

    [UpstreamTest("EX-251 should timeout during first locator handler check")]
    [LocalFixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text 'bye'\\nBut was: 'null' \\nCall log: ...\"; upstream expects \"expect(locator).toHaveText(expected) failed\" with the Locator, Expected, Received and Timeout lines. The call log has Locator(\"div\"), upstream expects locator('div')")]
    public async Task ShouldTimeoutDuringFirstLocatorHandlerCheck(IPage page, Server server)
    {
        await page.AddLocatorHandlerAsync(page.Locator("div"), async locator => { });
        await page.SetContentAsync("<div>hello</div><span>bye</span>");
        var error = await Catch(() => Expect(page.Locator("span")).ToHaveTextAsync("bye", new() { Timeout = 3000 }));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveText(expected) failed

Locator:  locator('span')
Expected: ""bye""
Received: """"
Timeout:  3000ms

Call log:
"));
        Assert.That(error.Message, Does.Contain("locator handler has finished, waiting for locator('div') to be hidden"));
        Assert.That(error.Message, Does.Contain("locator resolved to visible <div>hello</div>"));
    }

    [UpstreamTest("EX-252 should not miss element that appears between retries before the deadline")]
    public async Task ShouldNotMissElementThatAppearsBetweenRetriesBeforeTheDeadline(IPage page)
    {
        await page.SetContentAsync("<div id=\"target\" style=\"display:none\">content</div>");
        await page.EvaluateAsync(@"() => {
    window.builtins.setTimeout(() => {
      document.getElementById('target').style.display = 'block';
    }, 1500);
  }");
        await Expect(page.Locator("#target")).ToBeVisibleAsync(new() { Timeout = 1800 });
    }
}
