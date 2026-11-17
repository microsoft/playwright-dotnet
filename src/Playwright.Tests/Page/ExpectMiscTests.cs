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

/// <summary>Port of tests/page/expect-misc.spec.ts.</summary>
public class ExpectMiscTests
{
    public class ToHaveCount
    {
        [UpstreamTest("EX-181 toHaveCount pass")]
        public async Task ToHaveCountPass(IPage page)
        {
            await page.SetContentAsync("<select><option>One</option></select>");
            var locator = page.Locator("option");
            var done = false;
            // .NET: expect(locator).toHaveCount(2).then(() => { done = true; }) is a local function that is called in its place.
            async Task Then()
            {
                await Expect(locator).ToHaveCountAsync(2);
                done = true;
            }
            var promise = Then();
            await page.WaitForTimeoutAsync(1000);
            Assert.That(done, Is.EqualTo(false));
            await page.SetContentAsync("<select><option>One</option><option>Two</option></select>");
            await promise;
            Assert.That(done, Is.EqualTo(true));
        }

        [UpstreamTest("EX-182 pass zero")]
        public async Task PassZero(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            var locator = page.Locator("span");
            await Expect(locator).ToHaveCountAsync(0);
            await Expect(locator).Not.ToHaveCountAsync(1);
        }

        [UpstreamTest("EX-183 eventually pass zero")]
        public async Task EventuallyPassZero(IPage page)
        {
            await page.SetContentAsync("<div><span>hello</span></div>");
            var locator = page.Locator("span");
            SetTimeout(() => IgnoreErrors(page.EvaluateAsync("() => document.querySelector('div').textContent = ''")), 200);
            await Expect(locator).ToHaveCountAsync(0);
            await Expect(locator).Not.ToHaveCountAsync(1);
        }

        [UpstreamTest("EX-184 eventually pass non-zero")]
        public async Task EventuallyPassNonZero(IPage page)
        {
            await page.SetContentAsync("<ul></ul>");
            SetTimeout(async () =>
            {
                await page.SetContentAsync("<ul><li>one</li><li>two</li></ul>");
            }, 500);
            var locator = page.Locator("li");
            await Expect(locator).ToHaveCountAsync(2);
        }

        [UpstreamTest("EX-185 eventually pass not non-zero")]
        public async Task EventuallyPassNotNonZero(IPage page)
        {
            await page.SetContentAsync("<ul><li>one</li><li>two</li></ul>");
            SetTimeout(async () =>
            {
                await page.SetContentAsync("<ul></ul>");
            }, 500);
            var locator = page.Locator("li");
            await Expect(locator).Not.ToHaveCountAsync(2);
        }

        [UpstreamTest("EX-186 fail zero")]
        [Fixme("dotnet: the message of ToHaveCountAsync is \"Locator expected to have count '0'\\nBut was: '1' \\nCall log: ...\"")]
        public async Task FailZero(IPage page)
        {
            await page.SetContentAsync("<div><span></span></div>");
            var locator = page.Locator("span");
            var error = await Catch(() => Expect(locator).ToHaveCountAsync(0, new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveCount(expected) failed

Locator:  locator('span')
Expected: 0
Received: 1
Timeout:  1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toHaveCount\" locator('span') with timeout 1000ms"));
        }

