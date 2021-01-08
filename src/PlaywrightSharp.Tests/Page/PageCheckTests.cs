using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>click.spec.js</playwright-file>
    ///<playwright-describe>Page.check</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageCheckTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageCheckTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.check</playwright-describe>
        ///<playwright-it>should check the box</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckTheBox()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
            await Page.CheckAsync("input");
            Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.check</playwright-describe>
        ///<playwright-it>should not check the checked box</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotCheckTheCheckedBox()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
            await Page.CheckAsync("input");
            Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.check</playwright-describe>
        ///<playwright-it>should uncheck the box</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUncheckTheBox()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
            await Page.UncheckAsync("input");
            Assert.False(await Page.EvaluateAsync<bool?>("checkbox.checked"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.check</playwright-describe>
        ///<playwright-it>should check the box by label</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckTheBoxByLabel()
        {
            await Page.SetContentAsync("<label for='checkbox'><input id='checkbox' type='checkbox'></input></label>");
            await Page.CheckAsync("label");
            Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.check</playwright-describe>
        ///<playwright-it>should check the box outside label</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckTheBoxOutsideLabel()
        {
            await Page.SetContentAsync("<label for='checkbox'>Text</label><div><input id='checkbox' type='checkbox'></input></div>");
            await Page.CheckAsync("label");
            Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.check</playwright-describe>
        ///<playwright-it>should check the box inside label w/o id</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckTheBoxInsideLabelWoId()
        {
            await Page.SetContentAsync("<label>Text<span><input id='checkbox' type='checkbox'></input></span></label>");
            await Page.CheckAsync("label");
            Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.check</playwright-describe>
        ///<playwright-it>should check radio</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckRadio()
        {
            await Page.SetContentAsync(@"
                <input type='radio'>one</input>
                <input id='two' type='radio'>two</input>
                <input type='radio'>three</input>");
            await Page.CheckAsync("#two");
            Assert.True(await Page.EvaluateAsync<bool?>("two.checked"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.check</playwright-describe>
        ///<playwright-it>should check the box by aria role</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckTheBoxByAriaRole()
        {
            await Page.SetContentAsync(@"
                <div role='checkbox' id='checkbox'>CHECKBOX</div>
                <script>
                checkbox.addEventListener('click', () => checkbox.setAttribute('aria-checked', 'true'));
                </script>");
            await Page.CheckAsync("div");
            Assert.Equal("true", await Page.EvaluateAsync<string>("checkbox.getAttribute('aria-checked')"));
        }
    }
}
