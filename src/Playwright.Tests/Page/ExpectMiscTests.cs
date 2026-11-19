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

// .NET: the message of a failed Expect() has the layout of upstream and names the .NET call:
// "expect(locator).not.toHaveText(expected) failed" is "Expect(locator).Not.ToHaveTextAsync(expected) failed",
// and 'Expect "not toHaveText"' in the call log is 'Expect "Not.ToHaveTextAsync"'.
/// <summary>Port of tests/page/expect-misc.spec.ts.</summary>
public class ExpectMiscTests : UpstreamPageTest
{
    public class ToHaveCount : UpstreamPageTest
    {
        [UpstreamTest("EX-181 toHaveCount pass")]
        public async Task ToHaveCountPass()
        {
            await Page.SetContentAsync("<select><option>One</option></select>");
            var locator = Page.Locator("option");
            var done = false;
            // .NET: expect(locator).toHaveCount(2).then(() => { done = true; }) is a local function that is called in its place.
            async Task Then()
            {
                await Expect(locator).ToHaveCountAsync(2);
                done = true;
            }
            var promise = Then();
            await Page.WaitForTimeoutAsync(1000);
            Assert.That(done, Is.EqualTo(false));
            await Page.SetContentAsync("<select><option>One</option><option>Two</option></select>");
            await promise;
            Assert.That(done, Is.EqualTo(true));
        }

        [UpstreamTest("EX-182 pass zero")]
        public async Task PassZero()
        {
            await Page.SetContentAsync("<div></div>");
            var locator = Page.Locator("span");
            await Expect(locator).ToHaveCountAsync(0);
            await Expect(locator).Not.ToHaveCountAsync(1);
        }

        [UpstreamTest("EX-183 eventually pass zero")]
        public async Task EventuallyPassZero()
        {
            await Page.SetContentAsync("<div><span>hello</span></div>");
            var locator = Page.Locator("span");
            SetTimeout(() => IgnoreErrors(Page.EvaluateAsync("() => document.querySelector('div').textContent = ''")), 200);
            await Expect(locator).ToHaveCountAsync(0);
            await Expect(locator).Not.ToHaveCountAsync(1);
        }

        [UpstreamTest("EX-184 eventually pass non-zero")]
        public async Task EventuallyPassNonZero()
        {
            await Page.SetContentAsync("<ul></ul>");
            SetTimeout(async () =>
            {
                await Page.SetContentAsync("<ul><li>one</li><li>two</li></ul>");
            }, 500);
            var locator = Page.Locator("li");
            await Expect(locator).ToHaveCountAsync(2);
        }

        [UpstreamTest("EX-185 eventually pass not non-zero")]
        public async Task EventuallyPassNotNonZero()
        {
            await Page.SetContentAsync("<ul><li>one</li><li>two</li></ul>");
            SetTimeout(async () =>
            {
                await Page.SetContentAsync("<ul></ul>");
            }, 500);
            var locator = Page.Locator("li");
            await Expect(locator).Not.ToHaveCountAsync(2);
        }

        [UpstreamTest("EX-186 fail zero")]
        public async Task FailZero()
        {
            await Page.SetContentAsync("<div><span></span></div>");
            var locator = Page.Locator("span");
            var error = await Catch(() => Expect(locator).ToHaveCountAsync(0, new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveCountAsync(expected) failed

Locator:  Locator(""span"")
Expected: 0
Received: 1
Timeout:  1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"ToHaveCountAsync\" Locator(\"span\") with timeout 1000ms"));
        }

