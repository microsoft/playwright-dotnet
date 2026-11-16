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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/expect-to-have-text.spec.ts.</summary>
public class ExpectToHaveTextTests
{
    public class ToHaveTextWithRegex
    {
        [UpstreamTest("EX-274 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text   content</div>");
            var locator = page.Locator("#node");
            await Expect(locator).ToHaveTextAsync(new Regex("Text"));

            // Should not normalize whitespace.
            await Expect(locator).ToHaveTextAsync(new Regex("Text   content"));
            // Should respect ignoreCase.
            await Expect(locator).ToHaveTextAsync(new Regex("text   content"), new() { IgnoreCase = true });
            // Should override regex flag with ignoreCase.
            await Expect(locator).Not.ToHaveTextAsync(new Regex("text   content", RegexOptions.IgnoreCase), new() { IgnoreCase = false });
        }

        [UpstreamTest("EX-275 fail")]
        [Fixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text matching regex 'Text 2'\\nBut was: 'Text content' \\nCall log: ...\"; upstream expects \"expect(locator).toHaveText(expected) failed\" with the Locator, Expected pattern, Received string and Timeout lines")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            var locator = page.Locator("#node");
            var error = await Catch(() => Expect(locator).ToHaveTextAsync(new Regex("Text 2"), new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveText(expected) failed

Locator: locator('#node')
Expected pattern: /Text 2/
Received string:  ""Text content""
Timeout: 1000ms

Call log:
"));
        }
    }

    public class ToContainTextWithRegex
    {
        [UpstreamTest("EX-276 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text   content</div>");
            var locator = page.Locator("#node");
            await Expect(locator).ToContainTextAsync(new Regex("ex"));

            // Should not normalize whitespace.
            await Expect(locator).ToContainTextAsync(new Regex("ext   cont"));
        }

        [UpstreamTest("EX-277 fail")]
        [Fixme("dotnet: the message of ToContainTextAsync is \"Locator expected text matching regex 'ex2'\\nBut was: 'Text content' \\nCall log: ...\"; upstream expects 'Expected pattern: /ex2/' and 'Received string:  \"Text content\"'")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            var locator = page.Locator("#node");
            var error = await Catch(() => Expect(locator).ToContainTextAsync(new Regex("ex2"), new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain("Expected pattern: /ex2/"));
            Assert.That(StripAnsi(error.Message), Does.Contain("Received string:  \"Text content\""));
        }
    }

    public class ToContainTextWithString
    {
        [UpstreamTest("EX-278 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text   content</div>");
            var locator = page.Locator("#node");
            await Expect(locator).ToContainTextAsync("content");
        }

