using System.Threading.Tasks;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>dispatchevent.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageDispatchEventTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageDispatchEventTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click event")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDispatchClickEvent()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.DispatchEventAsync("button", "click");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click event properties")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDispatchClickEventProperties()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.DispatchEventAsync("button", "click");
            Assert.True(await Page.EvaluateAsync<bool>("() => bubbles"));
            Assert.True(await Page.EvaluateAsync<bool>("() => cancelable"));
            Assert.True(await Page.EvaluateAsync<bool>("() => composed"));
        }

        [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click svg")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDispatchClickSvg()
        {
            await Page.SetContentAsync(@"
            <svg height=""100"" width=""100"">
                <circle onclick=""javascript:window.__CLICKED=42"" cx=""50"" cy=""50"" r=""40"" stroke=""black"" stroke-width = ""3"" fill=""red"" />
            </svg>");

            await Page.DispatchEventAsync("circle", "click");
            Assert.Equal(42, await Page.EvaluateAsync<int>("() => window.__CLICKED"));
        }

        [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click on a span with an inline element inside")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click after navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDispatchClickAfterNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.DispatchEventAsync("button", "click");
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.DispatchEventAsync("button", "click");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click after a cross origin navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDispatchClickAfterACrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.DispatchEventAsync("button", "click");
            await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/input/button.html");
            await Page.DispatchEventAsync("button", "click");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("page-dispatchevent.spec.ts", "should not fail when element is blocked on hover")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click when node is added in shadow dom")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-dispatchevent.spec.ts", "should be atomic")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAtomic()
        {
            const string createDummySelector = @"({
                create(root, target) {},
                query(root, selector) {
                    const result = root.querySelector(selector);
                    if (result)
                    Promise.resolve().then(() => result.onclick = '');
                    return result;
                },
                queryAll(root, selector) {
                    const result = Array.from(root.querySelectorAll(selector));
                    for (const e of result)
                    Promise.resolve().then(() => e.onclick = null);
                    return result;
                }
            })";

            await TestUtils.RegisterEngineAsync(Playwright, "page-dispatchevent", createDummySelector);
            await Page.SetContentAsync("<div onclick=\"window._clicked = true\">Hello</div>");
            await Page.DispatchEventAsync("page-dispatchevent=div", "click");
            Assert.True(await Page.EvaluateAsync<bool>("() => window['_clicked']"));
        }

        [PlaywrightTest("page-dispatchevent.spec.ts", "Page.dispatchEvent(drag)", "should dispatch drag drop events")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldDispatchDragDropEvents()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/drag-n-drop.html");
            var dataTransfer = await Page.EvaluateHandleAsync("() => new DataTransfer()");
            await Page.DispatchEventAsync("#source", "dragstart", new { dataTransfer });
            await Page.DispatchEventAsync("#target", "drop", new { dataTransfer });

            var source = await Page.QuerySelectorAsync("#source");
            var target = await Page.QuerySelectorAsync("#target");
            Assert.True(await Page.EvaluateAsync<bool>(@"() => {
                return source.parentElement === target;
            }", new { source, target }));
        }

        [PlaywrightTest("page-dispatchevent.spec.ts", "Page.dispatchEvent(drag)", "should dispatch drag drop events")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ElementHandleShouldDispatchDragDropEvents()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/drag-n-drop.html");
            var dataTransfer = await Page.EvaluateHandleAsync("() => new DataTransfer()");
            var source = await Page.QuerySelectorAsync("#source");
            await source.DispatchEventAsync("dragstart", new { dataTransfer });
            var target = await Page.QuerySelectorAsync("#target");
            await target.DispatchEventAsync("drop", new { dataTransfer });

            Assert.True(await Page.EvaluateAsync<bool>(@"() => {
                return source.parentElement === target;
            }", new { source, target }));
        }

        [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click event")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ElementHandleShouldDispatchClickEvent()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await button.DispatchEventAsync("click");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }
    }
}