        [UpstreamTest("EX-187 fail zero 2")]
        public async Task FailZero2()
        {
            await Page.SetContentAsync("<div><span></span></div>");
            var locator = Page.Locator("span");
            var error = await Catch(() => Expect(locator).Not.ToHaveCountAsync(1, new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).Not.ToHaveCountAsync(expected) failed

Locator:  Locator(""span"")
Expected: not 1
Received: 1
Timeout:  1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"Not.ToHaveCountAsync\" Locator(\"span\") with timeout 1000ms"));
        }
    }

    public class ToHaveJSProperty : UpstreamPageTest
    {
        [UpstreamTest("EX-188 pass")]
        public async Task Pass()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = { a: 1, b: 'string', c: new Date(1627503992000) }");
            var locator = Page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo", new { a = 1, b = "string", c = DateTime.UnixEpoch.AddMilliseconds(1627503992000) });
        }

        [UpstreamTest("EX-189 fail")]
        public async Task Fail()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = { a: 1, b: 'string', c: new Date(1627503992000) }");
            var locator = Page.Locator("div");
            var error = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo", new { a = 1, b = "string", c = DateTime.UnixEpoch.AddMilliseconds(1627503992001) }, new() { Timeout = 1000 }));
            // .NET: the message has the two objects on the Expected and the Received lines, where upstream prints a diff of them.
            Assert.That(error.Message, Does.Contain("Expected: {\"a\": 1, \"b\": \"string\", \"c\": 2021-07-28T20:26:32.001Z}"));
        }

        [UpstreamTest("EX-190 pass string")]
        public async Task PassString()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = 'string'");
            var locator = Page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo", "string");
        }

        [UpstreamTest("EX-191 fail string")]
        public async Task FailString()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = 'string'");
            var locator = Page.Locator("div");
            var error = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo", "error", new() { Timeout = 200 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveJSPropertyAsync(expected) failed

Locator:  Locator(""div"")
Expected: ""error""
Received: ""string""
Timeout:  200ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"ToHaveJSPropertyAsync\" Locator(\"div\") with timeout 200ms"));
        }

        [UpstreamTest("EX-192 pass number")]
        public async Task PassNumber()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = 2021");
            var locator = Page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo", 2021);
        }

        [UpstreamTest("EX-193 fail number")]
        public async Task FailNumber()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = 2021");
            var locator = Page.Locator("div");
            var error = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo", 1, new() { Timeout = 200 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveJSPropertyAsync(expected) failed

Locator:  Locator(""div"")
Expected: 1
Received: 2021
Timeout:  200ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"ToHaveJSPropertyAsync\" Locator(\"div\") with timeout 200ms"));
        }

        [UpstreamTest("EX-194 pass boolean")]
        public async Task PassBoolean()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = true");
            var locator = Page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo", true);
        }

        [UpstreamTest("EX-195 fail boolean")]
        public async Task FailBoolean()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = false");
            var locator = Page.Locator("div");
            var error = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo", true, new() { Timeout = 200 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveJSPropertyAsync(expected) failed

Locator:  Locator(""div"")
Expected: true
Received: false
Timeout:  200ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"ToHaveJSPropertyAsync\" Locator(\"div\") with timeout 200ms"));
        }

        [UpstreamTest("EX-196 pass boolean 2")]
        public async Task PassBoolean2()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = false");
            var locator = Page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo", false);
        }

        [UpstreamTest("EX-197 fail boolean 2")]
        public async Task FailBoolean2()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = false");
            var locator = Page.Locator("div");
            var error = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo", true, new() { Timeout = 200 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveJSPropertyAsync(expected) failed