        [UpstreamTest("EX-279 fail")]
        [Fixme("dotnet: the message of ToContainTextAsync is \"Locator expected to contain text 'foo'\\nBut was: 'Text content' \\nCall log: ...\"; upstream expects \"expect(locator).toContainText(expected) failed\" with the Locator, Expected substring, Received string and Timeout lines")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            var locator = page.Locator("#node");
            var error = await Catch(() => Expect(locator).ToContainTextAsync("foo", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toContainText(expected) failed

Locator: locator('#node')
Expected substring: ""foo""
Received string:    ""Text content""
Timeout: 1000ms

Call log:
"));
        }
    }

    public class ToHaveTextWithText
    {
        [UpstreamTest("EX-280 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div id=node><span></span>Text \ncontent&nbsp;    </div>");
            var locator = page.Locator("#node");
            // Should normalize whitespace.
            await Expect(locator).ToHaveTextAsync("Text                        content");
            // Should normalize zero width whitespace.
            await Expect(locator).ToHaveTextAsync("T​e​x​t content");
            // Should support ignoreCase.
            await Expect(locator).ToHaveTextAsync("text CONTENT", new() { IgnoreCase = true });
            // Should support falsy ignoreCase.
            await Expect(locator).Not.ToHaveTextAsync("TEXT", new() { IgnoreCase = false });
            // Should normalize soft hyphens.
            await Expect(locator).ToHaveTextAsync("T­e­xt content");
        }

        [UpstreamTest("EX-281 pass contain")]
        public async Task PassContain(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            var locator = page.Locator("#node");
            await Expect(locator).ToContainTextAsync("Text");
            // Should normalize whitespace.
            await Expect(locator).ToContainTextAsync("   ext        cont\n  ");
            // Should support ignoreCase.
            await Expect(locator).ToContainTextAsync("EXT", new() { IgnoreCase = true });
            // Should support falsy ignoreCase.
            await Expect(locator).Not.ToContainTextAsync("TEXT", new() { IgnoreCase = false });
        }

        [UpstreamTest("EX-282 fail")]
        [Fixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text 'Text'\\nBut was: 'Text content' \\nCall log: ...\"; upstream expects 'Expected: \"Text\"' and 'Received: \"Text content\"'")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            var locator = page.Locator("#node");
            var error = await Catch(() => Expect(locator).ToHaveTextAsync("Text", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain("Expected: \"Text\""));
            Assert.That(StripAnsi(error.Message), Does.Contain("Received: \"Text content\""));
        }

        [UpstreamTest("EX-283 pass eventually")]
        public async Task PassEventually(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            var locator = page.Locator("#node");
            async Task Then()
            {
                await page.WaitForTimeoutAsync(1000);
                await locator.EvaluateAsync("element => element.textContent = 'Text 2 content'");
            }
            await PromiseAll(
                Expect(locator).ToHaveTextAsync(new Regex("Text 2")),
                Then());
        }

        [UpstreamTest("EX-284 with userInnerText")]
        public async Task WithUserInnerText(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text <span hidden>garbage</span> content</div>");
            var locator = page.Locator("#node");
            await Expect(locator).ToHaveTextAsync("Text content", new() { UseInnerText = true });
        }

        [UpstreamTest("EX-285 in shadow dom")]
        public async Task InShadowDom(IPage page)
        {
            await page.SetContentAsync(@"
      <div></div>
      <script>
        const div = document.querySelector('div');
        const span = document.createElement('span');
        span.textContent = 'some text';
        div.attachShadow({ mode: 'open' }).appendChild(span);
      </script>
    ");
            await Expect(page.Locator("span")).ToHaveTextAsync("some text");
            await Expect(page.Locator("span")).ToContainTextAsync("text");
            await Expect(page.Locator("div")).ToHaveTextAsync("some text");
            await Expect(page.Locator("div")).ToContainTextAsync("text");
            await Expect(page.Locator("span")).ToHaveTextAsync("some text", new() { UseInnerText = true });
            await Expect(page.Locator("span")).ToContainTextAsync("text", new() { UseInnerText = true });
            // Playwright intentionally does not perform innerText piercing on shadow dom.
            await Expect(page.Locator("div")).Not.ToHaveTextAsync("some text", new() { UseInnerText = true });
            await Expect(page.Locator("div")).Not.ToContainTextAsync("text", new() { UseInnerText = true });
        }

        [UpstreamTest("EX-286 fail with impossible timeout")]
        [Fixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text 'Text'\\nBut was: 'Text content' \\nCall log: ...\"; upstream expects 'Expected: \"Text\"' and 'Received: \"Text content\"'")]
        public async Task FailWithImpossibleTimeout(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            var error = await Catch(() => Expect(page.Locator("#node")).ToHaveTextAsync("Text", new() { Timeout = 1 }));
            Assert.That(StripAnsi(error.Message), Does.Contain("Expected: \"Text\""));
            Assert.That(StripAnsi(error.Message), Does.Contain("Received: \"Text content\""));
        }

        [UpstreamTest("EX-287 do not show \"element(s) not found\" when the real failure is a string mismatch")]
        [Fixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text 'Something'\\nBut was: 'Final value' \\nCall log: ...\"; upstream expects 'Expected: \"Something\"' and 'Received: \"Final value\"'")]
        public async Task DoNotShowElementSNotFoundWhenTheRealFailureIsAStringMismatch(IPage page)
        {
            It.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen", "The element is attached after 1 second.");
            await page.SetContentAsync(@"
      <div>Initial</div>
      <script>
        const field = document.querySelector('div');
        setTimeout(() => {
          field.id = 'field';
          field.textContent = 'Final value';
        }, 1000);
      </script>
    ");

            var cell = page.Locator("#field");
            var error = await Catch(() => Expect(cell).ToHaveTextAsync("Something", new() { Timeout = 3000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain("Expected: \"Something\""));
            Assert.That(StripAnsi(error.Message), Does.Contain("Received: \"Final value\""));
        }
    }

    public class NotToHaveText
    {
        [UpstreamTest("EX-288 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            var locator = page.Locator("#node");
            await Expect(locator).Not.ToHaveTextAsync("Text2");
            // Should be case-sensitive by default.
            await Expect(locator).Not.ToHaveTextAsync("TEXT");
        }

        [UpstreamTest("EX-289 fail")]
        [Fixme("dotnet: the message of Not.ToHaveTextAsync is \"Locator expected not to have text 'Text content'\\nBut was: 'Text content' \\nCall log: ...\"; upstream expects 'Expected: not \"Text content\"' and 'Received: \"Text content\"'")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            var locator = page.Locator("#node");
            var error = await Catch(() => Expect(locator).Not.ToHaveTextAsync("Text content", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain("Expected: not \"Text content\""));
            Assert.That(StripAnsi(error.Message), Does.Contain("Received: \"Text content"));
        }

        [UpstreamTest("EX-290 should work when selector does not match")]
        [Fixme("dotnet: the message of Not.ToHaveTextAsync is \"Locator expected not to have text\\nError: element(s) not found 'hello'\\nBut was: 'null' \\nCall log: ...\"; upstream expects 'Expected: not \"hello\"'. The call log has Locator(\"span\"), upstream expects locator('span')")]
        public async Task ShouldWorkWhenSelectorDoesNotMatch(IPage page)
        {
            await page.SetContentAsync("<div>hello</div>");
            var error = await Catch(() => Expect(page.Locator("span")).Not.ToHaveTextAsync("hello", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain("Expected: not \"hello\""));
            Assert.That(StripAnsi(error.Message), Does.Contain("Error: element(s) not found"));
            Assert.That(StripAnsi(error.Message), Does.Contain("waiting for locator('span')"));
        }
    }

    public class ToHaveTextWithArray
    {
        [UpstreamTest("EX-291 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div>Text    \n1</div><div>Text   2a</div>");
            var locator = page.Locator("div");
            // Should only normalize whitespace in the first item.
            // .NET: await expect(locator).toHaveText(['Text  1', /Text   \d+a/]) is left out:
            // ToHaveTextAsync() takes an array of strings or an array of regular expressions, not an array that mixes them.
            // Should support ignoreCase.
            await Expect(locator).ToHaveTextAsync(new[] { "tEXT 1", "TExt 2A" }, new() { IgnoreCase = true });
        }

        [UpstreamTest("EX-292 pass lazy")]
        public async Task PassLazy(IPage page)
        {
            await page.SetContentAsync("<div id=div></div>");
            var locator = page.Locator("p");
            SetTimeout(() =>
            {
                IgnoreErrors(page.EvaluateAsync(@"() => {
        document.querySelector('div').innerHTML = '<p>Text 1</p><p>Text 2</p>';
      }"));
            }, 500);
            await Expect(locator).ToHaveTextAsync(new[] { "Text 1", "Text 2" });
        }

        [UpstreamTest("EX-293 pass empty")]
        public async Task PassEmpty(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            var locator = page.Locator("p");
            await Expect(locator).ToHaveTextAsync(Array.Empty<string>());
        }

        [UpstreamTest("EX-294 pass not empty")]
        public async Task PassNotEmpty(IPage page)
        {
            await page.SetContentAsync("<div><p>Test</p></div>");
            var locator = page.Locator("p");
            await Expect(locator).Not.ToHaveTextAsync(Array.Empty<string>());
        }

        [UpstreamTest("EX-295 pass on empty")]
        public async Task PassOnEmpty(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            var locator = page.Locator("p");
            await Expect(locator).Not.ToHaveTextAsync(new[] { "Test" });
        }

        [UpstreamTest("EX-296 fail on not+empty")]
        [Fixme("dotnet: the message of Not.ToHaveTextAsync is \"Locator expected not to have text '[]'\\nBut was: '[]' \\nCall log:\\n  - Expect \"ToHaveTextAsync\" Locator(\"p\") with timeout 1000ms ...\"; upstream expects \"expect(locator).not.toHaveText(expected)\", 'Timeout:  1000ms' and '- Expect \"not toHaveText\" locator('p') with timeout 1000ms'")]
        public async Task FailOnNotEmpty(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            var locator = page.Locator("p");
            var error = await Catch(() => Expect(locator).Not.ToHaveTextAsync(Array.Empty<string>(), new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain("expect(locator).not.toHaveText(expected)"));
            Assert.That(StripAnsi(error.Message), Does.Contain("Timeout:  1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"not toHaveText\" locator('p') with timeout 1000ms"));
        }

        [UpstreamTest("EX-297 pass eventually empty")]
        public async Task PassEventuallyEmpty(IPage page)
        {
            await page.SetContentAsync("<div id=div><p>Text</p></div>");
            var locator = page.Locator("p");
            SetTimeout(() =>
            {
                IgnoreErrors(page.EvaluateAsync("() => document.querySelector('div').innerHTML = ''"));
            }, 500);
            await Expect(locator).Not.ToHaveTextAsync(Array.Empty<string>());
        }

        [UpstreamTest("EX-299 fail on repeating array matchers")]
        public async Task FailOnRepeatingArrayMatchers(IPage page)
        {
            await page.SetContentAsync("<div>KekFoo</div>");
            var locator = page.Locator("div");
            var error = await Catch(() => Expect(locator).ToContainTextAsync(new[] { "KekFoo", "KekFoo", "KekFoo" }, new() { Timeout = 1000 }));
            Assert.That(error.Message, Does.Contain("locator resolved to 1 element"));
        }
    }

    public class ToContainTextWithArray
    {
        [UpstreamTest("EX-300 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div>Text \n1</div><div>Text2</div><div>Text3</div>");
            var locator = page.Locator("div");
            // .NET: await expect(locator).toContainText(['ext     1', /ext3/]) is left out:
            // ToContainTextAsync() takes an array of strings or an array of regular expressions, not an array that mixes them.
            // Should support ignoreCase.
            await Expect(locator).ToContainTextAsync(new[] { "EXT 1", "eXt3" }, new() { IgnoreCase = true });
        }

        [UpstreamTest("EX-301 fail")]
        [Fixme("dotnet: the message of ToContainTextAsync has no diff of the arrays, \"Locator expected to contain text '['Text 2']'\\nBut was: '['Text 1', 'Text 3']' \\nCall log: ...\"; upstream expects the diff line '-   \"Text 2\"'")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<div>Text 1</div><div>Text 3</div>");
            var locator = page.Locator("div");
            var error = await Catch(() => Expect(locator).ToContainTextAsync(new[] { "Text 2" }, new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain("-   \"Text 2\""));
        }
    }
}
