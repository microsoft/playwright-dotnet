using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.Console</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class PageEventsConsoleTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventsConsoleTests(ITestOutputHelper output) : base(output)
        {
        }
        /*
        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Console</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            ConsoleMessage message = null;
            void EventHandler(object sender, ConsoleEventArgs e)
            {
                message = e.Message;
                Page.Console -= EventHandler;
            }
            Page.Console += EventHandler;
            await Task.WhenAll(
                Page.EvaluateAsync("() => console.log('hello', 5, { foo: 'bar'})"),
                Page.WaitForEvent<ConsoleEventArgs>(PageEvent.Console)
            );
            Assert.Equal("hello 5 JSHandle@object", message.Text);
            Assert.Equal(ConsoleType.Log, message.Type);
            Assert.Equal("hello", await message.Args.ElementAt(0).GetJsonValueAsync<string>());
            Assert.Equal(5, await message.Args.ElementAt(1).GetJsonValueAsync<int>());
            Assert.Equal("bar", (await message.Args.ElementAt(2).GetJsonValueAsync<JsonElement>()).GetProperty("foo").GetString());
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Console</playwright-describe>
        ///<playwright-it>should work for different console API calls</playwright-it>
        [Retry]
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
            Assert.Equal(new[] { ConsoleType.TimeEnd, ConsoleType.Trace, ConsoleType.Dir, ConsoleType.Warning, ConsoleType.Error, ConsoleType.Log }, messages.Select(msg => msg.Type));
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

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Console</playwright-describe>
        ///<playwright-it>should not fail for window object</playwright-it>
        [Retry]
        public async Task ShouldNotFailForWindowObject()
        {
            ConsoleMessage message = null;
            void EventHandler(object sender, ConsoleEventArgs e)
            {
                message = e.Message;
                Page.Console -= EventHandler;
            }
            Page.Console += EventHandler;
            await Task.WhenAll(
                Page.EvaluateAsync("() => console.error(window)"),
                Page.WaitForEvent<ConsoleEventArgs>(PageEvent.Console)
            );
            Assert.Equal("JSHandle@object", message.Text);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Console</playwright-describe>
        ///<playwright-it>should trigger correct Log</playwright-it>
        [Retry]
        public async Task ShouldTriggerCorrectLog()
        {
            await Page.GoToAsync("about:blank");
            var (messageEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent<ConsoleEventArgs>(PageEvent.Console),
                Page.EvaluateAsync("async url => fetch(url).catch (e => { })", TestConstants.EmptyPage)
            );
            Assert.Contains("Access-Control-Allow-Origin", messageEvent.Message.Text);
            Assert.Equal(ConsoleType.Error, messageEvent.Message.Type);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Console</playwright-describe>
        ///<playwright-it>should have location for console API calls</playwright-it>
        [Retry]
        public async Task ShouldHaveLocationForConsoleAPICalls()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var (messageEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent<ConsoleEventArgs>(PageEvent.Console),
                Page.GoToAsync(TestConstants.ServerUrl + "/consolelog.html")
            );
            Assert.Equal("yellow", messageEvent.Message.Text);
            Assert.Equal(ConsoleType.Log, messageEvent.Message.Type);
            var location = messageEvent.Message.Location;
            // Engines have different column notion.
            location.ColumnNumber = null;
            Assert.Equal(new ConsoleMessageLocation
            {
                URL = TestConstants.ServerUrl + "/consolelog.html",
                LineNumber = 7
            }, location);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Console</playwright-describe>
        ///<playwright-it>should not throw when there are console messages in detached iframes</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldNotThrowWhenThereAreConsoleMessagesInDetachedIframes()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"async () =>
            {
                // 1. Create a popup that Playwright is not connected to.
                var win = window.open(window.location.href, 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=780,height=200,top=0,left=0');
                while (window.document.readyState !== 'complete')
                {
                    await new Promise(f => setTimeout(f, 100));
                }
                // 2. In this popup, create an iframe that console.logs a message.
                win.document.body.innerHTML = `<iframe src='/consolelog.html'></iframe>`;
                var frame = win.document.querySelector('iframe');
                while (frame.contentDocument.readyState !== 'complete')
                {
                    await new Promise(f => setTimeout(f, 100));
                }
                // 3. After that, remove the iframe.
                frame.remove();
            }");
            // 4. Connect to the popup and make sure it doesn't throw.
            //await Page.BrowserContext.GetPagesAsync();
        }
        */
    }
}
