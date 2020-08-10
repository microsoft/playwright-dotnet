using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Input;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.DispatchEvent
{
    ///<playwright-file>dispatchevent.spec.js</playwright-file>
    ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageDispatchEventClickTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageDispatchEventClickTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>should dispatch click event</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDispatchClickEvent()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.DispatchEventAsync("button", "click");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>should dispatch click event properties</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDispatchClickEventProperties()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.DispatchEventAsync("button", "click");
            Assert.True(await Page.EvaluateAsync<bool>("() => bubbles"));
            Assert.True(await Page.EvaluateAsync<bool>("() => cancelable"));
            Assert.True(await Page.EvaluateAsync<bool>("() => composed"));
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>should dispatch click svg</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDispatchClickSvg()
        {
            await Page.SetContentAsync(@"
            <svg height=""100"" width=""100"">
                <circle onclick=""javascript:window.__CLICKED=42"" cx=""50"" cy=""50"" r=""40"" stroke=""black"" stroke-width = ""3"" fill=""red"" />
            </svg>");

            await Page.DispatchEventAsync("circle", "click");
            Assert.Equal(42, await Page.EvaluateAsync<int>("() => window.__CLICKED"));
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>should dispatch click on a span with an inline element inside</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDispatchClickOnASpanWithAnInlineElementInside()
        {
            await Page.SetContentAsync(@"
              <style>
                  span::before {
                    content: 'q';
                  }
              </style>
              <span onclick='javascript:window.CLICKED=42'></span>");

            await Page.DispatchEventAsync("span", "click");
            Assert.Equal(42, await Page.EvaluateAsync<int>("() => window.CLICKED"));
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>should dispatch click after navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDispatchClickAfterNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.DispatchEventAsync("button", "click");
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.DispatchEventAsync("button", "click");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>should dispatch click after a cross origin navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDispatchClickAfterACrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.DispatchEventAsync("button", "click");
            await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/input/button.html");
            await Page.DispatchEventAsync("button", "click");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>should not fail when element is blocked on hover</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotFailWhenElementIsBlockedOnHover()
        {
            await Page.SetContentAsync(@"
              <style>
                container { display: block; position: relative; width: 200px; height: 50px; }
                div, button { position: absolute; left: 0; top: 0; bottom: 0; right: 0; }
                div { pointer-events: none; }
                container:hover div { pointer-events: auto; background: red; }
            </style>
            <container>
                <button onclick=""window.clicked = true"">Click me</button>
                <div></div>
            </container>");

            await Page.DispatchEventAsync("button", "click");
            Assert.True(await Page.EvaluateAsync<bool>("() => window.clicked"));
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>should dispatch click when node is added in shadow dom</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDispatchClickWhenNodeIsAddedInShadowDom()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var watchdog = Page.DispatchEventAsync("span", "click");

            await Page.EvaluateAsync(@"() => {
              const div = document.createElement('div');
              div.attachShadow({mode: 'open'});
              document.body.appendChild(div);
            }");
            await Page.EvaluateAsync("() => new Promise(f => setTimeout(f, 100))");

            await Page.EvaluateAsync(@"() => {
              const span = document.createElement('span');
              span.textContent = 'Hello from shadow';
              span.addEventListener('click', () => window.clicked = true);
              document.querySelector('div').shadowRoot.appendChild(span);
            }");

            await watchdog;
            Assert.True(await Page.EvaluateAsync<bool>("() => window.clicked"));
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>Page.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>should be atomic</playwright-it>
        [Fact(Skip = "We need https://github.com/microsoft/playwright/issues/3267")]
        public void ShouldBeAtomic()
        {
        }
    }
}
