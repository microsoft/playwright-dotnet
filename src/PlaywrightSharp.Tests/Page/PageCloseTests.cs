using System;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.close</playwright-describe>
    public class PageCloseTests : PlaywrightSharpPageBaseTest
    {
        internal PageCloseTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should reject all promises when page is closed</playwright-it>
        [Fact]
        public async Task ShouldRejectAllPromisesWhenPageIsClosed()
        {
            var newPage = await Context.NewPageAsync();
            var exception = await Assert.ThrowsAsync<AggregateException>(() => Task.WhenAll(
                newPage.EvaluateAsync<string>("() => new Promise(r => { })"),
                newPage.CloseAsync()
            ));
            Assert.Contains("Protocol error", Assert.IsType<PlaywrightSharpException>(exception).Message);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should not be visible in context.pages</playwright-it>
        [Fact]
        public async Task ShouldNotBeVisibleInContextPages()
        {
            var newPage = await Context.NewPageAsync();
            Assert.Contains(newPage, await Context.GetPagesAsync());
            await newPage.CloseAsync();
            Assert.DoesNotContain(newPage, await Context.GetPagesAsync());
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should run beforeunload if asked for</playwright-it>
        [Fact]
        public async Task ShouldRunBeforeunloadIfAskedFor()
        {
            var newPage = await Context.NewPageAsync();
            await newPage.GoToAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");
            var pageClosingPromise = newPage.CloseAsync(new PageCloseOptions { RunBeforeUnload = true });
            var dialog = await newPage.WaitForEvent<DialogEventArgs>(PageEvent.Dialog).ContinueWith(task => task.Result.Dialog);
            Assert.Equal(DialogType.BeforeUnload, dialog.DialogType);
            Assert.Empty(dialog.DefaultValue);
            if (TestConstants.IsChromium)
            {
                Assert.Empty(dialog.Message);
            }
            else if (TestConstants.IsWebKit)
            {
                Assert.Equal("Leave?", dialog.Message);
            }
            else
            {
                Assert.Equal("This page is asking you to confirm that you want to leave - data you have entered may not be saved.", dialog.Message);
            }

            await dialog.AcceptAsync();
            await pageClosingPromise;
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should *not* run beforeunload by default</playwright-it>
        [Fact]
        public async Task ShouldNotRunBeforeunloadByDefault()
        {
            var newPage = await Context.NewPageAsync();
            await newPage.GoToAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");
            await newPage.CloseAsync();
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should set the page close state</playwright-it>
        [Fact]
        public async Task ShouldSetThePageCloseState()
        {
            var newPage = await Context.NewPageAsync();
            Assert.False(newPage.IsClosed);
            await newPage.CloseAsync();
            Assert.True(newPage.IsClosed);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should terminate network waiters</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldTerminateNetworkWaiters()
        {
            var newPage = await Context.NewPageAsync();
            var aggregateException = await Assert.ThrowsAsync<AggregateException>(() => Task.WhenAll(
                newPage.WaitForRequestAsync(TestConstants.EmptyPage),
                newPage.WaitForResponseAsync(TestConstants.EmptyPage),
                newPage.CloseAsync()
            ));
            for (int i = 0; i < 2; i++)
            {
                string message = aggregateException.InnerExceptions[i].Message;
                Assert.Contains("Target closed", message);
                Assert.DoesNotContain("Timeout", message);
            }
        }
    }
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.waitForResponse</playwright-describe>
    public class PageWaitForResponseTests : PlaywrightSharpPageBaseTest
    {
        internal PageWaitForResponseTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForResponse</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var (response, _) = await TaskUtils.WhenAll(
                Page.WaitForResponseAsync(TestConstants.ServerUrl + "/digits/2.png"),
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", response.Url);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForResponse</playwright-describe>
        ///<playwright-it>should respect timeout</playwright-it>
        [Fact]
        public async Task ShouldRespectTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => Page.WaitForEvent(PageEvent.Response, new WaitForEventOptions<ResponseEventArgs>
                {
                    Predicate = _ => false,
                    Timeout = 1
                }));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForResponse</playwright-describe>
        ///<playwright-it>should respect default timeout</playwright-it>
        [Fact]
        public async Task ShouldRespectDefaultTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => Page.WaitForEvent(PageEvent.Response, new WaitForEventOptions<ResponseEventArgs>
                {
                    Predicate = _ => false
                }));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForResponse</playwright-describe>
        ///<playwright-it>should work with predicate</playwright-it>
        [Fact]
        public async Task ShouldWorkWithPredicate()
        {

            await Page.GoToAsync(TestConstants.EmptyPage);
            var (responseEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent(PageEvent.Response, new WaitForEventOptions<ResponseEventArgs> { Predicate = e => e.Response.Url == TestConstants.ServerUrl + "/digits/2.png" }),
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", responseEvent.Response.Url);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForResponse</playwright-describe>
        ///<playwright-it>should work with no timeout</playwright-it>
        [Fact]
        public async Task ShouldWorkWithNoTimeout()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var (response, _) = await TaskUtils.WhenAll(
                Page.WaitForResponseAsync(TestConstants.ServerUrl + "/digits/2.png", new WaitForOptions { Timeout = 0 }),
                Page.EvaluateAsync<string>(@"() => setTimeout(() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }, 50)")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", response.Url);
        }

    }
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.PageError</playwright-describe>
    public class PageEventsPageErrorTests : PlaywrightSharpPageBaseTest
    {
        internal PageEventsPageErrorTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.PageError</playwright-describe>
        ///<playwright-it>should fire</playwright-it>
        [Fact]
        public async Task ShouldFire()
        {
            let error = null;
            Page.once('pageerror', e => error = e);
            await Promise.all([
              Page.GoToAsync(TestConstants.ServerUrl + '/error.html'),
              waitEvent(page, 'pageerror')
            ]);
            expect(error.message).toContain('Fancy');

        }

    }
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.title</playwright-describe>
    public class Page.titleTests
    {
        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.title</playwright-describe>
        ///<playwright-it>should return the page title</playwright-it>
        [Fact]
    public async Task ShouldReturnThePageTitle()
    {

        await Page.GoToAsync(TestConstants.ServerUrl + '/title.html');
        expect(await Page.title()).toBe('Woof-Woof');

    }

}
///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
public class Page.selectTests
    {
        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.select</playwright-describe>
        ///<playwright-it>should select single option</playwright-it>
        [Fact]
public async Task ShouldSelectSingleOption()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.select('select', 'blue');
    expect(await Page.EvaluateAsync<string>(() => result.onInput)).toEqual(['blue']);
    expect(await Page.EvaluateAsync<string>(() => result.onChange)).toEqual(['blue']);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should select single option by value</playwright-it>
[Fact]
public async Task ShouldSelectSingleOptionByValue()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.select('select', { value: 'blue' });
    expect(await Page.EvaluateAsync<string>(() => result.onInput)).toEqual(['blue']);
    expect(await Page.EvaluateAsync<string>(() => result.onChange)).toEqual(['blue']);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should select single option by label</playwright-it>
[Fact]
public async Task ShouldSelectSingleOptionByLabel()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.select('select', { label: 'Indigo' });
    expect(await Page.EvaluateAsync<string>(() => result.onInput)).toEqual(['indigo']);
    expect(await Page.EvaluateAsync<string>(() => result.onChange)).toEqual(['indigo']);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should select single option by handle</playwright-it>
[Fact]
public async Task ShouldSelectSingleOptionByHandle()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.select('select', await Page.QuerySelectorAsync('[id=whiteOption]'));
    expect(await Page.EvaluateAsync<string>(() => result.onInput)).toEqual(['white']);
    expect(await Page.EvaluateAsync<string>(() => result.onChange)).toEqual(['white']);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should select single option by index</playwright-it>
[Fact]
public async Task ShouldSelectSingleOptionByIndex()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.select('select', { index: 2 });
    expect(await Page.EvaluateAsync<string>(() => result.onInput)).toEqual(['brown']);
    expect(await Page.EvaluateAsync<string>(() => result.onChange)).toEqual(['brown']);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should select single option by multiple attributes</playwright-it>
[Fact]
public async Task ShouldSelectSingleOptionByMultipleAttributes()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.select('select', { value: 'green', label: 'Green' });
    expect(await Page.EvaluateAsync<string>(() => result.onInput)).toEqual(['green']);
    expect(await Page.EvaluateAsync<string>(() => result.onChange)).toEqual(['green']);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should not select single option when some attributes do not match</playwright-it>
[Fact]
public async Task ShouldNotSelectSingleOptionWhenSomeAttributesDoNotMatch()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.select('select', { value: 'green', label: 'Brown' });
    expect(await Page.EvaluateAsync<string>(() => document.querySelector('select').value)).toEqual('');

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should select only first option</playwright-it>
[Fact]
public async Task ShouldSelectOnlyFirstOption()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.select('select', 'blue', 'green', 'red');
    expect(await Page.EvaluateAsync<string>(() => result.onInput)).toEqual(['blue']);
    expect(await Page.EvaluateAsync<string>(() => result.onChange)).toEqual(['blue']);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should not throw when select causes navigation</playwright-it>
[Fact]
public async Task ShouldNotThrowWhenSelectCausesNavigation()
{
    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.$eval('select', select => select.addEventListener('input', () => window.location = '/empty.html'));
    await Promise.all([
      Page.select('select', 'blue'),
      Page.waitForNavigation(),

    ]);
    expect(Page.Url).toContain('empty.html');

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should select multiple options</playwright-it>
[Fact]
public async Task ShouldSelectMultipleOptions()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.EvaluateAsync<string>(() => makeMultiple());
    await Page.select('select', ['blue', 'green', 'red']);
    expect(await Page.EvaluateAsync<string>(() => result.onInput)).toEqual(['blue', 'green', 'red']);
    expect(await Page.EvaluateAsync<string>(() => result.onChange)).toEqual(['blue', 'green', 'red']);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should select multiple options with attributes</playwright-it>
[Fact]
public async Task ShouldSelectMultipleOptionsWithAttributes()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.EvaluateAsync<string>(() => makeMultiple());
    await Page.select('select', ['blue', { label: 'Green' }, { index: 4 }]);
    expect(await Page.EvaluateAsync<string>(() => result.onInput)).toEqual(['blue', 'gray', 'green']);
    expect(await Page.EvaluateAsync<string>(() => result.onChange)).toEqual(['blue', 'gray', 'green']);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should respect event bubbling</playwright-it>
[Fact]
public async Task ShouldRespectEventBubbling()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.select('select', 'blue');
    expect(await Page.EvaluateAsync<string>(() => result.onBubblingInput)).toEqual(['blue']);
    expect(await Page.EvaluateAsync<string>(() => result.onBubblingChange)).toEqual(['blue']);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should throw when element is not a <select></playwright-it>
[Fact]
public async Task ShouldThrowWhenElementIsNotA<select>()
{

    let error = null;
    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.select('body', '').catch (e => error = e);
    expect(error.message).toContain('Element is not a <select> element.');

    }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.select</playwright-describe>
        ///<playwright-it>should return [] on no matched values</playwright-it>
[Fact]
public async Task ShouldReturn[]OnNoMatchedValues()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    var result = await Page.select('select', '42', 'abc');
    expect(result).toEqual([]);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should return an array of matched values</playwright-it>
[Fact]
public async Task ShouldReturnAnArrayOfMatchedValues()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.EvaluateAsync<string>(() => makeMultiple());
    var result = await Page.select('select', 'blue', 'black', 'magenta');
    expect(result.reduce((accumulator, current) => ['blue', 'black', 'magenta'].includes(current) && accumulator, true)).toEqual(true);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should return an array of one element when multiple is not set</playwright-it>
[Fact]
public async Task ShouldReturnAnArrayOfOneElementWhenMultipleIsNotSet()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    var result = await Page.select('select',['42', 'blue', 'black', 'magenta']);
    expect(result.length).toEqual(1);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should return [] on no values</playwright-it>
[Fact]
public async Task ShouldReturn[]OnNoValues()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    var result = await Page.select('select');
    expect(result).toEqual([]);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should deselect all options when passed no values for a multiple select</playwright-it>
[Fact]
public async Task ShouldDeselectAllOptionsWhenPassedNoValuesForAMultipleSelect()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.EvaluateAsync<string>(() => makeMultiple());
    await Page.select('select', ['blue', 'black', 'magenta']);
    await Page.select('select');
    expect(await Page.$eval('select', select => Array.from(select.options).every(option => !option.selected))).toEqual(true);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should deselect all options when passed no values for a select without multiple</playwright-it>
[Fact]
public async Task ShouldDeselectAllOptionsWhenPassedNoValuesForASelectWithoutMultiple()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.select('select', ['blue', 'black', 'magenta']);
    await Page.select('select');
    expect(await Page.$eval('select', select => Array.from(select.options).every(option => !option.selected))).toEqual(true);

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should throw if passed wrong types</playwright-it>
[Fact]
public async Task ShouldThrowIfPassedWrongTypes()
{

    let error;
    await Page.SetContentAsync('<select><option value="12"/></select>');

    error = null;
    try
    {
        await Page.select('select', 12);
    }
    catch (e)
    {
        error = e;
    }
    expect(error.message).toContain('Values must be strings');

    error = null;
    try
    {
        await Page.select('select', { value: 12 });
    }
    catch (e)
    {
        error = e;
    }
    expect(error.message).toContain('Values must be strings');

    error = null;
    try
    {
        await Page.select('select', { label: 12 });
    }
    catch (e)
    {
        error = e;
    }
    expect(error.message).toContain('Labels must be strings');

    error = null;
    try
    {
        await Page.select('select', { index: '12' });
    }
    catch (e)
    {
        error = e;
    }
    expect(error.message).toContain('Indices must be numbers');

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.select</playwright-describe>
///<playwright-it>should work when re-defining top-level Event class</playwright-it>
[Fact]
public async Task ShouldWorkWhenRe-definingTop-levelEventClass()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/select.html');
    await Page.EvaluateAsync<string>(() => window.Event = null);
    await Page.select('select', 'blue');
    expect(await Page.EvaluateAsync<string>(() => result.onInput)).toEqual(['blue']);
    expect(await Page.EvaluateAsync<string>(() => result.onChange)).toEqual(['blue']);

}

    }
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.fill</playwright-describe>
    public class Page.fillTests
    {
        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should fill textarea</playwright-it>
        [Fact]
public async Task ShouldFillTextarea()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    await Page.fill('textarea', 'some value');
    expect(await Page.EvaluateAsync<string>(() => result)).toBe('some value');

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.fill</playwright-describe>
///<playwright-it>should fill input</playwright-it>
[Fact]
public async Task ShouldFillInput()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    await Page.fill('input', 'some value');
    expect(await Page.EvaluateAsync<string>(() => result)).toBe('some value');

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.fill</playwright-describe>
///<playwright-it>should throw on non-text inputs</playwright-it>
[Fact]
public async Task ShouldThrowOnNon-textInputs()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    for (var type of['color', 'number', 'date'])
    {
        await Page.$eval('input', (input, type) => input.setAttribute('type', type), type);
        let error = null;
        await Page.fill('input', '').catch (e => error = e);
    expect(error.message).toContain('Cannot fill input of type');
}

        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should fill different input types</playwright-it>
        [Fact]
public async Task ShouldFillDifferentInputTypes()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    for (var type of['password', 'search', 'tel', 'text', 'url'])
    {
        await Page.$eval('input', (input, type) => input.setAttribute('type', type), type);
        await Page.fill('input', 'text ' + type);
        expect(await Page.EvaluateAsync<string>(() => result)).toBe('text ' + type);
    }

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.fill</playwright-describe>
///<playwright-it>should fill contenteditable</playwright-it>
[Fact]
public async Task ShouldFillContenteditable()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    await Page.fill('div[contenteditable]', 'some value');
    expect(await Page.$eval('div[contenteditable]', div => div.textContent)).toBe('some value');

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.fill</playwright-describe>
///<playwright-it>should fill elements with existing value and selection</playwright-it>
[Fact]
public async Task ShouldFillElementsWithExistingValueAndSelection()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');

    await Page.$eval('input', input => input.value = 'value one');
    await Page.fill('input', 'another value');
    expect(await Page.EvaluateAsync<string>(() => result)).toBe('another value');

    await Page.$eval('input', input =>
    {
        input.selectionStart = 1;
        input.selectionEnd = 2;
    });
    await Page.fill('input', 'maybe this one');
    expect(await Page.EvaluateAsync<string>(() => result)).toBe('maybe this one');

    await Page.$eval('div[contenteditable]', div =>
    {
        div.innerHTML = 'some text <span>some more text<span> and even more text';
        var range = document.createRange();
        range.selectNodeContents(div.querySelector('span'));
        var selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(range);
    });
    await Page.fill('div[contenteditable]', 'replace with this');
    expect(await Page.$eval('div[contenteditable]', div => div.textContent)).toBe('replace with this');

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.fill</playwright-describe>
///<playwright-it>should throw when element is not an <input>, <textarea> or [contenteditable]</playwright-it>
[Fact]
public async Task ShouldThrowWhenElementIsNotAn<input>,<textarea>Or[contenteditable]()
        {

    let error = null;
    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    await Page.fill('body', '').catch (e => error = e);
    expect(error.message).toContain('Element is not an <input>');

    }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should throw if passed a non-string value</playwright-it>
[Fact]
public async Task ShouldThrowIfPassedANon-stringValue()
{

    let error = null;
    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    await Page.fill('textarea', 123).catch (e => error = e);
    expect(error.message).toContain('Value must be string.');

    }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should wait for visible visibilty</playwright-it>
[Fact]
public async Task ShouldWaitForVisibleVisibilty()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    await Page.fill('input', 'some value');
    expect(await Page.EvaluateAsync<string>(() => result)).toBe('some value');

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    await Page.$eval('input', i => i.style.display = 'none');
    await Promise.all([
      Page.fill('input', 'some value'),
      Page.$eval('input', i => i.style.display = 'block'),

    ]);
    expect(await Page.EvaluateAsync<string>(() => result)).toBe('some value');

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.fill</playwright-describe>
///<playwright-it>should throw on disabled and readonly elements</playwright-it>
[Fact]
public async Task ShouldThrowOnDisabledAndReadonlyElements()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    await Page.$eval('input', i => i.disabled = true);
    var disabledError = await Page.fill('input', 'some value').catch (e => e);
    expect(disabledError.message).toBe('Cannot fill a disabled input.');

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    await Page.$eval('textarea', i => i.readOnly = true);
    var readonlyError = await Page.fill('textarea', 'some value').catch (e => e);
    expect(readonlyError.message).toBe('Cannot fill a readonly textarea.');

    }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should throw on hidden and invisible elements</playwright-it>
[Fact]
public async Task ShouldThrowOnHiddenAndInvisibleElements()
{

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    await Page.$eval('input', i => i.style.display = 'none');
    var invisibleError = await Page.fill('input', 'some value', { waitFor: 'nowait' }).catch (e => e);
    expect(invisibleError.message).toBe('Element is not visible');

    await Page.GoToAsync(TestConstants.ServerUrl + '/input/textarea.html');
    await Page.$eval('input', i => i.style.visibility = 'hidden');
    var hiddenError = await Page.fill('input', 'some value', { waitFor: 'nowait' }).catch (e => e);
    expect(hiddenError.message).toBe('Element is hidden');

    }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.fill</playwright-describe>
        ///<playwright-it>should be able to fill the body</playwright-it>
[Fact]
public async Task ShouldBeAbleToFillTheBody()
{

    await Page.SetContentAsync(`< body contentEditable = "true" ></ body >`);
    await Page.fill('body', 'some value');
    expect(await Page.EvaluateAsync<string>(() => document.body.textContent)).toBe('some value');

}

///<playwright-file>page.spec.js</playwright-file>
///<playwright-describe>Page.fill</playwright-describe>
///<playwright-it>should be able to fill when focus is in the wrong frame</playwright-it>
[Fact]
public async Task ShouldBeAbleToFillWhenFocusIsInTheWrongFrame()
{

    await Page.SetContentAsync(`

      < div contentEditable = "true" ></ div >

      < iframe ></ iframe >
      `);
    await Page.focus('iframe');
    await Page.fill('div', 'some value');
    expect(await Page.$eval('div', d => d.textContent)).toBe('some value');

}

    }
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.Close</playwright-describe>
    public class Page.Events.CloseTests
    {
        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Close</playwright-describe>
        ///<playwright-it>should work with window.close</playwright-it>
        [Fact]
public async Task ShouldWorkWithWindow.close()
{
    page, context, server }) {
      var newPagePromise = new Promise(f => Page.once('popup', f));
await Page.EvaluateAsync<string>(() => window['newPage'] = window.open('about:blank'));
var newPage = await newPagePromise;
var closedPromise = new Promise(x => newPage.close', x));

await Page.EvaluateAsync<string>(() => window['newPage'].CloseAsync());
await closedPromise;

        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Close</playwright-describe>
        ///<playwright-it>should work with page.close</playwright-it>
        [Fact]
public async Task ShouldWorkWithPage.close()
{
    page, context, server }) {
      var newPage = await context.NewPageAsync();
var closedPromise = new Promise(x => newPage.close', x));

await newPage.CloseAsync();
await closedPromise;

        }

    }
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.browserContext</playwright-describe>
    public class Page.browserContextTests
    {
        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.browserContext</playwright-describe>
        ///<playwright-it>should return the correct browser instance</playwright-it>
        [Fact]
public async Task ShouldReturnTheCorrectBrowserInstance()
{
    page, context}) {
      expect(Page.browserContext()).toBe(context);

        }

    }

}
