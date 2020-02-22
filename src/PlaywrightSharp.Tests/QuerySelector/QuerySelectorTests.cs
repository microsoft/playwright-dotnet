using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$eval</playwright-describe>
    public class PageQuerySelectorEvaluateTests : PlaywrightSharpPageBaseTest
    {
        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with css selector</playwright-it>
        [Fact]
        public async Task ShouldWorkWithCssSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("css=section", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with id selector</playwright-it>
        [Fact]
        public async Task ShouldWorkWithIdSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            var idAttribute = await Page.QuerySelectorEvaluateAsync<string>('id=testAttribute', e => e.id);
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with data-test selector</playwright-it>
        [Fact]
        public async Task ShouldWorkWithDataTestSelector()
        {
            await Page.SetContentAsync("<section data-test=foo id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("data-test=foo", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with data-testid selector</playwright-it>
        [Fact]
        public async Task ShouldWorkWithDataTestidSelector()
        {
            await Page.SetContentAsync("<section data-testid=foo id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("data-testid=foo", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);

        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with data-test-id selector</playwright-it>
        [Fact]
        public async Task ShouldWorkWithDataTestIdSelector()
        {
            await Page.SetContentAsync("<section data-test-id=foo id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("data-test-id=foo", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with zs selector</playwright-it>
        [Fact]
        public async Task ShouldWorkWithZsSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("zs=\"43543\"", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with xpath selector</playwright-it>
        [Fact]
        public async Task ShouldWorkWithXpathSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("xpath=/html/body/section", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should work with text selector</playwright-it>
        [Fact]
        public async Task ShouldWorkWithTextSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("text=43543", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should auto-detect css selector</playwright-it>
        [Fact]
        public async Task ShouldAutoDetectCssSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("section", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should auto-detect css selector with attributes</playwright-it>
        [Fact]
        public async Task ShouldAutoDetectCssSelectorWithAttributes()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.QuerySelectorEvaluateAsync<string>("section[id=\"testAttribute\"]", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should accept arguments</playwright-it>
        [Fact]
        public async Task ShouldAcceptArguments()
        {
            await Page.SetContentAsync("<section>hello</section>");
            string text = await Page.QuerySelectorEvaluateAsync<string>("section", "(e, suffix) => e.textContent + suffix", " world!");
            Assert.Equal("hello world!", text);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should accept ElementHandles as arguments</playwright-it>
        [Fact]
        public async Task ShouldAcceptElementHandlesAsArguments()
        {
            await Page.SetContentAsync("<section>hello</section><div> world</div>");
            var divHandle = await Page.QuerySelectorAsync("div");
            string text = await Page.QuerySelectorEvaluateAsync<string>("section", "(e, div) => e.textContent + div.textContent", divHandle);
            Assert.Equal("hello world", text);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should throw error if no element is found</playwright-it>
        [Fact]
        public async Task ShouldThrowErrorIfNoElementIsFound()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(()
                => Page.QuerySelectorEvaluateAsync("section", "e => e.id"));
            Assert.Contains("failed to find element matching selector \"section\"", exception.Message);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should support >> syntax</playwright-it>
        [Fact]
        public async Task ShouldSupportDoubleGreaterThanSyntax()
        {
            await Page.SetContentAsync("<section><div>hello</div></section>");
            string text = await Page.QuerySelectorEvaluateAsync<string>("css=section >> css=div", "(e, suffix) => e.textContent + suffix", " world!");
            Assert.Equal("hello world!", text);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should support >> syntax with different engines</playwright-it>
        [Fact]
        public async Task ShouldSupportDoubleGreaterThanSyntaxWithDifferentEngines()
        {
            await Page.SetContentAsync("<section><div><span>hello</span></div></section>");
            string text = await Page.QuerySelectorEvaluateAsync<string>("xpath=/html/body/section >> css=div >> zs=\"hello\"", "(e, suffix) => e.textContent + suffix", " world!");
            Assert.Equal("hello world!", text);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should support spaces with >> syntax</playwright-it>
        [Fact]
        public async Task ShouldSupportSpacesWithDoubleGreaterThanSyntax()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/deep-shadow.html");
            string text = await Page.QuerySelectorEvaluateAsync<string>(" css = div >>css=div>>css   = span  ", "e => e.textContent");
            Assert.Equal("Hello from root2", text);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$eval</playwright-describe>
        ///<playwright-it>should enter shadow roots with >> syntax</playwright-it>
        [Fact]
        public async Task ShouldEnterShadowRootsWithDoubleGreaterThanSyntax()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/deep-shadow.html");
            string text1 = await Page.QuerySelectorEvaluateAsync<string>("css=div >> css=span", "e => e.textContent");
            Assert.Equal("Hello from root1", text1);
            string text2 = await Page.QuerySelectorEvaluateAsync<string>("css=div >> css=*:nth-child(2) >> css=span", "e => e.textContent");
            Assert.Equal("Hello from root2", text2);
            var nonExisting = await Page.QuerySelectorAsync("css=div div >> css=span");
            Assert.Null(nonExisting);
            string text3 = await Page.QuerySelectorEvaluateAsync<string>("css=section div >> css=span", "e => e.textContent");
            Assert.Equal("Hello from root1", text3);
            string text4 = await Page.QuerySelectorEvaluateAsync<string>("xpath=/html/body/section/div >> css=div >> css=span", "e => e.textContent");
            Assert.Equal("Hello from root2", text4);
            string text5 = await Page.QuerySelectorEvaluateAsync<string>("zs=section div >> css=div >> css=span", "e => e.textContent");
            Assert.Equal("Hello from root2", text5);
        }
    }

    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$$eval</playwright-describe>
    public class Page.$$evalTests
    {
        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$eval</playwright-describe>
        ///<playwright-it>should work with css selector</playwright-it>
        [Fact]
    public async Task ShouldWorkWithCssSelector()
    {

        await Page.SetContentAsync('<div>hello</div><div>beautiful</div><div>world!</div>');
        var divsCount = await Page.$$eval('css=div', divs => divs.length);
        expect(divsCount).toBe(3);

    }

    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$$eval</playwright-describe>
    ///<playwright-it>should work with zs selector</playwright-it>
    [Fact]
    public async Task ShouldWorkWithZsSelector()
    {

        await Page.SetContentAsync('<div>hello</div><div>beautiful</div><div>world!</div>');
        var divsCount = await Page.$$eval('zs=div', divs => divs.length);
        expect(divsCount).toBe(3);

    }

    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$$eval</playwright-describe>
    ///<playwright-it>should work with xpath selector</playwright-it>
    [Fact]
    public async Task ShouldWorkWithXpathSelector()
    {

        await Page.SetContentAsync('<div>hello</div><div>beautiful</div><div>world!</div>');
        var divsCount = await Page.$$eval('xpath=/html/body/div', divs => divs.length);
        expect(divsCount).toBe(3);

    }

    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$$eval</playwright-describe>
    ///<playwright-it>should auto-detect css selector</playwright-it>
    [Fact]
    public async Task ShouldAuto-detectCssSelector()
    {

        await Page.SetContentAsync('<div>hello</div><div>beautiful</div><div>world!</div>');
        var divsCount = await Page.$$eval('div', divs => divs.length);
        expect(divsCount).toBe(3);

    }

    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$$eval</playwright-describe>
    ///<playwright-it>should support >> syntax</playwright-it>
    [Fact]
    public async Task ShouldSupport>>Syntax()
    {

        await Page.SetContentAsync('<div><span>hello</span></div><div>beautiful</div><div><span>wo</span><span>rld!</span></div><span>Not this one</span>');
        var spansCount = await Page.$$eval('css=div >> css=span', spans => spans.length);
        expect(spansCount).toBe(3);

    }

    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$$eval</playwright-describe>
    ///<playwright-it>should enter shadow roots with >> syntax</playwright-it>
    [Fact]
    public async Task ShouldEnterShadowRootsWith>>Syntax()
    {

        await Page.GoToAsync(TestConstants.ServerUrl + '/deep-shadow.html');
        var spansCount = await Page.$$eval('css=div >> css=div >> css=span', spans => spans.length);
        expect(spansCount).toBe(2);

    }

}
///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>Page.$</playwright-describe>
public class Page.$Tests
    {
        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$</playwright-describe>
        ///<playwright-it>should query existing element with css selector</playwright-it>
        [Fact]
public async Task ShouldQueryExistingElementWithCssSelector()
{

    await Page.SetContentAsync('<section>test</section>');
    var element = await Page.QuerySelectorAsync('css=section');
    expect(element).toBeTruthy();

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>Page.$</playwright-describe>
///<playwright-it>should query existing element with zs selector</playwright-it>
[Fact]
public async Task ShouldQueryExistingElementWithZsSelector()
{

    await Page.SetContentAsync('<section>test</section>');
    var element = await Page.QuerySelectorAsync('zs="test"');
    expect(element).toBeTruthy();

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>Page.$</playwright-describe>
///<playwright-it>should query existing element with xpath selector</playwright-it>
[Fact]
public async Task ShouldQueryExistingElementWithXpathSelector()
{

    await Page.SetContentAsync('<section>test</section>');
    var element = await Page.QuerySelectorAsync('xpath=/html/body/section');
    expect(element).toBeTruthy();

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>Page.$</playwright-describe>
///<playwright-it>should return null for non-existing element</playwright-it>
[Fact]
public async Task ShouldReturnNullForNon-existingElement()
{

    var element = await Page.QuerySelectorAsync('non-existing-element');
    expect(element).toBe(null);

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>Page.$</playwright-describe>
///<playwright-it>should auto-detect xpath selector</playwright-it>
[Fact]
public async Task ShouldAuto-detectXpathSelector()
{

    await Page.SetContentAsync('<section>test</section>');
    var element = await Page.QuerySelectorAsync('//html/body/section');
    expect(element).toBeTruthy();

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>Page.$</playwright-describe>
///<playwright-it>should auto-detect text selector</playwright-it>
[Fact]
public async Task ShouldAuto-detectTextSelector()
{

    await Page.SetContentAsync('<section>test</section>');
    var element = await Page.QuerySelectorAsync('"test"');
    expect(element).toBeTruthy();

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>Page.$</playwright-describe>
///<playwright-it>should auto-detect css selector</playwright-it>
[Fact]
public async Task ShouldAuto-detectCssSelector()
{

    await Page.SetContentAsync('<section>test</section>');
    var element = await Page.QuerySelectorAsync('section');
    expect(element).toBeTruthy();

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>Page.$</playwright-describe>
///<playwright-it>should support >> syntax</playwright-it>
[Fact]
public async Task ShouldSupport>>Syntax()
{

    await Page.SetContentAsync('<section><div>test</div></section>');
    var element = await Page.QuerySelectorAsync('css=section >> css=div');
    expect(element).toBeTruthy();

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>Page.$</playwright-describe>
///<playwright-it>should respect waitFor visibility</playwright-it>
[Fact]
public async Task ShouldRespectWaitForVisibility()
{

    await Page.SetContentAsync('<section id="testAttribute">43543</section>');
    expect(await Page.waitForSelector('css=section', { waitFor: 'visible'})).toBeTruthy();
    expect(await Page.waitForSelector('css=section', { waitFor: 'any'})).toBeTruthy();
    expect(await Page.waitForSelector('css=section')).toBeTruthy();

    await Page.SetContentAsync('<section id="testAttribute" style="display: none">43543</section>');
    expect(await Page.waitForSelector('css=section', { waitFor: 'hidden'})).toBeTruthy();
    expect(await Page.waitForSelector('css=section', { waitFor: 'any'})).toBeTruthy();
    expect(await Page.waitForSelector('css=section')).toBeTruthy();

}

    }
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$$</playwright-describe>
    public class Page.$$Tests
    {
        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$</playwright-describe>
        ///<playwright-it>should query existing elements</playwright-it>
        [Fact]
public async Task ShouldQueryExistingElements()
{

    await Page.SetContentAsync('<div>A</div><br/><div>B</div>');
    var elements = await Page.QuerySelectorAllAsync('div');
    expect(elements.length).toBe(2);
    var promises = elements.map(element => Page.EvaluateAsync<string>(e => e.textContent, element));
    expect(await Promise.all(promises)).toEqual(['A', 'B']);

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>Page.$$</playwright-describe>
///<playwright-it>should return empty array if nothing is found</playwright-it>
[Fact]
public async Task ShouldReturnEmptyArrayIfNothingIsFound()
{

    await Page.GoToAsync(TestConstants.EmptyPage);
    var elements = await Page.QuerySelectorAllAsync('div');
    expect(elements.length).toBe(0);

}

    }
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>Page.$$ xpath</playwright-describe>
    public class Page.$$ xpathTests
    {
        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>Page.$$ xpath</playwright-describe>
        ///<playwright-it>should query existing element</playwright-it>
        [Fact]
public async Task ShouldQueryExistingElement()
{

    await Page.SetContentAsync('<section>test</section>');
    var elements = await Page.QuerySelectorAllAsync('xpath=/html/body/section');
    expect(elements[0]).toBeTruthy();
    expect(elements.length).toBe(1);

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>Page.$$ xpath</playwright-describe>
///<playwright-it>should return empty array for non-existing element</playwright-it>
[Fact]
public async Task ShouldReturnEmptyArrayForNon-existingElement()
{

    var element = await Page.QuerySelectorAllAsync('//html/body/non-existing-element');
    expect(element).toEqual([]);

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>Page.$$ xpath</playwright-describe>
///<playwright-it>should return multiple elements</playwright-it>
[Fact]
public async Task ShouldReturnMultipleElements()
{
    page, sever}) => {
      await Page.SetContentAsync('<div></div><div></div>');
var elements = await Page.QuerySelectorAllAsync('xpath=/html/body/div');
expect(elements.length).toBe(2);

        }

    }
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.$</playwright-describe>
    public class ElementHandle.$Tests
    {
        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$</playwright-describe>
        ///<playwright-it>should query existing element</playwright-it>
        [Fact]
public async Task ShouldQueryExistingElement()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/playground.html');
    await Page.SetContentAsync('<html><body><div class="second"><div class="inner">A</div></div></body></html>');
    var html = await Page.QuerySelectorAsync('html');
    var second = await html.QuerySelectorAsync('.second');
    var inner = await second.QuerySelectorAsync('.inner');
    var content = await Page.EvaluateAsync<string>(e => e.textContent, inner);
    expect(content).toBe('A');

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>ElementHandle.$</playwright-describe>
///<playwright-it>should query existing element with zs selector</playwright-it>
[Fact]
public async Task ShouldQueryExistingElementWithZsSelector()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/playground.html');
    await Page.SetContentAsync('<html><body><div class="second"><div class="inner">A</div></div></body></html>');
    var html = await Page.QuerySelectorAsync('zs=html');
    var second = await html.QuerySelectorAsync('zs=.second');
    var inner = await second.QuerySelectorAsync('zs=.inner');
    var content = await Page.EvaluateAsync<string>(e => e.textContent, inner);
    expect(content).toBe('A');

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>ElementHandle.$</playwright-describe>
///<playwright-it>should return null for non-existing element</playwright-it>
[Fact]
public async Task ShouldReturnNullForNon-existingElement()
{

    await Page.SetContentAsync('<html><body><div class="second"><div class="inner">B</div></div></body></html>');
    var html = await Page.QuerySelectorAsync('html');
    var second = await html.QuerySelectorAsync('.third');
    expect(second).toBe(null);

}

    }
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.$eval</playwright-describe>
    public class ElementHandle.$evalTests
    {
        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$eval</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
public async Task ShouldWork()
{

    await Page.SetContentAsync('<html><body><div class="tweet"><div class="like">100</div><div class="retweets">10</div></div></body></html>');
    var tweet = await Page.QuerySelectorAsync('.tweet');
    var content = await tweet.$eval('.like', node => node.innerText);
    expect(content).toBe('100');

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>ElementHandle.$eval</playwright-describe>
///<playwright-it>should retrieve content from subtree</playwright-it>
[Fact]
public async Task ShouldRetrieveContentFromSubtree()
{

    var htmlContent = '<div class="a">not-a-child-div</div><div id="myId"><div class="a">a-child-div</div></div>';
    await Page.SetContentAsync(htmlContent);
    var elementHandle = await Page.QuerySelectorAsync('#myId');
    var content = await elementHandle.$eval('.a', node => node.innerText);
    expect(content).toBe('a-child-div');

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>ElementHandle.$eval</playwright-describe>
///<playwright-it>should throw in case of missing selector</playwright-it>
[Fact]
public async Task ShouldThrowInCaseOfMissingSelector()
{

    var htmlContent = '<div class="a">not-a-child-div</div><div id="myId"></div>';
    await Page.SetContentAsync(htmlContent);
    var elementHandle = await Page.QuerySelectorAsync('#myId');
    var errorMessage = await elementHandle.$eval('.a', node => node.innerText).catch (error => error.message);
    expect(errorMessage).toBe(`Error: failed to find element matching selector ".a"`);

    }

}
///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>ElementHandle.$$eval</playwright-describe>
public class ElementHandle.$$evalTests
    {
        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$$eval</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
public async Task ShouldWork()
{

    await Page.SetContentAsync('<html><body><div class="tweet"><div class="like">100</div><div class="like">10</div></div></body></html>');
    var tweet = await Page.QuerySelectorAsync('.tweet');
    var content = await tweet.$$eval('.like', nodes => nodes.map(n => n.innerText));
    expect(content).toEqual(['100', '10']);

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>ElementHandle.$$eval</playwright-describe>
///<playwright-it>should retrieve content from subtree</playwright-it>
[Fact]
public async Task ShouldRetrieveContentFromSubtree()
{

    var htmlContent = '<div class="a">not-a-child-div</div><div id="myId"><div class="a">a1-child-div</div><div class="a">a2-child-div</div></div>';
    await Page.SetContentAsync(htmlContent);
    var elementHandle = await Page.QuerySelectorAsync('#myId');
    var content = await elementHandle.$$eval('.a', nodes => nodes.map(n => n.innerText));
    expect(content).toEqual(['a1-child-div', 'a2-child-div']);

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>ElementHandle.$$eval</playwright-describe>
///<playwright-it>should not throw in case of missing selector</playwright-it>
[Fact]
public async Task ShouldNotThrowInCaseOfMissingSelector()
{

    var htmlContent = '<div class="a">not-a-child-div</div><div id="myId"></div>';
    await Page.SetContentAsync(htmlContent);
    var elementHandle = await Page.QuerySelectorAsync('#myId');
    var nodesLength = await elementHandle.$$eval('.a', nodes => nodes.length);
    expect(nodesLength).toBe(0);

}

    }
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.$$</playwright-describe>
    public class ElementHandle.$$Tests
    {
        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$$</playwright-describe>
        ///<playwright-it>should query existing elements</playwright-it>
        [Fact]
public async Task ShouldQueryExistingElements()
{

    await Page.SetContentAsync('<html><body><div>A</div><br/><div>B</div></body></html>');
    var html = await Page.QuerySelectorAsync('html');
    var elements = await html.QuerySelectorAllAsync('div');
    expect(elements.length).toBe(2);
    var promises = elements.map(element => Page.EvaluateAsync<string>(e => e.textContent, element));
    expect(await Promise.all(promises)).toEqual(['A', 'B']);

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>ElementHandle.$$</playwright-describe>
///<playwright-it>should return empty array for non-existing elements</playwright-it>
[Fact]
public async Task ShouldReturnEmptyArrayForNon-existingElements()
{

    await Page.SetContentAsync('<html><body><span>A</span><br/><span>B</span></body></html>');
    var html = await Page.QuerySelectorAsync('html');
    var elements = await html.QuerySelectorAllAsync('div');
    expect(elements.length).toBe(0);

}

    }
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.$$ xpath</playwright-describe>
    public class ElementHandle.$$ xpathTests
    {
        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$$ xpath</playwright-describe>
        ///<playwright-it>should query existing element</playwright-it>
        [Fact]
public async Task ShouldQueryExistingElement()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/playground.html');
    await Page.SetContentAsync('<html><body><div class="second"><div class="inner">A</div></div></body></html>');
    var html = await Page.QuerySelectorAsync('html');
    var second = await html.QuerySelectorAllAsync(`xpath =./ body / div[contains(@class, 'second')]`);
    var inner = await second[0].QuerySelectorAllAsync(`xpath =./ div[contains(@class, 'inner')]`);
    var content = await Page.EvaluateAsync<string>(e => e.textContent, inner[0]);
    expect(content).toBe('A');

}

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>ElementHandle.$$ xpath</playwright-describe>
///<playwright-it>should return null for non-existing element</playwright-it>
[Fact]
public async Task ShouldReturnNullForNon-existingElement()
{

    await Page.SetContentAsync('<html><body><div class="second"><div class="inner">B</div></div></body></html>');
    var html = await Page.QuerySelectorAsync('html');
    var second = await html.QuerySelectorAllAsync(`xpath =/ div[contains(@class, 'third')]`);
    expect(second).toEqual([]);

}

    }
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>zselector</playwright-describe>
    public class zselectorTests
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>zselector</playwright-describe>
    ///<playwright-it>query</playwright-it>
    [Fact]
    public async Task Query()
    {
        page}) => {
      await Page.SetContentAsync(`<div>yo</div><div>ya</div><div>ye</div>`);
    expect(await Page.$eval(`zs= "ya"`, e => e.outerHTML)).toBe('<div>ya</div>');

    await Page.SetContentAsync(`<div foo = "baz" ></ div >< div foo= "bar space" ></ div >`);
    expect(await Page.$eval(`zs=[foo = "bar space"]`, e => e.outerHTML)).toBe('<div foo="bar space"></div>');

    await Page.SetContentAsync(`<div>yo<span></span></div>`);
    expect(await Page.$eval(`zs= span`, e => e.outerHTML)).toBe('<span></span>');
    expect(await Page.$eval(`zs= div > span`, e => e.outerHTML)).toBe('<span></span>');
    expect(await Page.$eval(`zs= div span`, e => e.outerHTML)).toBe('<span></span>');
    expect(await Page.$eval(`zs= "yo" > span`, e => e.outerHTML)).toBe('<span></span>');
    expect(await Page.$eval(`zs= "yo" span`, e => e.outerHTML)).toBe('<span></span>');
    expect(await Page.$eval(`zs= span ^`, e => e.outerHTML)).toBe('<div>yo<span></span></div>');
    expect(await Page.$eval(`zs= span ~div`, e => e.outerHTML)).toBe('<div>yo<span></span></div>');
    expect(await Page.$eval(`zs= span ~ "yo"`, e => e.outerHTML)).toBe('<div>yo<span></span></div>');

    await Page.SetContentAsync(`<div>yo</div><div>yo<span></span></div>`);
    expect(await Page.$eval(`zs= "yo"#0`, e => e.outerHTML)).toBe('<div>yo</div>');
      expect(await Page.$eval(`zs= "yo"#1`, e => e.outerHTML)).toBe('<div>yo<span></span></div>');
      expect(await Page.$eval(`zs= "yo" ~DIV#1`, e => e.outerHTML)).toBe('<div>yo<span></span></div>');
      expect(await Page.$eval(`zs= span ~div#1`, e => e.outerHTML)).toBe('<div>yo<span></span></div>');
      expect(await Page.$eval(`zs= span ~div#0`, e => e.outerHTML)).toBe('<div>yo<span></span></div>');
      expect(await Page.$eval(`zs= span ~ "yo"#1 ^ > div`, e => e.outerHTML)).toBe('<div>yo</div>');
      expect(await Page.$eval(`zs= span ~ "yo"#1 ^ > div#1`, e => e.outerHTML)).toBe('<div>yo<span></span></div>');

      await Page.SetContentAsync(`<div>yo<span id="s1"></span></div><div>yo<span id="s2"></span><span id = "s3" ></ span ></ div >`);
    expect(await Page.$eval(`zs= "yo"`, e => e.outerHTML)).toBe('<div>yo<span id="s1"></span></div>');
    expect(await Page.$$eval(`zs= "yo"`, es => es.map(e => e.outerHTML).join('\n'))).toBe('<div>yo<span id="s1"></span></div>\n<div>yo<span id="s2"></span><span id="s3"></span></div>');
    expect(await Page.$$eval(`zs= "yo"#1`, es => es.map(e => e.outerHTML).join('\n'))).toBe('<div>yo<span id="s2"></span><span id="s3"></span></div>');
      expect(await Page.$$eval(`zs= "yo" ~span`, es => es.map(e => e.outerHTML).join('\n'))).toBe('<span id="s1"></span>\n<span id="s2"></span>\n<span id="s3"></span>');
    expect(await Page.$$eval(`zs= "yo"#1 ~ span`, es => es.map(e => e.outerHTML).join('\n'))).toBe('<span id="s2"></span>\n<span id="s3"></span>');
      expect(await Page.$$eval(`zs= "yo" ~span#0`, es => es.map(e => e.outerHTML).join('\n'))).toBe('<span id="s1"></span>\n<span id="s2"></span>');
      expect(await Page.$$eval(`zs= "yo" ~span#1`, es => es.map(e => e.outerHTML).join('\n'))).toBe('<span id="s2"></span>\n<span id="s3"></span>');

        }

///<playwright-file>queryselector.spec.js</playwright-file>
///<playwright-describe>zselector</playwright-describe>
///<playwright-it>create</playwright-it>
[Fact]
public async Task Create()
{
    page}) => {
      await Page.SetContentAsync(`<div>yo</div><div>ya</div><div>ya</div>`);
expect(await selectors._createSelector('zs', await Page.QuerySelectorAsync('div'))).toBe('"yo"');
expect(await selectors._createSelector('zs', await Page.QuerySelectorAsync('div:nth-child(2)'))).toBe('"ya"');
expect(await selectors._createSelector('zs', await Page.QuerySelectorAsync('div:nth-child(3)'))).toBe('"ya"#1');

await Page.SetContentAsync(`<img alt = "foo bar" >`);
expect(await selectors._createSelector('zs', await Page.QuerySelectorAsync('img'))).toBe('img[alt="foo bar"]');

await Page.SetContentAsync(`<div>yo<span></span></div><span></span>`);
expect(await selectors._createSelector('zs', await Page.QuerySelectorAsync('span'))).toBe('"yo"~SPAN');
expect(await selectors._createSelector('zs', await Page.QuerySelectorAsync('span:nth-child(2)'))).toBe('SPAN#1');

        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>zselector</playwright-describe>
        ///<playwright-it>children of various display parents</playwright-it>
        [Fact]
public async Task ChildrenOfVariousDisplayParents()
{
    page}) => {
      await Page.SetContentAsync(`<body><div style = 'position: fixed;' >< span > yo </ span ></ div ></ body >`);
expect(await selectors._createSelector('zs', await Page.QuerySelectorAsync('span'))).toBe('"yo"');

await Page.SetContentAsync(`<div style = 'position: relative;' >< span > yo </ span ></ div >`);
expect(await selectors._createSelector('zs', await Page.QuerySelectorAsync('span'))).toBe('"yo"');

// "display: none" makes all children text invisible - fallback to tag name.
await Page.SetContentAsync(`<div style = 'display: none;' >< span > yo </ span ></ div >`);
expect(await selectors._createSelector('zs', await Page.QuerySelectorAsync('span'))).toBe('SPAN');

        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>zselector</playwright-describe>
        ///<playwright-it>boundary</playwright-it>
        [Fact]
public async Task Boundary()
{
    page}) => {
      await Page.SetContentAsync(`
        <div>hey</div>
        <div>hey</div>
        <div>hey</div>
        <div>
          <div>yo</div>
          <div>hello</div>
          <div>hello</div>
          <div>hello</div>
          <div>unique</div>
          <div>
            <div>hey2<span></span><span></span><span></span></div>
            <div>hello</div>
          </div>
          <div>
            <div>hey<span></span><span></span><span></span></div>
            <div>hello</div>
          </div>
        </div>
        <div>
          <div>ya<div>
          <div id = first > hello </ div >
          < div > hello </ div >
          < div > hello </ div >
          < div >
            < div > hey2<span> </ span >< span ></ span >< span ></ span ></ div >
            < div > hello </ div >
          </ div >
          < div >
            < div > hey<span> </ span >< span ></ span >< span ></ span ></ div >
            < div id= target > hello </ div >
          </ div >
        </ div >
        < div >
          < div > ya<div>
          < div id= first2 > hello </ div >
          < div > hello </ div >
          < div > hello </ div >
          < div >
            < div > hey2<span> </ span >< span ></ span >< span ></ span ></ div >
            < div > hello </ div >
          </ div >
          < div >
            < div > hey<span> </ span >< span ></ span >< span ></ span ></ div >
            < div id= target2 > hello </ div >
          </ div >
        </ div >`);
      expect(await selectors._createSelector('zs', await Page.QuerySelectorAsync('#target'))).toBe('"ya"~"hey"~"hello"');
      expect(await Page.$eval(`zs="ya"~"hey"~"hello"`, e => e.outerHTML)).toBe('<div id="target">hello</div>');
      expect(await Page.$eval(`zs="ya"~"hey"~"unique"`, e => e.outerHTML).catch(e => e.message)).toBe('Error: failed to find element matching selector "zs="ya"~"hey"~"unique""');
      expect(await Page.$$eval(`zs="ya" ~ "hey" ~ "hello"`, es => es.map(e => e.outerHTML).join('\n'))).toBe('<div id="target">hello</div>\n<div id="target2">hello</div>');

        }

    }
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>text selector</playwright-describe>
    public class text selectorTests
    {
        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>text selector</playwright-describe>
        ///<playwright-it>query</playwright-it>
        [Fact]
        public async Task Query()
        {
        page}) => {
      await Page.SetContentAsync(`<div>yo</div><div>ya</div><div>\nye  </div>`);
      expect(await Page.$eval(`text=ya`, e => e.outerHTML)).toBe('<div>ya</div>');
      expect(await Page.$eval(`text="ya"`, e => e.outerHTML)).toBe('<div>ya</div>');
      expect(await Page.$eval(`text=/^[ay]+$/`, e => e.outerHTML)).toBe('<div>ya</div>');
      expect(await Page.$eval(`text=/Ya/i`, e => e.outerHTML)).toBe('<div>ya</div>');
      expect(await Page.$eval(`text=ye`, e => e.outerHTML)).toBe('<div>\nye  </div>');

      await Page.SetContentAsync(`<div> ye </div><div>ye</div>`);
      expect(await Page.$eval(`text="ye"`, e => e.outerHTML)).toBe('<div>ye</div>');

      await Page.SetContentAsync(`<div>yo</div><div>"ya</div><div> hello world! </div>`);
      expect(await Page.$eval(`text="\\"ya"`, e => e.outerHTML)).toBe('<div>"ya</div>');
      expect(await Page.$eval(`text=/hello/`, e => e.outerHTML)).toBe('<div> hello world! </div>');
      expect(await Page.$eval(`text=/^\\s*heLLo/i`, e => e.outerHTML)).toBe('<div> hello world! </div>');

      await Page.SetContentAsync(`<div>yo<div>ya</div>hey<div>hey</div></div>`);
      expect(await Page.$eval(`text=hey`, e => e.outerHTML)).toBe('<div>yo<div>ya</div>hey<div>hey</div></div>');

      await Page.SetContentAsync(`<div>yo<span id="s1"></span></div><div>yo<span id="s2"></span><span id="s3"></span></div>`);
      expect(await Page.$$eval(`text=yo`, es => es.map(e => e.outerHTML).join('\n'))).toBe('<div>yo<span id="s1"></span></div>\n<div>yo<span id="s2"></span><span id="s3"></span></div>');

        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>text selector</playwright-describe>
        ///<playwright-it>create</playwright-it>
        [Fact]
        public async Task Create()
        {
        page}) => {
      await Page.SetContentAsync(`<div>yo</div><div>"ya</div><div>ye ye</div>`);
      expect(await selectors._createSelector('text', await Page.QuerySelectorAsync('div'))).toBe('yo');
      expect(await selectors._createSelector('text', await Page.QuerySelectorAsync('div:nth-child(2)'))).toBe('"\\"ya"');
      expect(await selectors._createSelector('text', await Page.QuerySelectorAsync('div:nth-child(3)'))).toBe('"ye ye"');

      await Page.SetContentAsync(`<div>yo</div><div>yo<div>ya</div>hey</div>`);
      expect(await selectors._createSelector('text', await Page.QuerySelectorAsync('div:nth-child(2)'))).toBe('hey');

      await Page.SetContentAsync(`<div> yo <div></div>ya</div>`);
      expect(await selectors._createSelector('text', await Page.QuerySelectorAsync('div'))).toBe('yo');

      await Page.SetContentAsync(`<div> "yo <div></div>ya</div>`);
      expect(await selectors._createSelector('text', await Page.QuerySelectorAsync('div'))).toBe('" \\"yo "');

        }

    }
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>selectors.register</playwright-describe>
    public class selectors.registerTests
    {
        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>selectors.register</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
        page}) => {
      var createTagSelector = () => ({
        name: 'tag',
        create(root, target) {
          return target.nodeName;
        },
        query(root, selector) {
          return root.querySelector(selector);
        },
        queryAll(root, selector) {
          return Array.from(root.querySelectorAll(selector));
        }
      });
      await selectors.register(`(${createTagSelector.ToString()})()`);
      await Page.SetContentAsync('<div><span></span></div><div></div>');
      expect(await selectors._createSelector('tag', await Page.QuerySelectorAsync('div'))).toBe('DIV');
      expect(await Page.$eval('tag=DIV', e => e.nodeName)).toBe('DIV');
      expect(await Page.$eval('tag=SPAN', e => e.nodeName)).toBe('SPAN');
      expect(await Page.$$eval('tag=DIV', es => es.length)).toBe(2);

        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>selectors.register</playwright-describe>
        ///<playwright-it>should update</playwright-it>
        [Fact]
        public async Task ShouldUpdate()
        {
        page}) => {
      await Page.SetContentAsync('<div><dummy id=d1></dummy></div><span><dummy id=d2></dummy></span>');
      expect(await Page.$eval('div', e => e.nodeName)).toBe('DIV');
      var error = await Page.QuerySelectorAsync('dummy=foo').catch(e => e);
      expect(error.message).toContain('Unknown engine dummy while parsing selector dummy=foo');
      var createDummySelector = (name) => ({
        name,
        create(root, target) {
          return target.nodeName;
        },
        query(root, selector) {
          return root.querySelector(name);
        },
        queryAll(root, selector) {
          return Array.from(root.querySelectorAll(name));
        }
      });
      await selectors.register(createDummySelector, 'dummy');
      expect(await Page.$eval('dummy=foo', e => e.id)).toBe('d1');
      expect(await Page.$eval('css=span >> dummy=foo', e => e.id)).toBe('d2');

        }

    }

}
