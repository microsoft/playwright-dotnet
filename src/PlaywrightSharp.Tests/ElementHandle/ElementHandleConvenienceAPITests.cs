using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle convenience API</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleConvenienceAPITests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleConvenienceAPITests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle convenience API</playwright-describe>
        ///<playwright-it>should have a nice preview</playwright-it>
        [Retry]
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

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle convenience API</playwright-describe>
        ///<playwright-it>getAttribute should work</playwright-it>
        [Retry]
        public async Task GetAttributeShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#outer");

            Assert.Equal("value", await handle.GetAttributeAsync("name"));
            Assert.Equal("value", await Page.GetAttributeAsync("#outer", "name"));
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle convenience API</playwright-describe>
        ///<playwright-it>innerHTML should work</playwright-it>
        [Retry]
        public async Task InnerHTMLShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#outer");

            Assert.Equal("<div id=\"inner\">Text,\nmore text</div>", await handle.GetInnerHtmlAsync());
            Assert.Equal("<div id=\"inner\">Text,\nmore text</div>", await Page.GetInnerHtmlAsync("#outer"));
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle convenience API</playwright-describe>
        ///<playwright-it>innerText should work</playwright-it>
        [Retry]
        public async Task InnerTextShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dom.html");
            var handle = await Page.QuerySelectorAsync("#outer");

            Assert.Equal("Text, more text", await handle.GetInnerTextAsync());
            Assert.Equal("Text, more text", await Page.GetInnerTextAsync("#outer"));
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle convenience API</playwright-describe>
        ///<playwright-it>'innerText should throw</playwright-it>
        [Retry]
        public async Task InnerTextShouldThrow()
        {
            await Page.SetContentAsync("<svg>text</svg>");
            var exception1 = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.GetInnerTextAsync("svg"));
            Assert.Contains("Not an HTMLElement", exception1.Message);

            var handle = await Page.QuerySelectorAsync("svg");
            var exception2 = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => handle.GetInnerTextAsync());
            Assert.Contains("Not an HTMLElement", exception1.Message);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle convenience API</playwright-describe>
        ///<playwright-it>textContent should work</playwright-it>
        [Retry]
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
        [Fact(Skip = "We need https://github.com/microsoft/playwright/issues/3267")]
        public void TextContentShouldBeAtomic()
        {
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>innerText should be atomic</playwright-it>
        [Fact(Skip = "We need https://github.com/microsoft/playwright/issues/3267")]
        public void InnerTextShouldBeAtomic()
        {
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>innerHTML should be atomic</playwright-it>
        [Fact(Skip = "We need https://github.com/microsoft/playwright/issues/3267")]
        public void InnerHtmlShouldBeAtomic()
        {
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>getAttribute should be atomic</playwright-it>
        [Fact(Skip = "We need https://github.com/microsoft/playwright/issues/3267")]
        public void GetAttributeShouldBeAtomic()
        {
        }
    }
}
