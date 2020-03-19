using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Mono.Unix;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Debugger;
using PlaywrightSharp.Chromium.Protocol.Network;
using PlaywrightSharp.Chromium.Protocol.Runtime;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Helpers.Linux;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/session.spec.js</playwright-file>
    ///<playwright-describe>Chromium.createCDPSession</playwright-describe>
    public class SessionTests : PlaywrightSharpPageBaseTest, IDisposable
    {
        /// <inheritdoc/>
        public SessionTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        ///<playwright-file>chromium/session.spec.js</playwright-file>
        ///<playwright-describe>Chromium.createCDPSession</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWork()
        {
            var client = await ((ChromiumTarget)Browser.GetPageTarget(Page)).CreateCDPSessionAsync();

            await Task.WhenAll(
              client.SendAsync(new RuntimeEnableRequest()),
              client.SendAsync(new RuntimeEvaluateRequest { Expression = "window.foo = 'bar'" })
            );
            string foo = await Page.EvaluateAsync<string>("window.foo");
            Assert.Equal("bar", foo);
        }

        ///<playwright-file>chromium/session.spec.js</playwright-file>
        ///<playwright-describe>Chromium.createCDPSession</playwright-describe>
        ///<playwright-it>should send events</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldSendEvents()
        {
            var client = await ((ChromiumTarget)Browser.GetPageTarget(Page)).CreateCDPSessionAsync();

            await client.SendAsync(new NetworkEnableRequest());
            var events = new List<IChromiumEvent>();

            client.MessageReceived += (sender, e) =>
            {
                if (e is NetworkRequestWillBeSentChromiumEvent)
                {
                    events.Add(e);
                }
            };

            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Single(events);
        }

        ///<playwright-file>chromium/session.spec.js</playwright-file>
        ///<playwright-describe>Chromium.createCDPSession</playwright-describe>
        ///<playwright-it>should enable and disable domains independently</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldEnableAndDisableDomainsIndependently()
        {
            var client = await ((ChromiumTarget)Browser.GetPageTarget(Page)).CreateCDPSessionAsync();
            await client.SendAsync(new RuntimeEnableRequest());
            await client.SendAsync(new DebuggerEnableRequest());
            // JS coverage enables and then disables Debugger domain.
            await Page.Coverage.StartJSCoverageAsync();
            await Page.Coverage.StopJSCoverageAsync();
            // generate a script in page and wait for the event.
            var eventTask = WaitEventAsync<DebuggerScriptParsedChromiumEvent>(client);
            await Task.WhenAll(
                eventTask,
                Page.EvaluateAsync("//# sourceURL=foo.js")
            );
            // expect events to be dispatched.
            Assert.Equal("foo.js", eventTask.Result.Url);
        }

        ///<playwright-file>chromium/session.spec.js</playwright-file>
        ///<playwright-describe>Chromium.createCDPSession</playwright-describe>
        ///<playwright-it>should be able to detach session</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldBeAbleToDetachSession()
        {
            var client = await ((ChromiumTarget)Browser.GetPageTarget(Page)).CreateCDPSessionAsync();
            await client.SendAsync(new RuntimeEnableRequest());
            var evalResponse = await client.SendAsync(new RuntimeEvaluateRequest()
            {
                Expression = "1 + 2",
                ReturnByValue = true
            });
            Assert.Equal(3, ((JsonElement)evalResponse.Result.Value).ToObject<int>());
            await client.DetachAsync();

            var exception = await Assert.ThrowsAnyAsync<Exception>(()
                => client.SendAsync(new RuntimeEvaluateRequest
                {
                    Expression = "3 + 1",
                    ReturnByValue = true
                }));
            Assert.Contains("Session closed.", exception.Message);
        }

        ///<playwright-file>chromium/session.spec.js</playwright-file>
        ///<playwright-describe>Chromium.createCDPSession</playwright-describe>
        ///<playwright-it>should throw nice errors</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldThrowNiceErrors()
        {
            var client = await ((ChromiumTarget)Browser.GetPageTarget(Page)).CreateCDPSessionAsync();
            async Task TheSourceOfTheProblems() => await client.SendAsync(new ThisCommandDoesNotExistRequest());

            var exception = await Assert.ThrowsAsync<MessageException>(async () =>
            {
                await TheSourceOfTheProblems();
            });
            Assert.Contains("TheSourceOfTheProblems", exception.StackTrace);
            Assert.Contains("ThisCommand.DoesNotExist", exception.Message);
        }
    }

    internal class ThisCommandDoesNotExistRequest : IChromiumRequest<ThisCommandDoesNotExistRequestResponse>
    {
        public string Command => "ThisCommand.DoesNotExist";
    }

    internal class ThisCommandDoesNotExistRequestResponse : IChromiumResponse
    {

    }
}