        [UpstreamTest("EX-187 fail zero 2")]
        [Fixme("dotnet: the message of Not.ToHaveCountAsync is \"Locator expected not to have count '1'\\nBut was: '1' \\nCall log: ...\"")]
        public async Task FailZero2(IPage page)
        {
            await page.SetContentAsync("<div><span></span></div>");
            var locator = page.Locator("span");
            var error = await Catch(() => Expect(locator).Not.ToHaveCountAsync(1, new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).not.toHaveCount(expected) failed

Locator:  locator('span')
Expected: not 1
Received: 1
Timeout:  1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"not toHaveCount\" locator('span') with timeout 1000ms"));
        }
    }

    public class ToHaveJSProperty
    {
        [UpstreamTest("EX-188 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = { a: 1, b: 'string', c: new Date(1627503992000) }");
            var locator = page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo", new { a = 1, b = "string", c = DateTime.UnixEpoch.AddMilliseconds(1627503992000) });
        }

        [UpstreamTest("EX-189 fail")]
        [Fixme("dotnet: the message of ToHaveJSPropertyAsync is \"Locator expected to have JavaScript property 'foo' '{ a = 1, b = string, c = <date> }'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\", without a diff of the two objects")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = { a: 1, b: 'string', c: new Date(1627503992000) }");
            var locator = page.Locator("div");
            var error = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo", new { a = 1, b = "string", c = DateTime.UnixEpoch.AddMilliseconds(1627503992001) }, new() { Timeout = 1000 }));
            Assert.That(error.Message, Does.Contain("-   \"c\""));
        }

        [UpstreamTest("EX-190 pass string")]
        public async Task PassString(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = 'string'");
            var locator = page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo", "string");
        }

        [UpstreamTest("EX-191 fail string")]
        [Fixme("dotnet: the message of ToHaveJSPropertyAsync is \"Locator expected to have JavaScript property 'foo' 'error'\\nBut was: 'string' \\nCall log: ...\"")]
        public async Task FailString(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = 'string'");
            var locator = page.Locator("div");
            var error = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo", "error", new() { Timeout = 200 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveJSProperty(expected) failed

Locator:  locator('div')
Expected: ""error""
Received: ""string""
Timeout:  200ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toHaveJSProperty\" locator('div') with timeout 200ms"));
        }

        [UpstreamTest("EX-192 pass number")]
        public async Task PassNumber(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = 2021");
            var locator = page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo", 2021);
        }

        [UpstreamTest("EX-193 fail number")]
        [Fixme("dotnet: the message of ToHaveJSPropertyAsync is \"Locator expected to have JavaScript property 'foo' '1'\\nBut was: '2021' \\nCall log: ...\"")]
        public async Task FailNumber(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = 2021");
            var locator = page.Locator("div");
            var error = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo", 1, new() { Timeout = 200 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveJSProperty(expected) failed

Locator:  locator('div')
Expected: 1
Received: 2021
Timeout:  200ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toHaveJSProperty\" locator('div') with timeout 200ms"));
        }

        [UpstreamTest("EX-194 pass boolean")]
        public async Task PassBoolean(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = true");
            var locator = page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo", true);
        }

        [UpstreamTest("EX-195 fail boolean")]
        [Fixme("dotnet: the message of ToHaveJSPropertyAsync is \"Locator expected to have JavaScript property 'foo' 'True'\\nBut was: 'False' \\nCall log: ...\"")]
        public async Task FailBoolean(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = false");
            var locator = page.Locator("div");
            var error = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo", true, new() { Timeout = 200 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveJSProperty(expected) failed

Locator:  locator('div')
Expected: true
Received: false
Timeout:  200ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toHaveJSProperty\" locator('div') with timeout 200ms"));
        }

        [UpstreamTest("EX-196 pass boolean 2")]
        public async Task PassBoolean2(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = false");
            var locator = page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo", false);
        }

        [UpstreamTest("EX-197 fail boolean 2")]
        [Fixme("dotnet: the message of ToHaveJSPropertyAsync is \"Locator expected to have JavaScript property 'foo' 'True'\\nBut was: 'False' \\nCall log: ...\"")]
        public async Task FailBoolean2(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = false");
            var locator = page.Locator("div");
            var error = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo", true, new() { Timeout = 200 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveJSProperty(expected) failed

Locator:  locator('div')
Expected: true
Received: false
Timeout:  200ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toHaveJSProperty\" locator('div') with timeout 200ms"));
        }

        [UpstreamTest("EX-199 pass null")]
        public async Task PassNull(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = null");
            var locator = page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo", null);
        }

        [UpstreamTest("EX-200 pass nested")]
        public async Task PassNested(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = { nested: { a: 1, b: 'string', c: new Date(1627503992000) } }");
            var locator = page.Locator("div");
            await Expect(locator).ToHaveJSPropertyAsync("foo.nested", new { a = 1, b = "string", c = DateTime.UnixEpoch.AddMilliseconds(1627503992000) });
            await Expect(locator).ToHaveJSPropertyAsync("foo.nested.a", 1);
            await Expect(locator).ToHaveJSPropertyAsync("foo.nested.b", "string");
            await Expect(locator).ToHaveJSPropertyAsync("foo.nested.c", DateTime.UnixEpoch.AddMilliseconds(1627503992000));
        }

        [UpstreamTest("EX-201 fail nested")]
        [Fixme("dotnet: the message of ToHaveJSPropertyAsync is \"Locator expected to have JavaScript property 'foo.bar' '{ a = 1, b = string, c = <date> }'\\nBut was: 'null' \\nCall log: ...\", upstream expects 'Received: undefined'")]
        public async Task FailNested(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            await page.EvalOnSelectorAsync("div", "e => e.foo = { nested: { a: 1, b: 'string', c: new Date(1627503992000) } }");
            var locator = page.Locator("div");
            var error1 = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo.bar", new { a = 1, b = "string", c = DateTime.UnixEpoch.AddMilliseconds(1627503992001) }, new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error1.Message), Does.Contain("Received: undefined"));
            var error2 = await Catch(() => Expect(locator).ToHaveJSPropertyAsync("foo.nested.a", 2, new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error2.Message), Does.Contain("Received: 1"));
        }
    }

    public class ToHaveClass
    {
        [UpstreamTest("EX-202 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div class=\"foo bar baz\"></div>");
            var locator = page.Locator("div");
            await Expect(locator).ToHaveClassAsync("foo bar baz");
        }

        [UpstreamTest("EX-203 pass with SVGs")]
        public async Task PassWithSVGs(IPage page)
        {
            await page.SetContentAsync("<svg class=\"c1 c2\" role=\"img\" xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 512 512\"></svg>");
            await Expect(page.Locator("svg")).ToHaveClassAsync(new Regex("c1"));
        }

        [UpstreamTest("EX-204 fail")]
        [Fixme("dotnet: the message of ToHaveClassAsync is \"Locator expected to have class 'foo bar baz'\\nBut was: 'bar baz' \\nCall log: ...\"")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<div class=\"bar baz\"></div>");
            var locator = page.Locator("div");
            var error = await Catch(() => Expect(locator).ToHaveClassAsync("foo bar baz", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveClass(expected) failed

Locator:  locator('div')
Expected: ""foo bar baz""
Received: ""bar baz""
Timeout:  1000ms

Call log:
"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toHaveClass\" locator('div') with timeout 1000ms"));
        }
    }

    public class ToContainClass
    {
        [UpstreamTest("EX-207 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div class=\"foo bar baz\"></div>");
            var locator = page.Locator("div");
            await Expect(locator).ToContainClassAsync("");
            await Expect(locator).ToContainClassAsync("bar");
            await Expect(locator).ToContainClassAsync("baz bar");
            await Expect(locator).ToContainClassAsync("  bar   foo ");
            await Expect(locator).Not.ToContainClassAsync("  baz   not-matching "); // Strip whitespace and match individual classes
            // .NET: left out, ToContainClassAsync takes strings only:
            // expect(() => expect(locator).toContainClass(/foo|bar/ as any)).toThrow(/"expected\" argument in toContainClass cannot be a RegExp value/);
        }

        [UpstreamTest("EX-208 pass with SVGs")]
        public async Task PassWithSVGs(IPage page)
        {
            await page.SetContentAsync("<svg class=\"c1 c2\" role=\"img\" xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 512 512\"></svg>");
            await Expect(page.Locator("svg")).ToContainClassAsync("c1");
        }

        [UpstreamTest("EX-209 fail")]
        [Fixme("dotnet: the message of ToContainClassAsync is \"Locator expected to contain class names 'does-not-exist'\\nBut was: 'bar baz' \\nCall log: ...\"")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<div class=\"bar baz\"></div>");
            var locator = page.Locator("div");
            var error = await Catch(() => Expect(locator).ToContainClassAsync("does-not-exist", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toContainClass(expected) failed

Locator:  locator('div')
Expected: ""does-not-exist""
Received: ""bar baz""
Timeout:  1000ms

Call log:
"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toContainClass\" locator('div') with timeout 1000ms"));
        }

        [UpstreamTest("EX-210 pass with array")]
        public async Task PassWithArray(IPage page)
        {
            await page.SetContentAsync("<div class=\"foo\"></div><div class=\"hello bar\"></div><div class=\"baz\"></div>");
            var locator = page.Locator("div");
            await Expect(locator).ToContainClassAsync(new[] { "foo", "hello", "baz" });
            // .NET: left out, ToContainClassAsync takes strings only:
            // expect(() => expect(locator).toContainClass(['foo', 'hello', /baz/] as any)).toThrow(/"expected" argument in toContainClass cannot contain RegExp values/);
            await Expect(locator).Not.ToHaveClassAsync(new[] { "not-there", "hello", "baz" }); // Class not there
            await Expect(locator).Not.ToHaveClassAsync(new[] { "foo", "hello" }); // Length mismatch
        }

        [UpstreamTest("EX-211 fail with array")]
        [Fixme("dotnet: the message of ToContainClassAsync is \"Locator expected to contain class names '['foo', 'bar', 'baz']'\\nBut was: '['foo', 'bar', 'bar']' \\nCall log: ...\"")]
        public async Task FailWithArray(IPage page)
        {
            await page.SetContentAsync("<div class=\"foo\"></div><div class=\"bar\"></div><div class=\"bar\"></div>");
            var locator = page.Locator("div");
            var error = await Catch(() => Expect(locator).ToContainClassAsync(new[] { "foo", "bar", "baz" }, new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain("expect(locator).toContainClass(expected) failed"));
            Assert.That(StripAnsi(error.Message), Does.Contain("Timeout: 1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toContainClass\" locator('div') with timeout 1000ms"));
        }
    }

    public class ToHaveTitle
    {
        [UpstreamTest("EX-212 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<title>  Hello     world</title>");
            await Expect(page).ToHaveTitleAsync("Hello  world");
        }

        [UpstreamTest("EX-213 fail")]
        [Fixme("dotnet: the message of ToHaveTitleAsync is \"Page title expected to be 'Hello'\\nBut was: 'Bye' \\nCall log: ...\"")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<title>Bye</title>");
            var error = await Catch(() => Expect(page).ToHaveTitleAsync("Hello", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(page).toHaveTitle(expected) failed

Expected: ""Hello""
Received: ""Bye""
Timeout:  1000ms

Call log:
"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toHaveTitle\" with timeout 1000ms"));
        }
    }

    public class ToHaveURL
    {
        [UpstreamTest("EX-214 pass")]
        public async Task Pass(IPage page)
        {
            await page.GotoAsync("data:text/html,<div>A</div>");
            await Expect(page).ToHaveURLAsync("data:text/html,<div>A</div>");
        }

        [UpstreamTest("EX-215 fail string")]
        [Fixme("dotnet: the message of ToHaveURLAsync is \"Page URL expected to be 'wrong'\\nBut was: 'data:text/html,<div>A</div>' \\nCall log: ...\"")]
        public async Task FailString(IPage page)
        {
            await page.GotoAsync("data:text/html,<div>A</div>");
            var error = await Catch(() => Expect(page).ToHaveURLAsync("wrong", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(page).toHaveURL(expected) failed

Expected: ""wrong""
Received: ""data:text/html,<div>A</div>""
Timeout:  1000ms

Call log:
"));
        }

        [UpstreamTest("EX-221 support ignoreCase")]
        public async Task SupportIgnoreCase(IPage page)
        {
            await page.GotoAsync("data:text/html,<div>A</div>");
            await Expect(page).ToHaveURLAsync("DATA:teXT/HTml,<div>a</div>", new() { IgnoreCase = true });
        }
    }

    public class ToHaveAttribute
    {
        [UpstreamTest("EX-223 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            var locator = page.Locator("#node");
            await Expect(locator).ToHaveAttributeAsync("id", "node");
        }

        [UpstreamTest("EX-224 should not match missing attribute")]
        [Fixme("dotnet: the message of ToHaveAttributeAsync is \"Locator expected to have attribute 'disabled' ''\\nBut was: 'null' \\nCall log: ...\"")]
        public async Task ShouldNotMatchMissingAttribute(IPage page)
        {
            await page.SetContentAsync("<div checked id=node>Text content</div>");
            var locator = page.Locator("#node");
            {
                var error = await Catch(() => Expect(locator).ToHaveAttributeAsync("disabled", "", new() { Timeout = 1000 }));
                Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveAttribute(expected) failed

Locator:  locator('#node')
Expected: """"
Received: serializes to the same string
Timeout:  1000ms

Call log:
"));
                Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toHaveAttribute\" locator('#node') with timeout 1000ms"));
            }
            {
                var error = await Catch(() => Expect(locator).ToHaveAttributeAsync("disabled", new Regex(".*"), new() { Timeout = 1000 }));
                Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveAttribute(expected) failed

Locator: locator('#node')
Expected pattern: /.*/
Received string:  """"
Timeout: 1000ms

Call log:
"));
                Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toHaveAttribute\" locator('#node') with timeout 1000ms"));
            }
            await Expect(locator).Not.ToHaveAttributeAsync("disabled", "");
            await Expect(locator).Not.ToHaveAttributeAsync("disabled", new Regex(".*"));
        }

        [UpstreamTest("EX-225 should match boolean attribute")]
        [Fixme("dotnet: the message of Not.ToHaveAttributeAsync is \"Locator expected not to have attribute 'checked' ''\\nBut was: '' \\nCall log: ...\"")]
        public async Task ShouldMatchBooleanAttribute(IPage page)
        {
            await page.SetContentAsync("<div checked id=node>Text content</div>");
            var locator = page.Locator("#node");
            await Expect(locator).ToHaveAttributeAsync("checked", "");
            await Expect(locator).ToHaveAttributeAsync("checked", new Regex(".*"));
            {
                var error = await Catch(() => Expect(locator).Not.ToHaveAttributeAsync("checked", "", new() { Timeout = 1000 }));
                Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).not.toHaveAttribute(expected) failed

Locator:  locator('#node')
Expected: not """"
Received: """"
Timeout:  1000ms

Call log:
"));
                Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"not toHaveAttribute\" locator('#node') with timeout 1000ms"));
            }
            {
                var error = await Catch(() => Expect(locator).Not.ToHaveAttributeAsync("checked", new Regex(".*"), new() { Timeout = 1000 }));
                Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).not.toHaveAttribute(expected) failed