Locator:  Locator(""div"")
Expected: true
Received: false
Timeout:  200ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"ToHaveJSPropertyAsync\" Locator(\"div\") with timeout 200ms"));
        }

        [UpstreamTest("EX-199 pass null")]
        public async Task PassNull()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = null");
            var locator = Page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo", null);
        }

        [UpstreamTest("EX-200 pass nested")]
        public async Task PassNested()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = { nested: { a: 1, b: 'string', c: new Date(1627503992000) } }");
            var locator = Page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo.nested", new { a = 1, b = "string", c = DateTime.UnixEpoch.AddMilliseconds(1627503992000) });
            await Expect(locator).ToHaveJSPropertyAsync("foo.nested.a", 1);
            await Expect(locator).ToHaveJSPropertyAsync("foo.nested.b", "string");
            await Expect(locator).ToHaveJSPropertyAsync("foo.nested.c", DateTime.UnixEpoch.AddMilliseconds(1627503992000));
        }

        [UpstreamTest("EX-201 fail nested")]
        public async Task FailNested()
        {
            await Page.SetContentAsync("<div></div>");
            await Page.EvalOnSelectorAsync("div", "e => e.foo = { nested: { a: 1, b: 'string', c: new Date(1627503992000) } }");
            var locator = Page.Locator("div");
            var error1 = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo.bar", new { a = 1, b = "string", c = DateTime.UnixEpoch.AddMilliseconds(1627503992001) }, new() { Timeout = 1000 }));
            // .NET: a value that is undefined in the page is null.
            Assert.That(StripAnsi(error1.Message), Does.Contain("Received: null"));
            var error2 = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo.nested.a", 2, new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error2.Message), Does.Contain("Received: 1"));
        }
    }

    public class ToHaveClass : UpstreamPageTest
    {
        [UpstreamTest("EX-202 pass")]
        public async Task Pass()
        {
            await Page.SetContentAsync("<div class=\"foo bar baz\"></div>");
            var locator = Page.Locator("div");
            await Expect(locator).ToHaveClassAsync("foo bar baz");
        }

        [UpstreamTest("EX-203 pass with SVGs")]
        public async Task PassWithSVGs()
        {
            await Page.SetContentAsync("<svg class=\"c1 c2\" role=\"img\" xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 512 512\"></svg>");
            await Expect(Page.Locator("svg")).ToHaveClassAsync(new Regex("c1"));
        }

        [UpstreamTest("EX-204 fail")]
        public async Task Fail()
        {
            await Page.SetContentAsync("<div class=\"bar baz\"></div>");
            var locator = Page.Locator("div");
            var error = await Catch(() => Expect(locator).ToHaveClassAsync("foo bar baz", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveClassAsync(expected) failed

Locator:  Locator(""div"")
Expected: ""foo bar baz""
Received: ""bar baz""
Timeout:  1000ms

Call log:
"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"ToHaveClassAsync\" Locator(\"div\") with timeout 1000ms"));
        }
    }

    public class ToContainClass : UpstreamPageTest
    {
        [UpstreamTest("EX-207 pass")]
        public async Task Pass()
        {
            await Page.SetContentAsync("<div class=\"foo bar baz\"></div>");
            var locator = Page.Locator("div");
            await Expect(locator).ToContainClassAsync("");
            await Expect(locator).ToContainClassAsync("bar");
            await Expect(locator).ToContainClassAsync("baz bar");
            await Expect(locator).ToContainClassAsync("  bar   foo ");
            await Expect(locator).Not.ToContainClassAsync("  baz   not-matching "); // Strip whitespace and match individual classes
            // .NET: left out, ToContainClassAsync takes strings only:
            // expect(() => expect(locator).toContainClass(/foo|bar/ as any)).toThrow(/"expected\" argument in toContainClass cannot be a RegExp value/);
        }

        [UpstreamTest("EX-208 pass with SVGs")]
        public async Task PassWithSVGs()
        {
            await Page.SetContentAsync("<svg class=\"c1 c2\" role=\"img\" xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 512 512\"></svg>");
            await Expect(Page.Locator("svg")).ToContainClassAsync("c1");
        }

        [UpstreamTest("EX-209 fail")]
        public async Task Fail()
        {
            await Page.SetContentAsync("<div class=\"bar baz\"></div>");
            var locator = Page.Locator("div");
            var error = await Catch(() => Expect(locator).ToContainClassAsync("does-not-exist", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToContainClassAsync(expected) failed

Locator:  Locator(""div"")
Expected: ""does-not-exist""
Received: ""bar baz""
Timeout:  1000ms

Call log:
"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"ToContainClassAsync\" Locator(\"div\") with timeout 1000ms"));
        }

        [UpstreamTest("EX-210 pass with array")]
        public async Task PassWithArray()
        {
            await Page.SetContentAsync("<div class=\"foo\"></div><div class=\"hello bar\"></div><div class=\"baz\"></div>");
            var locator = Page.Locator("div");
            await Expect(locator).ToContainClassAsync(new[] { "foo", "hello", "baz" });
            // .NET: left out, ToContainClassAsync takes strings only:
            // expect(() => expect(locator).toContainClass(['foo', 'hello', /baz/] as any)).toThrow(/"expected" argument in toContainClass cannot contain RegExp values/);
            await Expect(locator).Not.ToHaveClassAsync(new[] { "not-there", "hello", "baz" }); // Class not there
            await Expect(locator).Not.ToHaveClassAsync(new[] { "foo", "hello" }); // Length mismatch
        }

        [UpstreamTest("EX-211 fail with array")]
        public async Task FailWithArray()
        {
            await Page.SetContentAsync("<div class=\"foo\"></div><div class=\"bar\"></div><div class=\"bar\"></div>");
            var locator = Page.Locator("div");
            var error = await Catch(() => Expect(locator).ToContainClassAsync(new[] { "foo", "bar", "baz" }, new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain("Expect(locator).ToContainClassAsync(expected) failed"));
            // .NET: the message has the two arrays on the Expected and the Received lines, where upstream prints a diff of them. The Timeout line is aligned with them.
            Assert.That(StripAnsi(error.Message), Does.Contain("Timeout:  1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"ToContainClassAsync\" Locator(\"div\") with timeout 1000ms"));
        }
    }

    public class ToHaveTitle : UpstreamPageTest
    {
        [UpstreamTest("EX-212 pass")]
        public async Task Pass()
        {
            await Page.SetContentAsync("<title>  Hello     world</title>");
            await Expect(Page).ToHaveTitleAsync("Hello  world");
        }

        [UpstreamTest("EX-213 fail")]
        public async Task Fail()
        {
            await Page.SetContentAsync("<title>Bye</title>");
            var error = await Catch(() => Expect(Page).ToHaveTitleAsync("Hello", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(page).ToHaveTitleAsync(expected) failed

Expected: ""Hello""
Received: ""Bye""
Timeout:  1000ms

Call log:
"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"ToHaveTitleAsync\" with timeout 1000ms"));
        }
    }

    public class ToHaveURL : UpstreamPageTest
    {
        [UpstreamTest("EX-214 pass")]
        public async Task Pass()
        {
            await Page.GotoAsync("data:text/html,<div>A</div>");
            await Expect(Page).ToHaveURLAsync("data:text/html,<div>A</div>");
        }

        [UpstreamTest("EX-215 fail string")]
        public async Task FailString()
        {
            await Page.GotoAsync("data:text/html,<div>A</div>");
            var error = await Catch(() => Expect(Page).ToHaveURLAsync("wrong", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(page).ToHaveURLAsync(expected) failed

Expected: ""wrong""
Received: ""data:text/html,<div>A</div>""
Timeout:  1000ms

Call log:
"));
        }

        [UpstreamTest("EX-221 support ignoreCase")]
        public async Task SupportIgnoreCase()
        {
            await Page.GotoAsync("data:text/html,<div>A</div>");
            await Expect(Page).ToHaveURLAsync("DATA:teXT/HTml,<div>a</div>", new() { IgnoreCase = true });
        }
    }

    public class ToHaveAttribute : UpstreamPageTest
    {
        [UpstreamTest("EX-223 pass")]
        public async Task Pass()
        {
            await Page.SetContentAsync("<div id=node>Text content</div>");
            var locator = Page.Locator("#node");
            await Expect(locator).ToHaveAttributeAsync("id", "node");
        }

        [UpstreamTest("EX-224 should not match missing attribute")]
        public async Task ShouldNotMatchMissingAttribute()
        {
            await Page.SetContentAsync("<div checked id=node>Text content</div>");
            var locator = Page.Locator("#node");
            {
                var error = await Catch(() => Expect(locator).ToHaveAttributeAsync("disabled", "", new() { Timeout = 1000 }));
                Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveAttributeAsync(expected) failed

Locator:  Locator(""#node"")
Expected: """"
Received: serializes to the same string
Timeout:  1000ms

Call log:
"));
                Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"ToHaveAttributeAsync\" Locator(\"#node\") with timeout 1000ms"));
            }
            {
                var error = await Catch(() => Expect(locator).ToHaveAttributeAsync("disabled", new Regex(".*"), new() { Timeout = 1000 }));
                Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveAttributeAsync(expected) failed

Locator: Locator(""#node"")
Expected pattern: /.*/
Received string:  """"
Timeout: 1000ms

Call log:
"));
                Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"ToHaveAttributeAsync\" Locator(\"#node\") with timeout 1000ms"));
            }
            await Expect(locator).Not.ToHaveAttributeAsync("disabled", "");
            await Expect(locator).Not.ToHaveAttributeAsync("disabled", new Regex(".*"));
        }

        [UpstreamTest("EX-225 should match boolean attribute")]
        public async Task ShouldMatchBooleanAttribute()
        {
            await Page.SetContentAsync("<div checked id=node>Text content</div>");
            var locator = Page.Locator("#node");
            await Expect(locator).ToHaveAttributeAsync("checked", "");
            await Expect(locator).ToHaveAttributeAsync("checked", new Regex(".*"));
            {
                var error = await Catch(() => Expect(locator).Not.ToHaveAttributeAsync("checked", "", new() { Timeout = 1000 }));
                Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).Not.ToHaveAttributeAsync(expected) failed

Locator:  Locator(""#node"")
Expected: not """"
Received: """"
Timeout:  1000ms

Call log:
"));
                Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"Not.ToHaveAttributeAsync\" Locator(\"#node\") with timeout 1000ms"));
            }
            {
                var error = await Catch(() => Expect(locator).Not.ToHaveAttributeAsync("checked", new Regex(".*"), new() { Timeout = 1000 }));
                Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).Not.ToHaveAttributeAsync(expected) failed

Locator: Locator(""#node"")
Expected pattern: not /.*/
Received string: """"
Timeout: 1000ms"));
                Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"Not.ToHaveAttributeAsync\" Locator(\"#node\") with timeout 1000ms"));
            }
        }

        [UpstreamTest("EX-228 support ignoreCase")]
        public async Task SupportIgnoreCase()
        {
            await Page.SetContentAsync("<div id=NoDe>Text content</div>");
            var locator = Page.Locator("#NoDe");
            await Expect(locator).ToHaveAttributeAsync("id", "node", new() { IgnoreCase = true });
            await Expect(locator).Not.ToHaveAttributeAsync("id", "node");
        }
    }

    public class ToHaveCSS : UpstreamPageTest
    {
        [UpstreamTest("EX-229 pass")]
        public async Task Pass()
        {
            await Page.SetContentAsync("<div id=node style=\"color: rgb(255, 0, 0)\">Text content</div>");
            var locator = Page.Locator("#node");
            await Expect(locator).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        }

        [UpstreamTest("EX-230 should support pseudo element")]
        public async Task ShouldSupportPseudoElement()
        {
            await Page.SetContentAsync(@"
      <style>
        #node::before {
          color: rgb(255, 0, 0);
          content: ""Text content"";
        }
      </style>
      <div id=node></div>
    ");
            var locator = Page.Locator("#node");
            await Expect(locator).ToHaveCSSAsync("color", "rgb(255, 0, 0)", new() { Pseudo = PseudoElement.Before });
        }

        [UpstreamTest("EX-231 custom css properties")]
        public async Task CustomCssProperties()
        {
            await Page.SetContentAsync("<div id=node style=\"--custom-color-property:#FF00FF;\">Text content</div>");
            var locator = Page.Locator("#node");
            await Expect(locator).ToHaveCSSAsync("--custom-color-property", "#FF00FF");
        }
    }

    public class ToHaveId : UpstreamPageTest
    {
        [UpstreamTest("EX-232 pass")]
        public async Task Pass()
        {
            await Page.SetContentAsync("<div id=node>Text content</div>");
            var locator = Page.Locator("#node");
            await Expect(locator).ToHaveIdAsync("node");
        }
    }

    public class ToBeInViewport : UpstreamPageTest
    {
        [UpstreamTest("EX-233 should work")]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync(@"
      <div id=big style=""height: 10000px;""></div>
      <div id=small>foo</div>
    ");
            await Expect(Page.Locator("#big")).ToBeInViewportAsync();
            await Expect(Page.Locator("#small")).Not.ToBeInViewportAsync();
            await Page.Locator("#small").ScrollIntoViewIfNeededAsync();
            await Expect(Page.Locator("#small")).ToBeInViewportAsync();
            await Expect(Page.Locator("#small")).ToBeInViewportAsync(new() { Ratio = 1 });
        }

        [UpstreamTest("EX-234 should respect ratio option")]
        public async Task ShouldRespectRatioOption()
        {
            Info.Fixme(IsAndroid, "ratio 0.24 is not in viewport for unknown reason");

            await Page.SetContentAsync(@"
      <style>body, div, html { padding: 0; margin: 0; }</style>
      <div id=big style=""height: 400vh;""></div>
    ");
            await Expect(Page.Locator("div")).ToBeInViewportAsync();
            await Expect(Page.Locator("div")).ToBeInViewportAsync(new() { Ratio = 0.1f });
            await Expect(Page.Locator("div")).ToBeInViewportAsync(new() { Ratio = 0.2f });

            await Expect(Page.Locator("div")).ToBeInViewportAsync(new() { Ratio = 0.24f });
            // In this test, element's ratio is 0.25.
            await Expect(Page.Locator("div")).ToBeInViewportAsync(new() { Ratio = 0.25f });
            await Expect(Page.Locator("div")).Not.ToBeInViewportAsync(new() { Ratio = 0.26f });

            await Expect(Page.Locator("div")).Not.ToBeInViewportAsync(new() { Ratio = 0.3f });
            await Expect(Page.Locator("div")).Not.ToBeInViewportAsync(new() { Ratio = 0.7f });
            await Expect(Page.Locator("div")).Not.ToBeInViewportAsync(new() { Ratio = 0.8f });
        }

        [UpstreamTest("EX-235 should have good stack")]
        public async Task ShouldHaveGoodStack()
        {
            Exception error = null;
            try
            {
                await Expect(Page.Locator("body")).Not.ToBeInViewportAsync(new() { Timeout = 3000 });
            }
            catch (Exception e)
            {
                error = e;
            }
            Assert.That(error, Is.Not.Null);
            // .NET: error.stack, the message and the frames, is error.ToString().
            Assert.That(new Regex("unexpected value \"viewport ratio \\d+").IsMatch(error.ToString()), Is.EqualTo(true));
            // .NET: left out, a .NET stack trace has the frames of the library:
            // const stackFrames = error.stack.split('\n').filter(line => line.trim().startsWith('at '));
            // expect(stackFrames.length).toBe(1);
            // expect(stackFrames[0]).toContain(__filename);
        }

        [UpstreamTest("EX-236 should report intersection even if fully covered by other element")]
        public async Task ShouldReportIntersectionEvenIfFullyCoveredByOtherElement()
        {
            await Page.SetContentAsync(@"
      <h1>hello</h1>
      <div style=""position: relative; height: 10000px; top: -5000px;></div>
    ");
            await Expect(Page.Locator("h1")).ToBeInViewportAsync();
        }
    }

    [UpstreamTest("EX-237 toHaveCount should not produce logs twice")]
    public async Task ToHaveCountShouldNotProduceLogsTwice()
    {
        await Page.SetContentAsync("<select><option>One</option></select>");
        var error = await Catch(() => Expect(Page.Locator("option")).ToHaveCountAsync(2, new() { Timeout = 2000 }));
        var waitingForMessage = "waiting for Locator(\"option\")";
        Assert.That(error.Message, Does.Contain(waitingForMessage));
        Assert.That(error.Message, Does.Contain("locator resolved to 1 element"));
        Assert.That(error.Message, Does.Contain("unexpected value \"1\""));
        Assert.That(ReplaceFirst(error.Message, waitingForMessage, "<redacted>"), Does.Not.Contain(waitingForMessage));
    }

    [UpstreamTest("EX-238 toHaveText should not produce logs twice")]
    public async Task ToHaveTextShouldNotProduceLogsTwice()
    {
        await Page.SetContentAsync("<div>hello</div>");
        var error = await Catch(() => Expect(Page.Locator("div")).ToHaveTextAsync("world", new() { Timeout = 2000 }));
        var waitingForMessage = "waiting for Locator(\"div\")";
        Assert.That(error.Message, Does.Contain(waitingForMessage));
        Assert.That(error.Message, Does.Contain("locator resolved to <div>hello</div>"));
        Assert.That(error.Message, Does.Contain("unexpected value \"hello\""));
        Assert.That(ReplaceFirst(error.Message, waitingForMessage, "<redacted>"), Does.Not.Contain(waitingForMessage));
    }

    [UpstreamTest("EX-239 toHaveText that does not match should not produce logs twice")]
    public async Task ToHaveTextThatDoesNotMatchShouldNotProduceLogsTwice()
    {
        await Page.SetContentAsync("<div>hello</div>");
        var error = await Catch(() => Expect(Page.Locator("span")).ToHaveTextAsync("world", new() { Timeout = 2000 }));
        var waitingForMessage = "waiting for Locator(\"span\")";
        Assert.That(error.Message, Does.Contain(waitingForMessage));
        Assert.That(error.Message, Does.Not.Contain("locator resolved to"));
        Assert.That(ReplaceFirst(error.Message, waitingForMessage, "<redacted>"), Does.Not.Contain(waitingForMessage));
    }

    [UpstreamTest("EX-240 strict mode violation error format")]
    public async Task StrictModeViolationErrorFormat()
    {
        await Page.SetContentAsync("<div>a</div><div>b</div>");
        var error = await Catch(() => Expect(Page.Locator("div")).ToHaveTextAsync("foo"));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToHaveTextAsync(expected) failed

Locator: Locator(""div"")
Expected: ""foo""
Error: strict mode violation: Locator(""div"") resolved to 2 elements:
    1) <div>a</div> aka GetByText(""a"")
    2) <div>b</div> aka GetByText(""b"")

Call log:
"));
    }

    [UpstreamTest("EX-241 invalid selector error format")]
    public async Task InvalidSelectorErrorFormat()
    {
        await Page.SetContentAsync("<div>a</div><div>b</div>");
        var error = await Catch(() => Expect(Page.Locator("##")).ToBeVisibleAsync());
        Assert.That(StripAnsi(error.Message), Does.Contain(@"Expect(locator).ToBeVisibleAsync() failed

Locator: ##
Expected: visible
Error: Unexpected token ""#"" while parsing css selector ""##"". Did you mean to CSS.escape it?

Call log:
"));
    }

    [UpstreamTest("EX-242 should report expect error details when page closes during expect")]
    public async Task ShouldReportExpectErrorDetailsWhenPageClosesDuringExpect()
    {
        await Page.SetContentAsync("<div>hello</div>");
        var promise = Catch(() => Expect(Page.Locator("div")).ToHaveTextAsync("world", new() { Timeout = 10000 }));
        await Page.WaitForTimeoutAsync(1000);
        await Page.CloseAsync();
        var error = await promise;
        Assert.That(StripAnsi(error.Message), Does.Contain("Expect(locator).ToHaveTextAsync(expected) failed"));
        Assert.That(StripAnsi(error.Message), Does.Contain("Expected: \"world\""));
        Assert.That(StripAnsi(error.Message), Does.Contain("Received: \"hello\""));
    }

    [UpstreamTest("EX-243 should report page close reason when page closes during expect")]
    public async Task ShouldReportPageCloseReasonWhenPageClosesDuringExpect()
    {
        await Page.SetContentAsync("<div>hello</div>");
        var promise = Catch(() => Expect(Page.Locator("non-existent")).ToHaveTextAsync("world", new() { Timeout = 10000 }));
        await Page.CloseAsync(new() { Reason = "Bye bye." });
        var error = await promise;
        Assert.That(StripAnsi(error.Message), Does.Contain("Expect(locator).ToHaveTextAsync(expected) failed"));
        Assert.That(StripAnsi(error.Message), Does.Contain("Bye bye."));
        Assert.That(StripAnsi(error.Message), Does.Not.Contain("Internal server error"));
    }
}
