using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>web-socket.spec.js</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class WebSocketTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WebSocketTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>web-socket.spec.js</playwright-file>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            string value = await Page.EvaluateAsync<string>(@"port => {
                let cb;
                const result = new Promise(f => cb = f);
                const ws = new WebSocket('ws://localhost:' + port + '/ws');
                ws.addEventListener('message', data => { ws.close(); cb(data.data); });
                return result;
            }", TestConstants.Port);
            Assert.Equal("incoming", value);
        }

        ///<playwright-file>web-socket.spec.js</playwright-file>
        ///<playwright-it>should emit close events</playwright-it>
        [Fact]
        public async Task ShouldEmitCloseEvents()
        {
            var socketClosedTcs = new TaskCompletionSource<bool>();
            var log = new List<string>();
            IWebSocket webSocket = null;

            Page.WebSocket += (_, e) =>
            {
                log.Add($"open<{e.WebSocket.Url}>");
                webSocket = e.WebSocket;
                webSocket.Close += (_, __) =>
                {
                    log.Add("close");
                    socketClosedTcs.TrySetResult(true);
                };
            };

            await Page.EvaluateAsync(@"port => {
                const ws = new WebSocket('ws://localhost:' + port + '/ws');
                ws.addEventListener('open', () => ws.close());
            }", TestConstants.Port);

            await socketClosedTcs.Task.WithTimeout();
            Assert.Equal($"open<ws://localhost:${TestConstants.Port}/ws>:close", string.Join(':', log));
            Assert.True(webSocket.IsClosed);
        }
    }
}