Locator: locator('#node')
Expected pattern: not /.*/
Received string: """"
Timeout: 1000ms"));
                Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"not toHaveAttribute\" locator('#node') with timeout 1000ms"));
            }
        }

        [UpstreamTest("EX-228 support ignoreCase")]
        public async Task SupportIgnoreCase(IPage page)
        {
            await page.SetContentAsync("<div id=NoDe>Text content</div>");
            var locator = page.Locator("#NoDe");
            await Expect(locator).ToHaveAttributeAsync("id", "node", new() { IgnoreCase = true });
            await Expect(locator).Not.ToHaveAttributeAsync("id", "node");
        }
    }

    public class ToHaveCSS
    {
        [UpstreamTest("EX-229 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div id=node style=\"color: rgb(255, 0, 0)\">Text content</div>");
            var locator = page.Locator("#node");
            await Expect(locator).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        }

        [UpstreamTest("EX-230 should support pseudo element")]
        public async Task ShouldSupportPseudoElement(IPage page)
        {
            await page.SetContentAsync(@"
      <style>
        #node::before {
          color: rgb(255, 0, 0);
          content: ""Text content"";
        }
      </style>
      <div id=node></div>
    ");
            var locator = page.Locator("#node");
            await Expect(locator).ToHaveCSSAsync("color", "rgb(255, 0, 0)", new() { Pseudo = PseudoElement.Before });
        }

        [UpstreamTest("EX-231 custom css properties")]
        public async Task CustomCssProperties(IPage page)
        {
            await page.SetContentAsync("<div id=node style=\"--custom-color-property:#FF00FF;\">Text content</div>");
            var locator = page.Locator("#node");
            await Expect(locator).ToHaveCSSAsync("--custom-color-property", "#FF00FF");
        }
    }

    public class ToHaveId
    {
        [UpstreamTest("EX-232 pass")]
        public async Task Pass(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            var locator = page.Locator("#node");
            await Expect(locator).ToHaveIdAsync("node");
        }
    }

    public class ToBeInViewport
    {
        [UpstreamTest("EX-233 should work")]
        [Fixme("dotnet: Locator.WithElementAsync makes a negative timeout from the default timeout 0, and the call passes only when it ends before the timer of the driver")]
        public async Task ShouldWork(IPage page)
        {
            await page.SetContentAsync(@"
      <div id=big style=""height: 10000px;""></div>
      <div id=small>foo</div>
    ");
            await Expect(page.Locator("#big")).ToBeInViewportAsync();
            await Expect(page.Locator("#small")).Not.ToBeInViewportAsync();
            await page.Locator("#small").ScrollIntoViewIfNeededAsync();
            await Expect(page.Locator("#small")).ToBeInViewportAsync();
            await Expect(page.Locator("#small")).ToBeInViewportAsync(new() { Ratio = 1 });
        }

        [UpstreamTest("EX-234 should respect ratio option")]
        public async Task ShouldRespectRatioOption(IPage page, bool isAndroid)
        {
            It.Fixme(isAndroid, "ratio 0.24 is not in viewport for unknown reason");

            await page.SetContentAsync(@"
      <style>body, div, html { padding: 0; margin: 0; }</style>
      <div id=big style=""height: 400vh;""></div>
    ");
            await Expect(page.Locator("div")).ToBeInViewportAsync();
            await Expect(page.Locator("div")).ToBeInViewportAsync(new() { Ratio = 0.1f });
            await Expect(page.Locator("div")).ToBeInViewportAsync(new() { Ratio = 0.2f });

            await Expect(page.Locator("div")).ToBeInViewportAsync(new() { Ratio = 0.24f });
            // In this test, element's ratio is 0.25.
            await Expect(page.Locator("div")).ToBeInViewportAsync(new() { Ratio = 0.25f });
            await Expect(page.Locator("div")).Not.ToBeInViewportAsync(new() { Ratio = 0.26f });

            await Expect(page.Locator("div")).Not.ToBeInViewportAsync(new() { Ratio = 0.3f });
            await Expect(page.Locator("div")).Not.ToBeInViewportAsync(new() { Ratio = 0.7f });
            await Expect(page.Locator("div")).Not.ToBeInViewportAsync(new() { Ratio = 0.8f });
        }

        [UpstreamTest("EX-235 should have good stack")]
        public async Task ShouldHaveGoodStack(IPage page)
        {
            Exception error = null;
            try
            {
                await Expect(page.Locator("body")).Not.ToBeInViewportAsync(new() { Timeout = 3000 });
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
        public async Task ShouldReportIntersectionEvenIfFullyCoveredByOtherElement(IPage page)
        {
            await page.SetContentAsync(@"
      <h1>hello</h1>
      <div style=""position: relative; height: 10000px; top: -5000px;></div>
    ");
            await Expect(page.Locator("h1")).ToBeInViewportAsync();
        }
    }

    [UpstreamTest("EX-237 toHaveCount should not produce logs twice")]
    [Fixme("dotnet: the call log has Locator(\"option\"), upstream expects locator('option')")]
    public async Task ToHaveCountShouldNotProduceLogsTwice(IPage page)
    {
        await page.SetContentAsync("<select><option>One</option></select>");
        var error = await Catch(() => Expect(page.Locator("option")).ToHaveCountAsync(2, new() { Timeout = 2000 }));
        var waitingForMessage = "waiting for locator('option')";
        Assert.That(error.Message, Does.Contain(waitingForMessage));
        Assert.That(error.Message, Does.Contain("locator resolved to 1 element"));
        Assert.That(error.Message, Does.Contain("unexpected value \"1\""));
        Assert.That(ReplaceFirst(error.Message, waitingForMessage, "<redacted>"), Does.Not.Contain(waitingForMessage));
    }

    [UpstreamTest("EX-238 toHaveText should not produce logs twice")]
    [Fixme("dotnet: the call log has Locator(\"div\"), upstream expects locator('div')")]
    public async Task ToHaveTextShouldNotProduceLogsTwice(IPage page)
    {
        await page.SetContentAsync("<div>hello</div>");
        var error = await Catch(() => Expect(page.Locator("div")).ToHaveTextAsync("world", new() { Timeout = 2000 }));
        var waitingForMessage = "waiting for locator('div')";
        Assert.That(error.Message, Does.Contain(waitingForMessage));
        Assert.That(error.Message, Does.Contain("locator resolved to <div>hello</div>"));
        Assert.That(error.Message, Does.Contain("unexpected value \"hello\""));
        Assert.That(ReplaceFirst(error.Message, waitingForMessage, "<redacted>"), Does.Not.Contain(waitingForMessage));
    }

    [UpstreamTest("EX-239 toHaveText that does not match should not produce logs twice")]
    [Fixme("dotnet: the call log has Locator(\"span\"), upstream expects locator('span')")]
    public async Task ToHaveTextThatDoesNotMatchShouldNotProduceLogsTwice(IPage page)
    {
        await page.SetContentAsync("<div>hello</div>");
        var error = await Catch(() => Expect(page.Locator("span")).ToHaveTextAsync("world", new() { Timeout = 2000 }));
        var waitingForMessage = "waiting for locator('span')";
        Assert.That(error.Message, Does.Contain(waitingForMessage));
        Assert.That(error.Message, Does.Not.Contain("locator resolved to"));
        Assert.That(ReplaceFirst(error.Message, waitingForMessage, "<redacted>"), Does.Not.Contain(waitingForMessage));
    }

    [UpstreamTest("EX-240 strict mode violation error format")]
    [Fixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text\\nError: strict mode violation: Locator(\"div\") resolved to 2 elements:\\n    1) <div>a</div> aka GetByText(\"a\")\\n    2) <div>b</div> aka GetByText(\"b\")\\n 'foo'\\nBut was: 'null' \\nCall log: ...\"")]
    public async Task StrictModeViolationErrorFormat(IPage page)
    {
        await page.SetContentAsync("<div>a</div><div>b</div>");
        var error = await Catch(() => Expect(page.Locator("div")).ToHaveTextAsync("foo"));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toHaveText(expected) failed

Locator: locator('div')
Expected: ""foo""
Error: strict mode violation: locator('div') resolved to 2 elements:
    1) <div>a</div> aka getByText('a')
    2) <div>b</div> aka getByText('b')

Call log:
"));
    }

    [UpstreamTest("EX-241 invalid selector error format")]
    [Fixme("dotnet: the message of ToBeVisibleAsync is \"Locator expected to be visible\\nError: Unexpected token \"#\" while parsing css selector \"##\". Did you mean to CSS.escape it? \\nCall log: ...\"")]
    public async Task InvalidSelectorErrorFormat(IPage page)
    {
        await page.SetContentAsync("<div>a</div><div>b</div>");
        var error = await Catch(() => Expect(page.Locator("##")).ToBeVisibleAsync());
        Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toBeVisible() failed

Locator: ##
Expected: visible
Error: Unexpected token ""#"" while parsing css selector ""##"". Did you mean to CSS.escape it?

Call log:
"));
    }

    [UpstreamTest("EX-242 should report expect error details when page closes during expect")]
    [Fixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text 'world'\\nBut was: 'hello' \\nCall log: ...\"")]
    public async Task ShouldReportExpectErrorDetailsWhenPageClosesDuringExpect(IPage page)
    {
        await page.SetContentAsync("<div>hello</div>");
        var promise = Catch(() => Expect(page.Locator("div")).ToHaveTextAsync("world", new() { Timeout = 10000 }));
        await page.WaitForTimeoutAsync(1000);
        await page.CloseAsync();
        var error = await promise;
        Assert.That(StripAnsi(error.Message), Does.Contain("expect(locator).toHaveText(expected) failed"));
        Assert.That(StripAnsi(error.Message), Does.Contain("Expected: \"world\""));
        Assert.That(StripAnsi(error.Message), Does.Contain("Received: \"hello\""));
    }

    [UpstreamTest("EX-243 should report page close reason when page closes during expect")]
    [Fixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text\\nError: Bye bye. 'world'\\nBut was: 'null' \\nCall log: ...\"")]
    public async Task ShouldReportPageCloseReasonWhenPageClosesDuringExpect(IPage page)
    {
        await page.SetContentAsync("<div>hello</div>");
        var promise = Catch(() => Expect(page.Locator("non-existent")).ToHaveTextAsync("world", new() { Timeout = 10000 }));
        await page.CloseAsync(new() { Reason = "Bye bye." });
        var error = await promise;
        Assert.That(StripAnsi(error.Message), Does.Contain("expect(locator).toHaveText(expected) failed"));
        Assert.That(StripAnsi(error.Message), Does.Contain("Bye bye."));
        Assert.That(StripAnsi(error.Message), Does.Not.Contain("Internal server error"));
    }
}
