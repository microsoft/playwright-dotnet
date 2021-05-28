using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>capabilities.spec.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class CapabilitiesTests : PageTestEx
    {
        [PlaywrightTest("capabilities.spec.ts", "Web Assembly should work")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true, skipWindows: true)]
        public async Task WebAssemblyShouldWork()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/wasm/table2.html");
            Assert.AreEqual("42, 83", await Page.EvaluateAsync<string>("() => loadTable()"));
        }

#if NETCOREAPP
        [PlaywrightTest("capabilities.spec.ts", "WebSocket should work")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true, skipWindows: true)]
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
            Assert.AreEqual("incoming", value);
        }
#endif

        [PlaywrightTest("capabilities.spec.ts", "should respect CSP")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectCSP()
        {
            HttpServer.Server.SetRoute("/empty.html", context =>
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

            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.AreEqual("SUCCESS", await Page.EvaluateAsync<string>("() => window.testStatus"));
        }

        [PlaywrightTest("capabilities.spec.ts", "should play video")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldPlayVideo()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + (TestConstants.IsWebKit ? "/video_mp4.html" : "/video.html"));
            await Page.EvalOnSelectorAsync("video", "v => v.play()");
            await Page.EvalOnSelectorAsync("video", "v => v.pause()");
        }
    }
}
