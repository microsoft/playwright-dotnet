using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>capabilities.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class CapabilitiesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public CapabilitiesTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("capabilities.spec.ts", "Web Assembly should work")]
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipWindows: true)]
        public async Task WebAssemblyShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/wasm/table2.html");
            Assert.Equal("42, 83", await Page.EvaluateAsync<string>("() => loadTable()"));
        }

#if NETCOREAPP
        [PlaywrightTest("capabilities.spec.ts", "WebSocket should work")]
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipWindows: true)]
        public async Task WebSocketShouldWork()
        {
            string value = await Page.EvaluateAsync<string>(
                $@"(port) => {{
                    let cb;
                    const result = new Promise(f => cb = f);
                    const ws = new WebSocket('ws://localhost:' + port + '/ws');
                    ws.addEventListener('message', data => {{ ws.close(); cb(data.data); console.log(data); console.log(data.data) }});
                    ws.addEventListener('error', error => cb('Error'));
                    return result;
                }}",
                TestConstants.Port);
            Assert.Equal("incoming", value);
        }
#endif

        [PlaywrightTest("capabilities.spec.ts", "should respect CSP")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectCSP()
        {
            Server.SetRoute("/empty.html", context =>
            {
                const string message = @"
                    <script>
                      window.testStatus = 'SUCCESS';
                      window.testStatus = eval(""'FAILED'"");
                    </script>
                ";

                context.Response.Headers["Content-Length"] = message.Length.ToString();
                context.Response.Headers["Content-Security-Policy"] = "script-src 'unsafe-inline';";
                return context.Response.WriteAsync(message);
            });

            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("SUCCESS", await Page.EvaluateAsync<string>("() => window.testStatus"));
        }

        [PlaywrightTest("capabilities.spec.ts", "should play video")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldPlayVideo()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + (TestConstants.IsWebKit ? "/video_mp4.html" : "/video.html"));
            await Page.EvalOnSelectorAsync("video", "v => v.play()");
            await Page.EvalOnSelectorAsync("video", "v => v.pause()");
        }
    }
}
