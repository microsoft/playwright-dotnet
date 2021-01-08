using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle-convenience.spec.js</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleConvenienceTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleConvenienceTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle-convenience.spec.js</playwright-file>
        ///<playwright-it>should have a nice preview</playwright-it>
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

        ///<playwright-file>elementhandle-convenience.spec.js</playwright-file>
        ///<playwright-it>getAttribute should work</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task GetAttributeShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#outer");

            Assert.Equal("value", await handle.GetAttributeAsync("name"));
            Assert.Equal("value", await Page.GetAttributeAsync("#outer", "name"));
        }

        ///<playwright-file>elementhandle-convenience.spec.js</playwright-file>
        ///<playwright-it>innerHTML should work</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task InnerHTMLShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#outer");

            Assert.Equal("<div id=\"inner\">Text,\nmore text</div>", await handle.GetInnerHtmlAsync());
            Assert.Equal("<div id=\"inner\">Text,\nmore text</div>", await Page.GetInnerHtmlAsync("#outer"));
        }

        ///<playwright-file>elementhandle-convenience.spec.js</playwright-file>
        ///<playwright-it>innerText should work</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task InnerTextShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#inner");

            Assert.Equal("Text, more text", await handle.GetInnerTextAsync());
            Assert.Equal("Text, more text", await Page.GetInnerTextAsync("#inner"));
        }

        ///<playwright-file>elementhandle-convenience.spec.js</playwright-file>
        ///<playwright-it>'innerText should throw</playwright-it>
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

        ///<playwright-file>elementhandle-convenience.spec.js</playwright-file>
        ///<playwright-it>textContent should work</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task TextContentShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#outer");

            Assert.Equal("Text,\nmore text", await handle.GetTextContentAsync());
            Assert.Equal("Text,\nmore text", await Page.GetTextContentAsync("#outer"));
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>textContent should be atomic</playwright-it>
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

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>innerText should be atomic</playwright-it>
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

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>innerHTML should be atomic</playwright-it>
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
            string tc = await Page.GetInnerHtmlAsync("innerHtml=div");
            Assert.Equal("Hello", tc);
            Assert.Equal("modified", await Page.EvaluateAsync<string>("() => document.querySelector('div').textContent"));
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>getAttribute should be atomic</playwright-it>
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
    }
}
