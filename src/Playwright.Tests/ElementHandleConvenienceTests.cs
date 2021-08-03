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

using System.Threading.Tasks;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>elementhandle-convenience.spec.ts</playwright-file>
    [TestClass]
    public class ElementHandleConvenienceTests : PageTestEx
    {
        [PlaywrightTest("elementhandle-convenience.spec.ts", "should have a nice preview")]
        public async Task ShouldHaveANicePreview()
        {
            await Page.GotoAsync(Server.Prefix + "/dom.html");
            var outer = await Page.QuerySelectorAsync("#outer");
            var inner = await Page.QuerySelectorAsync("#inner");
            var check = await Page.QuerySelectorAsync("#check");
            var text = await inner.EvaluateHandleAsync("e => e.firstChild");
            await Page.EvaluateAsync("() => 1");  // Give them a chance to calculate the preview.
            Assert.AreEqual("JSHandle@<div id=\"outer\" name=\"value\">…</div>", outer.ToString());
            Assert.AreEqual("JSHandle@<div id=\"inner\">Text,↵more text</div>", inner.ToString());
            Assert.AreEqual("JSHandle@#text=Text,↵more text", text.ToString());
            Assert.AreEqual("JSHandle@<input checked id=\"check\" foo=\"bar\"\" type=\"checkbox\"/>", check.ToString());
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "getAttribute should work")]
        public async Task GetAttributeShouldWork()
        {
            await Page.GotoAsync(Server.Prefix + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#outer");

            Assert.AreEqual("value", await handle.GetAttributeAsync("name"));
            Assert.AreEqual("value", await Page.GetAttributeAsync("#outer", "name"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "innerHTML should work")]
        public async Task InnerHTMLShouldWork()
        {
            await Page.GotoAsync(Server.Prefix + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#outer");

            Assert.AreEqual("<div id=\"inner\">Text,\nmore text</div>", await handle.InnerHTMLAsync());
            Assert.AreEqual("<div id=\"inner\">Text,\nmore text</div>", await Page.InnerHTMLAsync("#outer"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "innerText should work")]
        public async Task InnerTextShouldWork()
        {
            await Page.GotoAsync(Server.Prefix + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#inner");

            Assert.AreEqual("Text, more text", await handle.InnerTextAsync());
            Assert.AreEqual("Text, more text", await Page.InnerTextAsync("#inner"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "'innerText should throw")]
        public async Task InnerTextShouldThrow()
        {
            await Page.SetContentAsync("<svg>text</svg>");
            var exception1 = await Assert.ThrowsExceptionAsync<PlaywrightException>(async () => await Page.InnerTextAsync("svg"));
            StringAssert.Contains(exception1.Message, "Not an HTMLElement");

            var handle = await Page.QuerySelectorAsync("svg");
            var exception2 = await Assert.ThrowsExceptionAsync<PlaywrightException>(async () => await handle.InnerTextAsync());
            StringAssert.Contains(exception2.Message, "Not an HTMLElement");
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "textContent should work")]
        public async Task TextContentShouldWork()
        {
            await Page.GotoAsync(Server.Prefix + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#outer");

            Assert.AreEqual("Text,\nmore text", await handle.TextContentAsync());
            Assert.AreEqual("Text,\nmore text", await Page.TextContentAsync("#outer"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "Page.dispatchEvent(click)", "textContent should be atomic")]
        public async Task TextContentShouldBeAtomic()
        {
            const string createDummySelector = @"({
                create(root, target) { },
                query(root, selector) {
                  const result = root.querySelector(selector);
                  if (result)
                    Promise.resolve().then(() => result.textContent = 'modified');
                  return result;
                },
                queryAll(root, selector) {
                  const result = Array.from(root.querySelectorAll(selector));
                  for (const e of result)
                    Promise.resolve().then(() => e.textContent = 'modified');
                  return result;
                }
            })";

            await TestUtils.RegisterEngineAsync(Playwright, "textContent", createDummySelector);
            await Page.SetContentAsync("<div>Hello</div>");
            string tc = await Page.TextContentAsync("textContent=div");
            Assert.AreEqual("Hello", tc);
            Assert.AreEqual("modified", await Page.EvaluateAsync<string>("() => document.querySelector('div').textContent"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "Page.dispatchEvent(click)", "innerText should be atomic")]
        public async Task InnerTextShouldBeAtomic()
        {
            const string createDummySelector = @"({
                create(root, target) { },
                query(root, selector) {
                  const result = root.querySelector(selector);
                  if (result)
                    Promise.resolve().then(() => result.textContent = 'modified');
                  return result;
                },
                queryAll(root, selector) {
                  const result = Array.from(root.querySelectorAll(selector));
                  for (const e of result)
                    Promise.resolve().then(() => e.textContent = 'modified');
                  return result;
                }
            })";

            await TestUtils.RegisterEngineAsync(Playwright, "innerText", createDummySelector);
            await Page.SetContentAsync("<div>Hello</div>");
            string tc = await Page.InnerTextAsync("innerText=div");
            Assert.AreEqual("Hello", tc);
            Assert.AreEqual("modified", await Page.EvaluateAsync<string>("() => document.querySelector('div').textContent"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "Page.dispatchEvent(click)", "innerHTML should be atomic")]
        public async Task InnerHtmlShouldBeAtomic()
        {
            const string createDummySelector = @"({
                create(root, target) { },
                query(root, selector) {
                  const result = root.querySelector(selector);
                  if (result)
                    Promise.resolve().then(() => result.textContent = 'modified');
                  return result;
                },
                queryAll(root, selector) {
                  const result = Array.from(root.querySelectorAll(selector));
                  for (const e of result)
                    Promise.resolve().then(() => e.textContent = 'modified');
                  return result;
                }
            })";

            await TestUtils.RegisterEngineAsync(Playwright, "innerHtml", createDummySelector);
            await Page.SetContentAsync("<div>Hello</div>");
            string tc = await Page.InnerHTMLAsync("innerHtml=div");
            Assert.AreEqual("Hello", tc);
            Assert.AreEqual("modified", await Page.EvaluateAsync<string>("() => document.querySelector('div').textContent"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "Page.dispatchEvent(click)", "getAttribute should be atomic")]
        public async Task GetAttributeShouldBeAtomic()
        {
            const string createDummySelector = @"({
                create(root, target) { },
                query(root, selector) {
                  const result = root.querySelector(selector);
                  if (result)
                    Promise.resolve().then(() => result.setAttribute('foo', 'modified'));
                  return result;
                },
                queryAll(root, selector) {
                  const result = Array.from(root.querySelectorAll(selector));
                  for (const e of result)
                    Promise.resolve().then(() => e.setAttribute('foo', 'modified'));
                  return result;
                }
            })";

            await TestUtils.RegisterEngineAsync(Playwright, "getAttribute", createDummySelector);
            await Page.SetContentAsync("<div foo=Hello></div>");
            string tc = await Page.GetAttributeAsync("getAttribute=div", "foo");
            Assert.AreEqual("Hello", tc);
            Assert.AreEqual("modified", await Page.EvaluateAsync<string>("() => document.querySelector('div').getAttribute('foo')"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "isVisible and isHidden should work")]
        public async Task IsVisibleAndIsHiddenShouldWork()
        {
            await Page.SetContentAsync("<div>Hi</div><span></span>");
            var div = await Page.QuerySelectorAsync("div");
            Assert.IsTrue(await div.IsVisibleAsync());
            Assert.IsFalse(await div.IsHiddenAsync());
            Assert.IsTrue(await Page.IsVisibleAsync("div"));
            Assert.IsFalse(await Page.IsHiddenAsync("div"));
            var span = await Page.QuerySelectorAsync("span");
            Assert.IsFalse(await span.IsVisibleAsync());
            Assert.IsTrue(await span.IsHiddenAsync());
            Assert.IsFalse(await Page.IsVisibleAsync("span"));
            Assert.IsTrue(await Page.IsHiddenAsync("span"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "isEnabled and isDisabled should work")]
        public async Task IsEnabledAndIsDisabledShouldWork()
        {
            await Page.SetContentAsync(@"
                <button disabled>button1</button>
                <button>button2</button>
                <div>div</div>");
            var div = await Page.QuerySelectorAsync("div");
            Assert.IsTrue(await div.IsEnabledAsync());
            Assert.IsFalse(await div.IsDisabledAsync());
            Assert.IsTrue(await Page.IsEnabledAsync("div"));
            Assert.IsFalse(await Page.IsDisabledAsync("div"));
            var button1 = await Page.QuerySelectorAsync(":text('button1')");
            Assert.IsFalse(await button1.IsEnabledAsync());
            Assert.IsTrue(await button1.IsDisabledAsync());
            Assert.IsFalse(await Page.IsEnabledAsync(":text('button1')"));
            Assert.IsTrue(await Page.IsDisabledAsync(":text('button1')"));
            var button2 = await Page.QuerySelectorAsync(":text('button2')");
            Assert.IsTrue(await button2.IsEnabledAsync());
            Assert.IsFalse(await button2.IsDisabledAsync());
            Assert.IsTrue(await Page.IsEnabledAsync(":text('button2')"));
            Assert.IsFalse(await Page.IsDisabledAsync(":text('button2')"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "isEditable should work")]
        public async Task IsEditableShouldWork()
        {
            await Page.SetContentAsync(@"<input id=input1 disabled><textarea></textarea><input id=input2>");
            await Page.EvalOnSelectorAsync("textarea", "t => t.readOnly = true");
            var input1 = await Page.QuerySelectorAsync("#input1");
            Assert.IsFalse(await input1.IsEditableAsync());
            Assert.IsFalse(await Page.IsEditableAsync("#input1"));
            var input2 = await Page.QuerySelectorAsync("#input2");
            Assert.IsTrue(await input2.IsEditableAsync());
            Assert.IsTrue(await Page.IsEditableAsync("#input2"));
            var textarea = await Page.QuerySelectorAsync("textarea");
            Assert.IsFalse(await textarea.IsEditableAsync());
            Assert.IsFalse(await Page.IsEditableAsync("textarea"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "isChecked should work")]
        public async Task IsCheckedShouldWork()
        {
            await Page.SetContentAsync(@"<input type='checkbox' checked><div>Not a checkbox</div>");
            var handle = await Page.QuerySelectorAsync("input");
            Assert.IsTrue(await handle.IsCheckedAsync());
            Assert.IsTrue(await Page.IsCheckedAsync("input"));
            await handle.EvaluateAsync("input => input.checked = false");
            Assert.IsFalse(await handle.IsCheckedAsync());
            Assert.IsFalse(await Page.IsCheckedAsync("input"));
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.IsCheckedAsync("div"));
            StringAssert.Contains(exception.Message, "Not a checkbox or radio button");
        }
    }
}
