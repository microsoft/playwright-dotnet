using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventConsoleTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventConsoleTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-event-console.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            ConsoleMessage message = null;
            void EventHandler(object sender, ConsoleEventArgs e)
            {
                message = e.Message;
                Page.Console -= EventHandler;
            }
            Page.Console += EventHandler;
            await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Console),
                Page.EvaluateAsync("() => console.log('hello', 5, { foo: 'bar'})"));

            Assert.Equal("hello 5 JSHandle@object", message.Text);
            Assert.Equal("log", message.Type);
            Assert.Equal("hello", await message.Args.ElementAt(0).GetJsonValueAsync<string>());
            Assert.Equal(5, await message.Args.ElementAt(1).GetJsonValueAsync<int>());
            Assert.Equal("bar", (await message.Args.ElementAt(2).GetJsonValueAsync<JsonElement>()).GetProperty("foo").GetString());
        }

        [PlaywrightTest("page-event-console.spec.ts", "should emit same log twice")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmitSameLogTwice()
        {
            var messages = new List<string>();

            Page.Console += (object sender, ConsoleEventArgs e) => messages.Add(e.Message.Text);
            await Page.EvaluateAsync("() => { for (let i = 0; i < 2; ++i ) console.log('hello'); } ");

            Assert.Equal(new[] { "hello", "hello" }, messages.ToArray());
        }

        [PlaywrightTest("page-event-console.spec.ts", "should work for different console API calls")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForDifferentConsoleAPICalls()
        {
            var messages = new List<ConsoleMessage>();
            Page.Console += (sender, e) => messages.Add(e.Message);
            // All console events will be reported before `Page.evaluate` is finished.
            await Page.EvaluateAsync(@"() => {
                // A pair of time/timeEnd generates only one Console API call.
                console.time('calling console.time');
                console.timeEnd('calling console.time');
                console.trace('calling console.trace');
                console.dir('calling console.dir');
                console.warn('calling console.warn');
                console.error('calling console.error');
                console.log(Promise.resolve('should not wait until resolved!'));
            }");
            Assert.Equal(new[] { "timeEnd", "trace", "dir", "warning", "error", "log" }, messages.Select(msg => msg.Type));
            Assert.Contains("calling console.time", messages[0].Text);
            Assert.Equal(new[]
            {
                "calling console.trace",
                "calling console.dir",
                "calling console.warn",
                "calling console.error",
                "JSHandle@promise"
            }, messages.Skip(1).Select(msg => msg.Text));
        }

        [PlaywrightTest("page-event-console.spec.ts", "should not fail for window object")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotFailForWindowObject()
        {
            ConsoleMessage message = null;
            void EventHandler(object sender, ConsoleEventArgs e)
            {
                message = e.Message;
                Page.Console -= EventHandler;
            }
            Page.Console += EventHandler;
            await TaskUtils.WhenAll(
                Page.EvaluateAsync("() => console.error(window)"),
                Page.WaitForEventAsync(PageEvent.Console)
            );
            Assert.Equal("JSHandle@object", message.Text);
        }

        [PlaywrightTest("page-event-console.spec.ts", "should trigger correct Log")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTriggerCorrectLog()
        {
            await Page.GoToAsync("about:blank");
            var (messageEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Console),
                Page.EvaluateAsync("async url => fetch(url).catch (e => { })", TestConstants.EmptyPage)
            );
            Assert.Contains("Access-Control-Allow-Origin", messageEvent.Message.Text);
            Assert.Equal("error", messageEvent.Message.Type);
        }

        [PlaywrightTest("page-event-console.spec.ts", "should have location for console API calls")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveLocationForConsoleAPICalls()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var (messageEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Console),
                Page.GoToAsync(TestConstants.ServerUrl + "/consolelog.html")
            );
            Assert.Equal("yellow", messageEvent.Message.Text);
            Assert.Equal("log", messageEvent.Message.Type);
            var location = messageEvent.Message.Location;
            // Engines have different column notion.
            location.ColumnNumber = null;
            Assert.Equal(new ConsoleMessageLocation
            {
                URL = TestConstants.ServerUrl + "/consolelog.html",
                LineNumber = 7
            }, location);
        }

        [PlaywrightTest("page-event-console.spec.ts", "should not throw when there are console messages in detached iframes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotThrowWhenThereAreConsoleMessagesInDetachedIframes()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var (popup, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Popup),
                Page.EvaluateAsync<bool>(@"async () =>
                {
                    // 1. Create a popup that Playwright is not connected to.
                    const win = window.open('');
                    window._popup = win;
                    if (window.document.readyState !== 'complete')
                      await new Promise(f => window.addEventListener('load', f));
                    // 2. In this popup, create an iframe that console.logs a message.
                    win.document.body.innerHTML = `<iframe src='/consolelog.html'></iframe>`;
                    const frame = win.document.querySelector('iframe');
                    if (!frame.contentDocument || frame.contentDocument.readyState !== 'complete')
                      await new Promise(f => frame.addEventListener('load', f));
                    // 3. After that, remove the iframe.
                    frame.remove();
                }"));
            // 4. Connect to the popup and make sure it doesn't throw.
            Assert.Equal(2, await popup.Page.EvaluateAsync<int>("1 + 1"));
        }
    }
}
