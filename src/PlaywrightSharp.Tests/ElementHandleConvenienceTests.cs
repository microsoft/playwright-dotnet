using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>elementhandle-convenience.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleConvenienceTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleConvenienceTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "should have a nice preview")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveANicePreview()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dom.html");
            var outer = await Page.QuerySelectorAsync("#outer");
            var inner = await Page.QuerySelectorAsync("#inner");
            var check = await Page.QuerySelectorAsync("#check");
            var text = await inner.EvaluateHandleAsync("e => e.firstChild");
            await Page.EvaluateAsync("() => 1");  // Give them a chance to calculate the preview.
            Assert.Equal("JSHandle@<div id=\"outer\" name=\"value\">…</div>", outer.ToString());
            Assert.Equal("JSHandle@<div id=\"inner\">Text,↵more text</div>", inner.ToString());
            Assert.Equal("JSHandle@#text=Text,↵more text", text.ToString());
            Assert.Equal("JSHandle@<input checked id=\"check\" foo=\"bar\"\" type=\"checkbox\"/>", check.ToString());
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "getAttribute should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task GetAttributeShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#outer");

            Assert.Equal("value", await handle.GetAttributeAsync("name"));
            Assert.Equal("value", await Page.GetAttributeAsync("#outer", "name"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "innerHTML should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task InnerHTMLShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#outer");

            Assert.Equal("<div id=\"inner\">Text,\nmore text</div>", await handle.GetInnerHTMLAsync());
            Assert.Equal("<div id=\"inner\">Text,\nmore text</div>", await Page.GetInnerHTMLAsync("#outer"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "innerText should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task InnerTextShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#inner");

            Assert.Equal("Text, more text", await handle.GetInnerTextAsync());
            Assert.Equal("Text, more text", await Page.GetInnerTextAsync("#inner"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "'innerText should throw")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task InnerTextShouldThrow()
        {
            await Page.SetContentAsync("<svg>text</svg>");
            var exception1 = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.GetInnerTextAsync("svg"));
            Assert.Contains("Not an HTMLElement", exception1.Message);

            var handle = await Page.QuerySelectorAsync("svg");
            var exception2 = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => handle.GetInnerTextAsync());
            Assert.Contains("Not an HTMLElement", exception1.Message);
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "textContent should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task TextContentShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#outer");

            Assert.Equal("Text,\nmore text", await handle.GetTextContentAsync());
            Assert.Equal("Text,\nmore text", await Page.GetTextContentAsync("#outer"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "Page.dispatchEvent(click)", "textContent should be atomic")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            string tc = await Page.GetTextContentAsync("textContent=div");
            Assert.Equal("Hello", tc);
            Assert.Equal("modified", await Page.EvaluateAsync<string>("() => document.querySelector('div').textContent"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "Page.dispatchEvent(click)", "innerText should be atomic")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            string tc = await Page.GetInnerTextAsync("innerText=div");
            Assert.Equal("Hello", tc);
            Assert.Equal("modified", await Page.EvaluateAsync<string>("() => document.querySelector('div').textContent"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "Page.dispatchEvent(click)", "innerHTML should be atomic")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            string tc = await Page.GetInnerHTMLAsync("innerHtml=div");
            Assert.Equal("Hello", tc);
            Assert.Equal("modified", await Page.EvaluateAsync<string>("() => document.querySelector('div').textContent"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "Page.dispatchEvent(click)", "getAttribute should be atomic")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            Assert.Equal("Hello", tc);
            Assert.Equal("modified", await Page.EvaluateAsync<string>("() => document.querySelector('div').getAttribute('foo')"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "isVisible and isHidden should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task GetIsVisibleAndIsHiddenShouldWork()
        {
            await Page.SetContentAsync("<div>Hi</div><span></span>");
            var div = await Page.QuerySelectorAsync("div");
            Assert.True(await div.GetIsVisibleAsync());
            Assert.False(await div.GetIsHiddenAsync());
            Assert.True(await Page.GetIsVisibleAsync("div"));
            Assert.False(await Page.GetIsHiddenAsync("div"));
            var span = await Page.QuerySelectorAsync("span");
            Assert.False(await span.GetIsVisibleAsync());
            Assert.True(await span.GetIsHiddenAsync());
            Assert.False(await Page.GetIsVisibleAsync("span"));
            Assert.True(await Page.GetIsHiddenAsync("span"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "isEnabled and isDisabled should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task GetIsEnabledAndIsDisabledShouldWork()
        {
            await Page.SetContentAsync(@"
                <button disabled>button1</button>
                <button>button2</button>
                <div>div</div>");
            var div = await Page.QuerySelectorAsync("div");
            Assert.True(await div.GetIsEnabledAsync());
            Assert.False(await div.GetIsDisabledAsync());
            Assert.True(await Page.GetIsEnabledAsync("div"));
            Assert.False(await Page.GetIsDisabledAsync("div"));
            var button1 = await Page.QuerySelectorAsync(":text('button1')");
            Assert.False(await button1.GetIsEnabledAsync());
            Assert.True(await button1.GetIsDisabledAsync());
            Assert.False(await Page.GetIsEnabledAsync(":text('button1')"));
            Assert.True(await Page.GetIsDisabledAsync(":text('button1')"));
            var button2 = await Page.QuerySelectorAsync(":text('button2')");
            Assert.True(await button2.GetIsEnabledAsync());
            Assert.False(await button2.GetIsDisabledAsync());
            Assert.True(await Page.GetIsEnabledAsync(":text('button2')"));
            Assert.False(await Page.GetIsDisabledAsync(":text('button2')"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "isEditable should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task GetIsEditableShouldWork()
        {
            await Page.SetContentAsync(@"<input id=input1 disabled><textarea></textarea><input id=input2>");
            await Page.EvalOnSelectorAsync("textarea", "t => t.readOnly = true");
            var input1 = await Page.QuerySelectorAsync("#input1");
            Assert.False(await input1.IsEditableAsync());
            Assert.False(await Page.IsEditableAsync("#input1"));
            var input2 = await Page.QuerySelectorAsync("#input2");
            Assert.True(await input2.IsEditableAsync());
            Assert.True(await Page.IsEditableAsync("#input2"));
            var textarea = await Page.QuerySelectorAsync("textarea");
            Assert.False(await textarea.IsEditableAsync());
            Assert.False(await Page.IsEditableAsync("textarea"));
        }

        [PlaywrightTest("elementhandle-convenience.spec.ts", "isChecked should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task GetIsCheckedShouldWork()
        {
            await Page.SetContentAsync(@"<input type='checkbox' checked><div>Not a checkbox</div>");
            var handle = await Page.QuerySelectorAsync("input");
            Assert.True(await handle.IsCheckedAsync());
            Assert.True(await Page.IsCheckedAsync("input"));
            await handle.EvaluateAsync("input => input.checked = false");
            Assert.False(await handle.IsCheckedAsync());
            Assert.False(await Page.IsCheckedAsync("input"));
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.IsCheckedAsync("div"));
            Assert.Contains("Not a checkbox or radio button", exception.Message);
        }
    }
}
